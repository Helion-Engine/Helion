using Helion.Maps.Shared;
using Helion.World;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Models
{
    public class WorldModel
    {
        public string IWad;
        public List<string> Files;

        public string MapName { get; set; }
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
        public IList<object> Specials { get; set; } = Array.Empty<object>();
        public IList<SectorDamageSpecialModel> DamageSpecials { get; set; } = Array.Empty<SectorDamageSpecialModel>();

        public IList<ISpecialModel> GetSpecials() => Specials.Cast<ISpecialModel>().ToList();
    }
}
