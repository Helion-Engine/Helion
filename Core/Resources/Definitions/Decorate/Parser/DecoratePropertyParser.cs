using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Helion.Graphics;
using Helion.Graphics.Palettes;
using Helion.Maps.Specials;
using Helion.Resources.Definitions.Decorate.Properties;
using Helion.Resources.Definitions.Decorate.Properties.Enums;
using Helion.Util;
using Helion.Util.Extensions;

namespace Helion.Resources.Definitions.Decorate.Parser;

/// <summary>
/// Responsible for parsing all the property data.
/// </summary>
public partial class DecorateParser
{
    private List<string> ConsumeStringListAtLeastOne()
    {
        List<string> elements = new List<string> { ConsumeString() };
        while (ConsumeIf(','))
            elements.Add(ConsumeString());
        return elements;
    }

    private Color ConvertStringToColor(string colorString)
    {
        switch (colorString.ToUpperInvariant())
        {
        case "BLACK":
            return Color.Black;
        case "BLUE":
            return Color.Blue;
        case "BRICK":
            return Color.Firebrick;
        case "BROWN":
            return Color.Brown;
        case "CREAM":
            return Color.PeachPuff;
        case "CYAN":
            return Color.Cyan;
        case "DARKBROWN":
            return Color.DarkBrown;
        case "DARKGRAY":
        case "DARKGREY":
            return Color.DarkGray;
        case "DARKGREEN":
            return Color.DarkGreen;
        case "DARKRED":
            return Color.DarkRed;
        case "GOLD":
            return Color.Gold;
        case "GRAY":
        case "GREY":
            return Color.Gray;
        case "GREEN":
            return (0, 255, 0);
        case "LIGHTBLUE":
            return Color.LightBlue;
        case "OLIVE":
            return Color.Olive;
        case "ORANGE":
            return Color.Orange;
        case "PURPLE":
            return Color.Purple;
        case "RED":
            return Color.Red;
        case "TAN":
            return Color.Tan;
        case "WHITE":
            return Color.White;
        case "YELLOW":
            return Color.Yellow;
        }

        if (colorString.Length != 8)
            throw MakeException($"Expecting 'rr gg bb' format for a color in actor '{m_currentDefinition.Name}");

        string redStr = colorString.Substring(0, 2);
        if (!int.TryParse(redStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int r))
            throw MakeException($"Cannot parse red component from 'rr gg bb' format for a color in actor '{m_currentDefinition.Name}");

        string greenStr = colorString.Substring(3, 2);
        if (!int.TryParse(greenStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int g))
            throw MakeException($"Cannot parse red component from 'rr gg bb' format for a color in actor '{m_currentDefinition.Name}");

        string blueStr = colorString.Substring(6, 2);
        if (!int.TryParse(blueStr, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int b))
            throw MakeException($"Cannot parse red component from 'rr gg bb' format for a color in actor '{m_currentDefinition.Name}");

        return Color.FromInts(255, r, g, b);
    }

    private WeaponBob ConsumeBobStyleProperty()
    {
        string bobStyleText = ConsumeString();
        return bobStyleText.ToUpperInvariant() switch
        {
            "ALPHA" => WeaponBob.Alpha,
            "INVERSEALPHA" => WeaponBob.InverseAlpha,
            "INVERSENORMAL" => WeaponBob.InverseNormal,
            "INVERSESMOOTH" => WeaponBob.InverseSmooth,
            "NORMAL" => WeaponBob.Normal,
            "SMOOTH" => WeaponBob.Smooth,
            _ => throw MakeException($"Unknown weapon bob type '{bobStyleText}' on actor '{m_currentDefinition.Name}'"),
        };
    }

    private PowerupModeType ConsumePowerupMode()
    {
        // TODO: This supports combinations, but gives no example of how...
        string mode = ConsumeString();
        return mode.ToUpperInvariant() switch
        {
            "NONE" => PowerupModeType.None,
            "CUMULATIVE" => PowerupModeType.Cumulative,
            "FUZZY" => PowerupModeType.Fuzzy,
            "OPAQUE" => PowerupModeType.Opaque,
            "REFLECTIVE" => PowerupModeType.Reflective,
            "STENCIL" => PowerupModeType.Stencil,
            "TRANSLUCENT" => PowerupModeType.Translucent,
            _ => throw MakeException($"Unknown powerup mode type '{mode}' on actor '{m_currentDefinition.Name}'"),
        };
    }

    private Range ConsumePlayerColorRange()
    {
        int translationIndexStart = ConsumeInteger();
        if (translationIndexStart < 0 || translationIndexStart > 255)
            throw MakeException("Property Player.ColorRange start index is out of range (0 - 255 only)");

        Consume(',');

        int translationIndexEnd = ConsumeInteger();
        if (translationIndexEnd < 0 || translationIndexEnd > 255)
            throw MakeException("Property Player.ColorRange start index is out of range (0 - 255 only)");

        if (translationIndexEnd < translationIndexStart)
            throw MakeException("Property Player.ColorRange start index larger than the end index");

        return new Range(translationIndexStart, translationIndexEnd);
    }

    private PlayerColorSetProperty ConsumePlayerColorSet()
    {
        int number = ConsumeInteger();
        Consume(',');

        string name = ConsumeString();
        Consume(',');

        int start = ConsumeInteger();
        Consume(',');
        int end = ConsumeInteger();
        Consume(',');
        if (start < 0 || start > 255)
            throw MakeException("Player color set property has a starting index that is out of the (0 - 255) range");
        if (end < 0 || end > 255)
            throw MakeException("Player color set property has a ending index that is out of the (0 - 255) range");
        if (start > end)
            throw MakeException("Player color set property has a starting index larger than the ending index");

        List<string> colors = new List<string> { ConsumeString() };
        while (ConsumeIf(','))
            colors.Add(ConsumeString());

        return new PlayerColorSetProperty(number, name, new Range(start, end), colors);
    }

    private PlayerColorSetFileProperty ConsumePlayerColorSetFile()
    {
        int number = ConsumeInteger();
        Consume(',');
        string name = ConsumeString();
        Consume(',');
        // TODO: No idea if table number is right; bad documentation sucks.
        int tableNumber = ConsumeInteger();
        Consume(',');
        string color = ConsumeString();

        return new PlayerColorSetFileProperty(number, name, tableNumber, color);
    }

    private HexenArmorProperty ConsumePlayerHexenArmor()
    {
        int baseValue = ConsumeInteger();
        if (baseValue % 5 != 0)
            throw MakeException("Base value for player hexen armor property must be divisible by 5");

        int armorValue = ConsumeInteger();
        if (armorValue % 5 != 0)
            throw MakeException(" value for player hexen armor property must be divisible by 5");

        int shieldValue = ConsumeInteger();
        if (shieldValue % 5 != 0)
            throw MakeException(" value for player hexen armor property must be divisible by 5");

        int helmValue = ConsumeInteger();
        if (helmValue % 5 != 0)
            throw MakeException(" value for player hexen armor property must be divisible by 5");

        int amuletValue = ConsumeInteger();
        if (amuletValue % 5 != 0)
            throw MakeException(" value for player hexen armor property must be divisible by 5");

        return new HexenArmorProperty(baseValue, armorValue, shieldValue, helmValue, amuletValue);
    }

