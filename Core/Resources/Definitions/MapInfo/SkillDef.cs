using Helion.Util.Configs;
using Helion.World.Entities.Definition.Flags;
using System;
using System.Drawing;

namespace Helion.Resources.Definitions.MapInfo
{
    public class SkillDef
    {
        public double AmmoFactor { get; set; } = 1.0;
        public double DropAmmoFactor { get; set; } = 1.0;
        public double DoubleAmmoFactor { get; set; } = 1.0;
        public double DamageFator { get; set; } = 1.0;
        public double ArmorFactor { get; set; } = 1.0;
        public double HealthFactor { get; set; } = 1.0;
        public double KickbackFactor { get; set; } = 1.0;
        public double MonsterHealthFactor { get; set; } = 1.0;
        public double FriendlyHealthFactor { get; set; } = 1.0;
        public TimeSpan RespawnTime { get; set; }
        public int RespawnLimit { get; set; }
        public double MonsterAggressiveness { get; set; }
        public int SpawnFilter { get; set; }
        public string ACSReturn { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string SkillName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PlayerClassName { get; set; } = string.Empty;
        public string PicName { get; set; } = string.Empty;
        public Color TextColor { get; set; }
        public bool EasyBossBrain { get; set; }
        public bool EasyKey { get; set; }
        public bool FastMonsters { get; set; }
        public bool SlowMonsters { get; set; }
        public bool DisableCheats { get; set; }
        public bool AutoUseHealth { get; set; }
        public bool NoPain { get; set; }
        public bool NoInfighting { get; set; }
        public bool TotalInfighting { get; set; }
        public bool NoMenu { get; set; }
        public bool PlayerRespawn { get; set; }
        public bool MustConfirm { get; set; }
        public bool Default { get; set; }

        public int GetAmmoAmount(int amount, EntityFlags? flags)
        {
            if (flags != null && flags.Value.Dropped)
                return (int)(amount * DropAmmoFactor);

            return (int)(amount * AmmoFactor);
        }

        public int GetDamage(int damage) => (int)(damage * DamageFator);

        public bool IsFastMonsters(IConfig config) => config.Game.FastMonsters || FastMonsters;
    }
}
