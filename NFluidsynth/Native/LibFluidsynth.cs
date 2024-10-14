using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace NFluidsynth.Native
{
    internal static partial class LibFluidsynth
    {
        // Supports both ABI 2 and ABI 3 of Fluid Synth
        // https://abi-laboratory.pro/index.php?view=timeline&l=fluidsynth
        public static int LibraryVersion { get; private set; } = 3;

        public const int FluidOk = 0;
        public const int FluidFailed = -1;

#if LINUX
        public const string LibraryName = "libfluidsynth.so.3";
#else
        public const string LibraryName = "fluidsynth-3.dll";
#endif

#if !LINUX && !WINDOWS
        private static bool RegisteredResolver;
        private static IntPtr m_dllHandle = IntPtr.Zero;

        static LibFluidsynth()
        {
            RegisteredResolver = false;
            RegisterDllResolver();
        }

        internal static void RegisterDllResolver()
        {
            if (!RegisteredResolver)
            {
                NativeLibrary.SetDllImportResolver(typeof(LibFluidsynth).Assembly, ImportResolver);
                RegisteredResolver = true;
            }
        }

        private static string GetRuntimePath()
        {
#pragma warning disable IDE0046 // if/else collapsing produces very dense code here
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && Environment.Is64BitProcess)
                return "runtimes\\win-x64\\native\\";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && Environment.Is64BitProcess)
                return "runtimes/linux-x64/native/";

            throw new NotSupportedException("This library does not support the current OS.");
#pragma warning restore IDE0046
        }

        private static string[] GetExpectedLibraryNames()
        {
#pragma warning disable IDE0046 // if/else collapsing produces very dense code here
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return ["fluidsynth.dll", "fluidsynth64.dll", "libfluidsynth.dll", "libfluidsynth64.dll", "fluidsynth-3.dll"];

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return ["libfluidsynth.so", "libfluidsynth.so.2", "libfluidsynth.so.3", "fluidsynth.so", "fluidsynth.so.2", "fluidsynth.so.3"];

            throw new NotSupportedException("This library does not support the current OS.");
#pragma warning restore IDE0046
        }

        private static IntPtr ImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            if (libraryName == LibraryName)
            {
                if (m_dllHandle != IntPtr.Zero)
                {
                    return m_dllHandle;
                }

                string runtimePath = GetRuntimePath();
                string[] libraryNames = GetExpectedLibraryNames();

                foreach (string library in libraryNames)
                {
                    LibraryVersion = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && library.EndsWith("3") ? 3 : 2;

                    // e.g. appdir/runtimes/linux-x64/native/fluidsynth.so
                    if (NativeLibrary.TryLoad($"{baseDirectory}{runtimePath}{library}", out m_dllHandle))
                    {
                        return m_dllHandle;
                    }

                    // e.g. appdir/fluidsynth.so
                    if (NativeLibrary.TryLoad($"{baseDirectory}{library}", out m_dllHandle))
                    {
                        return m_dllHandle;
                    }
                }

                foreach(string primaryLibrary in libraryNames)
                {
                    LibraryVersion = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) && primaryLibrary.EndsWith("3") ? 3 : 2;

                    // default runtime search paths
                    if (NativeLibrary.TryLoad(primaryLibrary, out m_dllHandle))
                    {
                        return m_dllHandle;
                    }
                }

                throw new DllNotFoundException($"Could not load a suitable substitute for DllImport {libraryName}.");
            }

            return IntPtr.Zero;
        }
#endif
    }
}