    private PlayerDamageScreenProperty ConsumePlayerDamageScreenColor()
    {
        if (PeekInteger())
            throw MakeException("Player damage color integer support");

        Color color = ConvertStringToColor(ConsumeString());
        double? intensity = null;
        string? damageType = null;

        if (ConsumeIf(','))
        {
            intensity = ConsumeFloat();
            if (intensity > 1.0)
                throw MakeException("Player damage screen color intensity out of the 0.0 - 1.0 range");
        }

        if (ConsumeIf(','))
            damageType = ConsumeString();

        return new PlayerDamageScreenProperty(color, intensity, damageType);
    }

    private DecorateHealRadius? ConsumePlayerHealRadiusType()
    {
        string mode = ConsumeString();
        return mode.ToUpperInvariant() switch
        {
            "ARMOR" => (DecorateHealRadius?)DecorateHealRadius.Armor,
            "HEALTH" => (DecorateHealRadius?)DecorateHealRadius.Health,
            "MANA" => (DecorateHealRadius?)DecorateHealRadius.Mana,
            _ => throw MakeException($"Unknown heal radius type '{mode}' on actor '{m_currentDefinition.Name}'"),
        };
    }

    private void ConsumeAndHandlePlayerStartItem()
    {
        string className = ConsumeString();
        int? amount = null;
        if (ConsumeIf(','))
            amount = ConsumeInteger();

        PlayerStartItem newStartItem = new PlayerStartItem(className, amount);
        if (m_currentDefinition.Properties.Player.StartItem == null)
            m_currentDefinition.Properties.Player.StartItem = new List<PlayerStartItem> { newStartItem };
        else
            m_currentDefinition.Properties.Player.StartItem.Add(newStartItem);
    }

    private void ConsumeAndHandlePlayerWeaponSlot()
    {
        int slot = ConsumeInteger();
        Consume(',');

        HashSet<string> weapons = new HashSet<string> { ConsumeString() };
        while (ConsumeIf(','))
            weapons.Add(ConsumeString());

        if (m_currentDefinition.Properties.WeaponSlot != null)
        {
            if (m_currentDefinition.Properties.WeaponSlot.TryGetValue(slot, out var weaponList))
                weaponList.UnionWith(weapons);
            else
                m_currentDefinition.Properties.WeaponSlot[slot] = weapons;
        }
        else
            m_currentDefinition.Properties.WeaponSlot = new Dictionary<int, HashSet<string>> { [slot] = weapons };
    }

    private DecorateSpecialActivationType? ConsumeDecorateSpecialActivationType()
    {
        string value = ConsumeString();
        return value.ToUpperInvariant() switch
        {
            "THINGSPEC_DEFAULT" => (DecorateSpecialActivationType?)DecorateSpecialActivationType.Default,
            "THINGSPEC_THINGACTS" => (DecorateSpecialActivationType?)DecorateSpecialActivationType.ThingActs,
            "THINGSPEC_TRIGGERACTS" => (DecorateSpecialActivationType?)DecorateSpecialActivationType.TriggerActs,
            "THINGSPEC_THINGTARGETS" => (DecorateSpecialActivationType?)DecorateSpecialActivationType.ThingTargets,
            "THINGSPEC_TRIGGERTARGETS" => (DecorateSpecialActivationType?)DecorateSpecialActivationType.TriggerTargets,
            "THINGSPEC_MONSTERTRIGGER" => (DecorateSpecialActivationType?)DecorateSpecialActivationType.MonsterTrigger,
            "THINGSPEC_MISSILETRIGGER" => (DecorateSpecialActivationType?)DecorateSpecialActivationType.MissileTrigger,
            "THINGSPEC_CLEARSPECIAL" => (DecorateSpecialActivationType?)DecorateSpecialActivationType.ClearSpecial,
            "THINGSPEC_NODEATHSPECIAL" => (DecorateSpecialActivationType?)DecorateSpecialActivationType.NoDeathSpecial,
            "THINGSPEC_ACTIVATE" => (DecorateSpecialActivationType?)DecorateSpecialActivationType.Activate,
            "THINGSPEC_DEACTIVATE" => (DecorateSpecialActivationType?)DecorateSpecialActivationType.Deactivate,
            "THINGSPEC_SWITCH" => (DecorateSpecialActivationType?)DecorateSpecialActivationType.Switch,
            _ => throw MakeException($"Unknown special activation type '{value}' on actor '{m_currentDefinition.Name}'"),
        };
    }

    private DamageRangeProperty ConsumeDamageProperty()
    {
        if (ConsumeIf('('))
        {
            int exactDamage = ConsumeInteger();
            if (!Peek(')'))
                throw MakeException("Currently do not support damage expressions yet");
            Consume(')');

            return new DamageRangeProperty(exactDamage, true);
        }

        return new DamageRangeProperty(ConsumeInteger(), false);
    }

    private SpecialArgs ConsumeSpecialArgs()
    {
        int arg0 = ConsumeInteger();
        if (arg0 < 0 || arg0 > 255)
            throw MakeException($"Actor arg0 must be in the range [0, 255] on actor '{m_currentDefinition.Name}'");

        int arg1 = ConsumeIf(',') ? ConsumeInteger() : 0;
        if (arg1 < 0 || arg1 > 255)
            throw MakeException($"Actor arg1 must be in the range [0, 255] on actor '{m_currentDefinition.Name}'");

        int arg2 = ConsumeIf(',') ? ConsumeInteger() : 0;
        if (arg2 < 0 || arg2 > 255)
            throw MakeException($"Actor arg2 must be in the range [0, 255] on actor '{m_currentDefinition.Name}'");

        int arg3 = ConsumeIf(',') ? ConsumeInteger() : 0;
        if (arg3 < 0 || arg3 > 255)
            throw MakeException($"Actor arg3 must be in the range [0, 255] on actor '{m_currentDefinition.Name}'");

        int arg4 = ConsumeIf(',') ? ConsumeInteger() : 0;
        if (arg4 < 0 || arg4 > 255)
            throw MakeException($"Actor arg4 must be in the range [0, 255] on actor '{m_currentDefinition.Name}'");

        return new SpecialArgs((byte)arg0, (byte)arg1, (byte)arg2, (byte)arg3, (byte)arg4);
    }

    private void ConsumeAndHandleDropItem()
    {
        string name = ConsumeString();

        byte? probability = null;
        if (ConsumeIf(','))
            probability = (byte)ConsumeInteger();

        int? amount = null;
        if (ConsumeIf(','))
            amount = ConsumeInteger();

        m_currentDefinition.Properties.DropItem.ClassName = name;
        m_currentDefinition.Properties.DropItem.Probability = probability;
        m_currentDefinition.Properties.DropItem.Amount = amount;
    }

