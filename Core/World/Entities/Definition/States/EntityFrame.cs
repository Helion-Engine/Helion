using System;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Util;
using static Helion.Util.Assertion.Assert;
using static Helion.World.Entities.Definition.States.EntityActionFunctions;

namespace Helion.World.Entities.Definition.States
{
    public class EntityFrame
    {
        public string VanillaActorName { get; private set; }
        public string Sprite { get; private set; }
        public string OriginalSprite { get; private set; }
        public int Frame { get; set; }
        public int Ticks { get; set; }
        public bool IsInvisible { get; private set; }
        public readonly EntityFrameProperties Properties;
        public  ActionFunction? ActionFunction { get; set; }
        public int NextFrameIndex { get; set; }
        public ActorStateBranch BranchType { get; set; }

        public int DehackedMisc1 { get; set; }
        public int DehackedMisc2 { get; set; }
        public int MasterFrameIndex { get; set; }
        public int VanillaIndex { get; set; }
        public EntityFrame NextFrame => m_table.Frames[NextFrameIndex];

        private readonly EntityFrameTable m_table;

        public EntityFrame(EntityFrameTable table, string sprite, int frame, int ticks, EntityFrameProperties properties,
            ActionFunction? actionFunction, int nextFrameIndex, string vanillaActorName)
        {
            Precondition(nextFrameIndex >= 0, "Cannot have a negative 'next frame index' for an entity frame");
            m_table = table;
            VanillaActorName = vanillaActorName;
            Sprite = sprite;
            OriginalSprite = sprite;
            Frame = frame;
            Ticks = ticks;
            Properties = properties;
            ActionFunction = actionFunction;
            NextFrameIndex = nextFrameIndex;
            BranchType = ActorStateBranch.None;
            CheckSetInvisible();
        }

        public void SetSprite(string sprite)
        {
            Sprite = sprite;
            CheckSetInvisible();
        }

        public void SetTicks(int tics) => Ticks = tics;
        
        public override string ToString() => $"{Sprite} {Frame} {Ticks} action={ActionString} flow={BranchType} next={NextFrameIndex}]";

        private string ActionString =>
            ActionFunction == null ? "none" : ActionFunction.Method.Name.ToString();

        private void CheckSetInvisible() =>
            IsInvisible = Sprite.Equals(Constants.InvisibleSprite, StringComparison.OrdinalIgnoreCase);
    }
}