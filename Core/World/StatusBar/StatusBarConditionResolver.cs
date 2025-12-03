using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Resources.Definitions.Id24;
using Helion.Resources.Definitions.StatusBar;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Players;
using Helion.Resources.Definitions.StatusBar.Enums;

namespace Helion.World.StatusBar;

public static class StatusBarConditionResolver
{
    public static bool Evaluate(StatusBarContext context, List<StatusBarConditionDef>? conditions)
    {
        if (conditions == null || conditions.Count == 0)
            return true;

        foreach (var condition in conditions)
        {
            if (!CheckSingle(context, condition))
                return false;
        }
        return true;
    }

    private static bool CheckSingle(StatusBarContext context, StatusBarConditionDef c)
    {
        var player = context.Player;
        var composer = WorldStatic.EntityManager.DefinitionComposer;
        var gameConf = WorldStatic.World.ArchiveCollection.Definitions.GameConfDefinition;

        return c.Condition switch
        {
            StatusBarConditionType.WeaponOwned => CheckWeaponOwned(player, c.Param, composer),
            StatusBarConditionType.WeaponSelected => CheckWeaponSelected(player, c.Param, composer),
            StatusBarConditionType.WeaponNotSelected => !CheckWeaponSelected(player, c.Param, composer),
            
            StatusBarConditionType.WeaponHasAmmo => CheckWeaponHasAmmo(player, c.Param, composer),
            StatusBarConditionType.SelectedWeaponHasAmmo => CheckSelectedWeaponHasAmmo(player),
            StatusBarConditionType.AmmoMatch => CheckAmmoMatch(player, c.Param, composer),
            
            StatusBarConditionType.SlotOwned => CheckSlotOwned(player, c.Param),
            StatusBarConditionType.SlotNotOwned => !CheckSlotOwned(player, c.Param),
            StatusBarConditionType.SlotSelected => CheckSlotSelected(player, c.Param),
            StatusBarConditionType.SlotNotSelected => !CheckSlotSelected(player, c.Param),
            
            StatusBarConditionType.ItemOwned => CheckItemOwned(player, c.Param, composer),
            StatusBarConditionType.ItemNotOwned => !CheckItemOwned(player, c.Param, composer),
            
            StatusBarConditionType.GameVersionGe => CheckGameVersion(gameConf, c.Param, greaterEqual: true),
            StatusBarConditionType.GameVersionLt => CheckGameVersion(gameConf, c.Param, greaterEqual: false),
            
            StatusBarConditionType.SessionTypeEq => CheckSessionType(c.Param, equals: true),
            StatusBarConditionType.SessionTypeNeq => CheckSessionType(c.Param, equals: false),
            
            StatusBarConditionType.GameModeEq => CheckGameMode(gameConf, c.Param, equals: true),
            StatusBarConditionType.GameModeNeq => CheckGameMode(gameConf, c.Param, equals: false),
            
            StatusBarConditionType.HudModeEq => CheckHudMode(c.Param),
            
            // v1.1 Extensions
            StatusBarConditionType.AutomapModeEq => CheckAutomap(context, c.Param),
            StatusBarConditionType.WidgetEnabled => CheckWidgetEnabled(c.Param),
            StatusBarConditionType.WidgetDisabled => !CheckWidgetEnabled(c.Param),
            StatusBarConditionType.WeaponNotOwned => !CheckWeaponOwned(player, c.Param, composer),
            
            StatusBarConditionType.HealthGe => player.Health >= c.Param,
            StatusBarConditionType.HealthLt => player.Health < c.Param,
            StatusBarConditionType.HealthPercentGe => CheckHealthPercent(player, c.Param, greaterEqual: true),
            StatusBarConditionType.HealthPercentLt => CheckHealthPercent(player, c.Param, greaterEqual: false),
            
            StatusBarConditionType.ArmorGe => player.Armor >= c.Param,
            StatusBarConditionType.ArmorLt => player.Armor < c.Param,
            StatusBarConditionType.ArmorPercentGe => CheckArmorPercent(player, c.Param, greaterEqual: true),
            StatusBarConditionType.ArmorPercentLt => CheckArmorPercent(player, c.Param, greaterEqual: false),

            StatusBarConditionType.SelectedAmmoGe => CheckSelectedAmmo(player, c.Param, greaterEqual: true),
            StatusBarConditionType.SelectedAmmoLt => CheckSelectedAmmo(player, c.Param, greaterEqual: false),
            StatusBarConditionType.SelectedAmmoPercentGe => CheckSelectedAmmoPercent(player, c.Param, greaterEqual: true),
            StatusBarConditionType.SelectedAmmoPercentLt => CheckSelectedAmmoPercent(player, c.Param, greaterEqual: false),

            StatusBarConditionType.AmmoGe => CheckAmmoAmount(player, c.Param2, c.Param, greaterEqual: true, composer),
            StatusBarConditionType.AmmoLt => CheckAmmoAmount(player, c.Param2, c.Param, greaterEqual: false, composer),
            StatusBarConditionType.AmmoPercentGe => CheckAmmoPercent(player, c.Param2, c.Param, greaterEqual: true, composer),
            StatusBarConditionType.AmmoPercentLt => CheckAmmoPercent(player, c.Param2, c.Param, greaterEqual: false, composer),

            StatusBarConditionType.WidescreenModeEq => CheckWidescreen(context, c.Param),
            
            StatusBarConditionType.EpisodeEq => CheckEpisode(c.Param),
            StatusBarConditionType.LevelGe => CheckLevel(c.Param, greaterEqual: true),
            StatusBarConditionType.LevelLt => CheckLevel(c.Param, greaterEqual: false),

            _ => false
        };
    }

