using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Special.Specials;
using Xunit;
using System.Linq;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UdmfSectorScroll
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public UdmfSectorScroll()
    {
        World = WorldAllocator.LoadMap("Resources/udmfsectorscroll.zip", "udmfsectorscroll.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "Udmf scroll floor and ceiling textures")]
    public void ScrollFloorAndCeilingTextures()
    {
        var sector = GameActions.GetSectorByTag(World, 1);
        AssertTextureScroll(sector.Floor, (0, 5));
        AssertTextureScroll(sector.Ceiling, (0, 4));
        AssertMonsterCarry(sector, false);
        AssertStaticCarry(sector, false);
        AssertPlayerCarry(sector, false);
    }

    [Fact(DisplayName = "Udmf scroll floor and ceiling textures and monsters")]
    public void ScrollFloorAndCeilingTexturesAndMonsters()
    {
        var sector = GameActions.GetSectorByTag(World, 2);
        AssertTextureScroll(sector.Floor, (5, 0));
        AssertTextureScroll(sector.Ceiling, (4, 0));
        AssertMonsterCarry(sector, true);
        AssertStaticCarry(sector, false);
        AssertPlayerCarry(sector, false);
    }

    [Fact(DisplayName = "Udmf scroll floor and ceiling textures and static objects")]
    public void ScrollFloorAndCeilingTexturesAndStatic()
    {
        var sector = GameActions.GetSectorByTag(World, 3);
        AssertTextureScroll(sector.Floor, (4, 2));
        AssertTextureScroll(sector.Ceiling, (5, 3));
        AssertMonsterCarry(sector, false);
        AssertStaticCarry(sector, true);
        AssertPlayerCarry(sector, false);
    }

    [Fact(DisplayName = "Udmf scroll floor and ceiling textures and players")]
    public void ScrollFloorAndCeilingTexturesAndPlayers()
    {
        var sector = GameActions.GetSectorByTag(World, 4);
        AssertTextureScroll(sector.Floor, (4, 2));
        AssertTextureScroll(sector.Ceiling, (5, 3));
        AssertMonsterCarry(sector, false);
        AssertStaticCarry(sector, false);
        AssertPlayerCarry(sector, true);
    }

    [Fact(DisplayName = "Udmf carry all objects")]
    public void CarryAllObjects()
    {
        var sector = GameActions.GetSectorByTag(World, 5);
        AssertTextureScroll(sector.Floor, Vec2D.Zero);
        AssertTextureScroll(sector.Ceiling, Vec2D.Zero);
        AssertMonsterCarry(sector, true);
        AssertStaticCarry(sector, true);
        AssertPlayerCarry(sector, true);
    }

    [Fact(DisplayName = "Udmf multiple scroll specials on the same sector stack")]
    public void ScrollSpecialStack()
    {
        var sector = GameActions.GetSectorByTag(World, 6);
        var specials = World.SpecialManager.GetSpecials().Where(x => x is ScrollSpecial scroll && scroll.SectorPlane != null && scroll.SectorPlane.Sector == sector).Cast<ScrollSpecial>().ToArray();
        specials.Length.Should().Be(3);
        specials.Count(x => (x.Options & ScrollPlaneOptions.Textures) != 0).Should().Be(2);
        specials.Count(x => (x.Options & ScrollPlaneOptions.CarryAllObjects) != 0).Should().Be(1);
        // Speeds 192 and 256
        // (192-128)/32 = 2 | (256-128)/32 = 4
        AssertTextureScroll(sector.Floor, (0, 6));
        AssertMonsterCarry(sector, true);
        AssertStaticCarry(sector, true);
        AssertPlayerCarry(sector, true);
    }

    private void AssertStaticCarry(Sector sector, bool carry)
    {
        var candle = GameActions.GetSectorEntity(World, sector.Id, "Candlestick");
        GameActions.TickWorld(World, 1);
        AssertEntityCarry(candle, carry);
    }

    private void AssertMonsterCarry(Sector sector, bool carry)
    {
        var monster = GameActions.GetSectorEntity(World, sector.Id, "DoomImp");
        GameActions.TickWorld(World, 1);
        AssertEntityCarry(monster, carry);
    }

    private void AssertPlayerCarry(Sector sector, bool carry)
    {
        var useLine = sector.Lines[0];
        foreach (var line in sector.Lines)
        {
            if (line.Segment.Start.Y == line.Segment.End.Y)
            {
                useLine = line;
                break;
            }
        }

        var monster = GameActions.GetSectorEntity(World, sector.Id, "DoomImp");
        World.EntityManager.Destroy(monster);

        GameActions.SetEntityToLine(World, Player, useLine.Id, 128);
        Player.Velocity.XY.Should().Be(Vec2D.Zero);

        GameActions.TickWorld(World, 1);
        AssertEntityCarry(Player, carry);
    }

    private static void AssertEntityCarry(Entity entity, bool carry)
    {
        if (carry)
            entity.Velocity.XY.Should().NotBe(Vec2D.Zero);
        else
            entity.Velocity.XY.Should().Be(Vec2D.Zero);
    }

    private void AssertTextureScroll(SectorPlane plane, Vec2D amount)
    {
        amount.X = -amount.X;
        var offset = plane.RenderOffsets.Offset;
        GameActions.TickWorld(World, 1);
        var diff = plane.RenderOffsets.Offset - offset;
        diff.Should().Be(amount);
    }
}