    private DecorateBounceType? ConsumeDecorateBounceType()
    {
        string bounce = ConsumeString();
        return bounce.ToUpperInvariant() switch
        {
            "NONE" => (DecorateBounceType?)DecorateBounceType.None,
            "DOOM" => (DecorateBounceType?)DecorateBounceType.Doom,
            "HERETIC" => (DecorateBounceType?)DecorateBounceType.Heretic,
            "HEXEN" => (DecorateBounceType?)DecorateBounceType.Hexen,
            "CLASSIC" => (DecorateBounceType?)DecorateBounceType.Classic,
            "GRENADE" => (DecorateBounceType?)DecorateBounceType.Grenade,
            "DOOMCOMPAT" => (DecorateBounceType?)DecorateBounceType.DoomCompat,
            "HERETICCOMPAT" => (DecorateBounceType?)DecorateBounceType.HereticCompat,
            "HEXENCOMPAT" => (DecorateBounceType?)DecorateBounceType.HexenCompat,
            _ => throw MakeException($"Unknown heal radius type '{bounce}' on actor '{m_currentDefinition.Name}'"),
        };
    }

    private RenderStyle ConsumeRenderStyle()
    {
        var style = ConsumeString();
        if (style.EqualsIgnoreCase("NONE"))
            return RenderStyle.None;
        else if (style.EqualsIgnoreCase("NORMAL"))
            return RenderStyle.Normal;
        else if (style.EqualsIgnoreCase("OPTFUZZY"))
            return RenderStyle.Fuzzy;
        else if (style.EqualsIgnoreCase("TRANSLUCENT"))
            return RenderStyle.Translucent;
        else if (style.EqualsIgnoreCase("ADD"))
            return RenderStyle.Add;
        else if (style.EqualsIgnoreCase("COLORADD"))
            return RenderStyle.ColorAdd;
        else if (style.EqualsIgnoreCase("COLORADDFULLBRIGHT"))
            return RenderStyle.ColorAddFullBright;

        return RenderStyle.Normal;
    }

    private Range ConsumeVisibleAngles()
    {
        int start = ConsumeInteger();
        if (start < 0 || start > 360)
            throw MakeException($"Visible start angle out of range [0, 360] on actor '{m_currentDefinition.Name}'");

        int end = ConsumeInteger();
        if (end < 0 || end > 360)
            throw MakeException($"Visible end angle out of range [0, 360] on actor '{m_currentDefinition.Name}'");

        return new Range(start, end);
    }

    private Range ConsumeVisiblePitch()
    {
        int start = ConsumeInteger();
        if (start < -180 || start > 180)
            throw MakeException($"Visible start pitch out of range [-180, 180] on actor '{m_currentDefinition.Name}'");

        int end = ConsumeInteger();
        if (end < -180 || end > 180)
            throw MakeException($"Visible end pitch out of range [-180, 180] on actor '{m_currentDefinition.Name}'");

        return new Range(start, end);
    }

    private MorphStyle? ConsumeMorphStyleProperty()
    {
        string style = ConsumeString();
        return style.ToUpperInvariant() switch
        {
            "MRF_ADDSTAMINA" => (MorphStyle?)MorphStyle.AddStamina,
            "MRF_FAILNOLAUGH" => (MorphStyle?)MorphStyle.FailNoLaugh,
            "MRF_FAILNOTELEFRAG" => (MorphStyle?)MorphStyle.FailNotTelefrag,
            "MRF_FULLHEALTH" => (MorphStyle?)MorphStyle.FullHealth,
            "MRF_LOSEACTUALWEAPON" => (MorphStyle?)MorphStyle.LoseActualWeapon,
            "MRF_NEWTIDBEHAVIOUR" => (MorphStyle?)MorphStyle.NewTidBehavior,
            "MRF_TRANSFERTRANSLATION" => (MorphStyle?)MorphStyle.TransferTranslation,
            "MRF_UNDOALWAYS" => (MorphStyle?)MorphStyle.UndoAlways,
            "MRF_UNDOBYTOMEOFPOWER" => (MorphStyle?)MorphStyle.UndoByTomeOfPower,
            "MRF_UNDOBYCHAOSDEVICE" => (MorphStyle?)MorphStyle.UndoByChaosDevice,
            "MRF_UNDOBYDEATH" => (MorphStyle?)MorphStyle.UndoByDeath,
            "MRF_UNDOBYDEATHFORCED" => (MorphStyle?)MorphStyle.UndoByDeathForced,
            "MRF_UNDOBYDEATHSAVES" => (MorphStyle?)MorphStyle.UndoByDeathSaves,
            "MRF_WHENINVULNERABLE" => (MorphStyle?)MorphStyle.WhenInvulnerable,
            _ => throw MakeException($"Unknown morph style type '{style}' on actor '{m_currentDefinition.Name}'"),
        };
    }

    private void ConsumeAndHandlePowerupColor()
    {
        if (PeekInteger())
        {
            string r = ((byte)ConsumeInteger()).ToString("X2", CultureInfo.InvariantCulture);
            Consume(',');
            string g = ((byte)ConsumeInteger()).ToString("X2", CultureInfo.InvariantCulture);
            Consume(',');
            string b = ((byte)ConsumeInteger()).ToString("X2", CultureInfo.InvariantCulture);
            m_currentDefinition.Properties.Powerup.Color = new PowerupColor($"{r} {g} {b}");
        }
        else if (PeekString())
            m_currentDefinition.Properties.Powerup.Color = new PowerupColor(ConsumeString());
        else
            throw MakeException($"Expecting an rgb string or 3 comma-separated integers after powerup color on actor '{m_currentDefinition.Name}'");

        if (ConsumeIf(','))
            m_currentDefinition.Properties.Powerup.Color.Alpha = ConsumeFloat();
    }

    private void ConsumeAndHandlePowerupColormap()
    {
        double r = ConsumeFloat();
        if (!MathHelper.InNormalRange(r))
            throw MakeException("Powerup colormap destination R value is not in the 0.0 - 1.0 range");
        Consume(',');
        double g = ConsumeFloat();
        if (!MathHelper.InNormalRange(r))
            throw MakeException("Powerup colormap destination G value is not in the 0.0 - 1.0 range");
        Consume(',');
        double b = ConsumeFloat();
        if (!MathHelper.InNormalRange(r))
            throw MakeException("Powerup colormap destination B value is not in the 0.0 - 1.0 range");

        Color dest = Color.FromInts(255, (int)(r * 255), (int)(g * 255), (int)(b * 255));
        if (ConsumeIf(','))
        {
            double sourceR = ConsumeFloat();
            if (!MathHelper.InNormalRange(r))
                throw MakeException("Powerup colormap source R value is not in the 0.0 - 1.0 range");

            Consume(',');
            double sourceG = ConsumeFloat();
            if (!MathHelper.InNormalRange(r))
                throw MakeException("Powerup colormap source G value is not in the 0.0 - 1.0 range");

            Consume(',');
            double sourceB = ConsumeFloat();
            if (!MathHelper.InNormalRange(r))
                throw MakeException("Powerup colormap source B value is not in the 0.0 - 1.0 range");

            Color source = Color.FromInts(255, (int)(sourceR * 255), (int)(sourceG * 255), (int)(sourceB * 255));
            m_currentDefinition.Properties.Powerup.Colormap = new PowerupColorMap(source, dest);
        }
        else
            m_currentDefinition.Properties.Powerup.Colormap = new PowerupColorMap(dest);
    }

