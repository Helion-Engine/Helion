namespace Helion.Worlds.Entities.Definition.Properties.Components
{
    public struct PlayerMoveProperty
    {
        public readonly double Walk;
        public readonly double Run;

        public PlayerMoveProperty(double walk, double run)
        {
            Walk = walk;
            Run = run;
        }
    }
}