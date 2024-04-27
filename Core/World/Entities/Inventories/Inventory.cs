using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Models;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Strings;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;

namespace Helion.World.Entities.Inventories;

public class Inventory
{
    public static readonly string AmmoClassName = "AMMO";
    public static readonly string BackPackBaseClassName = "BACKPACKITEM";
    public static readonly string WeaponClassName = "WEAPON";
    public static readonly string HealthClassName = "HEALTH";
    public static readonly string ArmorClassName = "ARMOR";
    public static readonly string BasicArmorBonusClassName = "BASICARMORBONUS";
    public static readonly string BasicArmorPickupClassName = "BASICARMORPICKUP";
    public static readonly string KeyClassName = "KEY";
    public static readonly string PowerupGiverClassName = "POWERUPGIVER";
    public static readonly string PowerupClassName = "POWERUP";
    public static readonly string RadSuitClassName = "RADSUIT";

    private static readonly List<string> PowerupEnumStringValues = GetPowerEnumValues();
    private static readonly Dictionary<string, string> PowerupLookup = new();
    private static readonly string PowerString = "Power";

    private readonly List<string> m_addedBaseNames = new();
    private readonly Comparison<InventoryItem> m_sortKeyCompare = new(CompareKeys);

    private static List<string> GetPowerEnumValues()
    {
        List<string> values = new();
        var enumValues = Enum.GetValues(typeof(PowerupType));
        foreach (PowerupType value in enumValues)
            values.Add(value.ToString());
        return values;
    }

    /// <summary>
    /// All of the items owned by the player that are not a special type of
    /// item (ex: weapons, which need more logic).
    /// </summary>
    private readonly Dictionary<string, InventoryItem> Items = new(StringComparer.OrdinalIgnoreCase);
    private LookupArray<InventoryItem?> ItemsById = new();
    private readonly List<InventoryItem> ItemList = new();
    private readonly List<InventoryItem> Keys = new();
    private readonly EntityDefinitionComposer EntityDefinitionComposer;
    private readonly Player Owner;

    /// <summary>
    /// All of the weapons owned by the player.
    /// </summary>
    public readonly Weapons Weapons;

    public readonly List<IPowerup> Powerups = new();

    public IPowerup? PowerupEffectColor { get; private set; }
    public IPowerup? PowerupEffectColorMap { get; private set; }

    public Inventory(Player owner, EntityDefinitionComposer composer)
    {
        Owner = owner;
        EntityDefinitionComposer = composer;
        Weapons = new Weapons(WorldStatic.World.GameInfo.WeaponSlots, composer);
    }

    public Inventory(PlayerModel playerModel, Player owner, EntityDefinitionComposer composer)
    {
        Owner = owner;
        EntityDefinitionComposer = composer;
        Weapons = new Weapons(WorldStatic.World.GameInfo.WeaponSlots, composer);

        for (int i = 0; i < playerModel.Inventory.Items.Count; i++)
        {
            InventoryItemModel item = playerModel.Inventory.Items[i];
            EntityDefinition? definition = EntityDefinitionComposer.GetByName(item.Name);
            if (definition != null)
            {
                InventoryItem inventoryItem = new(definition, item.Amount);
                AddItem(definition, inventoryItem);
            }
        }

        for (int i = 0; i < playerModel.Inventory.Weapons.Count; i++)
        {
            string weaponName = playerModel.Inventory.Weapons[i];
            EntityDefinition? definition = EntityDefinitionComposer.GetByName(weaponName);
            if (definition != null)
            {
                if (weaponName.Equals(playerModel.AnimationWeapon, StringComparison.OrdinalIgnoreCase))
                    Weapons.Add(definition, owner, WorldStatic.EntityManager, playerModel.AnimationWeaponFrame, playerModel.WeaponFlashFrame);
                else
                    Weapons.Add(definition, owner, WorldStatic.EntityManager);
            }
        }

        for (int i = 0; i < playerModel.Inventory.Powerups.Count; i++)
        {
            var powerupModel = playerModel.Inventory.Powerups[i];
            EntityDefinition? definition = EntityDefinitionComposer.GetByName(powerupModel.Name);
            if (definition == null)
                continue;

            Powerups.Add(new PowerupBase(owner, definition, powerupModel));
        }

        SortKeys();
        SetPriorityPowerupEffects();
    }

