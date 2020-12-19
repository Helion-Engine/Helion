using Helion.Resource.Definitions.Decorate.States;
using Helion.Resource.Sprites;
using Helion.Util;
using MoreLinq;
using static Helion.Util.Assertion.Assert;
using static Helion.Worlds.Entities.Definition.States.EntityActionFunctions;

namespace Helion.Worlds.Entities.Definition.States
{
    public class EntityFrame
    {
        public readonly Sprite Sprite;
        public int Ticks { get; private set; }
        public bool IsInvisible { get; }
        public readonly EntityFrameProperties Properties;
        public readonly ActionFunction? ActionFunction;
        public int NextFrameIndex;
        public ActorStateBranch BranchType;

        public EntityFrame(Sprite sprite, int ticks, EntityFrameProperties properties,
            ActionFunction? actionFunction, int nextFrameIndex)
        {
            Precondition(nextFrameIndex >= 0, "Cannot have a negative 'next frame index' for an entity frame");

            Sprite = sprite;
            Ticks = ticks;
            Properties = properties;
            ActionFunction = actionFunction;
            NextFrameIndex = nextFrameIndex;
            BranchType = ActorStateBranch.None;
            IsInvisible = sprite.Name.StartsWith(Constants.InvisibleSprite);
        }

        public void SetTicks(int tics) => Ticks = tics;

        public override string ToString() => $"{Sprite} {Ticks} action={ActionFunction != null} flow={BranchType}]";
    }
}