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

        public void WriteNewSaveGame(IWorld world, string title) =>
            WriteSaveGame(world, title, null);

        public void WriteSaveGame(IWorld world, string title, SaveGame? existingSave)
        {
            string filename = existingSave?.FileName ?? GetNewSaveName();
            SaveGame.WriteSaveGame(world, title, filename);
        }

        public List<SaveGame> GetSaveGames()
        {
            return Directory.GetFiles(Directory.GetCurrentDirectory(), "*.hsg")
                .Select(f => new SaveGame(f))
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
                if (files.Any(x => x.Equals(files[number], StringComparison.OrdinalIgnoreCase)))
                    return $"savegame{number}.hsg";
                number++;
            }
        }
    }
}
