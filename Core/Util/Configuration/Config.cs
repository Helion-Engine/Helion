using System;
using Helion.Input;
using Helion.Render.Shared;
using Helion.Window;

namespace Helion.Util.Configuration
{
    // TODO: Add 'Constraining' functions for sanity checks to the functions as well.
    
    [ConfigComponent]
    public class EngineConsoleConfig 
    {
        public readonly ConfigValue<int> MaxMessages = new ConfigValue<int>(256);
    }

    [ConfigComponent]
    public class EngineDeveloperConfig
    {
        public readonly ConfigValue<bool> GCStats = new ConfigValue<bool>(false);
        public readonly ConfigValue<bool> MouseFocus = new ConfigValue<bool>(true);
        public readonly ConfigValue<bool> RenderDebug = new ConfigValue<bool>(false);
    }
    
    [ConfigComponent]
    public class EngineMouseConfig
    {
        public readonly ConfigValue<double> Pitch = new ConfigValue<double>(1.0);
        public readonly ConfigValue<double> PixelDivisor = new ConfigValue<double>(1024.0);
        public readonly ConfigValue<double> Sensitivity = new ConfigValue<double>(1.0);
        public readonly ConfigValue<double> Yaw = new ConfigValue<double>(1.0);
        public readonly ConfigValue<bool> MouseLook = new ConfigValue<bool>(true);
    }

    [ConfigComponent]
    public class EngineControlConfig
    {
        public readonly ConfigValue<InputKey> MoveForward = new ConfigValue<InputKey>(InputKey.W);
        public readonly ConfigValue<InputKey> MoveLeft = new ConfigValue<InputKey>(InputKey.A);
        public readonly ConfigValue<InputKey> MoveBackward = new ConfigValue<InputKey>(InputKey.S);
        public readonly ConfigValue<InputKey> MoveRight = new ConfigValue<InputKey>(InputKey.D);
        public readonly ConfigValue<InputKey> Use = new ConfigValue<InputKey>(InputKey.E);
        public readonly ConfigValue<InputKey> Jump = new ConfigValue<InputKey>(InputKey.Space);
        public readonly ConfigValue<InputKey> Crouch = new ConfigValue<InputKey>(InputKey.C);
        public readonly ConfigValue<InputKey> Console = new ConfigValue<InputKey>(InputKey.Backtick);
    }

    [ConfigComponent]
    public class EngineRenderAnisotropyConfig
    {
        public readonly ConfigValue<bool> Enable = new ConfigValue<bool>(true);
        public readonly ConfigValue<bool> UseMaxSupported = new ConfigValue<bool>(true);
        public readonly ConfigValue<double> Value = new ConfigValue<double>(8.0);
    }

    [ConfigComponent]
    public class EngineRenderMultisampleConfig
    {
        public readonly ConfigValue<bool> Enable = new ConfigValue<bool>(false);
        public readonly ConfigValue<int> Value = new ConfigValue<int>(4);
    }

    [ConfigComponent]
    public class EngineRenderConfig
    {
        public readonly EngineRenderAnisotropyConfig Anisotropy = new EngineRenderAnisotropyConfig();
        public readonly EngineRenderMultisampleConfig Multisample = new EngineRenderMultisampleConfig();
        public readonly ConfigValue<FilterType> Filter = new ConfigValue<FilterType>(FilterType.Trilinear);
        public readonly ConfigValue<double> FieldOfView = new ConfigValue<double>(90.0);
    }

    [ConfigComponent]
    public class EngineWindowConfig
    {
        public readonly ConfigValue<int> Height = new ConfigValue<int>(768);
        public readonly ConfigValue<WindowStatus> State = new ConfigValue<WindowStatus>(WindowStatus.Fullscreen);
        public readonly ConfigValue<VerticalSync> VSync = new ConfigValue<VerticalSync>(VerticalSync.Off);
        public readonly ConfigValue<int> Width = new ConfigValue<int>(1024);
    }

    [ConfigSection]
    public class EngineConfig
    {
        public readonly EngineConsoleConfig Console = new EngineConsoleConfig();
        public readonly EngineDeveloperConfig Developer = new EngineDeveloperConfig();
        public readonly EngineMouseConfig Mouse = new EngineMouseConfig();
        public readonly EngineControlConfig Controls = new EngineControlConfig();
        public readonly EngineRenderConfig Render = new EngineRenderConfig();
        public readonly EngineWindowConfig Window = new EngineWindowConfig();
    }
    
    public class Config : IDisposable
    {
        public readonly EngineConfig Engine = new EngineConfig();
        private readonly string configPath;
        private bool disposed;

        public Config(string path = "config.ini")
        {
            configPath = path;
            ConfigReflectionReader.ReadIntoFieldsRecursively(this, configPath);
        }
        
        public void Dispose()
        {
            if (disposed) 
                return;
            
            ConfigReflectionWriter.WriteFieldsRecursively(this, configPath);
            disposed = true;
        }
    }
}