namespace Helion.Worlds.Entities.Definition.Properties.Components
{
    public struct PlayerFallingScreamSpeed
    {
        private double Min;
        private double Max;

        public PlayerFallingScreamSpeed(double min, double max)
        {
            Min = min;
            Max = max;
        }
    }
}