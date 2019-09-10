namespace Helion.World.Special
{
    public interface ISpecial
    {
        SpecialTickStatus Tick();
        void Use();
    }
}