using Helion.Maps.Shared;
using Helion.World;
using System;
using System.Collections.Generic;

namespace Helion.Models
{
    public class WorldModel
    {
        public FileModel IWad { get; set; } = FileModel.Default;
        public IList<FileModel> Files { get; set; } = Array.Empty<FileModel>();

        public string MapName { get; set; } = string.Empty;
        public WorldState WorldState { get; set; }
        public int Gametick { get; set; }
        public int LevelTime { get; set; }
        public int SoundCount { get; set; }
        public byte RandomIndex { get; set; }
        public double Gravity { get; set; }
        public SkillLevel Skill { get; set; }

        public PlayerModel Player { get; set; }
        public IList<EntityModel> Entities { get; set; } = Array.Empty<EntityModel>();
        public IList<SectorModel> Sectors { get; set; } = Array.Empty<SectorModel>();
        public IList<LineModel> Lines { get; set; } = Array.Empty<LineModel>();
        public IList<ISpecialModel> Specials { get; set; } = Array.Empty<ISpecialModel>();
        public IList<SectorDamageSpecialModel> DamageSpecials { get; set; } = Array.Empty<SectorDamageSpecialModel>();
    }
}
