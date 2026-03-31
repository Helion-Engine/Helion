using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UdmfSectorTransformSpecials
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public UdmfSectorTransformSpecials()
    {
        World = WorldAllocator.LoadMap("Resources/udmfsectortransform.zip", "udmfsectortransform.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "Udmf Sector_SetRotation")]
    public void SectorSetRotation()
    {
        var sector = GameActions.GetSectorByTag(World, 1);
        sector.Floor.RenderOffsets.Rotate.Should().Be(0);
        sector.Ceiling.RenderOffsets.Rotate.Should().Be(0);
        GameActions.ActivateLine(World, Player, 6, ActivationContext.UseLine).Should().BeTrue();
        sector.Floor.RenderOffsets.Rotate.Should().BeApproximately(0.78, 2);
        sector.Ceiling.RenderOffsets.Rotate.Should().BeApproximately(1.20, 2);
    }

    [Fact(DisplayName = "Udmf Sector_SetCeilingPanning")]
    public void SectorSetCeilingPanning()
    {
        var sector = GameActions.GetSectorByTag(World, 2);
        sector.Ceiling.RenderOffsets.Offset.X.Should().Be(0);
        sector.Ceiling.RenderOffsets.Offset.Y.Should().Be(0);
        GameActions.ActivateLine(World, Player, 46, ActivationContext.UseLine).Should().BeTrue();
        sector.Ceiling.RenderOffsets.Offset.X.Should().Be(2.05);
        sector.Ceiling.RenderOffsets.Offset.Y.Should().Be(0.04);
    }

    [Fact(DisplayName = "Udmf Sector_SetFloorPanning")]
    public void SectorSetFloorPanning()
    {
        var sector = GameActions.GetSectorByTag(World, 2);
        sector.Floor.RenderOffsets.Offset.X.Should().Be(0);
        sector.Floor.RenderOffsets.Offset.Y.Should().Be(0);
        GameActions.ActivateLine(World, Player, 50, ActivationContext.UseLine).Should().BeTrue();
        sector.Floor.RenderOffsets.Offset.X.Should().Be(2.05);
        sector.Floor.RenderOffsets.Offset.Y.Should().Be(0.04);
    }

    [Fact(DisplayName = "Udmf Sector_SetCeilingScale")]
    public void SectorSetCeilingScale()
    {
        var sector = GameActions.GetSectorByTag(World, 3);
        sector.Ceiling.RenderOffsets.Scale.X.Should().Be(1);
        sector.Ceiling.RenderOffsets.Scale.Y.Should().Be(1);
        GameActions.ActivateLine(World, Player, 22, ActivationContext.UseLine).Should().BeTrue();
        sector.Ceiling.RenderOffsets.Scale.X.Should().Be(0.4);
        sector.Ceiling.RenderOffsets.Scale.Y.Should().Be(2.5);
    }

    [Fact(DisplayName = "Udmf Sector_SetFloorScale")]
    public void SectorSetFloorScale()
    {
        var sector = GameActions.GetSectorByTag(World, 3);
        sector.Floor.RenderOffsets.Scale.X.Should().Be(1);
        sector.Floor.RenderOffsets.Scale.Y.Should().Be(1);
        GameActions.ActivateLine(World, Player, 30, ActivationContext.UseLine).Should().BeTrue();
        sector.Floor.RenderOffsets.Scale.X.Should().BeApproximately(0.48, 2);
        sector.Floor.RenderOffsets.Scale.Y.Should().Be(25);
    }

    [Fact(DisplayName = "Udmf Sector_SetDamage")]
    public void SectorSetDamage()
    {
        var sector = GameActions.GetSectorByTag(World, 4);
        sector.SectorDamageSpecial.Should().BeNull();
        GameActions.ActivateLine(World, Player, 34, ActivationContext.UseLine).Should().BeTrue();
        sector.SectorDamageSpecial.Should().NotBeNull();
        sector.SectorDamageSpecial.Damage.Should().Be(15);
        sector.SectorDamageSpecial.DamageInterval.Should().Be(69);
        sector.SectorDamageSpecial.RadSuitLeakChance.Should().Be(128);
    }

    [Fact(DisplayName = "Udmf Sector_SetGravity")]
    public void SectorSetGravity()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        sector.Gravity.Should().Be(1);
        GameActions.ActivateLine(World, Player, 42, ActivationContext.UseLine).Should().BeTrue();
        sector.Gravity.Should().Be(2.05);
    }
}
