using System;
using Helion.Util.Configs.Components;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Configs
{
    public class Config : IDisposable
    {
        public readonly ConfigFiles Files = new();
        public readonly ConfigWindow Window = new();
        private readonly string m_path;
        private bool m_disposed;

        public Config(string path = "config.ini")
        {
            m_path = path;
        }

        ~Config()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void Dispose()
        {
            if (m_disposed)
                return;

            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            // TODO

            m_disposed = true;
        }
    }
}