    private void ConsumeActorPropertyOrCombo()
    {
        string property = ConsumeString();

        if (ConsumeIf('.'))
        {
            switch (property.ToUpperInvariant())
            {
            case "AMMO":
                ConsumeAmmoProperty();
                break;
            case "ARMOR":
                ConsumeArmorProperty();
                break;
            case "FAKEINVENTORY":
                ConsumeFakeInventoryProperty();
                break;
            case "HEALTH":
                ConsumeHealthProperty();
                break;
            case "HEALTHPICKUP":
                ConsumeHealthPickupProperty();
                break;
            case "INVENTORY":
                ConsumeInventoryProperty();
                break;
            case "MORPHPROJECTILE":
                ConsumeMorphProjectileProperty();
                break;
            case "PLAYER":
                ConsumePlayerProperty();
                break;
            case "POWERUP":
                ConsumePowerupProperty();
                break;
            case "PUZZLEITEM":
                ConsumePuzzleItemProperty();
                break;
            case "WEAPON":
                ConsumeWeaponProperty();
                break;
            case "WEAPONPIECE":
                ConsumeWeaponPieceProperty();
                break;
            default:
                throw MakeException($"Unknown prefix property '{property}' on actor '{m_currentDefinition.Name}'");
            }
        }
        else
            ConsumeTopLevelPropertyOrCombo(property);
    }

    private void ConsumeAmmoProperty()
    {
        string ammoProperty = ConsumeIdentifier();
        switch (ammoProperty.ToUpperInvariant())
        {
        case "BACKPACKAMOUNT":
            m_currentDefinition.Properties.Ammo.BackpackAmount = ConsumeInteger();
            break;
        case "BACKPACKMAXAMOUNT":
            m_currentDefinition.Properties.Ammo.BackpackMaxAmount = ConsumeInteger();
            break;
        case "DROPAMOUNT":
            m_currentDefinition.Properties.Ammo.DropAmount = ConsumeInteger();
            break;
        default:
            throw MakeException($"Unknown ammo suffix property '{ammoProperty}' on actor '{m_currentDefinition.Name}'");
        }
    }

    private void ConsumeArmorProperty()
    {
        string armorProperty = ConsumeIdentifier();
        switch (armorProperty.ToUpperInvariant())
        {
        case "MAXABSORB":
            m_currentDefinition.Properties.Armor.MaxAbsorb = ConsumeInteger();
            break;
        case "MAXBONUS":
            m_currentDefinition.Properties.Armor.MaxBonus = ConsumeInteger();
            break;
        case "MAXBONUSMAX":
            m_currentDefinition.Properties.Armor.MaxBonusMax = ConsumeInteger();
            break;
        case "MAXFULLABSORB":
            m_currentDefinition.Properties.Armor.MaxFullAbsorb = ConsumeInteger();
            break;
        case "MAXSAVEAMOUNT":
            m_currentDefinition.Properties.Armor.MaxSaveAmount = ConsumeInteger();
            break;
        case "SAVEAMOUNT":
            m_currentDefinition.Properties.Armor.SaveAmount = ConsumeInteger();
            break;
        case "SAVEPERCENT":
            m_currentDefinition.Properties.Armor.SavePercent = ConsumeFloat();
            break;
        default:
            throw MakeException($"Unknown armor suffix property '{armorProperty}' on actor '{m_currentDefinition.Name}'");
        }
    }

    private void ConsumeFakeInventoryProperty()
    {
        string fakeInventoryProperty = ConsumeString();
        if (fakeInventoryProperty.Equals("RESPAWNS", StringComparison.OrdinalIgnoreCase))
            m_currentDefinition.Properties.FakeInventoryProperty.Respawns = true;
        else
            throw MakeException($"Unknown fake inventory property '{fakeInventoryProperty}' on actor '{m_currentDefinition.Name}'");
    }

    private void ConsumeHealthProperty()
    {
        string healthProperty = ConsumeString();
        if (healthProperty.Equals("LOWMESSAGE", StringComparison.OrdinalIgnoreCase))
        {
            m_currentDefinition.Properties.HealthProperty.LowMessageHealth = ConsumeInteger();
            Consume(',');
            m_currentDefinition.Properties.HealthProperty.LowMessage = ConsumeString();
        }
        else
            throw MakeException($"Unknown health property '{healthProperty}' on actor '{m_currentDefinition.Name}'");
    }

    private void ConsumeHealthPickupProperty()
    {
        string healthPickupProperty = ConsumeString();
        if (healthPickupProperty.Equals("AUTOUSE", StringComparison.OrdinalIgnoreCase))
        {
            int autoUseValue = ConsumeInteger();
            if (autoUseValue < 0 || autoUseValue > 3)
                throw MakeException($"HealthPickup AutoUse property out of [0, 3] range, on actor '{m_currentDefinition.Name}'");
            m_currentDefinition.Properties.HealthPickupAutoUse = (HealthPickupAutoUse)autoUseValue;
        }
        else
            throw MakeException($"Unknown health pickup property '{healthPickupProperty}' on actor '{m_currentDefinition.Name}'");
    }

    private void ConsumeInventoryProperty()
    {
        string inventoryProperty = ConsumeIdentifier();
        switch (inventoryProperty.ToUpperInvariant())
        {
        case "ALTHUDICON":
            m_currentDefinition.Properties.Inventory.AltHUDIcon = ConsumeString();
            break;
        case "AMOUNT":
            m_currentDefinition.Properties.Inventory.Amount = ConsumeInteger();
            break;
        case "DEFMAXAMOUNT":
            m_currentDefinition.Properties.Inventory.DefMaxAmount = true;
            break;
        case "FORBIDDENTO":
            m_currentDefinition.Properties.Inventory.ForbiddenTo = ConsumeStringListAtLeastOne();
            break;
        case "GIVEQUEST":
            m_currentDefinition.Properties.Inventory.GiveQuest = ConsumeInteger();
            break;
        case "INTERHUBAMOUNT":
            m_currentDefinition.Properties.Inventory.InterHubAmount = ConsumeInteger();
            break;
        case "ICON":
            m_currentDefinition.Properties.Inventory.Icon = ConsumeString();
            break;
        case "MAXAMOUNT":
            m_currentDefinition.Properties.Inventory.MaxAmount = ConsumeInteger();
            break;
        case "PICKUPFLASH":
            m_currentDefinition.Properties.Inventory.PickupFlash = ConsumeString();
            break;
        case "PICKUPMESSAGE":
            m_currentDefinition.Properties.Inventory.PickupMessage = ConsumeString();
            break;
        case "PICKUPSOUND":
            m_currentDefinition.Properties.Inventory.PickupSound = ConsumeString();
            break;
        case "RESPAWNTICS":
            m_currentDefinition.Properties.Inventory.RespawnTics = ConsumeInteger();
            break;
        case "RESTRICTEDTO":
            m_currentDefinition.Properties.Inventory.RestrictedTo = ConsumeStringListAtLeastOne();
            break;
        case "USESOUND":
            m_currentDefinition.Properties.Inventory.UseSound = ConsumeString();
            break;
        default:
            throw MakeException($"Unknown inventory suffix property '{inventoryProperty}' on actor '{m_currentDefinition.Name}'");
        }
    }