    // --- Helper Methods ---

    private static bool TryGetId24PickupType(EntityDefinitionComposer composer, int pickupItemType, [NotNullWhen(true)] out EntityDefinition? definition)
    {
        string? entityName = pickupItemType switch
        {
            // Keys
            1 => "BlueCard",
            2 => "YellowCard",
            3 => "RedCard",
            4 => "BlueSkull",
            5 => "YellowSkull",
            6 => "RedSkull",
            // Items
            7 => "Backpack",
            8 => "HealthBonus",
            9 => "Stimpack",
            10 => "Medikit",
            11 => "Soulsphere",
            12 => "Megasphere",
            13 => "ArmorBonus",
            14 => "GreenArmor",
            15 => "BlueArmor", 
            16 => "ComputerAreaMap",
            // Powerups
            17 => "LightAmp",
            18 => "Berserk",
            19 => "BlurSphere", 
            20 => "RadSuit",
            21 => "InvulnerabilitySphere",
            // Weapons (100+)
            100 => "Chainsaw",
            101 => "Shotgun",
            102 => "SuperShotgun",
            103 => "Chaingun",
            104 => "RocketLauncher",
            105 => "PlasmaRifle",
            106 => "BFG9000",
            _ => null
        };

        if (entityName != null)
        {
            // FIX: Use GetByName instead of GetByNameOrDefault
            definition = composer.GetByName(entityName);
            
            if (definition == null)
            {
                // Fallbacks
                if (entityName == "ComputerAreaMap") 
                    definition = composer.GetByName("AllMap");
                else if (entityName == "InvulnerabilitySphere") 
                    definition = composer.GetByName("Invulnerability");
            }
            return definition != null;
        }

        definition = null;
        return false;
    }

    public static bool TryGetId24AmmoType(EntityDefinitionComposer composer, int ammoTypeIndex, [NotNullWhen(true)] out EntityDefinition? def)
    {
        string? ammoName = ammoTypeIndex switch
        {
            0 => "Clip",
            1 => "Shell",
            2 => "Cell",
            3 => "RocketAmmo",
            _ => null
        };

        if (ammoName != null)
        {
            def = composer.GetByName(ammoName);
            return def != null;
        }

        def = null;
        return false;
    }
    
    private static bool CheckWeaponOwned(Player player, int param, EntityDefinitionComposer composer)
    {
        if (!TryGetId24PickupType(composer, param, out var def))
            return false;
        return player.Inventory.Weapons.OwnsWeapon(def);
    }

    private static bool CheckWeaponSelected(Player player, int param, EntityDefinitionComposer composer)
    {
        if (player.Weapon == null || !TryGetId24PickupType(composer, param, out var def))
            return false;
        return player.Weapon.Definition == def;
    }

