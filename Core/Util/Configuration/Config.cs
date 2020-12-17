using System;
using Helion.Util.Configuration.Components;

namespace Helion.Util.Configuration
{
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