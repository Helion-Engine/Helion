using Helion.Resources.Definitions;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Resources.Definitions.Language;
using Helion.Util;
using Helion.World.Cheats;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using Helion.World.Entities.Definition.States;
using NLog;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static Helion.Dehacked.DehackedDefinition;

namespace Helion.Dehacked
{
    public static class DehackedApplier
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static readonly List<string> RemoveLabels = new();

        public static void Apply(DehackedDefinition dehacked, DefinitionEntries definitionEntries, EntityDefinitionComposer composer)
        {
            ApplyVanillaIndex(dehacked, definitionEntries.EntityFrameTable);

            ApplyThings(dehacked, definitionEntries.EntityFrameTable, composer);
            ApplyFrames(dehacked, definitionEntries.EntityFrameTable);
            ApplyPointers(dehacked, definitionEntries.EntityFrameTable);
            ApplyWeapons(dehacked, definitionEntries.EntityFrameTable, composer);
            ApplyAmmo(dehacked, composer);
            ApplyText(dehacked, definitionEntries.EntityFrameTable, definitionEntries.Language);
            ApplyCheats(dehacked);

            ApplyBexText(dehacked, definitionEntries.Language);

            RemoveLabels.Clear();
        }

        private static void ApplyVanillaIndex(DehackedDefinition dehacked, EntityFrameTable table)
        {
            for (int i = 0; i < typeof(ThingState).GetEnumValues().Length; i++)
            {
                if (!GetFrameIndex(dehacked, table, i, out int frameIndex))
                    continue;

                table.Frames[frameIndex].VanillaIndex = i;
                table.VanillaFrameMap[i] = table.Frames[frameIndex];
            }
        }

        private static void ApplyWeapons(DehackedDefinition dehacked, EntityFrameTable entityFrameTable, EntityDefinitionComposer composer)
        {
            foreach (var weapon in dehacked.Weapons)
            {
                EntityDefinition? weaponDef = GetWeaponDefinition(weapon.WeaponNumber, composer);
                if (weaponDef == null)
                    return;

                // Deselect and select are backwards in dehacked...
                if (weapon.DeselectFrame.HasValue)
                    ApplyThingFrame(dehacked, entityFrameTable, weaponDef, weapon.DeselectFrame.Value, Constants.FrameStates.Select);
                if (weapon.SelectFrame.HasValue)
                    ApplyThingFrame(dehacked, entityFrameTable, weaponDef, weapon.SelectFrame.Value, Constants.FrameStates.Deselect);
                if (weapon.BobbingFrame.HasValue)
                    ApplyThingFrame(dehacked, entityFrameTable, weaponDef, weapon.BobbingFrame.Value, Constants.FrameStates.Ready);
                if (weapon.ShootingFrame.HasValue)
                    ApplyThingFrame(dehacked, entityFrameTable, weaponDef, weapon.ShootingFrame.Value, Constants.FrameStates.Fire);
                if (weapon.FiringFrame.HasValue)
                    ApplyThingFrame(dehacked, entityFrameTable, weaponDef, weapon.FiringFrame.Value, Constants.FrameStates.Flash);
                if (weapon.AmmoType.HasValue)
                    SetWeaponAmmo(weaponDef, weapon.AmmoType.Value);
            }
        }

        private static void SetWeaponAmmo(EntityDefinition weaponDef, int ammoType)
        {
            switch (ammoType)
            {
                case 0:
                    weaponDef.Properties.Weapons.AmmoType = "Clip";
                    break;
                case 1:
                    weaponDef.Properties.Weapons.AmmoType = "Shell";
                    break;
                case 2:
                    weaponDef.Properties.Weapons.AmmoType = "Cell";
                    break;
                case 3:
                    weaponDef.Properties.Weapons.AmmoType = "RocketAmmo";
                    break;
                case 5:
                    weaponDef.Properties.Weapons.AmmoType = string.Empty;
                    break;
            }

            Warning($"Invalid ammo type {ammoType}");
        }

