namespace Helion.Maps.Special.Specials
{
    class ExitSpecial : ISpecial
    {
        public ExitSpecial()
        {

        }

        public SpecialTickStatus Tick()
        {
            return SpecialTickStatus.Destroy;
        }
    }
}