    public InventoryModel ToInventoryModel()
    {
        List<InventoryItemModel> inventoryItems = new();
        for (int i = 0; i < ItemList.Count; i++)
        {
            var item = ItemList[i];
            inventoryItems.Add(new InventoryItemModel()
            {
                Name = item.Definition.Name.ToString(),
                Amount = item.Amount
            });
        }

        List<PowerupModel> powerupModels = new();
        for (int i = 0; i < Powerups.Count; i++)
            powerupModels.Add(Powerups[i].ToPowerupModel());

        return new InventoryModel()
        {
            Items = inventoryItems,
            Weapons = Weapons.GetOwnedWeaponNames(),
            Powerups = powerupModels,
        };
    }

    public static string GetBaseInventoryName(EntityDefinition definition)
    {
        if (definition.BaseInventoryName != null)
            return definition.BaseInventoryName;

        int index = -1;
        for (int i = 0; i < definition.ParentClassNames.Count; i++)
        {
            if (definition.ParentClassNames[i].Equals(AmmoClassName, StringComparison.OrdinalIgnoreCase))
            {
                index = i;
                break;
            }
        }

        if (index > 0 && index < definition.ParentClassNames.Count - 1)
        {
            definition.BaseInventoryName = definition.ParentClassNames[index + 1];
            return definition.BaseInventoryName;
        }

        definition.BaseInventoryName = definition.Name;
        return definition.BaseInventoryName;
    }

    public static bool IsPowerup(EntityDefinition def) =>
        def.IsType(PowerupGiverClassName) ||
        !string.IsNullOrEmpty(def.Properties.Powerup.Type) ||
        def.IsType("Powerup") ||
        def.IsType("MapRevealer");

    public int ItemCount() => Items.Count;

    public bool IsPowerupActive(PowerupType type)
    {
        if (type == PowerupType.ComputerAreaMap)
            return HasItemOfClass("MapRevealer");

        return GetPowerup(type) != null;
    }

    public IPowerup? GetPowerup(PowerupType type)
    {
        for (int i = 0; i < Powerups.Count; i++)
        {
           if (Powerups[i].PowerupType == type)
                return Powerups[i];
        }

        return null;
    }

    public void RemovePowerup(IPowerup powerup)
    {
        Powerups.Remove(powerup);
        SetPriorityPowerupEffects();
    }

    public void ClearPowerups()
    {
        for (int i = 0; i < Powerups.Count; i++)
            Remove(Powerups[i].EntityDefinition.Name, 1);
        Powerups.Clear();
        PowerupEffectColor = null;
        PowerupEffectColorMap = null;
    }

    public void Tick()
    {
        bool setPriority = false;
        for (int i = 0; i < Powerups.Count; i++)
        {
            IPowerup powerup = Powerups[i];
            if (powerup.Tick(Owner) == InventoryTickStatus.Destroy)
            {
                Remove(powerup.EntityDefinition.Name, 1);
                Powerups.RemoveAt(i);
                i--;
                setPriority = true;
            }

            if (!powerup.DrawEffectActive && (ReferenceEquals(powerup, PowerupEffectColor) || ReferenceEquals(powerup, PowerupEffectColorMap)))
                setPriority = true;
        }

        if (setPriority)
            SetPriorityPowerupEffects();
    }

