using Helion.Resources;
using Helion.Resources.Definitions.Id24;
using Helion.Resources.Definitions.StatusBar;
using Helion.Resources.Definitions.StatusBar.Enums;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using System;

namespace Helion.World.StatusBar;

public class StatusBarConditionResolver(LookupArray<EntityDefinition> id24PickupTypeLookup, LookupArray<EntityDefinition> id24AmmoTypeLookup)
{
    readonly LookupArray<EntityDefinition> Id24PickupTypeLookup = id24PickupTypeLookup;
    readonly LookupArray<EntityDefinition> Id24AmmoTypeLookup = id24AmmoTypeLookup;

    public bool ShouldEvaluate = true;

    public bool Evaluate(StatusBarContext context, StatusBarBaseDef statusBarDef)
    {
        if (!ShouldEvaluate)
            return statusBarDef.LastEvaluatedConditionValue;

        if (statusBarDef.Conditions == null || statusBarDef.Conditions.Length == 0)
        {
            statusBarDef.LastEvaluatedConditionValue = true;
            return true;
        }

        for (int i = 0; i < statusBarDef.Conditions.Length; i++)
        {
            ref var condition = ref statusBarDef.Conditions[i];
            if (!CheckSingle(context, ref condition))
            {
                statusBarDef.LastEvaluatedConditionValue = false;
                return false;
            }
        }

        statusBarDef.LastEvaluatedConditionValue = true;
        return true;
    }

