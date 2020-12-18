using Helion.Resource.Definitions.Decorate.States;
using Helion.Worlds.Entities.Definition.States;

namespace Helion.Worlds.Entities.Definition.Composer
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