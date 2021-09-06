using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Helion.Audio;
using Helion.Audio.Impl;
using Helion.Audio.Sounds;
using Helion.Client.Input;
using Helion.Client.Music;
using Helion.Layer;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Locator;
using Helion.Util;
using Helion.Util.CommandLine;
using Helion.Util.Configs;
using Helion.Util.Configs.Impl;
using Helion.Util.Consoles;
using Helion.Util.Consoles.Commands;
using Helion.Util.Extensions;
using Helion.Util.Profiling;
using Helion.Util.Timing;
using Helion.Window;
using Helion.World.Save;
using NLog;
using OpenTK.Graphics.OpenGL;
using OpenTK.Windowing.Common;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client
{
    public partial class Client : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly ArchiveCollection m_archiveCollection;
        private readonly IAudioSystem m_audioSystem;
        private readonly CommandLineArgs m_commandLineArgs;
        private readonly IConfig m_config;
        private readonly HelionConsole m_console;
        private readonly GameLayerManager m_layerManager;
        private readonly NativeWinMouse? m_nativeWinMouse;
        private readonly SoundManager m_soundManager;
        private readonly SaveGameManager m_saveGameManager;
        private readonly Window m_window;
        private readonly FpsTracker m_fpsTracker = new();
        private readonly ConsoleCommands m_consoleCommands = new();
        private readonly Profiler m_profiler = new();
        private bool m_disposed;
        private bool m_takeScreenshot;

        private Client(CommandLineArgs commandLineArgs, IConfig config, HelionConsole console, IAudioSystem audioSystem,
            ArchiveCollection archiveCollection)
        {
            m_commandLineArgs = commandLineArgs;
            m_config = config;
            m_console = console;
            m_audioSystem = audioSystem;
            m_archiveCollection = archiveCollection;
            m_saveGameManager = new SaveGameManager(config);
            m_soundManager = new SoundManager(audioSystem, archiveCollection);
            m_window = new Window(config, archiveCollection, m_fpsTracker);
            m_layerManager = new GameLayerManager(config, m_window, console, m_consoleCommands, archiveCollection, 
                m_soundManager, m_saveGameManager, m_profiler);

            m_consoleCommands.RegisterMethodsOrThrow(this);
            
            m_console.OnConsoleCommandEvent += Console_OnCommand;
            m_window.RenderFrame += Window_MainLoop;

            if (config.Mouse.RawInput)
                m_nativeWinMouse = new NativeWinMouse(HandleWinMouseMove);

            RegisterConfigChanges();
        }

        ~Client()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        [Conditional("DEBUG")]
        private void CheckForErrorsIfDebug()
        {
            m_audioSystem.ThrowIfErrorCheckFails();
        }

        private void HandleInput()
        {
            m_profiler.Input.Start();
            
            IConsumableInput input = m_window.InputManager.Poll();
            if (!m_takeScreenshot)
                m_takeScreenshot = m_config.Keys.ConsumeCommandKeyPress(Constants.Input.Screenshot, input);
            
            m_layerManager.HandleInput(input);
            
            // Because we had to tightly bound the consumable input to the
            // input manager, we only want to clear the state after we've
            // handled all of the input. This wipes the input manager clean,
            // and we only should do that after we are done with the input.
            m_window.InputManager.Reset();
            
            m_profiler.Input.Stop();
        }

        private void RunLogic()
        {
            m_profiler.Logic.Start();
            
            m_layerManager.RunLogic();
            
            m_profiler.Logic.Stop();
        }

        private bool ShouldRender()
        {
            return m_fpsLimitValue <= 0 || m_fpsLimit.ElapsedTicks * StopwatchFrequencyValue / Stopwatch.Frequency >= m_fpsLimitValue;
        }

        private void PerformRender()
        {
            m_layerManager.Render(m_window.Renderer);
            m_window.Renderer.PerformThrowableErrorChecks();
        }
        
        private void HandleScreenshot()
        {
            if (!m_takeScreenshot)
                return;

            m_takeScreenshot = false;
            GL.Finish();
            
            // TODO: This should be delegated to the renderer, not done here.
            (int w, int h) = m_window.Dimension;
            Bitmap bmp = new(w, h);
            Rectangle rect = new(0, 0, w, h);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            GL.ReadPixels(0, 0, w, h, OpenTK.Graphics.OpenGL.PixelFormat.Bgr, PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);

            string path = $"helion_{DateTime.Now:yyyyMMdd_hh.mm.ss.FFFF}.png";
            Log.Info($"Saving screenshot to {path}");
            bmp.Save(path);
        }

        private void Render()
        {
            if (!ShouldRender())
                return;

            m_profiler.Render.Total.Start();
            
            m_fpsLimit.Restart();
            PerformRender();
            HandleScreenshot();
            
            m_profiler.Render.SwapBuffers.Start();
            m_window.SwapBuffers();
            m_profiler.Render.SwapBuffers.Stop();
            
            m_fpsTracker.FinishFrame();   
            
            m_profiler.Render.Total.Stop();
        }

        private void Window_MainLoop(FrameEventArgs frameEventArgs)
        {
            m_profiler.ResetTimers();
            m_profiler.Global.Start();
            
            CheckForErrorsIfDebug();

            HandleInput();
            RunLogic();
            Render();
            
            m_soundManager.Update();
            
            m_profiler.Global.Stop();
        }

        /// <summary>
        /// Runs the client until the client requests the game exit.
        /// </summary>
        public void Run()
        {
            Initialize();
            m_window.Run();
        }

        private void HandleWinMouseMove(int deltaX, int deltaY)
        {
            bool focus = m_window.IsFocused && m_layerManager.ShouldFocus();
            m_window.SetGrabCursor(focus);

            if (!focus)
                return;

            m_window.HandleRawMouseMovement(-deltaX, -deltaY);

            int x = m_window.Location.X + (m_window.Size.X / 2);
            int y = m_window.Location.Y + (m_window.Size.Y / 2);
            NativeMethods.SetMousePosition(x, y);
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;

            m_console.OnConsoleCommandEvent -= Console_OnCommand;
            m_window.RenderFrame -= Window_MainLoop;
            UnregisterConfigChanges();

            m_soundManager.Dispose();
            m_layerManager.Dispose();
            m_window.Dispose();

            m_disposed = true;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        [Conditional("DEBUG")]
        private static void ForceFinalizersIfDebugMode()
        {
            // Apparently garbage collection only happens if we call it twice,
            // since they are not truly garbage collected until the second pass
            // over the objects.
            //
            // We also do this because we want to have assertion failures occur
            // if we accidentally forget to dispose of anything. At termination
            // of the program, the finalizers might not be called and we'd not
            // know if we failed to Dispose() something. At least in the debug
            // mode we will get assertions that trigger if we force all of the
            // finalizers to run.
            //
            // This should mean that in debug mode, the following invocations
            // of the GC will cause us to be alerted if we ever fail to dispose
            // of anything.
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private static void LogClientInfo()
        {
            Log.Info("{0} v{1}", Constants.ApplicationName, Constants.ApplicationVersion);
            Log.Info("Processor: {0} {1}", Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"), RuntimeInformation.OSArchitecture);
            Log.Info("Processor count: {0}", Environment.ProcessorCount);
            Log.Info("OS: {0} {1} (running {2})", Environment.OSVersion, Environment.Is64BitOperatingSystem ? "x64" : "x86", Environment.Is64BitProcess ? "x64" : "x86");
        }

        private static void LogAnyCommandLineErrors(CommandLineArgs commandLineArgs)
        {
            if (commandLineArgs.Errors.Empty())
                return;

            Log.Error("Bad command line arguments detected:");
            commandLineArgs.Errors.ForEach(Log.Error);
        }

        public static void Main(string[] args)
        {
            CommandLineArgs commandLineArgs = CommandLineArgs.Parse(args);
            Logging.Initialize(commandLineArgs);
            LogClientInfo();
            LogAnyCommandLineErrors(commandLineArgs);

#if DEBUG
            Run(commandLineArgs);
#else
            RunRelease(commandLineArgs);
#endif

            ForceFinalizersIfDebugMode();
            LogManager.Shutdown();
        }

        private static void RunRelease(CommandLineArgs commandLineArgs)
        {
            try
            {
                Run(commandLineArgs);
            }
            catch (Exception e)
            {
                string msg = e.ToString();
                Log.Error(msg);
                File.WriteAllText("errorlog.txt", msg);
                // TODO verify this doesn't prevent from loading on other platforms...
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    ShowFatalError(msg);
            }
        }

        private static void ShowFatalError(string msg) =>
            MessageBox.Show(msg, "Fatal Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

        private static FileConfig ReadConfigFileOrTerminate(string path)
        {
            try
            {
                return new FileConfig(path, true);
            }
            catch
            {
                ShowFatalError("Critical error parsing config file");
                Environment.Exit(1);
                throw;
            }
        }
        
        private static void Run(CommandLineArgs commandLineArgs)
        {
            FileConfig config = ReadConfigFileOrTerminate(FileConfig.DefaultConfigPath);

            try
            {
                ArchiveCollection archiveCollection = new(new FilesystemArchiveLocator(config), config.Compatibility);
                using HelionConsole console = new(config, commandLineArgs);
                using IMusicPlayer musicPlayer = new FluidSynthMusicPlayer(@"SoundFonts\Default.sf2");
                musicPlayer.SetVolume((float)config.Audio.MusicVolume.Value);
                using IAudioSystem audioPlayer = new OpenALAudioSystem(config, archiveCollection, musicPlayer);
                audioPlayer.SetVolume(config.Audio.SoundVolume.Value);

                using Client client = new(commandLineArgs, config, console, audioPlayer, archiveCollection);
                client.Run();
            }
            finally
            {
                if (!config.Write(FileConfig.DefaultConfigPath))
                    Log.Error($"Unable to write config to {FileConfig.DefaultConfigPath}");
            }
        }
    }
}