    private bool CheckSingle(StatusBarContext context, ref StatusBarConditionDef c)
    {
        var world = context.World;
        var player = context.Player;
        var gameConf = context.World.ArchiveCollection.Definitions.GameConfDefinition;

        return c.Condition switch
        {
            StatusBarConditionType.WeaponOwned => CheckWeaponOwned(player, c.Param),
            StatusBarConditionType.WeaponSelected => CheckWeaponSelected(player, c.Param),
            StatusBarConditionType.WeaponNotSelected => !CheckWeaponSelected(player, c.Param),
            
            StatusBarConditionType.WeaponHasAmmo => CheckWeaponHasAmmo(player, c.Param),
            StatusBarConditionType.SelectedWeaponHasAmmo => CheckSelectedWeaponHasAmmo(player),
            StatusBarConditionType.AmmoMatch => CheckAmmoMatch(player, c.Param),
            
            StatusBarConditionType.SlotOwned => CheckSlotOwned(player, c.Param),
            StatusBarConditionType.SlotNotOwned => !CheckSlotOwned(player, c.Param),
            StatusBarConditionType.SlotSelected => CheckSlotSelected(player, c.Param),
            StatusBarConditionType.SlotNotSelected => !CheckSlotSelected(player, c.Param),
            
            StatusBarConditionType.ItemOwned => CheckItemOwned(player, c.Param),
            StatusBarConditionType.ItemNotOwned => !CheckItemOwned(player, c.Param),
            
            StatusBarConditionType.GameVersionGe => CheckGameVersion(gameConf, c.Param, greaterEqual: true),
            StatusBarConditionType.GameVersionLt => CheckGameVersion(gameConf, c.Param, greaterEqual: false),
            
            StatusBarConditionType.SessionTypeEq => CheckSessionType(world, c.Param, equals: true),
            StatusBarConditionType.SessionTypeNeq => CheckSessionType(world, c.Param, equals: false),
            
            StatusBarConditionType.GameModeEq => CheckGameMode(gameConf, c.Param, equals: true),
            StatusBarConditionType.GameModeNeq => CheckGameMode(gameConf, c.Param, equals: false),
            
            StatusBarConditionType.HudModeEq => CheckHudMode(context, c.Param),
            
            // v1.1 Extensions
            StatusBarConditionType.AutomapModeEq => CheckAutomap(context, c.Param),
            StatusBarConditionType.WidgetEnabled => CheckWidgetEnabled(world, player, c.Param, c.ParamString),
            StatusBarConditionType.WidgetDisabled => !CheckWidgetEnabled(world, player, c.Param, c.ParamString),
            StatusBarConditionType.WeaponNotOwned => !CheckWeaponOwned(player, c.Param),
            
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
            StatusBarConditionType.SelectedAmmoPercentGe => CheckSelectedAmmoPercent(world, player, context.HasBackPack, c.Param, greaterEqual: true),
            StatusBarConditionType.SelectedAmmoPercentLt => CheckSelectedAmmoPercent(world, player, context.HasBackPack, c.Param, greaterEqual: false),

            StatusBarConditionType.AmmoGe => CheckAmmoAmount(player, c.Param2, c.Param, greaterEqual: true),
            StatusBarConditionType.AmmoLt => CheckAmmoAmount(player, c.Param2, c.Param, greaterEqual: false),
            StatusBarConditionType.AmmoPercentGe => CheckAmmoPercent(player, context.HasBackPack, c.Param2, c.Param, greaterEqual: true),
            StatusBarConditionType.AmmoPercentLt => CheckAmmoPercent(player, context.HasBackPack, c.Param2, c.Param, greaterEqual: false),

            StatusBarConditionType.WidescreenModeEq => CheckWidescreen(context, c.Param),
            
            StatusBarConditionType.EpisodeEq => CheckEpisode(world, c.Param),
            StatusBarConditionType.LevelGe => CheckLevel(world, c.Param, greaterEqual: true),
            StatusBarConditionType.LevelLt => CheckLevel(world, c.Param, greaterEqual: false),

            // v1.2 Extensions
            StatusBarConditionType.PatchEmpty => CheckPatchEmpty(context, c.ParamString, true),
            StatusBarConditionType.PatchNotEmpty => CheckPatchEmpty(context, c.ParamString, false),
            StatusBarConditionType.KillsLt => world.LevelStats.KillCount < c.Param,
            StatusBarConditionType.KillsGe => world.LevelStats.KillCount >= c.Param,
            StatusBarConditionType.ItemsLt => world.LevelStats.ItemCount < c.Param,
            StatusBarConditionType.ItemsGe => world.LevelStats.ItemCount >= c.Param,
            StatusBarConditionType.SecretsLt => world.LevelStats.SecretCount < c.Param,
            StatusBarConditionType.SecretsGe => world.LevelStats.SecretCount >= c.Param,
            StatusBarConditionType.KillsPercentLt => CheckStatPercent(world.LevelStats.KillCount, world.LevelStats.TotalMonsters, c.Param, false),
            StatusBarConditionType.KillsPercentGe => CheckStatPercent(world.LevelStats.KillCount, world.LevelStats.TotalMonsters, c.Param, true),
            StatusBarConditionType.ItemsPercentLt => CheckStatPercent(world.LevelStats.ItemCount, world.LevelStats.TotalItems, c.Param, false),
            StatusBarConditionType.ItemsPercentGe => CheckStatPercent(world.LevelStats.ItemCount, world.LevelStats.TotalItems, c.Param, true),
            StatusBarConditionType.SecretsPercentLt => CheckStatPercent(world.LevelStats.SecretCount, world.LevelStats.TotalSecrets, c.Param, false),
            StatusBarConditionType.SecretsPercentGe => CheckStatPercent(world.LevelStats.SecretCount, world.LevelStats.TotalSecrets, c.Param, true),
            StatusBarConditionType.PowerupDurationLt => CheckPowerupDuration(player, MapSbarPowerup(c.Param2), c.Param, false),
            StatusBarConditionType.PowerupDurationGe => CheckPowerupDuration(player, MapSbarPowerup(c.Param2), c.Param, true),
            StatusBarConditionType.PowerupDurationPercentLt => CheckPowerupPercent(player, MapSbarPowerup(c.Param2), c.Param, false),
            StatusBarConditionType.PowerupDurationPercentGe => CheckPowerupPercent(player, MapSbarPowerup(c.Param2), c.Param, true),

            _ => false
        };
    }
    
