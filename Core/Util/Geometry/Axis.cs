namespace Helion.Util.Geometry
{
    public enum Axis2D
    {
        X,
        Y
    }

    public enum Axis3D
    {
        X,
        Y,
        Z
    }

    public static class Axis2DExtensions
    {
        public static Axis2D Opposite(this Axis2D axis) => axis == Axis2D.X ? Axis2D.Y : Axis2D.X;
    }
}