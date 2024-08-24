﻿using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Vanilla;

[Collection("GameActions")]
public class VanillaStairActions
{
    private static readonly string ResourceZip = "Resources/vanillastairs.zip";
    private static readonly string MapName = "MAP01";

    private readonly SinglePlayerWorld World;

    private Player Player => World.Player;

    public VanillaStairActions()
    {
        World = WorldAllocator.LoadMap(ResourceZip, "vanillastairs.WAD", MapName, GetType().Name, WorldInit, IWadType.Doom2);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
    }

    [Fact(DisplayName = "Vanilla stairs with same tag raise (Doom Action 7 (S1) Raise stairs 8)")]
    public void VanillaStairsRaiseWithSameTag()
    {
        World.Config.Compatibility.Stairs.Set(false);
        const int StairActivationLine = 9;

        GameActions.EntityUseLine(World, Player, StairActivationLine).Should().BeTrue();

        int[] sectorStairIds = { 1, 2, 3, 4, 5, 6 };
        GameActions.RunStairs(World, sectorStairIds, 0, 8, 2);

        // Make sure the floors elsewhere didn't change accidentally.
        foreach ((int sectorId, double startingFloorZ) in new[] { (0, 0.0), (7, 56.0) })
            GameActions.GetSector(World, sectorId).Floor.Z.Should().Be(startingFloorZ, $"Sector {sectorId} is not a stair, should not move from starting Z = {startingFloorZ}");
    }

    [Fact(DisplayName = "Vanilla stairs that are joined together outside of the map like TNT30 (Doom Action 7 (S1) Raise stairs 8)")]
    public void VanillaStairsJoinedSectorExtraRaiseHeight()
    {
        World.Config.Compatibility.Stairs.Set(true);
        const int StairActivationLine = 34;

        GameActions.EntityUseLine(World, Player, StairActivationLine).Should().BeTrue();
        GameActions.TickWorld(World, 35 * 5);

        // Check the stairs manually.
        foreach ((int sectorId, double startingFloorZ) in new[] { (12, 16.0), (14, 40.0) })
            GameActions.GetSector(World, sectorId).Floor.Z.Should().Be(startingFloorZ, $"Sector {sectorId} is not a stair, should not move from starting Z = {startingFloorZ}");

        // Make sure the floors elsewhere didn't change accidentally.
        foreach ((int sectorId, double startingFloorZ) in new[] { (0, 0.0), (10, 48.0) })
            GameActions.GetSector(World, sectorId).Floor.Z.Should().Be(startingFloorZ, $"Sector {sectorId} is not a stair, should not move from starting Z = {startingFloorZ}");
    }

    [Fact(DisplayName = "Stairs that complete out of order")]
    public void StairOutOfOrderTest()
    {
        GameActions.EntityUseLine(World, Player, 70).Should().BeTrue();

        // This tests for a bug that happened when stairs were completed out of order causing the stair special to short out early.
        int[] sectorStairIds = { 25, 26, 27, 28, 29, 33, 23, 20, 21, 22, 18, 32, 31, 30, 19, 15, 16, 17 };
        GameActions.TickWorld(World, 35 * 20);

        int stairZ = 24;
        foreach (var sectorId in sectorStairIds)
        {
            var sector = GameActions.GetSector(World, sectorId);
            sector.ActiveFloorMove.Should().BeNull();
            sector.Floor.Z.Should().Be(stairZ);
            stairZ += 8;
        }
    }
}