    private bool CheckWeaponOwned(Player player, int param)
    {
        if (!Id24PickupTypeLookup.TryGetValue(param, out var def))
            return false;
        return player.Inventory.Weapons.OwnsWeapon(def);
    }

    private bool CheckWeaponSelected(Player player, int param)
    {
        if (player.Weapon == null || !Id24PickupTypeLookup.TryGetValue(param, out var def))
            return false;
        return player.Weapon.Definition == def;
    }

    private bool CheckWeaponHasAmmo(Player player, int param)
    {
        if (!Id24PickupTypeLookup.TryGetValue(param, out var def))
            return false;
        string ammoType = def.Properties.Weapons.AmmoType;
        if (string.IsNullOrEmpty(ammoType)) return true;
        return player.Inventory.Amount(ammoType) >= def.Properties.Weapons.AmmoUse;
    }

    private static bool CheckSelectedWeaponHasAmmo(Player player)
    {
        var ammoDef = player.Weapon?.Definition.Properties.Weapons.AmmoTypeDef;
        if (ammoDef == null)
            return false;

        return player.Inventory.Amount(ammoDef) > 0;
    }

    private bool CheckAmmoMatch(Player player, int param)
    {
        if (player.Weapon == null)
            return false;
        if (!Id24AmmoTypeLookup.TryGetValue(param, out var ammoDefFromParam))
            return false;
        var weaponAmmoName = player.Weapon.Definition.Properties.Weapons.AmmoType;
        if (string.IsNullOrEmpty(weaponAmmoName))
            return false; 
        return ammoDefFromParam.Name.EqualsIgnoreCase(weaponAmmoName);
    }

    private static bool CheckSlotOwned(Player player, int slot)
    {
        return player.Inventory.Weapons.HasWeaponSlot(slot);
    }

    private static bool CheckSlotSelected(Player player, int slot)
    {
        if (player.Weapon == null)
            return false;

        return player.Inventory.Weapons.GetWeaponSlot(player.Weapon.Definition).Slot == slot;
    }

    private bool CheckItemOwned(Player player, int param)
    {
        PowerupType? pType = param switch
        {
            16 => PowerupType.ComputerAreaMap,
            17 => PowerupType.LightAmp,
            18 => PowerupType.Strength,
            19 => PowerupType.Invisibility,
            20 => PowerupType.IronFeet,
            21 => PowerupType.Invulnerable,
            _ => null
        };

        if (pType.HasValue)
            return player.Inventory.IsPowerupActive(pType.Value);

        if (!Id24PickupTypeLookup.TryGetValue(param, out var def))
            return false;

        if (player.Inventory.HasItem(def))
            return true;
        if (player.ArmorDefinition != null && player.ArmorDefinition.IsType(def.Name))
            return true;
        if (player.Weapon != null && player.Weapon.Definition == def)
            return true;

        return false;
    }

    private static bool CheckGameVersion(GameConfDefinition? gameConf, int param, bool greaterEqual)
    {
        int currentIndex = GameConfConstants.ValidExecutables.Length - 1; 
        if (gameConf?.Data?.Executable != null)
        {
            currentIndex = Array.IndexOf(GameConfConstants.ValidExecutables, gameConf.Data.Executable);
            if (currentIndex == -1)
                return false;
        }
        return greaterEqual ? currentIndex >= param : currentIndex < param;
    }

    private static bool CheckSessionType(IWorld world, int param, bool equals)
    {
        int currentType = 0; // Single Player
        if (world.WorldType == WorldType.Deathmatch)
            currentType = 2;
        else if (world.WorldType == WorldType.Cooperative)
            currentType = 1;
        return equals ? (currentType == param) : (currentType != param);
    }

    private static bool CheckGameMode(GameConfDefinition? gameConf, int param, bool equals)
    {
        if (gameConf?.Data?.Mode == null)
            return false;
        int currentIndex = Array.IndexOf(GameConfConstants.ValidModes, gameConf.Data.Mode);
        if (currentIndex == -1)
            return false;
        return equals ? (currentIndex == param) : (currentIndex != param);
    }

