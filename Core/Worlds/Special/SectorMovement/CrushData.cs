using Helion.Maps.Specials.ZDoom;

namespace Helion.Worlds.Special.SectorMovement
{
    public class CrushData
    {
        public readonly ZDoomCrushMode CrushMode;
        public readonly int Damage;
        public readonly double ReturnFactor;

        public CrushData(ZDoomCrushMode crushMode, int damage, double returnFactor = 1.0)
        {
            CrushMode = crushMode;
            Damage = damage;
            ReturnFactor = returnFactor;
        }
    }
}
