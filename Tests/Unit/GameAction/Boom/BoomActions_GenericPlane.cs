using FluentAssertions;
using Helion.Maps.Specials;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

public partial class BoomActions
{
    const string DefaultFloor = "RROCK19";
    const string DefaultCeiling = "SLIME16";

    [Fact(DisplayName = "Generic floor to next floor + transfer special")]
    public void GenericFloorToNextFloorTransferSpecial()
    {
        var sector = GameActions.GetSectorByTag(World, 29);
        sector.Secret.Should().BeFalse();
        sector.KillEffect.Should().Be(InstantKillEffect.None);
        GameActions.CheckPlaneTexture(World, sector.Floor, DefaultFloor);
        GameActions.ActivateLine(World, Player, 457, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, 1);
        sector.Secret.Should().BeFalse();
        sector.KillEffect.Should().Be(InstantKillEffect.None);
        GameActions.CheckPlaneTexture(World, sector.Floor, DefaultFloor);

        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Secret.Should().BeTrue();
        sector.KillEffect.Should().Be(InstantKillEffect.KillUnprotectedPlayer);
        GameActions.CheckPlaneTexture(World, sector.Floor, "FLOOR5_1");
        sector.Floor.Z.Should().Be(16);
    }

    [Fact(DisplayName = "Generic floor to lowest ceiling + transfer special")]
    public void GenericFloorToLowestCeilingTransferSpecial()
    {
        var sector = GameActions.GetSectorByTag(World, 30);
        sector.Secret.Should().BeFalse();
        sector.KillEffect.Should().Be(InstantKillEffect.None);
        GameActions.CheckPlaneTexture(World, sector.Floor, DefaultFloor);
        GameActions.ActivateLine(World, Player, 484, ActivationContext.UseLine).Should().BeTrue();

        GameActions.TickWorld(World, 1);
        sector.Secret.Should().BeFalse();
        sector.KillEffect.Should().Be(InstantKillEffect.None);
        GameActions.CheckPlaneTexture(World, sector.Floor, DefaultFloor);

        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Secret.Should().BeTrue();
        sector.KillEffect.Should().Be(InstantKillEffect.KillUnprotectedPlayer);
        GameActions.CheckPlaneTexture(World, sector.Floor, "SLIME13");
        sector.Floor.Z.Should().Be(48);
    }

    [Fact(DisplayName = "Generic ceiling to lowest ceiling + transfer special")]
    public void GenericCeilingToLowestCeilingTransferSpecial()
    {
        var sector = GameActions.GetSectorByTag(World, 31);
        sector.Secret.Should().BeFalse();
        sector.KillEffect.Should().Be(InstantKillEffect.None);
        GameActions.CheckPlaneTexture(World, sector.Ceiling, DefaultCeiling);
        GameActions.ActivateLine(World, Player, 512, ActivationContext.UseLine).Should().BeTrue();

        sector.Secret.Should().BeFalse();
        sector.KillEffect.Should().Be(InstantKillEffect.None);
        GameActions.CheckPlaneTexture(World, sector.Ceiling, DefaultCeiling);

        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Secret.Should().BeTrue();
        sector.KillEffect.Should().Be(InstantKillEffect.KillUnprotectedPlayer);
        GameActions.CheckPlaneTexture(World, sector.Ceiling, "RROCK03");
        sector.Ceiling.Z.Should().Be(64);
    }

    [Fact(DisplayName = "Generic ceiling to highest floor + transfer special")]
    public void GenericCeilingToHighestFloorTransferSpecial()
    {
        var sector = GameActions.GetSectorByTag(World, 32);
        sector.Secret.Should().BeFalse();
        sector.KillEffect.Should().Be(InstantKillEffect.None);
        GameActions.CheckPlaneTexture(World, sector.Ceiling, DefaultCeiling);
        GameActions.ActivateLine(World, Player, 485, ActivationContext.UseLine).Should().BeTrue();

        sector.Secret.Should().BeFalse();
        sector.KillEffect.Should().Be(InstantKillEffect.None);
        GameActions.CheckPlaneTexture(World, sector.Ceiling, DefaultCeiling);

        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Secret.Should().BeTrue();
        sector.KillEffect.Should().Be(InstantKillEffect.KillUnprotectedPlayer);
        GameActions.CheckPlaneTexture(World, sector.Ceiling, "SLIME13");
        sector.Ceiling.Z.Should().Be(64);
    }
}
