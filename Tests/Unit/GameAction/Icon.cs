using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util.Extensions;
using Helion.Util.RandomGenerators;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Impl.SinglePlayer;
using System;
using System.Collections.Generic;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public class Icon
    {
        private readonly SinglePlayerWorld World;

        public Icon()
        {
            World = WorldAllocator.LoadMap("Resources/icon.zip", "icon.wad", "MAP30", GetType().Name, WorldInit, IWadType.Doom2);
            World.SetRandom(new NoRandom());
            World.CheatManager.ActivateCheat(World.Player, CheatType.God);
        }

        private void WorldInit(SinglePlayerWorld world)
        {

        }

        [Fact(DisplayName = "Icon of sin")]
        public void IconOfSin()
        {
            var bossEye = GameActions.GetEntity(World, 2);
            World.Tick();
            GameActions.AssertFrameStateFunction(bossEye, "A_Look");
            GameActions.SetEntityPosition(World, World.Player, new Vec2D(-416, -480));
            GameActions.TickWorld(World, 10);
            GameActions.AssertFrameStateFunction(bossEye, "A_BrainAwake");
            GameActions.AssertSound(World, bossEye, "dsbossit");
            GameActions.SetEntityOutOfBounds(World, World.Player);

            var target1 = GameActions.GetEntity(World, 1);
            var target2 = GameActions.GetEntity(World, 4);
            var target3 = GameActions.GetEntity(World, 5);

            // Every monster will be an imp since random is forced to zero.
            List<Entity> monsters = new();
            RunBrainSpit(bossEye, target1, "DoomImp", monsters);
            RunBrainSpit(bossEye, target2, "DoomImp", monsters);
            RunBrainSpit(bossEye, target3, "DoomImp", monsters);

            // Resets back to first target. Telefrag existing monster.
            monsters[0].IsDead.Should().BeFalse();
            RunBrainSpit(bossEye, target1, "DoomImp", monsters);
            monsters[0].IsDead.Should().BeTrue();

            bool exited = false;
            World.LevelExit += World_LevelExit;
            var bossBrain = GameActions.GetEntity(World, 3);
            bossBrain.Kill(null);
            GameActions.AssertSound(World, bossBrain, "dsbosdth");
            GameActions.TickWorld(World, 35 * 30);
            exited.Should().BeTrue();

            void World_LevelExit(object? sender, LevelChangeEvent e)
            {
                e.Cancel = true;
                exited = true;
            }
        }

        private void RunBrainSpit(Entity bossEye, Entity target, string monsterName, List<Entity> monsters)
        {
            TickUntilAction(bossEye, "A_BrainSpit", 150);
            GameActions.AssertSound(World, bossEye, "dsbospit");

            var cube = FindEntity("SpawnShot", monsters);
            cube.Target.Entity.Should().Be(target);
            TickUntilDisposed(cube);
            var fire = FindEntity("ArchvileFire", Array.Empty<Entity>());
            var monster = FindEntity(monsterName, monsters);
            fire.Position.Should().Be(target.Position);
            monster.Position.Should().Be(target.Position);
            monsters.Add(monster);
        }

        private void TickUntilAction(Entity entity, string name, int ticks)
        {
            GameActions.TickWorld(World, () =>
            {
                if (entity.Frame.ActionFunction == null)
                    return true;

                if (entity.Frame.ActionFunction.Method.Name.EqualsIgnoreCase(name) && entity.FrameState.CurrentTick == ticks)
                    return false;

                return true;
            }, () => { });
        }

        private void TickUntilDisposed(Entity entity)
        {
            GameActions.TickWorld(World, () => { return !entity.IsDisposed; }, () => { });
        }

        private Entity FindEntity(string name, IList<Entity> except)
        {
            Entity? entity = null;
            var node = World.Entities.Head;
            while (node != null)
            {
                if (except.Contains(node.Value) || !node.Value.Definition.Name.EqualsIgnoreCase(name))
                {
                    node = node.Next;
                    continue;
                }

                entity = node.Value;
                break;
            }

            entity.Should().NotBeNull();
            return entity!;
        }
    }
}