    private static bool CheckHudMode(StatusBarContext context, int param)
    {
        int mode = context.IsCompact ? 1 : 0;
        return mode == param;
    }
    
    private static bool CheckAutomap(StatusBarContext context, int param)
    {
        bool visible = context.AutomapVisible;
        if ((param & 0x01) != 0 && visible)
            return true;
        if ((param & 0x04) != 0 && !visible)
            return true;
        if ((param & 0x02) != 0 && visible)
            return true; 
        return false;
    }

    private static bool CheckHealthPercent(Player player, int param, bool greaterEqual)
    {
        int percent = (int)(player.Health / 100.0f * 100);
        return greaterEqual ? percent >= param : percent < param;
    }

    private static bool CheckArmorPercent(Player player, int param, bool greaterEqual)
    {
        var max = 100;
        if (player.ArmorProperties != null)
            max = player.ArmorProperties.Armor.SaveAmount;

        var percent = (int)(player.Armor / (float)max * 100);
        return greaterEqual ? percent >= param : percent < param;
    }

    private static bool CheckSelectedAmmo(Player player, int param, bool greaterEqual)
    {
        if (player.Weapon == null)
            return false;
        var ammoType = player.Weapon.Definition.Properties.Weapons.AmmoType;
        if (string.IsNullOrEmpty(ammoType))
            return false;
        
        int amount = player.Inventory.Amount(ammoType);
        return greaterEqual ? amount >= param : amount < param;
    }

    private static bool CheckSelectedAmmoPercent(IWorld world, Player player, bool hasBackPack, int param, bool greaterEqual)
    {
        if (player.Weapon == null)
            return false;
        var ammoType = player.Weapon.Definition.Properties.Weapons.AmmoType;
        if (string.IsNullOrEmpty(ammoType))
            return false;

        var def = world.EntityManager.DefinitionComposer.GetByName(ammoType);
        if (def == null)
            return false;
        
        int amount = player.Inventory.Amount(def);
        int max = GetMaxAmount(def, hasBackPack);
        if (max == 0)
            return false;

        int percent = (int)(amount / (float)max * 100);
        return greaterEqual ? percent >= param : percent < param;
    }

    private bool CheckAmmoAmount(Player player, int ammoTypeIndex, int val, bool greaterEqual)
    {
        if (!Id24AmmoTypeLookup.TryGetValue(ammoTypeIndex, out var def))
            return false;
        
        int amount = player.Inventory.Amount(def.Name);
        return greaterEqual ? amount >= val : amount < val;
    }
    
    private bool CheckAmmoPercent(Player player, bool hasBackPack, int ammoTypeIndex, int val, bool greaterEqual)
    {
        if (!Id24AmmoTypeLookup.TryGetValue(ammoTypeIndex, out var def))
            return false;

        int amount = player.Inventory.Amount(def);
        int max = GetMaxAmount(def, hasBackPack);
        if (max == 0) return false;

        int percent = (int)((amount / (float)max) * 100);
        return greaterEqual ? percent >= val : percent < val;
    }

    private static bool CheckWidescreen(StatusBarContext context, int param)
    {
        return (param == 1) == context.Widescreen;
    }

    private static bool CheckEpisode(IWorld world, int param)
    {
        var mapName = world.MapInfo.MapName;
        if (mapName.Length >= 2 && char.ToUpperInvariant(mapName[0]) == 'E' && char.IsDigit(mapName[1]))
        {
            if (int.TryParse(mapName.AsSpan(1, 1), out int ep))
            {
                return ep == param;
            }
        }
        return param == 1;
    }

    private static bool CheckLevel(IWorld world, int param, bool greaterEqual)
    {
        return greaterEqual ? world.MapInfo.LevelNumber >= param : world.MapInfo.LevelNumber < param;
    }
    
