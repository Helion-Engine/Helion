using Helion.Resources.Definitions;
using Helion.Resources.Definitions.Language;
using Helion.World.Cheats;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using Helion.World.Entities.Definition.States;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using static Helion.Dehacked.DehackedDefinition;

namespace Helion.Dehacked
{
    public static class DehackedApplier
    {
        private static readonly List<string> RemoveLabels = new();

        public static void Apply(DehackedDefinition dehacked, DefinitionEntries definitionEntries, EntityDefinitionComposer composer)
        {
            ApplyThings(dehacked, composer);
            ApplyPointers(dehacked);
            ApplyFrames(dehacked);
            ApplyAmmo(dehacked, composer);
            ApplyText(dehacked, definitionEntries.Language);
            ApplyCheats(dehacked);
        }

        private static void ApplyPointers(DehackedDefinition dehacked)
        {
            foreach (var pointer in dehacked.Pointers)
            {
                if (!GetFrameIndex(pointer.Frame, out int frameIndex))
                    continue;

                var entityFrame = EntityFrameTable.Frames[frameIndex];
                if (ActionFunctionLookup.TryGetValue((ThingState)pointer.CodePointerFrame, out string? function))
                    entityFrame.ActionFunction = EntityActionFunctions.Find(function);
                else
                    entityFrame.ActionFunction = null;
            }
        }

        private static void ApplyFrames(DehackedDefinition dehacked)
        {
            foreach (var frame in dehacked.Frames)
            {
                if (!GetFrameIndex(frame.Frame, out int frameIndex))
                    continue;

                var entityFrame = EntityFrameTable.Frames[frameIndex];

                if (frame.SpriteNumber.HasValue && frame.SpriteNumber >= 0 && frame.SpriteNumber < Sprites.Length)
                    entityFrame.SetSprite(Sprites[frame.SpriteNumber.Value]);
                if (frame.Duration.HasValue)
                    entityFrame.Ticks = frame.Duration.Value;
                if (frame.SpriteSubNumber.HasValue)
                {
                    entityFrame.Frame = frame.SpriteSubNumber.Value & FrameMask;
                    entityFrame.Properties.Bright = (frame.SpriteSubNumber.Value & FullBright) > 0;
                }

                if (frame.NextFrame.HasValue && GetFrameIndex(frame.NextFrame.Value, out int nextFrameIndex))
                    entityFrame.NextFrameIndex = nextFrameIndex;
            }
        }

