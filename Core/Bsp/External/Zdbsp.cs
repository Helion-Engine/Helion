using System.Diagnostics;
using NLog;

namespace Helion.Bsp.External
{
    public class Zdbsp
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly string m_exe;
        private readonly string m_file;
        private readonly string m_output;

        public Zdbsp(string zdbspExe, string file, string outputFile)
        {
            m_exe = zdbspExe;
            m_file = file;
            m_output = outputFile;
        }

        public void Run()
        {
            ProcessStartInfo info = new()
            {
                FileName = m_exe,
                Arguments = CreateArgs(),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process? process = Process.Start(info);
            if (process == null)
            {
                Log.Warn("Unable to start ZDBSP process");
                return;
            }

            process.WaitForExit();
        }

        private string CreateArgs()
        {
            return $"--gl-only --output {m_output} {m_file}";
        }
    }
}
