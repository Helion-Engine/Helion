﻿using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util.Extensions;
using Helion.Util.RandomGenerators;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public class Monsters : IDisposable
    {
        private readonly SinglePlayerWorld World;
        private Player Player => World.Player;

        private class MonsterData
        {
            public readonly string Name;
            public readonly bool HasMissile;
            public readonly bool HasMelee;
            public readonly bool HasMissileLikeMelee;
            public readonly string MissileName;
            public readonly double MissileOffsetZ;

            private static readonly string[] MeleeMonsters = new[]
            {
                "DoomImp",
                "Demon",
                "HellKnight",
                "BaronOfHell",
                "Revenant"
            };

            public MonsterData(string name)
            {
                Name = name;

                HasMissile = !name.EqualsIgnoreCase("Demon");
                HasMelee = MeleeMonsters.Any(x => x.EqualsIgnoreCase(name));
                HasMissileLikeMelee = name.EqualsIgnoreCase("Cacodemon");
                MissileName = GetMissileName(name);

                if (name.EqualsIgnoreCase("Revenant"))
                    MissileOffsetZ = 16;
            }

            public static string GetMissileName(string name)
            {
                return name switch
                {
                    "DoomImp" => "DoomImpBall",
                    "HellKnight" or "BaronOfHell" => "BaronBall",
                    "Arachnotron" => "ArachnotronPlasma",
                    "Cyberdemon" => "Rocket",
                    "Fatso" => "FatShot",
                    "Cacodemon" => "CacodemonBall",
                    "Revenant" => "RevenantTracer",
                    _ => string.Empty,
                };
            }

            public bool CanMissileDamage(string dest)
            {
                string[] MissileAllDamage = new[]
                {
                    "ZombieMan",
                    "ShotgunGuy",
                    "ChaingunGuy",
                    "SpiderMastermind",
                    "HellKnight",
                    "BaronOfHell",
                    "LostSoul",
                    "Archvile"
                };

                string[] MissileDamageExceptSelf = new[]
                {
                    "DoomImp",
                    "Cacodemon",
                    "Arachnotron",
                    "Revenant",
                    "Fatso",
                    "Cyberdemon",
                };

                if (IsBaronCheck(dest))
                    return false;

                if (MissileAllDamage.Any(x => x.EqualsIgnoreCase(Name)))
                    return true;

                if (!dest.EqualsIgnoreCase(Name) && MissileDamageExceptSelf.Any(x => x.EqualsIgnoreCase(Name)))
                    return true;

                return false;
            }

            private bool IsBaronCheck(string dest)
            {
                if (Name.EqualsIgnoreCase("HellKnight") || Name.EqualsIgnoreCase("BaronOfHell"))
                    return dest.EqualsIgnoreCase("HellKnight") || dest.EqualsIgnoreCase("BaronOfHell");

                return false;
            }
        }

        private static readonly MonsterData[] MonsterNames = new[]
        {
            new MonsterData("ZombieMan"),
            new MonsterData("ShotgunGuy"),
            new MonsterData("ChaingunGuy"),
            new MonsterData("DoomImp"),
            new MonsterData("Demon"),
            new MonsterData("LostSoul"),
            new MonsterData("Cacodemon"),
            new MonsterData("HellKnight"),
            new MonsterData("BaronOfHell"),
            new MonsterData("Arachnotron"),
            new MonsterData("PainElemental"),
            new MonsterData("Revenant"),
            new MonsterData("Fatso"),
            new MonsterData("Archvile"),
            new MonsterData("SpiderMastermind"),
            new MonsterData("Cyberdemon"),
        };

        public Monsters()
        {
            World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
        }

        public void Dispose()
        {
            GameActions.DestroyCreatedEntities(World);
            GC.SuppressFinalize(this);
        }

        private void WorldInit(SinglePlayerWorld world)
        {
            world.CheatManager.ActivateCheat(world.Player, CheatType.God);
            world.SetRandom(new NoRandom());
        }

        private static void EntityCreated(Entity entity)
        {
            // Force everything to retaliate immediately
            entity.Flags.QuickToRetaliate = true;
            entity.Properties.MissileMovementSpeed = 0;
            entity.Properties.MonsterMovementSpeed = 0;
            entity.Threshold = 0;
        }

        private static void EntityCreateSetSpeeds(Entity entity)
        {
            entity.Properties.MissileMovementSpeed = 0;
            entity.Properties.MonsterMovementSpeed = 0;
        }

        [Fact(DisplayName = "Monster projectile")]
        public void MonsterProjectile()
        {
            var barrel = GameActions.CreateEntity(World, "ExplosiveBarrel", new Vec3D(-32, -480, 0), onCreated: EntityCreated);
            var monster = GameActions.CreateEntity(World, "Cacodemon", new Vec3D(-32, -416, 0), onCreated: EntityCreated);
            monster.SetTarget(barrel);
            monster.SetMissileState();

            var missileName = MonsterData.GetMissileName("Cacodemon");
            Entity? missile = null;
            Vec3D missileVelocity = Vec3D.Zero;
            Vec3D missilePos = Vec3D.Zero;
            GameActions.TickWorld(World, () => missile == null, () =>
            {
                var findMissiles = GameActions.GetEntities(World, missileName);
                if (findMissiles.Count > 0)
                {
                    missile = findMissiles[0];
                    missileVelocity = missile.Velocity;
                    missilePos = missile.Position;
                }                
            });

            missile.Should().NotBeNull();
            var speed = missile!.Definition.Properties.MissileMovementSpeed;
            missile!.Position.Should().Be(new Vec3D(-32, -455, 32));
            missile!.Velocity.Should().Be(new Vec3D(0, -10, 0));
        }

        [Fact(DisplayName = "Monster projectile different Z")]
        public void MonsterProjectileDifferentZ()
        {
            var barrel = GameActions.CreateEntity(World, "ExplosiveBarrel", new Vec3D(-32, -480, 0), onCreated: EntityCreated);
            var monster = GameActions.CreateEntity(World, "Cacodemon", new Vec3D(-32, -416, 48), onCreated: EntityCreated);
            monster.SetTarget(barrel);
            monster.SetMissileState();

            var missileName = MonsterData.GetMissileName("Cacodemon");
            Entity? missile = null;
            Vec3D missileVelocity = Vec3D.Zero;
            Vec3D missilePos = Vec3D.Zero;
            GameActions.TickWorld(World, () => missile == null, () =>
            {
                var findMissiles = GameActions.GetEntities(World, missileName);
                if (findMissiles.Count > 0)
                {
                    missile = findMissiles[0];
                    missileVelocity = missile.Velocity;
                    missilePos = missile.Position;
                }
            });

            missile.Should().NotBeNull();
            missile!.Position.Should().Be(new Vec3D(-32, -447.2, 56.6));
            missile!.Velocity.Should().Be(new Vec3D(0, -8, -6));
        }

        [Fact(DisplayName = "Barrel player damage source")]
        public void BarrelPlayerDamageSource()
        {
            var barrel = GameActions.CreateEntity(World, "ExplosiveBarrel", new Vec3D(-32, -480, 0), onCreated: EntityCreated);
            var monster = GameActions.CreateEntity(World, "BaronOfHell", new Vec3D(-32, -416, 0), onCreated: EntityCreated);
            barrel.Target.Entity.Should().BeNull();
            monster.Target.Entity.Should().BeNull();
            int startHealth = monster.Health;
            barrel.Damage(Player, barrel.Health, false, DamageType.AlwaysApply);
            GameActions.TickWorld(World, () => { return monster.Health == startHealth; }, () => { });

            monster.Health.Should().BeLessThan(startHealth);
            barrel.Target.Entity.Should().Be(Player);
            monster.Target.Entity.Should().Be(Player);
        }

        [Fact(DisplayName = "Barrel monster damage source")]
        public void BarrelPlayerMonsterSource()
        {
            var barrel = GameActions.CreateEntity(World, "ExplosiveBarrel", new Vec3D(-32, -480, 0), onCreated: EntityCreated);
            var monster = GameActions.CreateEntity(World, "BaronOfHell", new Vec3D(-32, -416, 0), onCreated: EntityCreated);
            var monster2 = GameActions.CreateEntity(World, "BaronOfHell", new Vec3D(-96, -480, 0), onCreated: EntityCreated);
            barrel.Target.Entity.Should().BeNull();
            monster.Target.Entity.Should().BeNull();
            monster2.Target.Entity.Should().BeNull();
            int startHealth = monster.Health;
            barrel.Damage(monster2, barrel.Health, false, DamageType.AlwaysApply);
            GameActions.TickWorld(World, () => { return monster.Health == startHealth; }, () => { });

            monster.Health.Should().BeLessThan(startHealth);
            monster2.Health.Should().BeLessThan(startHealth);
            barrel.Target.Entity.Should().Be(monster2);
            // A baron will target another baron through a barrel explosion and will eventually rip each other apart through melee attacks
            monster.Target.Entity.Should().Be(monster2);
            // monster 2 should not target itself from explosion
            monster2.Target.Entity.Should().BeNull();
        }

        [Fact(DisplayName = "Cyberdemon no radius damage")]
        public void CyberdemonNoRadiusDamage()
        {
            Vec3D pos = new(-256, -416, 0);
            GameActions.SetEntityPosition(World, Player, new Vec2D(-256, -368));
            var cyber = GameActions.CreateEntity(World, "Cyberdemon", pos, onCreated: EntityCreated);
            var rocket = GameActions.CreateEntity(World, "Rocket", pos);
            rocket.Kill(null);

            cyber.Health.Should().Be(cyber.Definition.Properties.Health);
            cyber.Velocity.Should().Be(Vec3D.Zero);

            Player.Velocity.Should().NotBe(Vec3D.Zero);
            Player.Velocity = Vec3D.Zero;
            GameActions.SetEntityOutOfBounds(World, Player);
        }

        [Fact(DisplayName = "Spider Mastermind no radius damage")]
        public void SpiderMastermindNoRadiusDamage()
        {
            Vec3D pos = new(-256, -416, 0);
            GameActions.SetEntityPosition(World, Player, new Vec2D(-256, -368));
            var cyber = GameActions.CreateEntity(World, "SpiderMastermind", pos, onCreated: EntityCreated);
            var rocket = GameActions.CreateEntity(World, "Rocket", pos);
            rocket.Kill(null);

            cyber.Health.Should().Be(cyber.Definition.Properties.Health);
            cyber.Velocity.Should().Be(Vec3D.Zero);

            Player.Velocity.Should().NotBe(Vec3D.Zero);
            Player.Velocity = Vec3D.Zero;
            GameActions.SetEntityOutOfBounds(World, Player);
        }

        [Fact(DisplayName = "Monster set to see state from spawn state after damage")]
        public void SetSeeStateAfterDamage()
        {
            var source = GameActions.CreateEntity(World, "ShotgunGuy", new(-256, -64, 0), onCreated: EntityCreated);
            var dest = GameActions.CreateEntity(World, "Cacodemon", new(-256, -416, 0), onCreated: EntityCreated);

            int savePainChance = dest.Definition.Properties.PainChance;
            dest.Definition.Properties.PainChance = 0;
            source.SetTarget(dest);
            RunMissileState(source, dest, MonsterNames.First(x => x.Name == "ShotgunGuy"), false);

            dest.Target.Entity.Should().NotBeNull();
            dest.Target.Entity.Should().Be(source);
            dest.FrameState.FrameIndex.Should().Be(dest.Definition.SeeState);
            dest.Definition.Properties.PainChance = savePainChance;
        }

        [Fact(DisplayName = "Monster fails to set to see state because it was set to pain state")]
        public void FailsToSetSeeStateAfterDamage()
        {
            // This is a mistake where the pain state is set first. Then the game checked if the monsters was in the spawn state to move to the see state.
            // If the monster hits the pain chance then it would never advance to the see state.
            var source = GameActions.CreateEntity(World, "ShotgunGuy", new(-256, -64, 0), onCreated: EntityCreated);
            var dest = GameActions.CreateEntity(World, "Cacodemon", new(-256, -416, 0), onCreated: EntityCreated);

            int savePainChance = dest.Definition.Properties.PainChance;
            dest.Definition.Properties.PainChance = 255;
            source.SetTarget(dest);
            RunMissileState(source, dest, MonsterNames.First(x => x.Name == "ShotgunGuy"), false);

            dest.Target.Entity.Should().NotBeNull();
            dest.Target.Entity.Should().Be(source);
            dest.FrameState.FrameIndex.Should().Be(dest.Definition.PainState);
            dest.Definition.Properties.PainChance = savePainChance;
        }

        [Fact(DisplayName = "Monster threshold")]
        public void MonsterThreshold()
        {
            // This is a mistake where the pain state is set first. Then the game checked if the monsters was in the spawn state to move to the see state.
            // If the monster hits the pain chance then it would never advance to the see state.
            var firstEnemy = GameActions.CreateEntity(World, "ShotgunGuy", new(-256, -64, 0), onCreated: EntityCreateSetSpeeds);
            var dest = GameActions.CreateEntity(World, "Cacodemon", new(-256, -416, 0), onCreated: EntityCreateSetSpeeds);

            firstEnemy.Threshold.Should().Be(0);
            dest.Threshold.Should().Be(0);

            int savePainChance = dest.Definition.Properties.PainChance;
            dest.Definition.Properties.PainChance = 0;
            firstEnemy.SetTarget(dest);
            RunMissileState(firstEnemy, dest, MonsterNames.First(x => x.Name == "ShotgunGuy"), false);

            dest.Target.Entity.Should().NotBeNull();
            dest.Target.Entity.Should().Be(firstEnemy);
            dest.Threshold.Should().Be(99);

            firstEnemy.Kill(dest);
            GameActions.TickWorld(World, 10);

            // Threshold resets when target is dead
            dest.Threshold.Should().Be(0);

            var secondEnemy = GameActions.CreateEntity(World, "ShotgunGuy", new(-256, -64, 0), onCreated: EntityCreateSetSpeeds);
            var thirdEnemy = GameActions.CreateEntity(World, "ZombieMan", new(-320, -64, 0), onCreated: EntityCreateSetSpeeds);
            secondEnemy.Threshold.Should().Be(0);
            thirdEnemy.Threshold.Should().Be(0);

            secondEnemy.SetTarget(dest);
            thirdEnemy.SetTarget(dest);
            RunMissileState(secondEnemy, dest, MonsterNames.First(x => x.Name == "ShotgunGuy"), false);
            
            dest.Threshold.Should().Be(99);
            dest.Target.Entity.Should().Be(secondEnemy);

            // Threshold prevents from targeting the third enemy
            RunMissileState(thirdEnemy, dest, MonsterNames.First(x => x.Name == "ZombieMan"), false, checkTarget: false);
            dest.Target.Entity.Should().Be(secondEnemy);

            GameActions.TickWorld(World, () => { return dest.Threshold != 0; }, () => { });
            RunMissileState(thirdEnemy, dest, MonsterNames.First(x => x.Name == "ZombieMan"), false, checkTarget: false);
            dest.Target.Entity.Should().Be(thirdEnemy);

            dest.Definition.Properties.PainChance = savePainChance;
        }

        [Fact(DisplayName = "Monster see state plays see sound and going back to spawn does not play see sound")]
        public void MonsterBackToSpawnState()
        {
            GameActions.SetEntityPosition(World, Player, (-320, 64, 0));
            var source = GameActions.CreateEntity(World, "ShotgunGuy", new(-256, -64, 0), onCreated: EntityCreated);
            int frameIndex = source.FrameState.Frame.MasterFrameIndex;
            source.AngleRadians = GameActions.GetAngle(Bearing.South);
            source.FrozenTics = 0;
            source.Flags.Friendly = true;
            var dest = GameActions.CreateEntity(World, "Cacodemon", new(-256, -416, 0), onCreated: EntityCreated);

            GameActions.TickWorld(World, () => { return source.Target.Entity == null; }, () => { });
            source.Target.Entity.Should().Be(dest);

            GameActions.SetEntityOutOfBounds(World, dest);
            source.FrameState.SetFrameIndex(frameIndex);
            GameActions.AssertAnySound(World, source);

            source.Target.Entity.Should().Be(dest);
            GameActions.TickWorld(World, 10);
            GameActions.AssertNoSound(World, source);
            (source.FrameState.Frame.ActionFunction == EntityActionFunctions.A_Look).Should().BeTrue();
        }

        [Fact(DisplayName = "Monster infighting tests")]
        public void MonsterInfight()
        {
            // Melee attacks will always damage even if they are the same type (barons vs barons)
            // Cacodemons don't technically have a melee and it's part of A_HeadAttack
            // Lost souls can damage each other
            Vec3D sourcePos = new(-256, -416, 0);

            foreach (var sourceData in MonsterNames)
            {
                var source = GameActions.CreateEntity(World, sourceData.Name, sourcePos, onCreated: EntityCreated);
                source.Health = int.MaxValue;

                foreach (var destData in MonsterNames)
                    RunSourceDestAttacks(sourcePos, sourceData, source, destData);

                World.EntityManager.Destroy(source);
            }
        }

        private void RunSourceDestAttacks(Vec3D sourcePos, MonsterData sourceData, Entity source, MonsterData destData)
        {
            DebugLog($"{sourceData.Name} -> {destData.Name}");
            var dest = GameActions.CreateEntity(World, destData.Name, new Vec3D(-256, -64, 0), onCreated: EntityCreated);
            dest.Health = int.MaxValue;
            source.SetTarget(dest);
            source.FrozenTics = 0;

            GameActions.SetEntityPosition(World, source, sourcePos);
            if (sourceData.HasMissile)
            {
                source.Definition.MissileState.Should().NotBeNull();
                RunMissileState(source, dest, sourceData, false);
            }

            if (sourceData.HasMissileLikeMelee)
            {
                source.Definition.MissileState.Should().NotBeNull();
                GameActions.SetEntityPosition(World, source, dest.Position.XY);
                RunMissileState(source, dest, sourceData, true);
            }

            if (sourceData.HasMelee)
            {
                source.Definition.MeleeState.Should().NotBeNull();
                GameActions.SetEntityPosition(World, source, dest.Position.XY);
                RunMeleeState(source, dest, sourceData);
            }

            source.SetTarget(null);
            dest.SetTarget(null);
            World.EntityManager.Destroy(dest);
            GameActions.TickWorld(World, 35 * 3);
        }

        private void RunMissileState(Entity source, Entity dest, MonsterData sourceData, bool isLikeMelee, bool checkTarget = true)
        {
            dest.Health = int.MaxValue;
            if (checkTarget)
                dest.SetTarget(null);
            source.SetMissileState();

            int startTicks = World.Gametick;
            bool timeout = false;

            Entity? missile = null;
            Vec3D missileVelocity = Vec3D.Zero;
            Vec3D missilePos = Vec3D.Zero;
            GameActions.TickWorld(World, () => { return !CheckAttackState(dest) && !timeout; }, () =>
            {
                // Needs to run long for archvile attack, lost soul skullfly etc
                if (World.Gametick - startTicks > 35 * 6)
                    timeout = true;

                if (!string.IsNullOrEmpty(sourceData.MissileName) && missile == null)
                {
                    missile = GameActions.FindEntity(World, sourceData.MissileName);
                    if (missile != null)
                    {
                        missileVelocity = missile.Velocity;
                        missilePos = missile.Position;
                    }
                }
            });

            if (!isLikeMelee && !string.IsNullOrEmpty(sourceData.MissileName))
            {
                missile.Should().NotBeNull();
                missilePos.Z.Should().Be(source.ProjectileAttackPos.Z + sourceData.MissileOffsetZ);
                missileVelocity.Z.Should().Be(0);
                missileVelocity.Y.Should().BeGreaterThan(0);
            }

            if (!checkTarget)
                return;
            
            if (sourceData.CanMissileDamage(dest.Definition.Name) || isLikeMelee)
            {
                // Monsters will not retaliate from archvile attack
                if (sourceData.Name.EqualsIgnoreCase("Archvile"))
                    dest.Target.Entity.Should().BeNull();
                else
                    dest.Target.Entity.Should().Be(source);

                DebugLog("Missile - Damaged");
                DebugLog(string.Format("Missile - {0}", dest.Target.Entity == null ? "No Target" : "Targeted"));
                dest.Health.Should().NotBe(int.MaxValue);
                return;
            }

            // Pain elementals shoot lost souls, the dest should be damaged by one
            if (sourceData.Name.Equals("PainElemental"))
            {
                DebugLog("Missile - Damaged and Targeted (Lost Soul)");
                dest.Target.Entity.Should().NotBeNull();
                dest.Target.Entity!.Definition.Name.Should().Be("LostSoul");
                dest.Health.Should().NotBe(int.MaxValue);

                // Destroy the lost soul, otherwise they will mess with the rest of the tests
                GameActions.DestroyEntities(World, "LostSoul");
                return;
            }

            DebugLog("Missile - No Damage");
            dest.Health.Should().Be(int.MaxValue);
            dest.Target.Entity.Should().BeNull();
        }

        private void RunMeleeState(Entity source, Entity dest, MonsterData sourceData)
        {
            dest.Health = int.MaxValue;
            dest.SetTarget(null);
            source.SetMeleeState();

            int startTicks = World.Gametick;
            bool timeout = false;
            GameActions.TickWorld(World, () => { return !CheckAttackState(dest) && !timeout; }, () => 
            {
                if (World.Gametick - startTicks > 35 * 3)
                    timeout = true;
            });

            DebugLog("Melee - Damaged and Targeted");
            // Melee attacks always damage, even if same species. Barell explosion bug is usually the only way this can happen.
            dest.Target.Entity.Should().Be(source);
            dest.Health.Should().NotBe(int.MaxValue);   
        }

        private static bool CheckAttackState(Entity dest)
        {
            if (dest.Health != int.MaxValue)
                return true;

            return false;
        }

        private static void DebugLog(string str)
        {
            Console.Out.WriteLine(str);
            System.Diagnostics.Debug.WriteLine(str);
        }
    }
}
