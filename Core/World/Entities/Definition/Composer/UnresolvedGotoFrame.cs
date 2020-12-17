using Helion.Resource.Definitions.Decorate.States;
using Helion.World.Entities.Definition.States;

namespace Helion.World.Entities.Definition.Composer
{
    public class UnresolvedGotoFrame
    {
        public readonly EntityFrame EntityFrame;
        public readonly ActorFrame ActorFrame;

        public UnresolvedGotoFrame(EntityFrame entityFrame, ActorFrame actorFrame)
        {
            EntityFrame = entityFrame;
            ActorFrame = actorFrame;
        }
    }
}