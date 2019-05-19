using Helion.BSP.Builder;
using Helion.Map;
using Helion.Projects.Impl.Local;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace BSPVisualizer
{
    static class Program
    {
        private static bool NotEnoughArguments(string[] args)
        {
            if (args.Length < 2)
            {
                MessageBox.Show("Two arguments required: <file> <mapname>", "BspVisualizer Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return true;
            }

            return false;
        }

        private static bool FileDoesNotExist(string path)
        {
            if (!File.Exists(path))
            {
                MessageBox.Show($"Cannot find file at {path}", "BspVisualizer Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return true;
            }

            return false;
        }

        private static bool BadMapName(string mapName)
        {
            if (mapName.Length == 0)
            {
                MessageBox.Show($"Need to provide a valid map name", "BspVisualizer Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return true;
            }

            return false;
        }

        private static bool HandledInvalidArguments(string[] args)
        {
            return NotEnoughArguments(args) || FileDoesNotExist(args[0]) || BadMapName(args[1]);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (HandledInvalidArguments(args))
                return;

            LocalProject project = new LocalProject();
            if (!project.Load(new List<string> { args[0] }))
            {
                MessageBox.Show($"Error loading file at {args[0]}", "BspVisualizer Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            ValidMapEntryCollection? map = project.GetMap(args[1]);
            if (map != null)
            {
                StepwiseBspBuilder bspBuilder = new StepwiseBspBuilder(map);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1(bspBuilder));
            }
            else
                MessageBox.Show($"Map '{args[1]}' does not exist or is corrupt", "BspVisualizer Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }
    }
}
