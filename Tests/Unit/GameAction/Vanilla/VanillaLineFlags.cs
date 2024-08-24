﻿using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Impl.SinglePlayer;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public class VanillaLineFlags
    {
        private readonly SinglePlayerWorld World;
        private Player Player => World.Player;
                
        public VanillaLineFlags()
        {
            World = WorldAllocator.LoadMap("Resources/vanillalineflags.zip", "vanillalineflags.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
        }

        private void WorldInit(SinglePlayerWorld world)
        {

        }

        [Fact(DisplayName = "Vanilla line flags")]
        public void VanillaLineFlagOptions()
        {
            var unpeggedFlags = typeof(UnpeggedFlags);
            var lineFlags = typeof(LineFlags);
            var lineBlockFlags = typeof(LineBlockFlags);
            var automapFlags = typeof(LineAutomapFlags);

            GameActions.AssertFlags(GameActions.GetLine(World, 15).Flags.Unpegged,
                unpeggedFlags.GetField(nameof(UnpeggedFlags.Lower)));

            GameActions.AssertFlags(GameActions.GetLine(World, 1).Flags.Unpegged,
                unpeggedFlags.GetField(nameof(UnpeggedFlags.Upper)));

            GameActions.AssertFlags(GameActions.GetLine(World, 8).Flags,
                lineFlags.GetField(nameof(LineFlags.TwoSided)));

            GameActions.AssertFlags(GameActions.GetLine(World, 8).Flags.Blocking,
                lineBlockFlags.GetField(nameof(LineBlockFlags.Players)),
                lineBlockFlags.GetField(nameof(LineBlockFlags.Monsters)));

            GameActions.AssertFlags(GameActions.GetLine(World, 9).Flags,
                lineFlags.GetField(nameof(LineFlags.TwoSided)));

            GameActions.AssertFlags(GameActions.GetLine(World, 9).Flags.Blocking,
                lineBlockFlags.GetField(nameof(LineBlockFlags.Monsters)));

            GameActions.AssertFlags(GameActions.GetLine(World, 12).Flags.Automap,
                automapFlags.GetField(nameof(LineAutomapFlags.NeverDraw)));

            GameActions.AssertFlags(GameActions.GetLine(World, 16).Flags.Automap,
                automapFlags.GetField(nameof(LineAutomapFlags.AlwaysDraw)));

            GameActions.AssertFlags(GameActions.GetLine(World, 14).Flags.Automap,
                automapFlags.GetField(nameof(LineAutomapFlags.DrawAsOneSided)));

            GameActions.AssertFlags(GameActions.GetLine(World, 13).Flags,
                lineFlags.GetField(nameof(LineFlags.BlockSound)));
        }

        [Fact(DisplayName = "Two-sided line block player and monsters")]
        public void TwoSidedLineBlockPlayerAndMonster()
        {            
            GameActions.EntityBlockedByLine(World, Player, 8).Should().BeTrue();

            var monster = GameActions.CreateEntity(World, "Zombieman", Vec3D.Zero);
            GameActions.EntityBlockedByLine(World, monster, 8).Should().BeTrue();
            monster.Dispose();

            // Doesn't block missile
            var rocket = GameActions.CreateEntity(World, "Rocket", Vec3D.Zero);
            GameActions.EntityBlockedByLine(World, rocket, 8).Should().BeFalse();

            // Hitscan passes through
            GameActions.SetEntityToLine(World, Player, 8, Player.Radius);
            var bi = GameActions.FireHitscanTest(World, Player);
            bi.Should().NotBeNull();
            bi!.Value.Line.Should().NotBeNull();
            bi!.Value.Line!.Id.Should().Be(2);
        }

        [Fact(DisplayName = "Two-sided line block monster")]
        public void TwoSidedBlockMonster()
        {
            GameActions.EntityBlockedByLine(World, Player, 9).Should().BeFalse();

            var monster = GameActions.CreateEntity(World, "Zombieman", Vec3D.Zero);
            GameActions.EntityBlockedByLine(World, monster, 9).Should().BeTrue();
            monster.Dispose();

            // Doesn't block missile
            var rocket = GameActions.CreateEntity(World, "Rocket", Vec3D.Zero);
            GameActions.EntityBlockedByLine(World, rocket, 9).Should().BeFalse();

            // Hitscan passes through
            GameActions.SetEntityToLine(World, Player, 9, Player.Radius);
            var bi = GameActions.FireHitscanTest(World, Player);
            bi.Should().NotBeNull();
            bi!.Value.Line.Should().NotBeNull();
            bi!.Value.Line!.Id.Should().Be(6);
        }
    }
}
