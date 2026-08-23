using FluentAssertions;
using Helion.Dehacked;
using Helion.Resources.Definitions.Decorate.Properties.Enums;
using Helion.Resources.IWad;
using Helion.World;
using Helion.World.Entities.Definition.States;
using Helion.World.Impl.SinglePlayer;
using Xunit;
using static Helion.Dehacked.DehackedDefinition;

namespace Helion.Tests.Unit.GameAction.Mbf21;

[Collection("GameActions")]
public class Flags
{
    private readonly SinglePlayerWorld World;

    public Flags()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        DehackedApplier.ApplyVanillaIndex(new(), World.ArchiveCollection.EntityFrameTable);
    }

    [Fact(DisplayName = "Mbf21 flags set to properties")]
    public void SetProperties()
    {
        var mbf21Flags = Mbf21ThingFlags.LOGRAV | Mbf21ThingFlags.SHORTMRANGE | Mbf21ThingFlags.HIGHERMPROB | Mbf21ThingFlags.LONGMELEE;
        var entity = GameActions.CreateEntity(World, "ZombieMan", default);
        DehackedApplier.SetEntityFlagsMbf21(entity.Properties, null, ref entity.Flags, (uint)mbf21Flags, false);
        entity.Properties.Gravity.Should().Be(DehackedApplier.LowGravity);
        entity.Properties.MaxTargetRange.Should().Be(DehackedApplier.ShortMissileRange);
        entity.Properties.MinMissileChance.Should().Be(DehackedApplier.HigherMissileProb);
        entity.Properties.MeleeThreshold.Should().Be(DehackedApplier.LongMeleeRange);
    }

    [Fact(DisplayName = "Mbf21 A_AddFlags")]
    public void A_AddFlags()
    {
        var flags = ThingProperties.TRANSLUCENT;
        var mbf21Flags = Mbf21ThingFlags.LOGRAV | Mbf21ThingFlags.SHORTMRANGE | Mbf21ThingFlags.HIGHERMPROB | Mbf21ThingFlags.LONGMELEE;
        var entity = GameActions.CreateEntity(World, "ZombieMan", default);
        entity.Alpha.Should().Be(1);
        entity.Gravity.Should().Be(1);
        entity.MaxTargetRange.Should().Be(0);
        entity.MinMissileChance.Should().Be(DehackedApplier.DefaultMissileProb);
        entity.MeleeThreshold.Should().Be(0);

        entity.FrameState.Frame.DehackedArgs1 = (int)flags;
        entity.FrameState.Frame.DehackedArgs2 = (int)mbf21Flags;
        EntityActionFunctions.A_AddFlags(entity);
        entity.RenderStyle.Should().Be(RenderStyle.ColorAddFullBright);
        entity.Alpha.Should().Be(DehackedApplier.TranslucentValue);
        entity.Gravity.Should().Be(DehackedApplier.LowGravity);
        entity.MaxTargetRange.Should().Be(DehackedApplier.ShortMissileRange);
        entity.MinMissileChance.Should().Be(DehackedApplier.HigherMissileProb);
        entity.MeleeThreshold.Should().Be(DehackedApplier.LongMeleeRange);
    }

    [Fact(DisplayName = "Mbf21 A_JumpIfFlagsSet")]
    public void A_JumpIfFlagsSet()
    {
        var entity = GameActions.CreateEntity(World, "ZombieMan", default);
        entity.Definition.DeathState.HasValue.Should().BeTrue();
        entity.Definition.SeeState.HasValue.Should().BeTrue();

        var jumpFrame = WorldStatic.Frames[entity.Definition.DeathState!.Value];
        var setFlagFrame = WorldStatic.Frames[entity.Definition.SeeState!.Value];
        var startFrame = entity.FrameState.Frame;
        jumpFrame.ActionFunction = null;
        setFlagFrame.ActionFunction = null;
        setFlagFrame.DehackedArgs1 = 0;
        startFrame.ActionFunction = null;
        startFrame.DehackedArgs1 = jumpFrame.VanillaIndex;
        startFrame.DehackedArgs2 = 0;
        startFrame.DehackedArgs3 = (int)Mbf21ThingFlags.LOGRAV;

        EntityActionFunctions.A_JumpIfFlagsSet(entity);
        entity.FrameState.Frame.MasterFrameIndex.Should().Be(startFrame.MasterFrameIndex);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.LOGRAV, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.LOGRAV, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.SHORTMRANGE, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.SHORTMRANGE, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.DMGIGNORED, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.DMGIGNORED, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.NORADIUSDMG, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.NORADIUSDMG, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.FORCERADIUSDMG, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.FORCERADIUSDMG, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.HIGHERMPROB, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.HIGHERMPROB, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.RANGEHALF, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.RANGEHALF, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.NOTHRESHOLD, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.NOTHRESHOLD, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.LONGMELEE, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.LONGMELEE, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.BOSS, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.BOSS, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.MAP07BOSS1, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.MAP07BOSS1, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.MAP07BOSS2, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.MAP07BOSS2, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.E1M8BOSS, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.E1M8BOSS, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.E2M8BOSS, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.E2M8BOSS, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.E3M8BOSS, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.E3M8BOSS, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.E4M6BOSS, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.E4M6BOSS, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.E4M8BOSS, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.E4M8BOSS, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.RIP, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.RIP, false);

        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.FULLVOLSOUNDS, true);
        AssertJumpTest(entity, jumpFrame, setFlagFrame, startFrame, Mbf21ThingFlags.FULLVOLSOUNDS, false);
    }

    private static void AssertJumpTest(World.Entities.Entity entity, EntityFrame jumpFrame, EntityFrame setFlagFrame, EntityFrame startFrame, Mbf21ThingFlags flags, bool setFlag)
    {
        // Clear all the flags first
        var mbf21Flags = 0xFFFFFF;
        DehackedApplier.SetEntityFlagsMbf21(null, entity, ref entity.Flags, ~(uint)mbf21Flags, true);

        if (setFlag)
        {
            startFrame.DehackedArgs3 = (int)flags;
            setFlagFrame.DehackedArgs2 = (int)flags;
            entity.FrameState.SetFrameIndex(entity, setFlagFrame.MasterFrameIndex);
            EntityActionFunctions.A_AddFlags(entity);
        }

        DehackedApplier.CheckEntityFlagsMbf21(entity, (uint)flags).Should().Be(setFlag);
        entity.FrameState.SetFrameIndex(entity, startFrame.MasterFrameIndex);
        EntityActionFunctions.A_JumpIfFlagsSet(entity);
        entity.FrameState.Frame.MasterFrameIndex.Should().Be(setFlag ? jumpFrame.MasterFrameIndex : startFrame.MasterFrameIndex);
        entity.FrameState.SetFrameIndex(entity, startFrame.MasterFrameIndex);
    }

    [Fact(DisplayName = "Mbf21 A_RemoveFlags")]
    public void A_RemoveFlags()
    {
        var flags = ThingProperties.TRANSLUCENT;
        var mbf21Flags = Mbf21ThingFlags.LOGRAV | Mbf21ThingFlags.SHORTMRANGE | Mbf21ThingFlags.HIGHERMPROB | Mbf21ThingFlags.LONGMELEE;
        var entity = GameActions.CreateEntity(World, "ZombieMan", default);

        entity.FrameState.Frame.DehackedArgs1 = (int)flags;
        entity.FrameState.Frame.DehackedArgs2 = (int)mbf21Flags;
        EntityActionFunctions.A_AddFlags(entity);

        entity.FrameState.Frame.DehackedArgs1 = 0;
        entity.FrameState.Frame.DehackedArgs2 = (int)Mbf21ThingFlags.LOGRAV;
        EntityActionFunctions.A_RemoveFlags(entity);
        entity.RenderStyle.Should().Be(RenderStyle.ColorAddFullBright);
        entity.Alpha.Should().Be(DehackedApplier.TranslucentValue);
        entity.Gravity.Should().Be(1);
        entity.MaxTargetRange.Should().Be(DehackedApplier.ShortMissileRange);
        entity.MinMissileChance.Should().Be(DehackedApplier.HigherMissileProb);
        entity.MeleeThreshold.Should().Be(DehackedApplier.LongMeleeRange);

        entity.FrameState.Frame.DehackedArgs2 = (int)Mbf21ThingFlags.SHORTMRANGE;
        EntityActionFunctions.A_RemoveFlags(entity);
        entity.RenderStyle.Should().Be(RenderStyle.ColorAddFullBright);
        entity.Alpha.Should().Be(DehackedApplier.TranslucentValue);
        entity.Gravity.Should().Be(1);
        entity.MaxTargetRange.Should().Be(0);
        entity.MinMissileChance.Should().Be(DehackedApplier.HigherMissileProb);
        entity.MeleeThreshold.Should().Be(DehackedApplier.LongMeleeRange);

        entity.FrameState.Frame.DehackedArgs2 = (int)Mbf21ThingFlags.HIGHERMPROB;
        EntityActionFunctions.A_RemoveFlags(entity);
        entity.RenderStyle.Should().Be(RenderStyle.ColorAddFullBright);
        entity.Alpha.Should().Be(DehackedApplier.TranslucentValue);
        entity.Gravity.Should().Be(1);
        entity.MaxTargetRange.Should().Be(0);
        entity.MinMissileChance.Should().Be(DehackedApplier.DefaultMissileProb);
        entity.MeleeThreshold.Should().Be(DehackedApplier.LongMeleeRange);

        entity.FrameState.Frame.DehackedArgs2 = (int)Mbf21ThingFlags.LONGMELEE;
        entity.RenderStyle.Should().Be(RenderStyle.ColorAddFullBright);
        entity.Alpha.Should().Be(DehackedApplier.TranslucentValue);
        EntityActionFunctions.A_RemoveFlags(entity);
        entity.Gravity.Should().Be(1);
        entity.MaxTargetRange.Should().Be(0);
        entity.MinMissileChance.Should().Be(DehackedApplier.DefaultMissileProb);
        entity.MeleeThreshold.Should().Be(0);

        entity.FrameState.Frame.DehackedArgs1 = unchecked((int)ThingProperties.TRANSLUCENT);
        entity.FrameState.Frame.DehackedArgs2 = 0;
        EntityActionFunctions.A_RemoveFlags(entity);
        entity.RenderStyle.Should().Be(RenderStyle.Normal);
        entity.Alpha.Should().Be(1);
        entity.Gravity.Should().Be(1);
        entity.MaxTargetRange.Should().Be(0);
        entity.MinMissileChance.Should().Be(DehackedApplier.DefaultMissileProb);
        entity.MeleeThreshold.Should().Be(0);
    }
}
