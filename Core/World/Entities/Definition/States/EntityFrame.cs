using System;
using Helion.Resources.Definitions.Decorate.States;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities.Definition.States
{
    public class EntityFrame
    {
        public readonly string Sprite;
        public readonly int Frame;
        public readonly int Ticks;
        public readonly EntityFrameProperties Properties;
        public readonly Action<Entity>? ActionFunction;
        public int NextFrameIndex;
        public ActorStateBranch BranchType;

        public EntityFrame(string sprite, int frame, int ticks, EntityFrameProperties properties,
            Action<Entity>? actionFunction, int nextFrameIndex)
        {
            Precondition(nextFrameIndex >= 0, "Cannot have a negative 'next frame index' for an entity frame");
            
            Sprite = sprite;
            Frame = frame;
            Ticks = ticks;
            Properties = properties;
            ActionFunction = actionFunction;
            NextFrameIndex = nextFrameIndex;
            BranchType = ActorStateBranch.None;
        }
        
        public override string ToString() => $"{Sprite} {Frame} {Ticks} action={ActionFunction != null} flow={BranchType}]";
    }
}