        private static EntityDefinition? GetWeaponDefinition(int weaponNumber, EntityDefinitionComposer composer)
        {
            switch (weaponNumber)
            {
                case 0:
                    return composer.GetByName("Fist");
                case 1:
                    return composer.GetByName("Pistol");
                case 2:
                    return composer.GetByName("Shotgun");
                case 3:
                    return composer.GetByName("Chaingun");
                case 4:
                    return composer.GetByName("RocketLauncher");
                case 5:
                    return composer.GetByName("PlasmaRifle");
                case 6:
                    return composer.GetByName("BFG9000");
                case 7:
                    return composer.GetByName("Chainsaw");
                case 8:
                    return composer.GetByName("SuperShotgun");
            }

            Warning($"Invalid weapon {weaponNumber}");
            return null;
        }

        private static void ApplyPointers(DehackedDefinition dehacked, EntityFrameTable entityFrameTable)
        {
            foreach (var pointer in dehacked.Pointers)
            {
                if (!GetFrameIndex(dehacked, entityFrameTable, pointer.Frame, out int frameIndex))
                {
                    Warning($"Invalid pointer frame {pointer.Frame}");
                    continue;
                }

                var entityFrame = entityFrameTable.Frames[frameIndex];
                if (pointer.CodePointerMnemonic != null)
                {
                    if (pointer.CodePointerMnemonic.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                    {
                        entityFrame.ActionFunction = null;
                    }
                    else
                    {
                        string functionName = "A_" + pointer.CodePointerMnemonic;
                        var function = EntityActionFunctions.Find(functionName);
                        if (function != null)
                            entityFrame.ActionFunction = function;
                        else
                            Warning($"Invalid pointer mnemonic {pointer.CodePointerMnemonic}");
                    }
                }
                else
                {
                    ThingState fromState = (ThingState)pointer.CodePointerFrame;
                    if (dehacked.ActionFunctionLookup.TryGetValue(fromState, out string? function))
                        entityFrame.ActionFunction = EntityActionFunctions.Find(function);
                    else
                        entityFrame.ActionFunction = null;
                }
            }
        }

        private static void ApplyFrames(DehackedDefinition dehacked, EntityFrameTable entityFrameTable)
        {
            foreach (var frame in dehacked.Frames)
            {
                if (!GetFrameIndex(dehacked, entityFrameTable, frame.Frame, out int frameIndex))
                {
                    Warning($"Invalid frame {frame.Frame}");
                    continue;
                }

                var entityFrame = entityFrameTable.Frames[frameIndex];

                if (frame.SpriteNumber.HasValue && frame.SpriteNumber >= 0 && frame.SpriteNumber < dehacked.Sprites.Length)
                    entityFrame.SetSprite(dehacked.Sprites[frame.SpriteNumber.Value]);
                if (frame.Duration.HasValue)
                    entityFrame.Ticks = frame.Duration.Value;

                if (frame.SpriteSubNumber.HasValue)
                {
                    entityFrame.Frame = frame.SpriteSubNumber.Value & FrameMask;
                    entityFrame.Properties.Bright = (frame.SpriteSubNumber.Value & FullBright) > 0;
                }

                if (frame.NextFrame.HasValue)
                {
                    if (GetFrameIndex(dehacked, entityFrameTable, frame.NextFrame.Value, out int nextFrameIndex))
                    {
                        entityFrame.NextFrameIndex = nextFrameIndex;
                        entityFrame.BranchType = ActorStateBranch.None;
                    }
                    else
                    {
                        Warning($"Invalid next frame {frame.NextFrame.Value}");
                    }
                }

                if (frame.Unknown1.HasValue)
                    entityFrame.DehackedMisc1 = frame.Unknown1.Value;
                if (frame.Unknown2.HasValue)
                    entityFrame.DehackedMisc2 = frame.Unknown2.Value;
            }
        }

        private static bool GetFrameIndex(DehackedDefinition dehacked, EntityFrameTable entityFrameTable, int frame, out int frameIndex)
        {
            frameIndex = -1;
            if (frame < 0 || frame >= dehacked.ThingStateLookups.Length)
                return false;

            var lookup = dehacked.ThingStateLookups[frame];
            int baseFrame = -1;

            for (int i = 0; i < entityFrameTable.Frames.Count; i++)
            {
                var frameItem = entityFrameTable.Frames[i];
                if (lookup.Frame != null && lookup.Frame != frameItem.Frame)
                    continue;

                if (lookup.ActorName != null && !lookup.ActorName.Equals(frameItem.VanillaActorName))
                    continue;

                if (frameItem.OriginalSprite.Equals(lookup.Sprite, StringComparison.OrdinalIgnoreCase))
                {
                    baseFrame = i;
                    break;
                }
            }

            if (baseFrame == -1)
                return false;

            frameIndex = baseFrame + lookup.Offset;
            return true;
        }

        private static void ApplyThings(DehackedDefinition dehacked, EntityFrameTable entityFrameTable, EntityDefinitionComposer composer)
        {
            foreach (var thing in dehacked.Things)
            {
                var definition = GetEntityDefinition(dehacked, thing.Number, composer);
                if (definition == null)
                {
                    Warning($"Invalid thing {thing.Number}");
                    continue;
                }

                var properties = definition.Properties;
                if (thing.ID.HasValue)
                    composer.ChangeEntityEditorID(definition, thing.ID.Value);
                if (thing.Hitpoints.HasValue)
                    properties.Health = thing.Hitpoints.Value;
                if (thing.ReactionTime.HasValue)
                    properties.ReactionTime = thing.ReactionTime.Value;
                if (thing.PainChance.HasValue)
                    properties.PainChance = thing.PainChance.Value;
                if (thing.Speed.HasValue)
                    properties.Speed = GetThingSpeed(thing.Speed.Value, definition);
                if (thing.Width.HasValue)
                    properties.Radius = GetDouble(thing.Width.Value);
                if (thing.Height.HasValue)
                    properties.Height = GetDouble(thing.Height.Value);
                if (thing.Mass.HasValue)
                    properties.Mass = thing.Mass.Value;
                if (thing.MisileDamage.HasValue)
                    properties.Damage.Value = thing.MisileDamage.Value;
                if (thing.Bits.HasValue)
                    SetActorFlags(definition, thing.Bits.Value);

                if (thing.AlertSound.HasValue)
                    properties.SeeSound = GetSound(dehacked, thing.AlertSound.Value);
                if (thing.AttackSound.HasValue)
                    properties.AttackSound = GetSound(dehacked, thing.AttackSound.Value);
                if (thing.PainSound.HasValue)
                    properties.PainSound = GetSound(dehacked, thing.PainSound.Value);
                if (thing.DeathSound.HasValue)
                    properties.DeathSound = GetSound(dehacked, thing.DeathSound.Value);
                if (thing.ActionSound.HasValue)
                    properties.ActiveSound = GetSound(dehacked, thing.ActionSound.Value);

                if (thing.CloseAttackFrame.HasValue)
                    ApplyThingFrame(dehacked, entityFrameTable, definition, thing.CloseAttackFrame.Value, Constants.FrameStates.Melee);
                if (thing.FarAttackFrame.HasValue)
                    ApplyThingFrame(dehacked, entityFrameTable, definition, thing.FarAttackFrame.Value, Constants.FrameStates.Missile);
                if (thing.DeathFrame.HasValue)
                    ApplyThingFrame(dehacked, entityFrameTable, definition, thing.DeathFrame.Value, Constants.FrameStates.Death);
                if (thing.ExplodingFrame.HasValue)
                    ApplyThingFrame(dehacked, entityFrameTable, definition, thing.ExplodingFrame.Value, Constants.FrameStates.XDeath);
                if (thing.InitFrame.HasValue)
                    ApplyThingFrame(dehacked, entityFrameTable, definition, thing.InitFrame.Value, Constants.FrameStates.Spawn);
                if (thing.InjuryFrame.HasValue)
                    ApplyThingFrame(dehacked, entityFrameTable, definition, thing.InjuryFrame.Value, Constants.FrameStates.Pain);
                if (thing.FirstMovingFrame.HasValue)
                    ApplyThingFrame(dehacked, entityFrameTable, definition, thing.FirstMovingFrame.Value, Constants.FrameStates.See);
                if (thing.RespawnFrame.HasValue)
                    ApplyThingFrame(dehacked, entityFrameTable, definition, thing.RespawnFrame.Value, Constants.FrameStates.Raise);
            }
        }

        private static int GetThingSpeed(int speed, EntityDefinition definition)
        {
            if (definition.Flags.Missile)
                return (int)GetDouble(speed);
            return speed;
        }

        private static void ApplyThingFrame(DehackedDefinition dehacked, EntityFrameTable entityFrameTable, 
            EntityDefinition definition, int frame, string actionLabel)
        {
            if (!dehacked.FrameLookup.TryGetValue((ThingState)frame, out FrameStateLookup? frameLookup))
            {
                Warning($"Invalid thing frame {frame} for {definition.Name}");
                return;
            }

            if (!entityFrameTable.FrameSets.TryGetValue(frameLookup.Label, out FrameSet? frameSet))
            {
                Warning($"Invalid thing frame {frame} for {definition.Name}");
                return;
            }

            RemoveActionLabels(definition, actionLabel);

            if (!frameLookup.Label.Equals("Actor::null", StringComparison.OrdinalIgnoreCase))
            {
                definition.States.Labels[actionLabel] = frameSet.StartFrameIndex + frameLookup.Offset;
                definition.States.Labels[$"{definition.Name}::{actionLabel}"] = frameSet.StartFrameIndex + frameLookup.Offset;
            }
        }

        private static void RemoveActionLabels(EntityDefinition definition, string actionLabel)
        {
            RemoveLabels.Clear();
            foreach (var pair in definition.States.Labels)
            {
                int index = pair.Key.IndexOf("::");
                if (index == -1 && !pair.Key.Equals(actionLabel, StringComparison.OrdinalIgnoreCase))
                    continue;
                else if (index != -1 && !pair.Key[(index + 2)..].Equals(actionLabel, StringComparison.OrdinalIgnoreCase))
                    continue;
                RemoveLabels.Add(pair.Key);
            }

            RemoveLabels.ForEach(x => definition.States.Labels.Remove(x));
        }

        private static EntityDefinition? GetEntityDefinition(DehackedDefinition dehacked, int thingNumber, EntityDefinitionComposer composer)
        {
            int index = thingNumber - 1;
            if (index < 0 || index >= dehacked.ActorNames.Length)
                return null;

            string actorName = dehacked.ActorNames[index];
            return composer.GetByName(actorName);           
        }

        private static void ApplyAmmo(DehackedDefinition dehacked, EntityDefinitionComposer composer)
        {
            foreach (var ammo in dehacked.Ammo)
            {
                if (ammo.AmmoNumber < 0 || ammo.AmmoNumber >= dehacked.AmmoNames.Length)
                {
                    Warning($"Invalid ammo {ammo.AmmoNumber}");
                    continue;
                }

                var definition = composer.GetByName(dehacked.AmmoNames[ammo.AmmoNumber]);
                ApplyAmmo(definition, ammo, 1);

                if (ammo.AmmoNumber >= dehacked.AmmoDoubleNames.Length)
                    continue;

                definition = composer.GetByName(dehacked.AmmoDoubleNames[ammo.AmmoNumber]);
                ApplyAmmo(definition, ammo, 2);
            }
        }

        private static void ApplyAmmo(EntityDefinition? definition, DehackedAmmo ammo, int multiplier)
        {
            if (definition == null)
                return;

            var inventory = definition.Properties.Inventory;
            if (ammo.PerAmmo.HasValue)
            {
                inventory.Amount = ammo.PerAmmo.Value * multiplier;
                definition.Properties.Ammo.BackpackAmount = ammo.PerAmmo.Value * multiplier;
            }

            if (ammo.MaxAmmo.HasValue)
            {
                inventory.MaxAmount = ammo.MaxAmmo.Value;
                definition.Properties.Ammo.BackpackMaxAmount = ammo.MaxAmmo.Value * 2;
            }
        }

        private static void ApplyText(DehackedDefinition dehacked, EntityFrameTable entityFrameTable, LanguageDefinition language)
        {
            string levelRegex = @"level \d+: ";
            foreach (var text in dehacked.Strings)
            {
                if (dehacked.SpriteNames.Contains(text.OldString))
                {
                    UpdateSpriteText(entityFrameTable, text);
                    continue;
                }

                var match = Regex.Match(text.OldString, levelRegex);
                if (match.Success)
                    text.OldString = text.OldString.Replace(match.Value, string.Empty);

                match = Regex.Match(text.NewString, levelRegex);
                if (match.Success)
                    text.NewString = text.NewString.Replace(match.Value, string.Empty);

                if (language.GetKeyByValue(text.OldString, out string? key) && key != null)
                    language.SetValue(key, text.NewString);
                else
                    Warning($"Invalid text {text.OldString}");
            }
        }

        private static void UpdateSpriteText(EntityFrameTable entityFrameTable, DehackedString text)
        {
            foreach (var frame in entityFrameTable.Frames)
            {
                if (!frame.Sprite.Equals(text.OldString))
                    continue;

                frame.SetSprite(text.NewString);
            }
        }

        private static void SetActorFlags(EntityDefinition def, uint value)
        {
            bool hadShadow = def.Flags.Shadow;

            ThingProperties thingProperties = (ThingProperties)value;
            def.Flags.Special = thingProperties.HasFlag(ThingProperties.SPECIAL);
            def.Flags.Solid = thingProperties.HasFlag(ThingProperties.SOLID);
            def.Flags.Shootable = thingProperties.HasFlag(ThingProperties.SHOOTABLE);
            def.Flags.NoSector = thingProperties.HasFlag(ThingProperties.NOSECTOR);
            def.Flags.NoBlockmap = thingProperties.HasFlag(ThingProperties.NOBLOCKMAP);
            def.Flags.Ambush = thingProperties.HasFlag(ThingProperties.AMBUSH);
            def.Flags.JustHit = thingProperties.HasFlag(ThingProperties.JUSTHIT);
            def.Flags.JustAttacked = thingProperties.HasFlag(ThingProperties.JUSTATTACKED);
            def.Flags.SpawnCeiling = thingProperties.HasFlag(ThingProperties.SPAWNCEILING);
            def.Flags.NoGravity = thingProperties.HasFlag(ThingProperties.NOGRAVITY);
            def.Flags.Dropoff = thingProperties.HasFlag(ThingProperties.DROPOFF);
            def.Flags.Pickup = thingProperties.HasFlag(ThingProperties.PICKUP);
            def.Flags.NoClip = thingProperties.HasFlag(ThingProperties.NOCLIP);
            def.Flags.SlidesOnWalls = thingProperties.HasFlag(ThingProperties.SLIDE);
            def.Flags.Float = thingProperties.HasFlag(ThingProperties.FLOAT);
            def.Flags.Teleport = thingProperties.HasFlag(ThingProperties.TELEPORT);
            def.Flags.Missile = thingProperties.HasFlag(ThingProperties.MISSILE);
            def.Flags.Dropped = thingProperties.HasFlag(ThingProperties.DROPPED);
            def.Flags.Shadow = thingProperties.HasFlag(ThingProperties.SHADOW);
            def.Flags.NoBlood = thingProperties.HasFlag(ThingProperties.NOBLOOD);
            def.Flags.Corpse = thingProperties.HasFlag(ThingProperties.CORPSE);
            def.Flags.CountKill = thingProperties.HasFlag(ThingProperties.COUNTKILL);
            def.Flags.CountItem = thingProperties.HasFlag(ThingProperties.COUNTITEM);
            def.Flags.Skullfly = thingProperties.HasFlag(ThingProperties.SKULLFLY);
            def.Flags.NotDMatch = thingProperties.HasFlag(ThingProperties.NOTDMATCH);
            def.Flags.NotDMatch = thingProperties.HasFlag(ThingProperties.NOTDMATCH);
            def.Flags.Touchy = thingProperties.HasFlag(ThingProperties.TOUCHY);
            def.Flags.MbfBouncer = thingProperties.HasFlag(ThingProperties.BOUNCES);
            def.Flags.Friendly = thingProperties.HasFlag(ThingProperties.FRIEND);

            // Apply correct alpha with shadow flag changes
            if (hadShadow && !def.Flags.Shadow)
                def.Properties.Alpha = 1;
            else if (!hadShadow && def.Flags.Shadow)
                def.Properties.Alpha = 0.4;

            // TODO can we support these?
            //if (thingProperties.HasFlag(ThingProperties.TRANSLATION1))
            //if (thingProperties.HasFlag(ThingProperties.TRANSLATION2))
            //if (thingProperties.HasFlag(ThingProperties.INFLOAT))
        }

        private static List<ThingProperties> GetThingProperties(ThingProperties properties)
        {
            List<ThingProperties> list = new();
            foreach (ThingProperties type in Enum.GetValues(typeof(ThingProperties)))
            {
                if (properties.HasFlag(type))
                    list.Add(type);
            }

            return list;
        }

        private static void ApplyCheats(DehackedDefinition dehacked)
        {
            if (dehacked.Cheat == null)
                return;

            var cheat = dehacked.Cheat;
            if (cheat.Chainsaw != null)
                CheatManager.Instance.SetCheatCode(CheatType.Chainsaw, cheat.Chainsaw);
            if (cheat.God != null)
                CheatManager.Instance.SetCheatCode(CheatType.God, cheat.God);
            if (cheat.AmmoAndKeys != null)
                CheatManager.Instance.SetCheatCode(CheatType.GiveAll, cheat.AmmoAndKeys);
            if (cheat.Ammo != null)
                CheatManager.Instance.SetCheatCode(CheatType.GiveAllNoKeys, cheat.Ammo);
            if (cheat.NoClip1 != null)
                CheatManager.Instance.SetCheatCode(CheatType.NoClip, cheat.NoClip1, 0);
            if (cheat.NoClip2 != null)
                CheatManager.Instance.SetCheatCode(CheatType.NoClip, cheat.NoClip2, 1);
            if (cheat.Behold != null)
                CheatManager.Instance.SetCheatCode(CheatType.Behold, cheat.Behold);
            if (cheat.Invincibility != null)
                CheatManager.Instance.SetCheatCode(CheatType.BeholdInvulnerability, cheat.Invincibility);
            if (cheat.Invisibility != null)
                CheatManager.Instance.SetCheatCode(CheatType.BeholdPartialInvisibility, cheat.Invisibility);
            if (cheat.RadSuit != null)
                CheatManager.Instance.SetCheatCode(CheatType.BeholdRadSuit, cheat.RadSuit);
            if (cheat.AutoMap != null)
                CheatManager.Instance.SetCheatCode(CheatType.BeholdComputerAreaMap, cheat.AutoMap);
            if (cheat.LiteAmp != null)
                CheatManager.Instance.SetCheatCode(CheatType.BeholdLightAmp, cheat.LiteAmp);
            if (cheat.LevelWarp != null)
                CheatManager.Instance.SetCheatCode(CheatType.ChangeLevel, cheat.LevelWarp);
            if (cheat.PlayerPos != null)
                CheatManager.Instance.SetCheatCode(CheatType.ShowPosition, cheat.PlayerPos);
        }

        private static void ApplyBexText(DehackedDefinition dehacked, LanguageDefinition language)
        {
            foreach (var text in dehacked.BexStrings)
            {
                if (!language.SetValue(text.Mnemonic, text.Value))
                    Log.Warn($"Unknown bex string mnemonic:{text.Mnemonic}");
            }
        }

        private static double GetDouble(int value) => value / 65536.0;

        private static string GetSound(DehackedDefinition dehacked, int sound)
        {
            if (sound < 0 || sound >= dehacked.SoundStrings.Length)
            {
                Warning($"Invalid sound {sound}");
                return string.Empty;
            }

            return dehacked.SoundStrings[sound];
        }

        private static void Warning(string warning)
        {
            Log.Warn($"Dehacked: {warning}");
        }
    }
}
