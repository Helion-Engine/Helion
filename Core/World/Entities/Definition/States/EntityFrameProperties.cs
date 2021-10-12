using Helion.Geometry.Vectors;
using Helion.Resources.Definitions.Decorate.States;

namespace Helion.World.Entities.Definition.States;

public class EntityFrameProperties
{
    public static readonly EntityFrameProperties Default = new EntityFrameProperties(new ActorFrameProperties());

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