    public bool Add(EntityDefinition definition, int amount, EntityFlags? flags = null)
    {
        if (amount <= 0)
            return false;

        if (definition.IsType(PowerupGiverClassName))
            AddPowerupGiver(definition);
        else if (definition.IsType(PowerupClassName))
            AddPowerup(definition);        

        string name = GetBaseInventoryName(definition);
        int maxAmount = definition.Properties.Inventory.MaxAmount;
        if (definition.IsType(AmmoClassName) && HasItemOfClass(BackPackBaseClassName) && definition.Properties.Ammo.BackpackMaxAmount > maxAmount)
            maxAmount = definition.Properties.Ammo.BackpackMaxAmount;

        bool isKey = definition.IsType(KeyClassName);

        if (Items.TryGetValue(name, out InventoryItem? item))
        {
            // If the player is maxed on this item, return true if AlwaysPickup is set to remove from the world
            bool alwaysPickup = flags != null && flags.Value.InventoryAlwaysPickup;
            if (isKey || item.Amount >= maxAmount)
                return alwaysPickup;

            item.Amount += amount;
            if (item.Amount > maxAmount)
                item.Amount = maxAmount;

            return true;
        }
        else
        {
            var findDefinition = EntityDefinitionComposer.GetByName(name);
            if (findDefinition == null)
                return false;

            InventoryItem inventoryItem = new(findDefinition, isKey ? 1 : amount);
            AddItem(findDefinition, inventoryItem);

            if (isKey)
                SortKeys();
        }

        return true;
    }

    public bool SetAmount(EntityDefinition definition, int amount)
    {
        if (!ItemsById.TryGetValue(definition.Id, out InventoryItem? item) || amount < 0)
            return false;

        item.Amount = amount;
        return true;
    }

    private void AddPowerup(EntityDefinition definition)
    {
        if (definition.ParentClassNames.Count == 0)
            return;

        var powerupDef = EntityDefinitionComposer.GetByName(definition.ParentClassNames[^1]);
        if (powerupDef == null)
            return;

        int index = definition.Name.StartsWith(PowerString, StringComparison.OrdinalIgnoreCase) ? PowerString.Length : 0;
        DoGivePowerup(powerupDef, definition.Name.AsSpan(index));
    }

    private void AddPowerupGiver(EntityDefinition definition)
    {
        var typeKey = definition.Properties.Powerup.Type;
        if (!PowerupLookup.TryGetValue(typeKey, out var powerupName))
        {
            powerupName = PowerString + typeKey;
            PowerupLookup[typeKey] = powerupName;
        }

        var powerupDef = EntityDefinitionComposer.GetByName(powerupName);
        if (powerupDef == null)
            return;

        DoGivePowerup(powerupDef, typeKey);
    }

    private void DoGivePowerup(EntityDefinition powerupDef, ReadOnlySpan<char> type)
    {
        PowerupType powerupType = GetPowerupType(type);
        if (powerupType == PowerupType.None)
            return;

        IPowerup? existingPowerup = GetPowerup(powerupType);
        if (existingPowerup != null)
        {
            existingPowerup.Reset();
            SetPriorityPowerupEffects();
            return;
        }

        Powerups.Add(new PowerupBase(Owner, powerupDef, powerupType));
        SetPriorityPowerupEffects();
    }

    private static PowerupType GetPowerupType(ReadOnlySpan<char> type)
    {
        for (int i = 0; i < PowerupEnumStringValues.Count; i++)
        {
            if (MemoryExtensions.Equals(type, PowerupEnumStringValues[i], StringComparison.OrdinalIgnoreCase))
                return (PowerupType)i;
        }

        return PowerupType.None;
    }

    private void SetPriorityPowerupEffects()
    {
        PowerupEffectColor = GetPriorityPowerupColor(PowerupEffectType.Color, checkActive: true);
        PowerupEffectColorMap = GetPriorityPowerupColor(PowerupEffectType.ColorMap, checkActive: false);
    }

    private IPowerup? GetPriorityPowerupColor(PowerupEffectType effectType, bool checkActive)
    {
        IPowerup? color = null;
        int type = int.MaxValue;
        foreach(var powerup in Powerups)
        {
            if (powerup.EffectType == effectType && (!checkActive || powerup.DrawEffectActive) && (int)powerup.PowerupType < type)
            {
                color = powerup;
                type = (int)powerup.PowerupType;
            }
        }
        return color;
    }