        private static bool GetFrameIndex(int frame, out int frameIndex)
        {
            frameIndex = -1;
            if (frame < 0 || frame > ThingStateLookups.Length)
                return false;

            var lookup = ThingStateLookups[frame];
            int baseFrame = -1;

            for (int i = 0; i < EntityFrameTable.Frames.Count; i++)
            {
                var frameItem = EntityFrameTable.Frames[i];
                if (lookup.Frame != null && lookup.Frame != frameItem.Frame)
                    continue;

                if (lookup.ActorName != null && !lookup.ActorName.Equals(frameItem.VanillaActorName))
                    continue;

                if (frameItem.Sprite.Equals(lookup.Sprite, StringComparison.OrdinalIgnoreCase))
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

        private static void ApplyThings(DehackedDefinition dehacked, EntityDefinitionComposer composer)
        {
            foreach (var thing in dehacked.Things)
            {
                var definition = GetEntityDefinition(thing.Number, composer);
                if (definition == null)
                    continue;

                var properties = definition.Properties;
                if (thing.Hitpoints.HasValue)
                    properties.Health = thing.Hitpoints.Value;
                if (thing.ReactionTime.HasValue)
                    properties.ReactionTime = thing.ReactionTime.Value;
                if (thing.PainChance.HasValue)
                    properties.PainChance = thing.PainChance.Value;
                if (thing.Speed.HasValue)
                    properties.Speed = thing.Speed.Value;
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
                    properties.SeeSound = GetSound(thing.AlertSound.Value);
                if (thing.AttackSound.HasValue)
                    properties.AttackSound = GetSound(thing.AttackSound.Value);
                if (thing.PainSound.HasValue)
                    properties.PainSound = GetSound(thing.PainSound.Value);
                if (thing.DeathSound.HasValue)
                    properties.DeathSound = GetSound(thing.DeathSound.Value);

                if (thing.CloseAttackFrame.HasValue)
                    ApplyThingFrame(definition, thing.CloseAttackFrame.Value, "melee");
                if (thing.FarAttackFrame.HasValue)
                    ApplyThingFrame(definition, thing.FarAttackFrame.Value, "missile");
                if (thing.DeathFrame.HasValue)
                    ApplyThingFrame(definition, thing.DeathFrame.Value, "death");
                if (thing.ExplodingFrame.HasValue)
                    ApplyThingFrame(definition, thing.ExplodingFrame.Value, "xdeath");
                if (thing.InitFrame.HasValue)
                    ApplyThingFrame(definition, thing.InitFrame.Value, "spawn");
                if (thing.InjuryFrame.HasValue)
                    ApplyThingFrame(definition, thing.InjuryFrame.Value, "pain");
                if (thing.FirstMovingFrame.HasValue)
                    ApplyThingFrame(definition, thing.FirstMovingFrame.Value, "see");
                if (thing.RespawnFrame.HasValue)
                    ApplyThingFrame(definition, thing.RespawnFrame.Value, "raise");
            }
        }

        private static void ApplyThingFrame(EntityDefinition definition, int frame, string actionLabel)
        {
            if (!FrameLookup.TryGetValue((ThingState)frame, out FrameStateLookup? frameLookup))
                return;

            if (!EntityFrameTable.FrameSets.TryGetValue(frameLookup.Label, out FrameSet? frameSet))
                return;

            RemoveActionLabels(definition, actionLabel);

            if (!frameLookup.Label.Equals("Actor::null", StringComparison.OrdinalIgnoreCase))
            {
                definition.States.Labels[actionLabel] = frameSet.StartFrameIndex + frameLookup.Offset;
                definition.States.Labels[frameLookup.Label] = frameSet.StartFrameIndex + frameLookup.Offset;
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

        private static EntityDefinition? GetEntityDefinition(int thingNumber, EntityDefinitionComposer composer)
        {
            int index = thingNumber - 1;
            if (index < 0 || index >= ActorNames.Length)
                return null;

            string actorName = ActorNames[index];
            return composer.GetByName(actorName);           
        }

        private static void ApplyAmmo(DehackedDefinition dehacked, EntityDefinitionComposer composer)
        {
            foreach (var ammo in dehacked.Ammo)
            {
                if (ammo.AmmoNumber < 0 || ammo.AmmoNumber >= AmmoNames.Length)
                    continue;

                var definition = composer.GetByName(AmmoNames[ammo.AmmoNumber]);
                ApplyAmmo(definition, ammo, 1);

                if (ammo.AmmoNumber >= AmmoDoubleNames.Length)
                    continue;

                definition = composer.GetByName(AmmoDoubleNames[ammo.AmmoNumber]);
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

        private static void ApplyText(DehackedDefinition dehacked, LanguageDefinition language)
        {
            string levelRegex = @"level \d+: ";
            foreach (var text in dehacked.Strings)
            {
                if (SpriteNames.Contains(text.OldString))
                {
                    UpdateSpriteText(text);
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
            }
        }

        private static void UpdateSpriteText(DehackedString text)
        {
            foreach (var frame in EntityFrameTable.Frames)
            {
                if (!frame.Sprite.Equals(text.OldString))
                    continue;

                frame.SetSprite(text.NewString);
            }
        }

        private static void SetActorFlags(EntityDefinition def, uint value)
        {
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

            // TODO can we support these?
            //if (thingProperties.HasFlag(ThingProperties.TRANSLATION1))
            //if (thingProperties.HasFlag(ThingProperties.TRANSLATION2))
            //if (thingProperties.HasFlag(ThingProperties.INFLOAT))
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

        private static double GetDouble(int value) => value / 65536.0;

        private static string GetSound(int sound)
        {
            if (sound < 0 || sound >= SoundStrings.Length)
            {
                // Log.Error
                return string.Empty;
            }

            return SoundStrings[sound];
        }
    }
}
