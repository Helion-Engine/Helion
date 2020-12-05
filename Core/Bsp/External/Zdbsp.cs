using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Helion.Bsp.External
{
    public class Zdbsp
    {
        private readonly string m_exe;
        private readonly string m_file;
        private readonly string m_map;
        private readonly string m_output;

        public Zdbsp(string zdbspExe, string file, string map, string outputFile)
        {
            m_exe = zdbspExe;
            m_file = file;
            m_map = map;
            m_output = outputFile;
        }

        public void Run()
        {
            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = m_exe,
                Arguments = CreateArgs(),
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process process = Process.Start(info);
            process.WaitForExit();
        }

        private string CreateArgs()
        {
            return $"{m_file} --map={m_map} --gl-only --output {m_output}";
        }
    }
}