    public void AddBackPackAmmo(EntityDefinitionComposer definitionComposer)
    {
        m_addedBaseNames.Clear();
        var ammoDefinitions = definitionComposer.GetAmmoDefinitions();
        foreach (EntityDefinition ammo in ammoDefinitions)
        {
            if (ammo.Properties.Ammo.BackpackAmount <= 0)
                continue;

            string baseName = GetBaseInventoryName(ammo);
            if (m_addedBaseNames.Contains(baseName))
                continue;

            Add(ammo, ammo.Properties.Ammo.BackpackAmount);
            m_addedBaseNames.Add(baseName);
        }
    }

    public void GiveAllAmmo(EntityDefinitionComposer definitionComposer)
    {
        EntityDefinition? backpackDef = definitionComposer.GetByName(BackPackBaseClassName);
        if (backpackDef != null)
            Add(backpackDef, 1);
        var ammoDefinitions = definitionComposer.GetAmmoDefinitions();
        foreach (var ammo in ammoDefinitions)
        {
            if (ammo.Name.EqualsIgnoreCase(AmmoClassName))
                continue;
            Add(ammo, Math.Max(ammo.Properties.Ammo.BackpackMaxAmount, ammo.Properties.Inventory.Amount));
        }
    }

    public void GiveAllKeys(EntityDefinitionComposer definitionComposer)
    {
        var keys = definitionComposer.GetKeyDefinitions();
        foreach (var key in keys)
        {
            if (key.EditorId.HasValue)
                Add(key, 1);
        }
    }

    public void ClearKeys()
    {
        for (int i = 0; i < Keys.Count; i++)
            RemoveItem(Keys[i].Definition.Name);
        Keys.Clear();
    }

    public void Clear()
    {
        Items.Clear();
        ItemsById.SetAll(null);

        ItemList.Clear();
        Keys.Clear();
        Weapons.Clear();
    }

    public bool HasItem(EntityDefinition definition) => ItemsById.TryGetValue(definition.Id, out _);

    public bool HasItem(string name) => Items.ContainsKey(name);

    public bool HasAnyItem(IEnumerable<string> names)
    {
        foreach (string name in names)
        {
            if (HasItem(name))
                return true;
        }

        return false;
    }

    public bool HasItemOfClass(string name)
    {
        foreach (var item in ItemList)
        {
            if (item.Definition.IsType(name))
                return true;
        }

        return false;
    }

    public int Amount(string name) => Items.TryGetValue(name, out var item) ? item.Amount : 0;

    public int Amount(EntityDefinition definition)
    {
        if (!ItemsById.TryGetValue(definition.Id, out InventoryItem? item))
            return 0;

        return item.Amount;
    }

    public void Remove(string name, int amount)
    {
        if (amount <= 0)
            return;

        if (Items.TryGetValue(name, out InventoryItem? item))
        {
            if (amount < item.Amount)
                item.Amount -= amount;
            else
                RemoveItem(name);

            if (item.Definition.IsType(KeyClassName))
            {
                Keys.Remove(item);
                SortKeys();
            }

            return;
        }

        // If we didn't find it, then it's possibly indexed in some other
        // data structure (ex: weapons).
        Weapons.Remove(name);
    }

    public List<InventoryItem> GetInventoryItems() => ItemList;
    public List<InventoryItem> GetKeys() => Keys;

    private void SortKeys() => Keys.Sort(m_sortKeyCompare);

    private static int CompareKeys(InventoryItem i1, InventoryItem i2)
    {
        if (!i1.Definition.EditorId.HasValue || !i2.Definition.EditorId.HasValue)
            return 1;

        return i1.Definition.EditorId.Value.CompareTo(i2.Definition.EditorId.Value);
    }

    private void RemoveItem(string name)
    {
        if (!Items.TryGetValue(name, out InventoryItem? item))
            return;

        ItemsById.Set(item.Definition.Id, null);
        Items.Remove(name);
        ItemList.Remove(item);
    }

    private void AddItem(EntityDefinition definition, InventoryItem item)
    {
        ItemsById.Set(definition.Id, item);
        Items[definition.Name] = item;
        ItemList.Add(item);
        if (definition.IsType(KeyClassName))
            Keys.Add(item);
    }
}
