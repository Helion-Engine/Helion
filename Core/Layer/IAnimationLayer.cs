namespace Helion.Layer;

public interface IAnimationLayer
{
    public InterpolationAnimation<IAnimationLayer> Animation { get; }
    public bool ShouldRemove();
}
