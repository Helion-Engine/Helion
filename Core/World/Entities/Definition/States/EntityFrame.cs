using Helion.Resources.Definitions.Decorate.States;
using Helion.Util;
using static Helion.Util.Assertion.Assert;
using static Helion.World.Entities.Definition.States.EntityActionFunctions;

namespace Helion.World.Entities.Definition.States
{
    public class EntityFrame
    {
        public readonly string Sprite;
        public readonly int Frame;
        public int Ticks { get; private set; }
        public bool IsInvisible { get; private set; }
        public readonly EntityFrameProperties Properties;
        public readonly ActionFunction? ActionFunction;
        public int NextFrameIndex;
        public ActorStateBranch BranchType;

        public EntityFrame(string sprite, int frame, int ticks, EntityFrameProperties properties,
            ActionFunction? actionFunction, int nextFrameIndex)
        {
            Precondition(nextFrameIndex >= 0, "Cannot have a negative 'next frame index' for an entity frame");
            
            Sprite = sprite;
            Frame = frame;
            Ticks = ticks;
            Properties = properties;
            ActionFunction = actionFunction;
            NextFrameIndex = nextFrameIndex;
            BranchType = ActorStateBranch.None;
            IsInvisible = sprite == Constants.InvisibleSprite;
        }

        public void SetTicks(int tics) => Ticks = tics;
        
        public override string ToString() => $"{Sprite} {Frame} {Ticks} action={ActionFunction != null} flow={BranchType}]";
    }
}