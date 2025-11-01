using FluentAssertions;
using Helion.Dehacked;
using Helion.Resources.Definitions.Decorate.Properties.Enums;
using Helion.Resources.IWad;
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
