using Helion.Maps.Shared;
using Helion.World;
using System;
using System.Collections.Generic;

namespace Helion.Models
{
    public class WorldModel
    {
        public GameFilesModel Files { get; set; }

        public string MapName { get; set; } = string.Empty;
        public WorldState WorldState { get; set; }
        public int Gametick { get; set; }
        public int LevelTime { get; set; }
        public int SoundCount { get; set; }
        public byte RandomIndex { get; set; }
        public double Gravity { get; set; }
        public SkillLevel Skill { get; set; }
        public int CurrentBossTarget { get; set; }

        public IList<PlayerModel> Players { get; set; } = Array.Empty<PlayerModel>();
        public IList<EntityModel> Entities { get; set; } = Array.Empty<EntityModel>();
        public IList<SectorModel> Sectors { get; set; } = Array.Empty<SectorModel>();
        public IList<LineModel> Lines { get; set; } = Array.Empty<LineModel>();
        public IList<ISpecialModel> Specials { get; set; } = Array.Empty<ISpecialModel>();
        public IList<SectorDamageSpecialModel> DamageSpecials { get; set; } = Array.Empty<SectorDamageSpecialModel>();

        public int TotalMonsters { get; set; }
        public int TotalItems { get; set; }
        public int TotalSecrets { get; set; }

        public int KillCount { get; set; }
        public int ItemCount { get; set; }
        public int SecretCount { get; set; }
    }
}