    private void ConsumeMorphProjectileProperty()
    {
        string morphProperty = ConsumeIdentifier();
        switch (morphProperty.ToUpperInvariant())
        {
        case "DURATION":
            m_currentDefinition.Properties.MorphProjectile.Duration = ConsumeSignedInteger();
            break;
        case "MONSTERCLASS":
            m_currentDefinition.Properties.MorphProjectile.MonsterClass = ConsumeString();
            break;
        case "MORPHFLASH":
            m_currentDefinition.Properties.MorphProjectile.MorphFlash = ConsumeString();
            break;
        case "MORPHSTYLE":
            m_currentDefinition.Properties.MorphProjectile.MorphStyle = ConsumeMorphStyleProperty();
            break;
        case "PLAYERCLASS":
            m_currentDefinition.Properties.MorphProjectile.PlayerClass = ConsumeString();
            break;
        case "UNMORPHFLASH":
            m_currentDefinition.Properties.MorphProjectile.UnmorphFlash = ConsumeString();
            break;
        default:
            throw MakeException($"Unknown morph suffix property '{morphProperty}' on actor '{m_currentDefinition.Name}'");
        }
    }

    private List<string> ConsumeTranslationProperties()
    {
        List<string> translations = new List<string> { ConsumeString() };

        if (!translations.First().Equals("ICE", StringComparison.OrdinalIgnoreCase))
            while (ConsumeIf(','))
                translations.Add(ConsumeString());

        return translations;
    }

    private List<string> ConsumeVisibleToPlayerClass()
    {
        List<string> translations = new List<string> { ConsumeString() };

        while (ConsumeIf(','))
            translations.Add(ConsumeString());

        return translations;
    }

    private void ConsumePlayerProperty()
    {
        string property = ConsumeIdentifier();
        switch (property.ToUpperInvariant())
        {
        case "AIRCAPACITY":
            m_currentDefinition.Properties.Player.AirCapacity = ConsumeFloat();
            break;
        case "ATTACKZOFFSET":
            m_currentDefinition.Properties.Player.AttackZOffset = ConsumeSignedInteger();
            break;
        case "COLORRANGE":
            m_currentDefinition.Properties.Player.ColorRange = ConsumePlayerColorRange();
            break;
        case "COLORSET":
            m_currentDefinition.Properties.Player.ColorSet = ConsumePlayerColorSet();
            break;
        case "COLORSETFILE":
            m_currentDefinition.Properties.Player.ColorSetFile = ConsumePlayerColorSetFile();
            break;
        case "CLEARCOLORSET":
            m_currentDefinition.Properties.Player.ClearColorSet = ConsumeInteger();
            break;
        case "CROUCHSPRITE":
            m_currentDefinition.Properties.Player.CrouchSprite = ConsumeString();
            break;
        case "DAMAGESCREENCOLOR":
            m_currentDefinition.Properties.Player.DamageScreenColor = ConsumePlayerDamageScreenColor();
            break;
        case "DISPLAYNAME":
            m_currentDefinition.Properties.Player.DisplayName = ConsumeString();
            break;
        case "FACE":
            m_currentDefinition.Properties.Player.Face = ConsumeString();
            break;
        case "FALLINGSCREAMSPEED":
            double minFallingSpeed = ConsumeFloat();
            Consume(',');
            double maxFallingSpeed = ConsumeFloat();
            if (maxFallingSpeed < minFallingSpeed)
                throw MakeException("Player falling scream speed has the min value being larger than the max value");
            m_currentDefinition.Properties.Player.FallingScreamSpeed = new PlayerFallingScreamSpeed(minFallingSpeed, maxFallingSpeed);
            break;
        case "FLECHETTETYPE":
            m_currentDefinition.Properties.Player.FlechetteType = ConsumeString();
            break;
        case "FORWARDMOVE":
            double forwardMoveWalk = ConsumeFloat();
            double forwardMoveRun = 1.0;
            if (ConsumeIf(','))
                forwardMoveRun = ConsumeFloat();
            m_currentDefinition.Properties.Player.ForwardMove = new PlayerMoveProperty(forwardMoveWalk, forwardMoveRun);
            break;
        case "GRUNTSPEED":
            m_currentDefinition.Properties.Player.GruntSpeed = ConsumeFloat();
            break;
        case "HEALRADIUSTYPE":
            m_currentDefinition.Properties.Player.HealRadiusType = ConsumePlayerHealRadiusType();
            break;
        case "HEXENARMOR":
            m_currentDefinition.Properties.Player.HexenArmor = ConsumePlayerHexenArmor();
            break;
        case "INVULNERABILITYMODE":
            m_currentDefinition.Properties.Player.InvulnerabilityMode = ConsumeString();
            break;
        case "JUMPZ":
            m_currentDefinition.Properties.Player.JumpZ = ConsumeFloat();
            break;
        case "MAXHEALTH":
            m_currentDefinition.Properties.Player.MaxHealth = ConsumeInteger();
            break;
        case "MORPHWEAPON":
            m_currentDefinition.Properties.Player.MorphWeapon = ConsumeString();
            break;
        case "MUGSHOTMAXHEALTH":
            m_currentDefinition.Properties.Player.MugShotMaxHealth = ConsumeInteger();
            break;
        case "PORTRAIT":
            m_currentDefinition.Properties.Player.Portrait = ConsumeString();
            break;
        case "RUNHEALTH":
            m_currentDefinition.Properties.Player.RunHealth = ConsumeInteger();
            break;
        case "SCOREICON":
            m_currentDefinition.Properties.Player.ScoreIcon = ConsumeString();
            break;
        case "SIDEMOVE":
            double sideMoveWalk = ConsumeFloat();
            double sideMoveRun = 1.0;
            if (ConsumeIf(','))
                sideMoveRun = ConsumeFloat();
            m_currentDefinition.Properties.Player.SideMove = new PlayerMoveProperty(sideMoveWalk, sideMoveRun);
            break;
        case "SOUNDCLASS":
            m_currentDefinition.Properties.Player.SoundClass = ConsumeString();
            break;
        case "SPAWNCLASS":
            m_currentDefinition.Properties.Player.SpawnClass = ConsumeString();
            break;
        case "STARTITEM":
            ConsumeAndHandlePlayerStartItem();
            break;
        case "TELEPORTFREEZETIME":
            m_currentDefinition.Properties.Player.TeleportFreezeTime = ConsumeInteger();
            break;
        case "USERANGE":
            m_currentDefinition.Properties.Player.UseRange = ConsumeFloat();
            break;
        case "WEAPONSLOT":
            ConsumeAndHandlePlayerWeaponSlot();
            break;
        case "VIEWBOB":
            m_currentDefinition.Properties.Player.ViewBob = ConsumeSignedFloat();
            break;
        case "VIEWHEIGHT":
            m_currentDefinition.Properties.Player.ViewHeight = ConsumeSignedFloat();
            break;
        default:
            throw MakeException($"Unknown PLAYER property '{property}' on actor '{m_currentDefinition.Name}'");
        }
    }

