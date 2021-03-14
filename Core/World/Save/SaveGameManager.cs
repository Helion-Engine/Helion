using Helion.Util.Configs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Helion.World.Save
{
    public class SaveGameManager
    {
        private readonly Config m_config;

        public SaveGameManager(Config config)
        {
            m_config = config;
        }

        public void WriteNewSaveGame(IWorld world, string title) =>
            WriteSaveGame(world, title, null);

        public void WriteSaveGame(IWorld world, string title, SaveGame? existingSave)
        {
            string filename = existingSave == null ? GetNewSaveName() : existingSave.FileName;
            SaveGame.WriteSaveGame(world, title, filename);
        }

        public IList<SaveGame> GetSaveGames()
        {
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.hsg");
            List<SaveGame> saveGames = new(files.Length);

            foreach (string file in files)
                saveGames.Add(new SaveGame(file));

            return saveGames;
        }

        private string GetNewSaveName()
        {
            int number = 0;
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.hsg");
            for (int i = 0; i < files.Length; i++)
                files[i] = Path.GetFileName(files[i]);
            bool check = true;

            do
            {
                string name = $"savegame{number}.hsg";
                check = files.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (check)
                    number++;

            } while (check);      

            return $"savegame{number}.hsg";
        }
    }
}
