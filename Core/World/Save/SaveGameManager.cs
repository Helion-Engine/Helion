using Helion.Util.Configs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Helion.Util.Extensions;

namespace Helion.World.Save
{
    public class SaveGameManager
    {
        private readonly Config m_config;

        public SaveGameManager(Config config)
        {
            m_config = config;
        }

        public string WriteNewSaveGame(IWorld world, string title) =>
            WriteSaveGame(world, title, null);

        public string WriteSaveGame(IWorld world, string title, SaveGame? existingSave)
        {
            string filename = existingSave?.FileName ?? GetNewSaveName();
            SaveGame.WriteSaveGame(world, title, filename);
            return filename;
        }

        public List<SaveGame> GetSaveGames()
        {
            return Directory.GetFiles(Directory.GetCurrentDirectory(), "*.hsg")
                .Select(f => new SaveGame(f))
                .OrderByDescending(f => f.Model?.Date)
                .ToList();
        }

        private string GetNewSaveName()
        {
            List<string> files = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.hsg")
                .Select(Path.GetFileName)
                .WhereNotNull()
                .ToList();

            int number = 0;
            while (true)
            {
                string name = GetSaveName(number);
                if (files.Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase)))
                    number++;
                else
                    return name;
            }
        }

        private static string GetSaveName(int number) => $"savegame{number}.hsg";
    }
}
