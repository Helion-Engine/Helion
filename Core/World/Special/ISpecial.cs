namespace Helion.World.Special
{
    public interface ISpecial
    {
        SpecialTickStatus Tick();
        void Use();
        SectorBaseSpecialType SectorBaseSpecialType => SectorBaseSpecialType.Default;
    }
}