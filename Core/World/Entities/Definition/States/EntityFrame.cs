using System;
using System.Collections.Generic;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Util;
using static Helion.Dehacked.DehackedDefinition;
using static Helion.Util.Assertion.Assert;
using static Helion.World.Entities.Definition.States.EntityActionFunctions;

namespace Helion.World.Entities.Definition.States;

public class EntityFrame
{
    public string VanillaActorName;
    public string Sprite;
    public int SpriteIndex;
    public string OriginalSprite;
    public int Frame;
    public int Ticks;
    public bool IsInvisible;
    public EntityFrameProperties Properties;
    public ActionFunction? ActionFunction;
    public int NextFrameIndex;
    public ActorStateBranch BranchType;

    public int DehackedMisc1;
    public int DehackedMisc2;
    public int DehackedArgs1;
    public int DehackedArgs2;
    public int DehackedArgs3;
    public int DehackedArgs4;
    public int DehackedArgs5;
    public int DehackedArgs6;
    public int DehackedArgs7;
    public int DehackedArgs8;
    public int MasterFrameIndex;
    public int VanillaIndex;
    public FrameArgs Args = FrameArgs.Default;
    public EntityFrame NextFrame => m_table.Frames[NextFrameIndex];
    public bool IsNullFrame => MasterFrameIndex == Constants.NullFrameIndex;
    public readonly bool IsSlowTickChase;
    public readonly bool IsSlowTickLook;
    public readonly bool IsSlowTickTracer;

    private readonly EntityFrameTable m_table;

    public EntityFrame(EntityFrameTable table, string sprite, int frame, int ticks, in EntityFrameProperties properties,
        ActionFunction? actionFunction, int nextFrameIndex, string vanillaActorName, IList<object>? frameArgs = null)
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

        if (frameArgs != null)
            Args = new FrameArgs(frameArgs);

        IsSlowTickChase = ActionFunction == EntityActionFunctions.A_Chase || ActionFunction == EntityActionFunctions.A_VileChase ||
            ActionFunction == EntityActionFunctions.A_HealChase;
        IsSlowTickLook = ActionFunction == EntityActionFunctions.A_Look;
        IsSlowTickTracer = ActionFunction == EntityActionFunctions.A_Tracer || ActionFunction == EntityActionFunctions.A_SeekTracer;

        CheckSetInvisible();
    }

    public void SetSprite(string sprite)
    {
        Sprite = sprite;
        SpriteIndex = m_table.GetSpriteIndex(sprite);
        CheckSetInvisible();
    }

    public override string ToString() => $"{Sprite} {Frame} {Ticks} action={ActionString} flow={BranchType} next={NextFrameIndex}]";

    private string ActionString =>
        ActionFunction == null ? "none" : ActionFunction.Method.Name;

    private void CheckSetInvisible() =>
        IsInvisible = Sprite.Equals(Constants.InvisibleSprite, StringComparison.OrdinalIgnoreCase);
}
