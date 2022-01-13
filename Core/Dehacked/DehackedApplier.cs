using Helion.Resources.Definitions;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Resources.Definitions.Language;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Util;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Entities.Definition.Properties;
using Helion.World.Entities.Definition.States;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static Helion.Dehacked.DehackedDefinition;

namespace Helion.Dehacked;

public class DehackedApplier
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly List<string> RemoveLabels = new();
    private readonly Dictionary<int, string> NewSoundLookup = new();
    private readonly Dictionary<int, string> NewSpriteLookup = new();
    private readonly Dictionary<int, EntityDefinition> NewThingLookup = new();
    private readonly Dictionary<int, EntityFrame> NewEntityFrameLookup = new();

    private const int DehExtraSpriteStart = 145;
    private const int DehExtraSoundStart = 500;

    public DehackedApplier(DefinitionEntries definitionEntries)
    {
        for (int i = 0; i < 100; i++)
            NewSpriteLookup[DehExtraSpriteStart + i] = $"SP{i.ToString().PadLeft(2, '0')}";

        for (int i = 0; i < 200; i++)
        {
            string name = $"deh/{i}";
            NewSoundLookup[DehExtraSoundStart + i] = name;
            definitionEntries.SoundInfo.Add(name, new SoundInfo(name, $"free{i.ToString().PadLeft(3, '0')}", 0));
        }
    }

    public void Apply(DehackedDefinition dehacked, DefinitionEntries definitionEntries, EntityDefinitionComposer composer)
    {
        ApplyVanillaIndex(dehacked, definitionEntries.EntityFrameTable);

        ApplySounds(dehacked, definitionEntries.SoundInfo);
        ApplyBexSounds(dehacked, definitionEntries.SoundInfo);
        ApplyBexSprites(dehacked);

        ApplyThings(dehacked, definitionEntries.EntityFrameTable, composer);
        ApplyPointers(dehacked, definitionEntries.EntityFrameTable);
        ApplyFrames(dehacked, definitionEntries.EntityFrameTable);
        ApplyWeapons(dehacked, definitionEntries.EntityFrameTable, composer);
        ApplyAmmo(dehacked, composer);
        ApplyText(dehacked, definitionEntries.EntityFrameTable, definitionEntries.Language);
        ApplyCheats(dehacked);
        ApplyMisc(dehacked, definitionEntries, composer);

        ApplyBexText(dehacked, definitionEntries.Language);
        ApplyBexPars(dehacked, definitionEntries.MapInfoDefinition);

        RemoveLabels.Clear();
        NewSoundLookup.Clear();
        NewSpriteLookup.Clear();
    }

    private void ApplySounds(DehackedDefinition dehacked, SoundInfoDefinition soundInfoDef)
    {
        foreach (DehackedSound dehSound in dehacked.Sounds)
        {
            string sound = GetSound(dehacked, dehSound.Number);
            if (string.IsNullOrEmpty(sound))
                continue;

            if (!soundInfoDef.GetSound(sound, out SoundInfo? soundInfo))
                continue;

            // Not doing anything with this yet...
        }
    }

    private static void ApplyVanillaIndex(DehackedDefinition dehacked, EntityFrameTable table)
    {
        for (int i = 0; i < (int)ThingState.Count; i++)
        {
            if (!GetVanillaFrameIndex(dehacked, table, i, out int frameIndex))
            {
                Warning($"Failed to find vanilla index for: {i}");
                continue;
            }

            table.Frames[frameIndex].VanillaIndex = i;
            table.VanillaFrameMap[i] = table.Frames[frameIndex];
        }
    }

    private void ApplyWeapons(DehackedDefinition dehacked, EntityFrameTable entityFrameTable, EntityDefinitionComposer composer)
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
            if (weapon.AmmoPerShot.HasValue)
                weaponDef.Properties.Weapons.AmmoUse = weapon.AmmoPerShot.Value;
            if (weapon.Mbf21Bits.HasValue)
                ApplyWeaponMbf21Bits(weaponDef, weapon.Mbf21Bits.Value);
        }
    }

    private static void ApplyWeaponMbf21Bits(EntityDefinition weaponDef, uint value)
    {
        Mbf21WeaponFlags flags = (Mbf21WeaponFlags)value;
        if (flags.HasFlag(Mbf21WeaponFlags.NOTHRUST))
        {
            weaponDef.Properties.Weapons.DefaultKickBack = false;
            weaponDef.Properties.Weapons.KickBack = 0;
        }

        weaponDef.Flags.WeaponNoAlert = flags.HasFlag(Mbf21WeaponFlags.SILENT);
        weaponDef.Flags.WeaponNoAutofire = flags.HasFlag(Mbf21WeaponFlags.NOAUTOFIRE);
        weaponDef.Flags.WeaponMeleeWeapon = flags.HasFlag(Mbf21WeaponFlags.FLEEMELEE);
        weaponDef.Flags.WeaponWimpyWeapon = flags.HasFlag(Mbf21WeaponFlags.AUTOSWITCHFROM);
        weaponDef.Flags.WeaponNoAutoSwitch = flags.HasFlag(Mbf21WeaponFlags.NOAUTOSWITCHTO);
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
            default:
                Warning($"Invalid ammo type {ammoType}");
                break;

        }
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

    private void ApplyPointers(DehackedDefinition dehacked, EntityFrameTable entityFrameTable)
    {
        foreach (var pointer in dehacked.Pointers)
        {
            if (!LookupFrameIndex(entityFrameTable, pointer.Frame, out int frameIndex))
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

    private void ApplyFrames(DehackedDefinition dehacked, EntityFrameTable entityFrameTable)
    {
        foreach (var frame in dehacked.Frames)
        {
            if (!LookupFrameIndex(entityFrameTable, frame.Frame, out int frameIndex))
            {
                Warning($"Invalid frame {frame.Frame}");
                continue;
            }

            var entityFrame = entityFrameTable.Frames[frameIndex];

            if (frame.SpriteNumber.HasValue && frame.SpriteNumber >= 0)
                SetSprite(entityFrame, dehacked, frame.SpriteNumber.Value);
            if (frame.Duration.HasValue)
                entityFrame.Ticks = frame.Duration.Value;

            if (frame.SpriteSubNumber.HasValue)
            {
                entityFrame.Frame = frame.SpriteSubNumber.Value & FrameMask;
                entityFrame.Properties.Bright = (frame.SpriteSubNumber.Value & FullBright) > 0;
            }

            if (frame.NextFrame.HasValue)
            {
                if (LookupFrameIndex(entityFrameTable, frame.NextFrame.Value, out int nextFrameIndex))
                {
                    entityFrame.NextFrameIndex = nextFrameIndex;
                    entityFrame.BranchType = ActorStateBranch.None;
                }
                else
                {
                    Warning($"Invalid next frame {frame.NextFrame.Value}");
                }
            }

            if (entityFrame.ActionFunction != null && DefaultFrameArgs.TryGetValue(entityFrame.ActionFunction, out DefaultArgs defaultArgs))
                ApplyDefaultArgs(frame, defaultArgs);

            entityFrame.DehackedMisc1 = frame.Unknown1;
            entityFrame.DehackedMisc2 = frame.Unknown2;
            entityFrame.DehackedArgs1 = frame.Args1 ?? 0;
            entityFrame.DehackedArgs2 = frame.Args2 ?? 0;
            entityFrame.DehackedArgs3 = frame.Args3 ?? 0;
            entityFrame.DehackedArgs4 = frame.Args4 ?? 0;
            entityFrame.DehackedArgs5 = frame.Args5 ?? 0;
            entityFrame.DehackedArgs6 = frame.Args6 ?? 0;
            entityFrame.DehackedArgs7 = frame.Args7 ?? 0;
            entityFrame.DehackedArgs8 = frame.Args8 ?? 0;

            if (frame.Mbf21Bits.HasValue)
                ApplyFrameMbf21Bits(entityFrame, frame.Mbf21Bits.Value);
        }
    }

    private static void ApplyDefaultArgs(DehackedFrame frame, in DefaultArgs defaultArgs)
    {
        if (!frame.Args1.HasValue)
            frame.Args1 = defaultArgs.Args1;
        if (!frame.Args2.HasValue)
            frame.Args2 = defaultArgs.Args2;
        if (!frame.Args3.HasValue)
            frame.Args3 = defaultArgs.Args3;
        if (!frame.Args4.HasValue)
            frame.Args4 = defaultArgs.Args4;
        if (!frame.Args5.HasValue)
            frame.Args5 = defaultArgs.Args5;
        if (!frame.Args6.HasValue)
            frame.Args6 = defaultArgs.Args6;
        if (!frame.Args7.HasValue)
            frame.Args7 = defaultArgs.Args7;
        if (!frame.Args8.HasValue)
            frame.Args8 = defaultArgs.Args8;
    }

    private static void ApplyFrameMbf21Bits(EntityFrame entityFrame, uint value)
    {
        Mbf21FrameFlags flags = (Mbf21FrameFlags)value;
        entityFrame.Properties.Fast = flags.HasFlag(Mbf21FrameFlags.SKILL5FAST);
    }

    private void SetSprite(EntityFrame entityFrame, DehackedDefinition dehacked, int spriteNumber)
    {
        if (spriteNumber < dehacked.Sprites.Length)
            entityFrame.SetSprite(dehacked.Sprites[spriteNumber]);
        else if (NewSpriteLookup.TryGetValue(spriteNumber, out string? sprite))
            entityFrame.SetSprite(sprite);
        else
            Warning($"Invalid sprite number {spriteNumber}");
    }

    private bool LookupFrameIndex(EntityFrameTable entityFrameTable, int frame, out int frameIndex)
    {
        if (entityFrameTable.VanillaFrameMap.TryGetValue(frame, out EntityFrame? entityFrame))
        {
            frameIndex = entityFrame.MasterFrameIndex;
            return true;
        }

        if (NewEntityFrameLookup.TryGetValue(frame, out entityFrame))
        {
            frameIndex = entityFrame.MasterFrameIndex;
            return true;
        }

        // Null frame that loops to itself
        frameIndex = entityFrameTable.Frames.Count;

        EntityFrame newFrame = new EntityFrame(entityFrameTable, Constants.InvisibleSprite, 0, -1,
            EntityFrameProperties.Default, null, Constants.NullFrameIndex, string.Empty);
        NewEntityFrameLookup[frame] = newFrame;
        newFrame.VanillaIndex = frame;
        newFrame.NextFrameIndex = frameIndex;

        entityFrameTable.AddFrame(newFrame);
        return true;
    }

    private static bool GetVanillaFrameIndex(DehackedDefinition dehacked, EntityFrameTable entityFrameTable, int frame, out int frameIndex)
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

    private void ApplyThings(DehackedDefinition dehacked, EntityFrameTable entityFrameTable, EntityDefinitionComposer composer)
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
            if (thing.MeleeRange.HasValue)
                properties.MeleeRange = thing.MeleeRange.Value;
            if (thing.FastSpeed.HasValue)
                properties.FastSpeed = thing.FastSpeed.Value;
            if (thing.Bits.HasValue)
            {
                ClearEntityFlags(ref definition.Flags);
                SetEntityFlags(definition.Properties, ref definition.Flags, thing.Bits.Value, false);
            }
            if (thing.Mbf21Bits.HasValue)
            {
                ClearEntityFlagsMbf21(ref definition.Flags);
                SetEntityFlagsMbf21(definition.Properties, ref definition.Flags, thing.Mbf21Bits.Value, false);
            }

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
            if (thing.RipSound.HasValue)
                properties.RipSound = GetSound(dehacked, thing.RipSound.Value);

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

            if (IsGroupValid(properties.InfightingGroup))
                properties.InfightingGroup = thing.InfightingGroup;
            if (IsGroupValid(properties.ProjectileGroup))
                properties.ProjectileGroup = thing.ProjectileGroup;
            if (IsGroupValid(properties.SplashGroup))
                properties.SplashGroup = thing.SplashGroup;
        }
    }

    // DSDA Doom doesn't count zero
    private static bool IsGroupValid(int? value) =>
         value == null || value.Equals(Constants.DefaultGroupNumber);

    private static int GetThingSpeed(int speed, EntityDefinition definition)
    {
        if (definition.Flags.Missile)
            return (int)GetDouble(speed);
        return speed;
    }

    private void ApplyThingFrame(DehackedDefinition dehacked, EntityFrameTable entityFrameTable,
        EntityDefinition definition, int frame, string actionLabel)
    {
        int frameIndex;
        bool isNull = false;
        if (frame >= (int)ThingState.Count && LookupFrameIndex(entityFrameTable, frame, out int newFrameIndex))
        {
            frameIndex = newFrameIndex;
        }
        else
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

            frameIndex = frameSet.StartFrameIndex + frameLookup.Offset;
            isNull = frameLookup.Label.Equals("Actor::null", StringComparison.OrdinalIgnoreCase);
        }

        RemoveActionLabels(definition, actionLabel);

        if (!isNull)
        {
            definition.States.Labels[actionLabel] = frameIndex;
            definition.States.Labels[$"{definition.Name}::{actionLabel}"] = frameIndex;
        }
    }

    private void RemoveActionLabels(EntityDefinition definition, string actionLabel)
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

    private EntityDefinition? GetEntityDefinition(DehackedDefinition dehacked, int thingNumber, EntityDefinitionComposer composer)
    {
        int index = thingNumber - 1;
        if (index < 0)
            return null;

        string actorName;

        if (index < dehacked.ActorNames.Length)
            actorName = dehacked.ActorNames[index];
        else
            actorName = GetNewActorName(index, composer);

        return composer.GetByName(actorName);
    }

    private string GetNewActorName(int index, EntityDefinitionComposer composer)
    {
        if (NewThingLookup.TryGetValue(index, out EntityDefinition? def))
            return def.Name;

        string newName = Guid.NewGuid().ToString();
        EntityDefinition definition = new EntityDefinition(0, newName, 0, new List<string>());
        composer.Add(definition);
        return newName;
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

    private static void ClearEntityFlagsMbf21(ref EntityFlags flags)
    {
        flags.NoTarget = false;
        flags.NoRadiusDmg = false;
        flags.ForceRadiusDmg = false;
        flags.MissileMore = false;
        flags.QuickToRetaliate = false;
        flags.Boss = false;
        flags.Map07Boss1 = false;
        flags.Map07Boss2 = false;
        flags.E1M8Boss = false;
        flags.E2M8Boss = false;
        flags.E3M8Boss = false;
        flags.E4M6Boss = false;
        flags.E4M8Boss = false;
        flags.Ripper = false;
        flags.FullVolSee = false;
        flags.FullVolDeath = false;
    }

    public static void SetEntityFlagsMbf21(EntityProperties properties, ref EntityFlags flags, uint value, bool opAnd)
    {
        Mbf21ThingFlags thingProperties = (Mbf21ThingFlags)value;
        properties.Gravity = thingProperties.HasFlag(Mbf21ThingFlags.LOGRAV) ? 1 / 8.0 : 1.0; // Lower gravity (1/8)
        properties.MaxTargetRange = thingProperties.HasFlag(Mbf21ThingFlags.SHORTMRANGE) ? 896 : 0; // Short missile range (archvile)
        properties.MinMissileChance = thingProperties.HasFlag(Mbf21ThingFlags.HIGHERMPROB) ? 160 : 200; // Higher missile attack probability (cyberdemon)
        properties.MeleeThreshold = thingProperties.HasFlag(Mbf21ThingFlags.LONGMELEE) ? 196 : 0; // Has long melee range (revenant)

        flags.NoTarget = GetNewFlagValue(flags.NoTarget, thingProperties.HasFlag(Mbf21ThingFlags.DMGIGNORED), opAnd);
        flags.NoRadiusDmg = GetNewFlagValue(flags.NoRadiusDmg, thingProperties.HasFlag(Mbf21ThingFlags.NORADIUSDMG), opAnd);
        flags.ForceRadiusDmg = GetNewFlagValue(flags.ForceRadiusDmg, thingProperties.HasFlag(Mbf21ThingFlags.FORCERADIUSDMG), opAnd);
        flags.MissileMore = GetNewFlagValue(flags.MissileMore, thingProperties.HasFlag(Mbf21ThingFlags.RANGEHALF), opAnd);
        flags.QuickToRetaliate = GetNewFlagValue(flags.QuickToRetaliate, thingProperties.HasFlag(Mbf21ThingFlags.NOTHRESHOLD), opAnd);
        flags.Boss = GetNewFlagValue(flags.Boss, thingProperties.HasFlag(Mbf21ThingFlags.BOSS), opAnd);
        flags.Map07Boss1 = GetNewFlagValue(flags.Map07Boss1, thingProperties.HasFlag(Mbf21ThingFlags.MAP07BOSS1), opAnd);
        flags.Map07Boss2 = GetNewFlagValue(flags.Map07Boss2, thingProperties.HasFlag(Mbf21ThingFlags.MAP07BOSS2), opAnd);
        flags.E1M8Boss = GetNewFlagValue(flags.E1M8Boss, thingProperties.HasFlag(Mbf21ThingFlags.E1M8BOSS), opAnd);
        flags.E2M8Boss = GetNewFlagValue(flags.E2M8Boss, thingProperties.HasFlag(Mbf21ThingFlags.E2M8BOSS), opAnd);
        flags.E3M8Boss = GetNewFlagValue(flags.E2M8Boss, thingProperties.HasFlag(Mbf21ThingFlags.E2M8BOSS), opAnd);
        flags.E4M6Boss = GetNewFlagValue(flags.E4M6Boss, thingProperties.HasFlag(Mbf21ThingFlags.E4M6BOSS), opAnd);
        flags.E4M8Boss = GetNewFlagValue(flags.E4M8Boss, thingProperties.HasFlag(Mbf21ThingFlags.E4M8BOSS), opAnd);
        flags.Ripper = GetNewFlagValue(flags.Ripper, thingProperties.HasFlag(Mbf21ThingFlags.RIP), opAnd);
        flags.FullVolSee = GetNewFlagValue(flags.FullVolSee, thingProperties.HasFlag(Mbf21ThingFlags.FULLVOLSOUNDS), opAnd);
        flags.FullVolDeath = GetNewFlagValue(flags.FullVolDeath, thingProperties.HasFlag(Mbf21ThingFlags.FULLVOLSOUNDS), opAnd);
    }

    private static bool GetNewFlagValue(bool existingFlag, bool newFlag, bool opAnd)
    {
        if (opAnd)
            return newFlag && existingFlag;

        return newFlag || existingFlag;
    }

    private static void ClearEntityFlags(ref EntityFlags flags)
    {
        flags.Special = false;
        flags.Solid = false;
        flags.Shootable = false;
        flags.NoSector = false;
        flags.NoBlockmap = false;
        flags.Ambush = false;
        flags.JustHit = false;
        flags.JustAttacked = false;
        flags.SpawnCeiling = false;
        flags.NoGravity = false;
        flags.Dropoff = false;
        flags.Pickup = false;
        flags.NoClip = false;
        flags.SlidesOnWalls = false;
        flags.Float = false;
        flags.Teleport = false;
        flags.Missile = false;
        flags.Dropped = false;
        flags.Shadow = false;
        flags.NoBlood = false;
        flags.Corpse = false;
        flags.CountKill = false;
        flags.CountItem = false;
        flags.Skullfly = false;
        flags.NotDMatch = false;
        flags.Touchy = false;
        flags.MbfBouncer = false;
        flags.Friendly = false;
    }

    public static void SetEntityFlags(EntityProperties properties, ref EntityFlags flags, uint value, bool opAnd)
    {
        bool hadShadow = flags.Shadow;

        ThingProperties thingProperties = (ThingProperties)value;
        flags.Special = GetNewFlagValue(flags.Special, thingProperties.HasFlag(ThingProperties.SPECIAL), opAnd);
        flags.Solid = GetNewFlagValue(flags.Solid, thingProperties.HasFlag(ThingProperties.SOLID), opAnd);
        flags.Shootable = GetNewFlagValue(flags.Shootable, thingProperties.HasFlag(ThingProperties.SHOOTABLE), opAnd);
        flags.NoSector = GetNewFlagValue(flags.NoSector, thingProperties.HasFlag(ThingProperties.NOSECTOR), opAnd);
        flags.NoBlockmap = GetNewFlagValue(flags.NoBlockmap, thingProperties.HasFlag(ThingProperties.NOBLOCKMAP), opAnd);
        flags.Ambush = GetNewFlagValue(flags.Ambush, thingProperties.HasFlag(ThingProperties.AMBUSH), opAnd);
        flags.JustHit = GetNewFlagValue(flags.JustHit, thingProperties.HasFlag(ThingProperties.JUSTHIT), opAnd);
        flags.JustAttacked = GetNewFlagValue(flags.JustAttacked, thingProperties.HasFlag(ThingProperties.JUSTATTACKED), opAnd);
        flags.SpawnCeiling = GetNewFlagValue(flags.SpawnCeiling, thingProperties.HasFlag(ThingProperties.SPAWNCEILING), opAnd);
        flags.NoGravity = GetNewFlagValue(flags.NoGravity, thingProperties.HasFlag(ThingProperties.NOGRAVITY), opAnd);
        flags.Dropoff = GetNewFlagValue(flags.Dropoff, thingProperties.HasFlag(ThingProperties.DROPOFF), opAnd);
        flags.Pickup = GetNewFlagValue(flags.Pickup, thingProperties.HasFlag(ThingProperties.PICKUP), opAnd);
        flags.NoClip = GetNewFlagValue(flags.NoClip, thingProperties.HasFlag(ThingProperties.NOCLIP), opAnd);
        flags.SlidesOnWalls = GetNewFlagValue(flags.SlidesOnWalls, thingProperties.HasFlag(ThingProperties.SLIDE), opAnd);
        flags.Float = GetNewFlagValue(flags.Float, thingProperties.HasFlag(ThingProperties.FLOAT), opAnd);
        flags.Teleport = GetNewFlagValue(flags.Teleport, thingProperties.HasFlag(ThingProperties.TELEPORT), opAnd);
        flags.Missile = GetNewFlagValue(flags.Missile, thingProperties.HasFlag(ThingProperties.MISSILE), opAnd);
        flags.Dropped = GetNewFlagValue(flags.Dropped, thingProperties.HasFlag(ThingProperties.DROPPED), opAnd);
        flags.Shadow = GetNewFlagValue(flags.Shadow, thingProperties.HasFlag(ThingProperties.SHADOW), opAnd);
        flags.NoBlood = GetNewFlagValue(flags.NoBlood, thingProperties.HasFlag(ThingProperties.NOBLOOD), opAnd);
        flags.Corpse = GetNewFlagValue(flags.Corpse, thingProperties.HasFlag(ThingProperties.CORPSE), opAnd);
        flags.CountKill = GetNewFlagValue(flags.CountKill, thingProperties.HasFlag(ThingProperties.COUNTKILL), opAnd);
        flags.CountItem = GetNewFlagValue(flags.CountItem, thingProperties.HasFlag(ThingProperties.COUNTITEM), opAnd);
        flags.Skullfly = GetNewFlagValue(flags.Skullfly, thingProperties.HasFlag(ThingProperties.SKULLFLY), opAnd);
        flags.NotDMatch = GetNewFlagValue(flags.NotDMatch, thingProperties.HasFlag(ThingProperties.NOTDMATCH), opAnd);
        flags.Touchy = GetNewFlagValue(flags.Touchy, thingProperties.HasFlag(ThingProperties.TOUCHY), opAnd);
        flags.MbfBouncer = GetNewFlagValue(flags.MbfBouncer, thingProperties.HasFlag(ThingProperties.BOUNCES), opAnd);
        flags.Friendly = GetNewFlagValue(flags.Friendly, thingProperties.HasFlag(ThingProperties.FRIEND), opAnd);

        // Apply correct alpha with shadow flag changes
        if (hadShadow && !flags.Shadow)
            properties.Alpha = 1;
        else if (!hadShadow && flags.Shadow)
            properties.Alpha = 0.4;

        // TODO can we support these?
        //ThingProperties.TRANSLATION1
        //ThingProperties.TRANSLATION2
        //ThingProperties.INFLOAT
        //ThingProperties.TRANSLUCENT
    }

    public static bool CheckEntityFlags(in EntityFlags entityFlags, uint flags)
    {
        // This could have been a lookup but it would have to to map to a property, invoking would likely be slow and this happens at runtime.
        ThingProperties thingProperties = (ThingProperties)flags;
        if (thingProperties.HasFlag(ThingProperties.SPECIAL) && !entityFlags.Special)
            return false;
        if (thingProperties.HasFlag(ThingProperties.SOLID) && !entityFlags.Solid)
            return false;
        if (thingProperties.HasFlag(ThingProperties.SHOOTABLE) && !entityFlags.Shootable)
            return false;
        if (thingProperties.HasFlag(ThingProperties.NOSECTOR) && !entityFlags.NoSector)
            return false;
        if (thingProperties.HasFlag(ThingProperties.NOBLOCKMAP) && !entityFlags.NoBlockmap)
            return false;
        if (thingProperties.HasFlag(ThingProperties.AMBUSH) && !entityFlags.Ambush)
            return false;
        if (thingProperties.HasFlag(ThingProperties.JUSTHIT) && !entityFlags.JustHit)
            return false;
        if (thingProperties.HasFlag(ThingProperties.JUSTATTACKED) && !entityFlags.JustAttacked)
            return false;
        if (thingProperties.HasFlag(ThingProperties.SPAWNCEILING) && !entityFlags.SpawnCeiling)
            return false;
        if (thingProperties.HasFlag(ThingProperties.NOGRAVITY) && !entityFlags.NoGravity)
            return false;
        if (thingProperties.HasFlag(ThingProperties.DROPOFF) && !entityFlags.Dropoff)
            return false;
        if (thingProperties.HasFlag(ThingProperties.PICKUP) && !entityFlags.Pickup)
            return false;
        if (thingProperties.HasFlag(ThingProperties.NOCLIP) && !entityFlags.NoClip)
            return false;
        if (thingProperties.HasFlag(ThingProperties.SLIDE) && !entityFlags.SlidesOnWalls)
            return false;
        if (thingProperties.HasFlag(ThingProperties.FLOAT) && !entityFlags.Float)
            return false;
        if (thingProperties.HasFlag(ThingProperties.TELEPORT) && !entityFlags.Teleport)
            return false;
        if (thingProperties.HasFlag(ThingProperties.MISSILE) && !entityFlags.Missile)
            return false;
        if (thingProperties.HasFlag(ThingProperties.DROPPED) && !entityFlags.Dropped)
            return false;
        if (thingProperties.HasFlag(ThingProperties.SHADOW) && !entityFlags.Shadow)
            return false;
        if (thingProperties.HasFlag(ThingProperties.NOBLOOD) && !entityFlags.NoBlood)
            return false;
        if (thingProperties.HasFlag(ThingProperties.CORPSE) && !entityFlags.Corpse)
            return false;
        if (thingProperties.HasFlag(ThingProperties.COUNTKILL) && !entityFlags.CountKill)
            return false;
        if (thingProperties.HasFlag(ThingProperties.COUNTITEM) && !entityFlags.CountItem)
            return false;
        if (thingProperties.HasFlag(ThingProperties.SKULLFLY) && !entityFlags.Skullfly)
            return false;
        if (thingProperties.HasFlag(ThingProperties.NOTDMATCH) && !entityFlags.NotDMatch)
            return false;
        if (thingProperties.HasFlag(ThingProperties.TOUCHY) && !entityFlags.Touchy)
            return false;
        if (thingProperties.HasFlag(ThingProperties.BOUNCES) && !entityFlags.MbfBouncer)
            return false;
        if (thingProperties.HasFlag(ThingProperties.FRIEND) && !entityFlags.Friendly)
            return false;

        return true;
    }

    public static bool CheckEntityFlagsMbf21(Entity entity, uint flags)
    {
        Mbf21ThingFlags thingProperties = (Mbf21ThingFlags)flags;
        if (thingProperties.HasFlag(Mbf21ThingFlags.LOGRAV) && entity.Properties.Gravity != 1 / 8.0)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.SHORTMRANGE) && entity.Properties.MaxTargetRange != 896)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.HIGHERMPROB) && entity.Properties.MaxTargetRange != 160)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.LONGMELEE) && entity.Properties.MaxTargetRange != 196)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.DMGIGNORED) && !entity.Flags.NoTarget)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.NORADIUSDMG) && !entity.Flags.NoRadiusDmg)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.FORCERADIUSDMG) && !entity.Flags.ForceRadiusDmg)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.RANGEHALF) && !entity.Flags.MissileMore)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.NOTHRESHOLD) && !entity.Flags.QuickToRetaliate)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.BOSS) && !entity.Flags.Boss)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.MAP07BOSS1) && !entity.Flags.Map07Boss1)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.MAP07BOSS2) && !entity.Flags.Map07Boss2)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.E1M8BOSS) && !entity.Flags.E1M8Boss)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.E2M8BOSS) && !entity.Flags.E2M8Boss)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.E3M8BOSS) && !entity.Flags.E3M8Boss)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.E4M6BOSS) && !entity.Flags.E4M6Boss)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.E4M8BOSS) && !entity.Flags.E4M8Boss)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.RIP) && !entity.Flags.Ripper)
            return false;
        if (thingProperties.HasFlag(Mbf21ThingFlags.FULLVOLSOUNDS) && !entity.Flags.FullVolSee && !entity.Flags.FullVolDeath)
            return false;

        return true;
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
            CheatManager.SetCheatCode(CheatType.Chainsaw, cheat.Chainsaw);
        if (cheat.God != null)
            CheatManager.SetCheatCode(CheatType.God, cheat.God);
        if (cheat.AmmoAndKeys != null)
            CheatManager.SetCheatCode(CheatType.GiveAll, cheat.AmmoAndKeys);
        if (cheat.Ammo != null)
            CheatManager.SetCheatCode(CheatType.GiveAllNoKeys, cheat.Ammo);
        if (cheat.NoClip1 != null)
            CheatManager.SetCheatCode(CheatType.NoClip, cheat.NoClip1, 0);
        if (cheat.NoClip2 != null)
            CheatManager.SetCheatCode(CheatType.NoClip, cheat.NoClip2, 1);
        if (cheat.Behold != null)
            CheatManager.SetCheatCode(CheatType.Behold, cheat.Behold);
        if (cheat.Invincibility != null)
            CheatManager.SetCheatCode(CheatType.BeholdInvulnerability, cheat.Invincibility);
        if (cheat.Invisibility != null)
            CheatManager.SetCheatCode(CheatType.BeholdPartialInvisibility, cheat.Invisibility);
        if (cheat.RadSuit != null)
            CheatManager.SetCheatCode(CheatType.BeholdRadSuit, cheat.RadSuit);
        if (cheat.AutoMap != null)
            CheatManager.SetCheatCode(CheatType.BeholdComputerAreaMap, cheat.AutoMap);
        if (cheat.LiteAmp != null)
            CheatManager.SetCheatCode(CheatType.BeholdLightAmp, cheat.LiteAmp);
        if (cheat.LevelWarp != null)
            CheatManager.SetCheatCode(CheatType.ChangeLevel, cheat.LevelWarp);
        if (cheat.PlayerPos != null)
            CheatManager.SetCheatCode(CheatType.ShowPosition, cheat.PlayerPos);
    }

    private static void ApplyMisc(DehackedDefinition dehacked, DefinitionEntries definitionEntries, EntityDefinitionComposer composer)
    {
        if (dehacked.Misc == null)
            return;

        var player = composer.GetByName("DoomPlayer");
        if (player != null)
        {
            if (dehacked.Misc.InitialHealth.HasValue)
                player.Properties.Health = dehacked.Misc.InitialHealth.Value;

            if (dehacked.Misc.MaxHealth.HasValue)
                player.Properties.Player.MaxHealth = dehacked.Misc.MaxHealth.Value;

            if (dehacked.Misc.InitialBullets.HasValue)
            {
                var startItem = player.Properties.Player.StartItem.FirstOrDefault(x => x.Name.Equals("Clip", StringComparison.OrdinalIgnoreCase));
                if (startItem != null)
                    startItem.Amount = dehacked.Misc.InitialBullets.Value;
            }
        }

        // Only appears to work for health bonus, powerups are will still max at 200
        if (dehacked.Misc.MaxHealth.HasValue)
            SetMaxAmount(composer, HealthBonusClass, dehacked.Misc.MaxHealth.Value);

        // Only appears to work for armor bonus, blue armor will still max at 200
        if (dehacked.Misc.MaxArmor.HasValue)
        {
            var armorBonus = composer.GetByName(ArmorBonusClass);
            if (armorBonus != null)
                armorBonus.Properties.Armor.MaxSaveAmount = dehacked.Misc.MaxArmor.Value;
        }

        if (dehacked.Misc.GreenArmorClass.HasValue && dehacked.Misc.GreenArmorClass.Value == BlueArmorClassNum)
            SetArmorClass(composer, GreenArmorClassName, dehacked.Misc.GreenArmorClass.Value);
        if (dehacked.Misc.BlueArmorClass.HasValue && dehacked.Misc.BlueArmorClass.Value == GreenArmorClassNum)
            SetArmorClass(composer, BlueArmorClassName, dehacked.Misc.BlueArmorClass.Value);

        if (dehacked.Misc.SoulsphereHealth.HasValue)
            SetAmount(composer, SoulsphereClass, dehacked.Misc.SoulsphereHealth.Value);
        if (dehacked.Misc.MaxSoulsphere.HasValue)
            SetMaxAmount(composer, SoulsphereClass, dehacked.Misc.MaxSoulsphere.Value);

        if (dehacked.Misc.MegasphereHealth.HasValue)
            SetAmount(composer, MegasphereHealthClass, dehacked.Misc.MegasphereHealth.Value);

        if (dehacked.Misc.BfgCellsPerShot.HasValue)
        {
            var bfg = composer.GetByName(BFG900Class);
            if (bfg != null)
                bfg.Properties.Weapons.AmmoUse = dehacked.Misc.BfgCellsPerShot.Value;
        }

        if (dehacked.Misc.MonstersInfight.HasValue)
        {
            // Enabling this option allows monsters of the same species to injur each other.
            bool set = dehacked.Misc.MonstersInfight.Value == MonsterInfightType.Enable;
            foreach (var mapInfo in definitionEntries.MapInfoDefinition.MapInfo.Maps)
                mapInfo.SetOption(MapOptions.TotalInfighting, set);
        }
    }

    private static void SetArmorClass(EntityDefinitionComposer composer, string armor, int classNumber)
    {
        var def = composer.GetByName(armor);
        if (def == null)
            return;

        def.Properties.Armor.SaveAmount = classNumber == GreenArmorClassNum ? 100 : 200;
        def.Properties.Armor.SavePercent = classNumber == GreenArmorClassNum ? 33.335 : 50;
    }

    private static void SetAmount(EntityDefinitionComposer composer, string name, int amount)
    {
        var set = composer.GetByName(name);
        if (set != null)
            set.Properties.Inventory.Amount = amount;
    }

    private static void SetMaxAmount(EntityDefinitionComposer composer, string name, int amount)
    {
        var set = composer.GetByName(name);
        if (set != null)
            set.Properties.Inventory.MaxAmount = amount;
    }

    private static void ApplyBexText(DehackedDefinition dehacked, LanguageDefinition language)
    {
        foreach (var text in dehacked.BexStrings)
        {
            if (!language.SetValue(text.Mnemonic, text.Value))
                Log.Warn($"Unknown bex string mnemonic:{text.Mnemonic}");
        }
    }

    private static void ApplyBexPars(DehackedDefinition dehacked, MapInfoDefinition mapInfoDefinition)
    {
        foreach (var par in dehacked.BexPars)
        {
            string mapName;
            if (par.Episode.HasValue)
                mapName = $"e{par.Episode.Value}m{par.Map}";
            else
                mapName = $"map{par.Map.ToString().PadLeft(2, '0')}";

            MapInfoDef? mapInfo = mapInfoDefinition.MapInfo.GetMap(mapName);
            if (mapInfo == null)
                Log.Warn($"Failed to find map{mapName} for par.");
            else
                mapInfo.ParTime = par.Par;
        }
    }

    private void ApplyBexSounds(DehackedDefinition dehacked, SoundInfoDefinition soundInfoDef)
    {
        foreach (var sound in dehacked.BexSounds)
        {
            if (sound.Index == null)
                continue;

            string id = Guid.NewGuid().ToString();
            soundInfoDef.Add(id, new SoundInfo(id, sound.EntryName, 0));
            NewSoundLookup[sound.Index.Value] = id;
        }
    }

    private void ApplyBexSprites(DehackedDefinition dehacked)
    {
        foreach (var sprite in dehacked.BexSprites)
        {
            if (sprite.Index == null)
                continue;

            NewSpriteLookup[sprite.Index.Value] = sprite.EntryName;
        }
    }

    private static double GetDouble(int value) => value / 65536.0;

    private string GetSound(DehackedDefinition dehacked, int sound)
    {
        if (sound < 0)
        {
            Warning($"Invalid sound {sound}");
            return string.Empty;
        }

        if (sound < dehacked.SoundStrings.Length)
            return dehacked.SoundStrings[sound];

        if (!NewSoundLookup.TryGetValue(sound, out string? value))
        {
            Warning($"Invalid sound {sound}");
            return string.Empty;
        }

        return value;
    }

    private static void Warning(string warning)
    {
        Log.Warn($"Dehacked: {warning}");
    }
}
