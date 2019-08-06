namespace Helion.Maps.Special
{
    public enum SpecialTickStatus
    {
        Continue,
        Destroy,
    }

    public interface ISpecial
    {
        SpecialTickStatus Tick();
    }
}
