using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class TeleportHeight
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public TeleportHeight()
    {
        World = WorldAllocator.LoadMap("Resources/teleportheight.zip", "teleportheight.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Action 208 - Teleport keeps height")]
    public void Action208_TeleportHeight()
    {
        foreach (var archive in World.ArchiveCollection.AllArchives)
            DebugLog($"Archive {archive.FullPath}");
        DebugLog($"Lines {World.Lines.Count}");
        DebugLog($"Sectors {World.Sectors.Count}");
        DebugLog($"Teleport line spec {World.Lines[8].Special.LineSpecialType}");

        GameActions.SetEntityPosition(World, Player, (-64, -256));
        GameActions.PlayerRunForward(World, Player.AngleRadians, () => { return Player.Sector.Tag != 3; });
        Player.Sector.Tag.Should().Be(3);
        Player.Sector.Floor.Z.Should().Be(0);
        Player.Velocity.Y.Should().BeApproximately(11.56, 2);
        Player.Position.Z.Should().Be(126);
    }

    [Fact(DisplayName = "Action 208 - Teleport keeps height and correctly clamps below ceiling")]
    public void Action208_TeleportHeightCeiling()
    {
        GameActions.SetEntityPosition(World, Player, (64, -256));
        GameActions.PlayerRunForward(World, Player.AngleRadians, () => { return Player.Sector.Tag != 4; });
        Player.Sector.Tag.Should().Be(4);
        Player.Sector.Floor.Z.Should().Be(72);
        Player.Velocity.Y.Should().BeApproximately(12.03, 2);
        Player.Position.Z.Should().Be(72);
    }

    [Fact(DisplayName = "Action 269 - Monster keeps height")]
    public void Action269_TeleportHeightMonster()
    {
        GameActions.SetEntityOutOfBounds(World, Player);
        var caco = GameActions.CreateEntity(World, "Cacodemon", (-64, -176, 64));
        caco.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.MoveEntity(World, caco, 32);
        caco.Sector.Tag.Should().Be(3);
        caco.Sector.Floor.Z.Should().Be(0);
        caco.Position.Z.Should().Be(128);
    }

    private static void DebugLog(string str)
    {
        Console.Out.WriteLine(str);
        System.Diagnostics.Debug.WriteLine(str);
    }
}