    private static bool CheckWidgetEnabled(IWorld world, Player player, int param, string? paramString)
    {
        if (!string.IsNullOrEmpty(paramString))
        {
            var config = world.Config.Hud;
            return paramString.ToLowerInvariant() switch
            {
                "stat_totals" => config.ShowStats.Value,
                "time" => config.ShowStats.Value,
                "coordinates" => player.Cheats.IsCheatActive(Cheats.CheatType.ShowPosition),
                "fps_counter" => config.ShowFPS.Value,
                _ => true 
            };
        }

        var cfg = world.Config.Hud;
        return param switch
        {
            0 => cfg.ShowStats.Value,
            1 => cfg.ShowStats.Value,
            2 => player.Cheats.IsCheatActive(Cheats.CheatType.ShowPosition),
            3 => cfg.ShowFPS.Value,
            _ => true 
        };
    }

    private static bool CheckPatchEmpty(StatusBarContext context, string? patch, bool empty)
    {
        if (string.IsNullOrEmpty(patch))
            return empty;
        var hasImage = context.World.ArchiveCollection.ImageRetriever.Get(patch, ResourceNamespace.Global) != null;
        return empty ? !hasImage : hasImage;
    }

    private static bool CheckStatPercent(int current, int total, int target, bool ge)
    {
        if (total <= 0)
            return ge ? 100 >= target : 100 < target;

        int pct = (current * 100) / total;
        return ge ? pct >= target : pct < target;
    }

    private static bool CheckPowerupDuration(Player player, PowerupType type, int seconds, bool ge)
    {
        if (type == PowerupType.None)
            return false;

        if (type == PowerupType.Strength || type == PowerupType.ComputerAreaMap)
        {
            int val = player.Inventory.IsPowerupActive(type) ? 1 : 0;
            return ge ? val >= seconds : val < seconds;
        }

        var p = player.Inventory.GetPowerup(type);
        int currentTicks = p?.Ticks ?? 0;
        int targetTicks = seconds * 35;
        return ge ? currentTicks >= targetTicks : currentTicks < targetTicks;
    }

    private static bool CheckPowerupPercent(Player player, PowerupType type, int targetPct, bool ge)
    {
        if (type == PowerupType.None)
            return false;

        if (type == PowerupType.Strength || type == PowerupType.ComputerAreaMap)
        {
            int binaryPct = player.Inventory.IsPowerupActive(type) ? 100 : 0;
            return ge ? binaryPct >= targetPct : binaryPct < targetPct;
        }

        var p = player.Inventory.GetPowerup(type);
        if (p == null)
            return ge ? 0 >= targetPct : 0 < targetPct;

        int maxTicks = type switch 
        {
            PowerupType.Invulnerable => 1050,
            PowerupType.Invisibility => 2100,
            PowerupType.IronFeet => 2100,
            PowerupType.LightAmp => 4200,
            _ => 1050
        };

        int durationPct = (int)((p.Ticks * 100L) / maxTicks);
        return ge ? durationPct >= targetPct : durationPct < targetPct;
    }

    private static int GetMaxAmount(EntityDefinition def, bool hasBackPack)
    {        
        var baseDef = Inventory.GetBaseInventoryDefinition(def);
        if (baseDef != null)
            def = baseDef;

        int max = def.Properties.Inventory.MaxAmount;
        if (hasBackPack && def.IsAmmo && def.Properties.Ammo.BackpackMaxAmount > max)
            max = def.Properties.Ammo.BackpackMaxAmount;
        return max;
    }
    
    private static PowerupType MapSbarPowerup(int sbarIndex)
    {
        return sbarIndex switch
        {
            0 => PowerupType.Invulnerable,
            1 => PowerupType.Strength,
            2 => PowerupType.Invisibility,
            3 => PowerupType.IronFeet,
            4 => PowerupType.ComputerAreaMap,
            5 => PowerupType.LightAmp,
            _ => PowerupType.None
        };
    }
}