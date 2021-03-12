using Helion.Models;
using Helion.Util.RandomGenerators;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using MoreLinq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Helion.World
{
    public partial class WorldBase
    {
        private static readonly JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto,
            DefaultValueHandling = DefaultValueHandling.Ignore
        };

        public void Serialize(StreamWriter stream)
        {
            WorldModel worldModel = new WorldModel()
            {
                MapName = MapName.ToString(),
                WorldState = WorldState,
                Gametick = Gametick,
                LevelTime = LevelTime,
                SoundCount = m_soundCount,
                Gravity = Gravity,
                RandomIndex = ((DoomRandom)Random).RandomIndex,
                Skill = ArchiveCollection.Definitions.MapInfoDefinition.MapInfo.GetSkillLevel(SkillDefinition),

                Player = EntityManager.Players[0].ToPlayerModel(),
                Entities = GetEntityModels(),
                Sectors = GetSectorModels(),
                Lines = GetLineModels(),
                Specials = SpecialManager.GetSpecialModels()
            };

            stream.Write(JsonConvert.SerializeObject(worldModel, DefaultSerializerSettings));
        }

        private List<EntityModel> GetEntityModels()
        {
            List<EntityModel> entityModels = new List<EntityModel>();
            EntityManager.Entities.ForEach(entity =>
            {
                if (entity is not Player)
                    entityModels.Add(entity.ToEntityModel(new EntityModel()));
            });
            return entityModels;
        }

        private List<SectorModel> GetSectorModels()
        {
            List<SectorModel> sectorModels = new List<SectorModel>();
            for (int i = 0; i < Sectors.Count; i++)
            {
                Sector sector = Sectors[i];
                if (sector.SoundTarget == null && !sector.DataChanged)
                    continue;

                sectorModels.Add(sector.ToSectorModel());
            }

            return sectorModels;
        }

        private List<LineModel> GetLineModels()
        {
            List<LineModel> lineModels = new List<LineModel>();
            for (int i = 0; i < Lines.Count; i++)
            {
                Line line = Lines[i];
                if (!line.DataChanged)
                    continue;

                lineModels.Add(line.ToLineModel());
            }

            return lineModels;
        }

        public void Deserialize(StreamReader stream)
        {
            WorldModel? worldModel = JsonConvert.DeserializeObject<WorldModel>(stream.ReadToEnd(), DefaultSerializerSettings);
            if (worldModel == null)
                Log.Error("Failed to load save game.");
            else
                LevelExit?.Invoke(this, new LevelChangeEvent(worldModel));
        }
    }
}
