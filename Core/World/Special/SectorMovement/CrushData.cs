using Helion.Maps.Specials.ZDoom;

namespace Helion.World.Special.SectorMovement
{
    public class CrushData
    {
        public readonly ZDoomCrushMode CrushMode;
        public readonly int Damage;

        public CrushData(ZDoomCrushMode crushMode, int damage)
        {
            CrushMode = crushMode;
            Damage = damage;
        }
    }
}