    private static bool CheckWeaponHasAmmo(Player player, int param, EntityDefinitionComposer composer)
    {
        if (!TryGetId24PickupType(composer, param, out var def))
            return false;
        string ammoType = def.Properties.Weapons.AmmoType;
        if (string.IsNullOrEmpty(ammoType)) return true;
        return player.Inventory.Amount(ammoType) >= def.Properties.Weapons.AmmoUse;
    }

    private static bool CheckSelectedWeaponHasAmmo(Player player)
    {
        return player.CheckAmmo();
    }

    private static bool CheckAmmoMatch(Player player, int param, EntityDefinitionComposer composer)
    {
        if (player.Weapon == null) return false;
        if (!TryGetId24AmmoType(composer, param, out var ammoDefFromParam)) return false;
        string weaponAmmoName = player.Weapon.Definition.Properties.Weapons.AmmoType;
        if (string.IsNullOrEmpty(weaponAmmoName)) return false; 
        return ammoDefFromParam.Name.Equals(weaponAmmoName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool CheckSlotOwned(Player player, int slot)
    {
        return player.Inventory.Weapons.HasWeaponSlot(slot);
    }

    private static bool CheckSlotSelected(Player player, int slot)
    {
        return player.WeaponSlot == slot;
    }

    private static bool CheckItemOwned(Player player, int param, EntityDefinitionComposer composer)
    {
        if (!TryGetId24PickupType(composer, param, out var def)) return false;
        
        // 1. Inventory Check
        if (player.Inventory.HasItem(def.Name)) return true;
        // 2. Armor Check
        if (player.ArmorDefinition != null && player.ArmorDefinition == def) return true;
        // 3. Weapon Check
        if (player.Weapon != null && player.Weapon.Definition == def) return true;

        return false;
    }

    private static bool CheckGameVersion(GameConfDefinition? gameConf, int param, bool greaterEqual)
    {
        int currentIndex = GameConfConstants.ValidExecutables.Length - 1; 
        if (gameConf?.Data?.Executable != null)
        {
            currentIndex = Array.IndexOf(GameConfConstants.ValidExecutables, gameConf.Data.Executable);
            if (currentIndex == -1) return false;
        }
        return greaterEqual ? currentIndex >= param : currentIndex < param;
    }

    private static bool CheckSessionType(int param, bool equals)
    {
        int currentType = 0; // Single Player
        var world = WorldStatic.World;
        if (world.WorldType == WorldType.Deathmatch) currentType = 2;
        else if (world.WorldType == WorldType.Cooperative) currentType = 1;
        return equals ? (currentType == param) : (currentType != param);
    }

    private static bool CheckGameMode(GameConfDefinition? gameConf, int param, bool equals)
    {
        if (gameConf?.Data?.Mode == null) return false;
        int currentIndex = Array.IndexOf(GameConfConstants.ValidModes, gameConf.Data.Mode);
        if (currentIndex == -1) return false;
        return equals ? (currentIndex == param) : (currentIndex != param);
    }

    private static bool CheckHudMode(int param)
    {
        // 0 = Standard, 1 = Compact. Helion treats Minimal as Compact.
        int mode = WorldStatic.World.Config.Hud.StatusBarSize.Value == StatusBarSizeType.Minimal ? 1 : 0;
        return mode == param;
    }

    private static bool CheckAutomap(StatusBarContext context, int param)
    {
        // 0x01: Enabled
        // 0x02: Overlay
        // 0x04: Disabled
        bool visible = context.AutomapVisible;
        if ((param & 0x01) != 0 && visible) return true;
        if ((param & 0x04) != 0 && !visible) return true;
        if ((param & 0x02) != 0 && visible) return true; 
        return false;
    }
    
    // --- v1.1 Extension Helpers ---

    private static bool CheckHealthPercent(Player player, int param, bool greaterEqual)
    {
        int percent = (int)((player.Health / 100.0f) * 100); 
        return greaterEqual ? percent >= param : percent < param;
    }

    private static bool CheckArmorPercent(Player player, int param, bool greaterEqual)
    {
        int max = 100;
        if (player.Inventory.HasItem("BlueArmor")) max = 200; 
        int percent = (int)((player.Armor / (float)max) * 100);
        return greaterEqual ? percent >= param : percent < param;
    }

    private static bool CheckSelectedAmmo(Player player, int param, bool greaterEqual)
    {
        if (player.Weapon == null) return false;
        string ammoType = player.Weapon.Definition.Properties.Weapons.AmmoType;
        if (string.IsNullOrEmpty(ammoType)) return false;
        
        int amount = player.Inventory.Amount(ammoType);
        return greaterEqual ? amount >= param : amount < param;
    }

    private static bool CheckSelectedAmmoPercent(Player player, int param, bool greaterEqual)
    {
        if (player.Weapon == null) return false;
        string ammoType = player.Weapon.Definition.Properties.Weapons.AmmoType;
        if (string.IsNullOrEmpty(ammoType)) return false;
        
        int amount = player.Inventory.Amount(ammoType);
        int max = GetMaxAmount(player, ammoType);
        if (max == 0) return false;

        int percent = (int)((amount / (float)max) * 100);
        return greaterEqual ? percent >= param : percent < param;
    }

    private static bool CheckAmmoAmount(Player player, int ammoTypeIndex, int val, bool greaterEqual, EntityDefinitionComposer composer)
    {
        if (!TryGetId24AmmoType(composer, ammoTypeIndex, out var def))
            return false;
        
        int amount = player.Inventory.Amount(def.Name);
        return greaterEqual ? amount >= val : amount < val;
    }
    
    private static bool CheckAmmoPercent(Player player, int ammoTypeIndex, int val, bool greaterEqual, EntityDefinitionComposer composer)
    {
        if (!TryGetId24AmmoType(composer, ammoTypeIndex, out var def))
            return false;

        int amount = player.Inventory.Amount(def.Name);
        int max = GetMaxAmount(player, def.Name);
        if (max == 0) return false;

        int percent = (int)((amount / (float)max) * 100);
        return greaterEqual ? percent >= val : percent < val;
    }

    private static bool CheckWidescreen(StatusBarContext context, int param)
    {
        return (param == 1) == context.Widescreen;
    }

    private static bool CheckEpisode(int param)
    {
        var mapName = WorldStatic.World.MapInfo.MapName;
        if (mapName.Length >= 2 && char.ToUpperInvariant(mapName[0]) == 'E' && char.IsDigit(mapName[1]))
        {
            if (int.TryParse(mapName.AsSpan(1, 1), out int ep))
            {
                return ep == param;
            }
        }
        return param == 1;
    }

    private static bool CheckLevel(int param, bool greaterEqual)
    {
        return greaterEqual ? WorldStatic.World.MapInfo.LevelNumber >= param : WorldStatic.World.MapInfo.LevelNumber < param;
    }
    
    private static bool CheckWidgetEnabled(int param)
    {
        // Mappings based on DSDA/Woof standards
        // 0: Level Stats (Kills/Items/Secrets)
        // 1: Time
        // 2: Coords
        // 3: FPS
        // 6: Speedometer
        
        var config = WorldStatic.World.Config.Hud;
        
        return param switch
        {
            0 => config.ShowStats.Value,
            1 => config.ShowStats.Value, // Helion groups Time with Stats usually
            2 => WorldStatic.World.GetCameraPlayer().Cheats.IsCheatActive(Cheats.CheatType.ShowPosition),
            3 => config.ShowFPS.Value,
            // Helion doesn't have specific booleans for Speedometer yet, default to allowed
            _ => true 
        };
    }
    
    private static int GetMaxAmount(Player player, string name)
    {
        var composer = WorldStatic.EntityManager.DefinitionComposer;
        var def = composer.GetByName(name);
        if (def == null) return 0;
        
        string baseName = Inventory.GetBaseInventoryName(def);
        var baseDef = composer.GetByName(baseName);
        if (baseDef != null) def = baseDef;

        int max = def.Properties.Inventory.MaxAmount;
        if (player.Inventory.HasItemOfClass(Inventory.BackPackBaseClassName) 
            && def.IsType(Inventory.AmmoClassName) 
            && def.Properties.Ammo.BackpackMaxAmount > max)
        {
            max = def.Properties.Ammo.BackpackMaxAmount;
        }
        return max;
    }
}