using Helion.Resources.Definitions.Decorate.States;
using Helion.Util.Geometry;

namespace Helion.World.Entities.Definition.States
{
    public class EntityFrameProperties
    {
        public bool Bright;
        public bool CanRaise;
        public bool Fast;
        public string? Light;
        public bool NoDelay;
        public Vec2I Offset;
        public bool Slow;

        public EntityFrameProperties(ActorFrameProperties actorFrameProperties)
        {
            Bright = actorFrameProperties.Bright ?? false;
            CanRaise = actorFrameProperties.CanRaise ?? false;
            Fast = actorFrameProperties.Fast ?? false;
            Light = actorFrameProperties.Light ?? ""; 
            NoDelay = actorFrameProperties.NoDelay ?? false;
            Offset = actorFrameProperties.Offset ?? Vec2I.Zero; 
            Slow = actorFrameProperties.Slow ?? false;
        }
    }
}