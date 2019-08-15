namespace Helion.Maps.Special
{
    public class CrushData
    {
        public CrushData(ZCrushMode crushMode, int damage)
        {
            CrushMode = crushMode;
            Damage = damage;
        }

        public ZCrushMode CrushMode { get; private set; }
        public int Damage { get; private set; }
    }
}
