using Helion.BspOld.Builder;
using Helion.Maps;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Helion.Entries.Archives.Locator;
using Helion.Resources.Archives.Collection;

namespace BspVisualizer
{
    static class Program
    {
        private static bool NotEnoughArguments(string[] args)
        {
            if (args.Length >= 2) 
                return false;
            
            MessageBox.Show(@"Two arguments required: <file> <mapname>", @"BspVisualizer Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            return true;
        }

        private static bool FileDoesNotExist(string path)
        {
            if (File.Exists(path)) 
                return false;
            
            MessageBox.Show($@"Cannot find file at {path}", @"BspVisualizer Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            return true;
        }

        private static bool BadMapName(string mapName)
        {
            if (mapName.Length != 0) 
                return false;
            
            MessageBox.Show($@"Need to provide a valid map name", @"BspVisualizer Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            return true;
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

            ArchiveCollection archiveCollection = new ArchiveCollection(new FilesystemArchiveLocator());
            if (!archiveCollection.Load(new List<string> { args[0] }))
            {
                MessageBox.Show($@"Error loading file at {args[0]}", @"BspVisualizer Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            (IMap? map, MapEntryCollection? _) = archiveCollection.FindMap(args[1]);
            if (map != null)
            {
                StepwiseBspBuilderBase bspBuilderBase = new StepwiseBspBuilderBase(map);

                if (args.Length >= 3)
                    bspBuilderBase.ExecuteUntilBranch(args[2]);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1(bspBuilderBase));
            }
            else
                MessageBox.Show($@"Map '{args[1]}' does not exist or is corrupt", @"BspVisualizer Error", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }
    }
}