    private void ConsumePowerupProperty()
    {
        string nestedProperty = ConsumeIdentifier();
        switch (nestedProperty.ToUpperInvariant())
        {
        case "COLOR":
            ConsumeAndHandlePowerupColor();
            break;
        case "COLORMAP":
            ConsumeAndHandlePowerupColormap();
            break;
        case "DURATION":
            m_currentDefinition.Properties.Powerup.Duration = ConsumeSignedInteger();
            break;
        case "MODE":
            m_currentDefinition.Properties.Powerup.Mode = ConsumePowerupMode();
            break;
        case "STRENGTH":
            m_currentDefinition.Properties.Powerup.Strength = ConsumeSignedInteger();
            break;
        case "TYPE":
            m_currentDefinition.Properties.Powerup.Type = ConsumeString();
            break;
        default:
            Log.Warn("Unknown powerup property suffix '{0}' for actor '{1}'", nestedProperty, m_currentDefinition.Name);
            break;
        }
    }

    private void ConsumePuzzleItemProperty()
    {
        string nestedProperty = ConsumeIdentifier();
        switch (nestedProperty.ToUpperInvariant())
        {
        case "NUMBER":
            m_currentDefinition.Properties.PuzzleItem.Number = ConsumeInteger();
            break;
        case "FAILMESSAGE":
            m_currentDefinition.Properties.PuzzleItem.FailMessage = ConsumeString();
            break;
        default:
            Log.Warn("Unknown puzzle item property suffix '{0}' for actor '{1}'", nestedProperty, m_currentDefinition.Name);
            break;
        }
    }

    private void ConsumeWeaponProperty()
    {
        string nestedProperty = ConsumeIdentifier();
        switch (nestedProperty.ToUpperInvariant())
        {
        case "AMMOGIVE":
            m_currentDefinition.Properties.Weapons.AmmoGive = ConsumeSignedInteger();
            break;
        case "AMMOGIVE1":
            m_currentDefinition.Properties.Weapons.AmmoGive1 = ConsumeSignedInteger();
            break;
        case "AMMOGIVE2":
            m_currentDefinition.Properties.Weapons.AmmoGive2 = ConsumeSignedInteger();
            break;
        case "AMMOTYPE":
            m_currentDefinition.Properties.Weapons.AmmoType = ConsumeString();
            break;
        case "AMMOTYPE1":
            m_currentDefinition.Properties.Weapons.AmmoType1 = ConsumeString();
            break;
        case "AMMOTYPE2":
            m_currentDefinition.Properties.Weapons.AmmoType2 = ConsumeString();
            break;
        case "AMMOUSE":
            m_currentDefinition.Properties.Weapons.AmmoUse = ConsumeInteger();
            break;
        case "AMMOUSE1":
            m_currentDefinition.Properties.Weapons.AmmoUse1 = ConsumeInteger();
            break;
        case "AMMOUSE2":
            m_currentDefinition.Properties.Weapons.AmmoUse2 = ConsumeInteger();
            break;
        case "BOBRANGEX":
            m_currentDefinition.Properties.Weapons.BobRangeX = ConsumeFloat();
            break;
        case "BOBRANGEY":
            m_currentDefinition.Properties.Weapons.BobRangeY = ConsumeFloat();
            break;
        case "BOBSPEED":
            m_currentDefinition.Properties.Weapons.BobSpeed = ConsumeFloat();
            break;
        case "BOBSTYLE":
            m_currentDefinition.Properties.Weapons.BobStyle = ConsumeBobStyleProperty();
            break;
        case "DEFAULTKICKBACK":
            m_currentDefinition.Properties.Weapons.DefaultKickBack = true;
            break;
        case "KICKBACK":
            m_currentDefinition.Properties.Weapons.KickBack = ConsumeInteger();
            break;
        case "LOOKSCALE":
            m_currentDefinition.Properties.Weapons.LookScale = ConsumeFloat();
            break;
        case "MINSELECTIONAMMO1":
            m_currentDefinition.Properties.Weapons.MinSelectionAmmo1 = ConsumeInteger();
            break;
        case "MINSELECTIONAMMO2":
            m_currentDefinition.Properties.Weapons.MinSelectionAmmo2 = ConsumeInteger();
            break;
        case "READYSOUND":
            m_currentDefinition.Properties.Weapons.ReadySound = ConsumeString();
            break;
        case "SELECTIONORDER":
            m_currentDefinition.Properties.Weapons.SelectionOrder = ConsumeInteger();
            break;
        case "SISTERWEAPON":
            m_currentDefinition.Properties.Weapons.SisterWeapon = ConsumeString();
            break;
        case "SLOTNUMBER":
            m_currentDefinition.Properties.Weapons.SlotNumber = ConsumeInteger();
            break;
        case "SLOTPRIORITY":
            m_currentDefinition.Properties.Weapons.SlotPriority = ConsumeFloat();
            break;
        case "UPSOUND":
            m_currentDefinition.Properties.Weapons.UpSound = ConsumeString();
            break;
        case "YADJUST":
            m_currentDefinition.Properties.Weapons.YAdjust = ConsumeInteger();
            break;
        default:
            Log.Warn("Unknown weapon property suffix '{0}' for actor '{1}'", nestedProperty, m_currentDefinition.Name);
            break;
        }
    }

    private void ConsumeWeaponPieceProperty()
    {
        string nestedProperty = ConsumeIdentifier();
        switch (nestedProperty.ToUpperInvariant())
        {
        case "NUMBER":
            m_currentDefinition.Properties.WeaponPieces.Number = ConsumeInteger();
            break;
        case "WEAPON":
            m_currentDefinition.Properties.WeaponPieces.Weapon = ConsumeString();
            break;
        default:
            Log.Warn("Unknown weapon piece property suffix '{0}' for actor '{1}'", nestedProperty, m_currentDefinition.Name);
            break;
        }
    }

