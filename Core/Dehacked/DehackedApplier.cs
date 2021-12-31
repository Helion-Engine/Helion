using Helion.Resources.Definitions;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Resources.Definitions.Language;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.Definitions.SoundInfo;
using Helion.Util;
using Helion.World.Cheats;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
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
        ApplyFrames(dehacked, definitionEntries.EntityFrameTable);
        ApplyPointers(dehacked, definitionEntries.EntityFrameTable);
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

            if (frame.Unknown1.HasValue)
                entityFrame.DehackedMisc1 = frame.Unknown1.Value;
            if (frame.Unknown2.HasValue)
                entityFrame.DehackedMisc2 = frame.Unknown2.Value;
        }
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
            if (thing.Bits.HasValue)
                SetActorFlags(definition, thing.Bits.Value);
            if (thing.Mbf21Bits.HasValue)
                SetActorFlagsMbf21(definition, thing.Mbf21Bits.Value);

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

    private static int GetThingSpeed(double speed, EntityDefinition definition)
    {
        if (definition.Flags.Missile)
            return (int)GetDouble(speed);
        return (int)speed;
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

    private static void SetActorFlagsMbf21(EntityDefinition def, uint value)
    {
        Mbf21ThingFlags thingProperties = (Mbf21ThingFlags)value;
        if (thingProperties.HasFlag(Mbf21ThingFlags.LOGRAV))
            def.Properties.Gravity = 1 / 8.0;
        if (thingProperties.HasFlag(Mbf21ThingFlags.SHORTMRANGE))
            def.Properties.MaxTargetRange = 896; // Short missile range (archvile)
        if (thingProperties.HasFlag(Mbf21ThingFlags.HIGHERMPROB))
            def.Properties.MinMissileChance = 160; // Higher missile attack probability (cyberdemon)
        if (thingProperties.HasFlag(Mbf21ThingFlags.LONGMELEE))
            def.Properties.MeleeThreshold = 196; // Has long melee range (revenant)

        def.Flags.NoTarget = thingProperties.HasFlag(Mbf21ThingFlags.DMGIGNORED);
        def.Flags.NoRadiusDmg = thingProperties.HasFlag(Mbf21ThingFlags.NORADIUSDMG);
        def.Flags.ForceRadiusDmg = thingProperties.HasFlag(Mbf21ThingFlags.FORCERADIUSDMG);
        def.Flags.MissileMore = thingProperties.HasFlag(Mbf21ThingFlags.RANGEHALF);
        def.Flags.QuickToRetaliate = thingProperties.HasFlag(Mbf21ThingFlags.NOTHRESHOLD);
        def.Flags.Boss = thingProperties.HasFlag(Mbf21ThingFlags.BOSS);
        def.Flags.Map07Boss1 = thingProperties.HasFlag(Mbf21ThingFlags.MAP07BOSS1);
        def.Flags.Map07Boss2 = thingProperties.HasFlag(Mbf21ThingFlags.MAP07BOSS2);
        def.Flags.E1M8Boss = thingProperties.HasFlag(Mbf21ThingFlags.E1M8BOSS);
        def.Flags.E2M8Boss = thingProperties.HasFlag(Mbf21ThingFlags.E2M8BOSS);
        def.Flags.E3M8Boss = thingProperties.HasFlag(Mbf21ThingFlags.E3M8BOSS);
        def.Flags.E4M6Boss = thingProperties.HasFlag(Mbf21ThingFlags.E4M6BOSS);
        def.Flags.E4M8Boss = thingProperties.HasFlag(Mbf21ThingFlags.E4M8BOSS);
        def.Flags.Ripper = thingProperties.HasFlag(Mbf21ThingFlags.RIP);
        def.Flags.FullVolSee = thingProperties.HasFlag(Mbf21ThingFlags.FULLVOLSOUNDS);
        def.Flags.FullVolDeath = thingProperties.HasFlag(Mbf21ThingFlags.FULLVOLSOUNDS);
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

    private static double GetDouble(double value) => value / 65536.0;

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