    private void ConsumeTopLevelPropertyOrCombo(string property)
    {
        switch (property.ToUpperInvariant())
        {
            // To reduce logic and be efficient, we do combos in this switch
            // statement as well since the property reader is the 'catch-all'.
            case "MONSTER":
                m_currentDefinition.Flags.Monster = true;
                break;
            case "PROJECTILE":
                m_currentDefinition.Flags.Projectile = true;
                break;
            // These are 'flag' properties.
            case "CLEARFLAGS":
                m_currentDefinition.FlagProperties.ClearFlags = true;
                break;
            case "DEFAULTALPHA":
                m_currentDefinition.FlagProperties.DefaultAlpha = true;
                break;
            case "SKIP_SUPER":
                m_currentDefinition.FlagProperties.SkipSuper = true;
                break;
            // The rest are all properties now.
            case "ACCURACY":
                m_currentDefinition.Properties.Accuracy = ConsumeInteger();
                break;
            case "ACTIVATION":
                m_currentDefinition.Properties.Activation = ConsumeDecorateSpecialActivationType();
                break;
            case "ACTIVESOUND":
                m_currentDefinition.Properties.ActiveSound = ConsumeString();
                break;
            case "ALPHA":
                m_currentDefinition.Properties.Alpha = ConsumeFloat();
                break;
            case "ARGS":
                m_currentDefinition.Properties.Args = ConsumeSpecialArgs();
                break;
            case "ATTACKSOUND":
                m_currentDefinition.Properties.AttackSound = ConsumeString();
                break;
            case "BLOODCOLOR":
                m_currentDefinition.Properties.BloodColor = ConvertStringToColor(ConsumeString());
                break;
            case "BLOODTYPE":
                m_currentDefinition.Properties.BloodType = ConsumeString();
                break;
            case "BOUNCECOUNT":
                m_currentDefinition.Properties.BounceCount = ConsumeInteger();
                break;
            case "BOUNCEFACTOR":
                m_currentDefinition.Properties.BounceFactor = ConsumeFloat();
                break;
            case "BOUNCESOUND":
                m_currentDefinition.Properties.BounceSound = ConsumeString();
                break;
            case "BOUNCETYPE":
                m_currentDefinition.Properties.BounceType = ConsumeDecorateBounceType();
                break;
            case "BURNHEIGHT":
                m_currentDefinition.Properties.BurnHeight = ConsumeFloat();
                break;
            case "CAMERAHEIGHT":
                m_currentDefinition.Properties.CameraHeight = ConsumeFloat();
                break;
            case "CONVERSATIONID":
                m_currentDefinition.Properties.ConversationID = ConsumeInteger();
                break;
            case "CRUSHPAINSOUND":
                m_currentDefinition.Properties.CrushPainSound = ConsumeString();
                break;
            case "DAMAGE":
                m_currentDefinition.Properties.Damage = ConsumeDamageProperty();
                break;
            case "DAMAGEFACTOR":
                if (PeekFloat())
                    m_currentDefinition.Properties.DamageFactor = new DamageFactor(ConsumeFloat());
                else
                    m_currentDefinition.Properties.DamageFactor = new DamageFactor(ConsumeString(), ConsumeFloat());
                break;
            case "DAMAGETYPE":
                m_currentDefinition.Properties.DamageType = ConsumeString();
                break;
            case "DEATHHEIGHT":
                m_currentDefinition.Properties.DeathHeight = ConsumeFloat();
                break;
            case "DEATHSOUND":
                m_currentDefinition.Properties.DeathSound = ConsumeString();
                break;
            case "DEATHTYPE":
                m_currentDefinition.Properties.DeathType = ConsumeString();
                break;
            case "DECAL":
                m_currentDefinition.Properties.Decal = ConsumeString();
                break;
            case "DEFTHRESHOLD":
                m_currentDefinition.Properties.DefThreshold = ConsumeInteger();
                break;
            case "DROPITEM":
                ConsumeAndHandleDropItem();
                break;
            case "DESIGNATEDTEAM":
                m_currentDefinition.Properties.DesignatedTeam = ConsumeInteger();
                break;
            case "DISTANCECHECK":
                m_currentDefinition.Properties.DistanceCheck = ConsumeString();
                break;
            case "EXPLOSIONDAMAGE":
                m_currentDefinition.Properties.ExplosionDamage = ConsumeInteger();
                break;
            case "EXPLOSIONRADIUS":
                m_currentDefinition.Properties.ExplosionRadius = ConsumeInteger();
                break;
            case "FASTSPEED":
                m_currentDefinition.Properties.FastSpeed = ConsumeInteger();
                break;
            case "FLOATBOBPHASE":
                m_currentDefinition.Properties.FloatBobPhase = ConsumeSignedFloat();
                break;
            case "FLOATBOBSTRENGTH":
                m_currentDefinition.Properties.FloatBobStrength = ConsumeSignedFloat();
                break;
            case "FLOATSPEED":
                m_currentDefinition.Properties.FloatSpeed = ConsumeFloat();
                break;
            case "FRICTION":
                m_currentDefinition.Properties.Friction = ConsumeFloat();
                break;
            case "FRIENDLYSEEBLOCKS":
                m_currentDefinition.Properties.FriendlySeeBlocks = ConsumeInteger();
                break;
            case "GAME":
                m_currentDefinition.Properties.Game = ConsumeString();
                break;
            case "GIBHEALTH":
                m_currentDefinition.Properties.GibHealth = ConsumeInteger();
                break;
            case "GRAVITY":
                m_currentDefinition.Properties.Gravity = ConsumeFloat();
                break;
            case "HEALTH":
                m_currentDefinition.Properties.Health = ConsumeInteger();
                break;
            case "HEIGHT":
                m_currentDefinition.Properties.Height = ConsumeFloat();
                break;
            case "HITOBITUARY":
                m_currentDefinition.Properties.HitObituary = ConsumeString();
                break;
            case "HOWLSOUND":
                m_currentDefinition.Properties.HowlSound = ConsumeString();
                break;
            case "MASS":
                m_currentDefinition.Properties.Mass = ConsumeFloat();
                break;
            case "MAXDROPOFFHEIGHT":
                m_currentDefinition.Properties.MaxDropOffHeight = ConsumeFloat();
                break;
            case "MAXSTEPHEIGHT":
                m_currentDefinition.Properties.MaxStepHeight = ConsumeFloat();
                break;
            case "MAXTARGETRANGE":
                m_currentDefinition.Properties.MaxTargetRange = ConsumeInteger();
                break;
            case "MELEEDAMAGE":
                m_currentDefinition.Properties.MeleeDamage = ConsumeInteger();
                break;
            case "MELEERANGE":
                m_currentDefinition.Properties.MeleeRange = ConsumeInteger();
                break;
            case "MELEESOUND":
                m_currentDefinition.Properties.MeleeSound = ConsumeString();
                break;
            case "MELEETHRESHOLD":
                m_currentDefinition.Properties.MeleeThreshold = ConsumeInteger();
                break;
            case "MINMISSILECHANCE":
                m_currentDefinition.Properties.MinMissileChance = ConsumeInteger();
                break;
            case "MISSILEHEIGHT":
                m_currentDefinition.Properties.MissileHeight = ConsumeInteger();
                break;
            case "MISSILETYPE":
                m_currentDefinition.Properties.MissileType = ConsumeInteger();
                break;
            case "OBITUARY":
                m_currentDefinition.Properties.Obituary = ConsumeString();
                break;
            case "PAINCHANCE":
                if (PeekFloat())
                    m_currentDefinition.Properties.PainChance = new PainChanceProperty(ConsumeFloat());
                else
                    m_currentDefinition.Properties.PainChance = new PainChanceProperty(ConsumeString(), ConsumeFloat());
                break;
            case "PAINSOUND":
                m_currentDefinition.Properties.PainSound = ConsumeString();
                break;
            case "PAINTHRESHOLD":
                m_currentDefinition.Properties.PainThreshold = ConsumeInteger();
                break;
            case "PAINTYPE":
                m_currentDefinition.Properties.PainType = ConsumeString();
                break;
            case "POISONDAMAGETYPE":
                m_currentDefinition.Properties.PoisonDamageType = ConsumeString();
                break;
            case "PROJECTILEKICKBACK":
                m_currentDefinition.Properties.ProjectileKickBack = ConsumeInteger();
                break;
            case "PROJECTILEPASSHEIGHT":
                m_currentDefinition.Properties.ProjectilePassHeight = ConsumeSignedInteger();
                break;
            case "PUSHFACTOR":
                m_currentDefinition.Properties.PushFactor = ConsumeFloat();
                break;
            case "RADIUS":
                m_currentDefinition.Properties.Radius = ConsumeFloat();
                break;
            case "RADIUSDAMAGEFACTOR":
                m_currentDefinition.Properties.RadiusDamageFactor = ConsumeFloat();
                break;
            case "REACTIONTIME":
                m_currentDefinition.Properties.ReactionTime = ConsumeInteger();
                break;
            case "RENDERRADIUS":
                m_currentDefinition.Properties.RenderRadius = ConsumeFloat();
                break;
            case "RENDERSTYLE":
                m_currentDefinition.Properties.RenderStyle = ConsumeRenderStyle();
                break;
            case "RIPLEVELMAX":
                m_currentDefinition.Properties.RipLevelMax = ConsumeInteger();
                break;
            case "RIPLEVELMIN":
                m_currentDefinition.Properties.RipLevelMin = ConsumeInteger();
                break;
            case "RIPPERLEVEL":
                m_currentDefinition.Properties.RipperLevel = ConsumeInteger();
                break;
            case "SCALE":
                m_currentDefinition.Properties.Scale = ConsumeFloat();
                break;
            case "SEESOUND":
                m_currentDefinition.Properties.SeeSound = ConsumeString();
                break;
            case "SELFDAMAGEFACTOR":
                m_currentDefinition.Properties.SelfDamageFactor = ConsumeFloat();
                break;
            case "SPAWNID":
                m_currentDefinition.Properties.SpawnId = ConsumeInteger();
                break;
            case "SPECIES":
                m_currentDefinition.Properties.Species = ConsumeString();
                break;
            case "SPEED":
                m_currentDefinition.Properties.Speed = ConsumeInteger();
                break;
            case "SPRITEANGLE":
                m_currentDefinition.Properties.SpriteAngle = ConsumeInteger();
                break;
            case "SPRITEROTATION":
                m_currentDefinition.Properties.SpriteRotation = ConsumeInteger();
                break;
            case "STAMINA":
                m_currentDefinition.Properties.Stamina = ConsumeInteger();
                break;
            case "STEALTHALPHA":
                m_currentDefinition.Properties.StealthAlpha = ConsumeFloat();
                break;
            case "STENCILCOLOR":
                m_currentDefinition.Properties.StencilColor = ConsumeInteger();
                break;
            case "TAG":
                m_currentDefinition.Properties.Tag = ConsumeString();
                break;
            case "TELEFOGDESTTYPE":
                m_currentDefinition.Properties.TeleFogDestType = ConsumeString();
                break;
            case "TELEFOGSOURCETYPE":
                m_currentDefinition.Properties.TeleFogSourceType = ConsumeString();
                break;
            case "THRESHOLD":
                m_currentDefinition.Properties.Threshold = ConsumeInteger();
                break;
            case "TRANSLATION":
                m_currentDefinition.Properties.Translation = ConsumeTranslationProperties();
                break;
            case "VSPEED":
                m_currentDefinition.Properties.VSpeed = ConsumeInteger();
                break;
            case "VISIBLEANGLES":
                m_currentDefinition.Properties.VisibleAngles = ConsumeVisibleAngles();
                break;
            case "VISIBLEPITCH":
                m_currentDefinition.Properties.VisiblePitch = ConsumeVisiblePitch();
                break;
            case "VISIBLETOTEAM":
                m_currentDefinition.Properties.VisibleToTeam = ConsumeInteger();
                break;
            case "VISIBLETOPLAYERCLASS":
                m_currentDefinition.Properties.VisibleToPlayerClass = ConsumeVisibleToPlayerClass();
                break;
            case "WALLBOUNCEFACTOR":
                m_currentDefinition.Properties.WallBounceFactor = ConsumeFloat();
                break;
            case "WALLBOUNCESOUND":
                m_currentDefinition.Properties.WallBounceSound = ConsumeString();
                break;
            case "WEAVEINDEXXY":
                int weaveXY = ConsumeInteger();
                if (weaveXY < 0 || weaveXY >= 64)
                    throw MakeException($"Actor property WeaveIndexXY must be in the range [0, 63] on actor '{m_currentDefinition.Name}'");
                m_currentDefinition.Properties.WeaveIndexXY = weaveXY;
                break;
            case "WEAVEINDEXZ":
                int weaveZ = ConsumeInteger();
                if (weaveZ < 0 || weaveZ >= 64)
                    throw MakeException($"Actor property WeaveIndexZ must be in the range [0, 63] on actor '{m_currentDefinition.Name}'");
                m_currentDefinition.Properties.WeaveIndexZ = ConsumeInteger();
                break;
            case "WOUNDHEALTH":
                m_currentDefinition.Properties.WoundHealth = ConsumeInteger();
                break;
            case "XSCALE":
                m_currentDefinition.Properties.XScale = ConsumeFloat();
                break;
            case "YSCALE":
                m_currentDefinition.Properties.YScale = ConsumeFloat();
                break;
            case "RIPSOUND":
                m_currentDefinition.Properties.RipSound = ConsumeString();
                break;
            case "AUTOBLOODCOLOR":
                m_currentDefinition.Properties.AutoBloodPaletteColor = GetPaletteColor(ConsumeString());
                break;
            default:
                throw MakeException($"Unknown property '{property}' on actor '{m_currentDefinition.Name}'");
        }
    }

    private PaletteColor? GetPaletteColor(string value)
    {
        if (Enum.TryParse(value, true, out PaletteColor color) && color != PaletteColor.None)
        {
            BloodColors.Add(color);
            return color;
        }
        return null;
    }
}
    