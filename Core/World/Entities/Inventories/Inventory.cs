using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Models;
using Helion.Util;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;

namespace Helion.World.Entities.Inventories
{
    public class Inventory
    {
        public static readonly CIString AmmoClassName = "AMMO";
        public static readonly CIString BackPackBaseClassName = "BACKPACKITEM";
        public static readonly CIString WeaponClassName = "WEAPON";
        public static readonly CIString HealthClassName = "HEALTH";
        public static readonly CIString ArmorClassName = "ARMOR";
        public static readonly CIString BasicArmorBonusClassName = "BASICARMORBONUS";
        public static readonly CIString BasicArmorPickupClassName = "BASICARMORPICKUP";
        public static readonly CIString KeyClassName = "KEY";
        public static readonly CIString PowerupClassName = "POWERUPGIVER";

        /// <summary>
        /// All of the items owned by the player that are not a special type of
        /// item (ex: weapons, which need more logic).
        /// </summary>
        private readonly Dictionary<CIString, InventoryItem> Items = new Dictionary<CIString, InventoryItem>();
        private readonly List<InventoryItem> Keys = new List<InventoryItem>();
        private readonly EntityDefinitionComposer EntityDefinitionComposer;
        private readonly Player Owner;
        
        /// <summary>
        /// All of the weapons owned by the player.
        /// </summary>
        public readonly Weapons Weapons = new();

        public readonly List<IPowerup> Powerups = new();

        public IPowerup? PowerupEffectColor { get; private set; }
        public IPowerup? PowerupEffectColorMap { get; private set; }

        public Inventory(Player owner, EntityDefinitionComposer composer)
        {
            Owner = owner;
            EntityDefinitionComposer = composer;
        }

        public Inventory(PlayerModel playerModel, Player owner, EntityDefinitionComposer composer)
        {
            Owner = owner;
            EntityDefinitionComposer = composer;

            foreach (InventoryItemModel item in playerModel.Inventory.Items)
            {
                EntityDefinition? definition = EntityDefinitionComposer.GetByName(item.Name);
                if (definition != null)
                {
                    InventoryItem inventoryItem = new InventoryItem(definition, item.Amount);
                    Items.Add(definition.Name, inventoryItem);
                    if (definition.IsType(KeyClassName))
                        Keys.Add(inventoryItem);
                }
            }

            foreach (string weaponName in playerModel.Inventory.Weapons)
            {
                EntityDefinition? definition = EntityDefinitionComposer.GetByName(weaponName);
                if (definition != null)
                {
                    if (weaponName == playerModel.AnimationWeapon)
                        Weapons.Add(definition, owner, owner.World.EntityManager, playerModel.AnimationWeaponFrame, playerModel.WeaponFlashFrame);
                    else
                        Weapons.Add(definition, owner, owner.World.EntityManager);
                }
            }

            foreach (var powerupModel in playerModel.Inventory.Powerups)
            {
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
            List<InventoryItemModel> inventoryItems = new List<InventoryItemModel>();
            foreach (var item in Items)
            {
                inventoryItems.Add(new InventoryItemModel()
                {
                    Name = item.Value.Definition.Name.ToString(),
                    Amount = item.Value.Amount
                });
            }

            List<PowerupModel> powerupModels = new List<PowerupModel>();
            foreach (var powerup in Powerups)
                powerupModels.Add(powerup.ToPowerupModel());

            return new InventoryModel()
            {
                Items = inventoryItems,
                Weapons = Weapons.GetOwnedWeaponNames(),
                Powerups = powerupModels,
            };
        }

        public static CIString GetBaseInventoryName(EntityDefinition definition)
        {
            int index = definition.ParentClassNames.IndexOf(AmmoClassName);
            if (index > 0 && index < definition.ParentClassNames.Count - 1)
                return definition.ParentClassNames[index + 1];

            return definition.Name;
        }

        public static bool IsPowerup(EntityDefinition def) => def.IsType(PowerupClassName) || !string.IsNullOrEmpty(def.Properties.Powerup.Type);

        public bool IsPowerupActive(PowerupType type) => Powerups.Any(x => x.PowerupType == type);

        public IPowerup? GetPowerup(PowerupType type) => Powerups.FirstOrDefault(x => x.PowerupType == type);

        public void ClearPowerups()
        {
            foreach (IPowerup powerup in Powerups)
                Remove(powerup.EntityDefinition.Name, 1);
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

            // TODO test hack until A_GiveInventory and Pickup states are implemented
            bool overridehack = false;
            if (definition.Name == "BERSERK")
            {
                definition.Properties.Powerup.Type = "Strength";
                overridehack = true;
                Weapon? fist = Owner.Inventory.Weapons.GetWeapon("FIST");
                if (fist != null)
                    Owner.ChangeWeapon(fist);
                if (Owner.Health < 100)
                    Owner.Health = 100;
            }

            if (definition.IsType(PowerupClassName) || overridehack)
                AddPowerup(definition);

            CIString name = GetBaseInventoryName(definition);
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
                EntityDefinition? findDefinition = EntityDefinitionComposer.GetByName(name);
                if (findDefinition == null)
                    return false;

                InventoryItem inventoryItem = new InventoryItem(findDefinition, isKey ? 1 : amount);
                Items[name] = inventoryItem;

                if (isKey)
                {
                    Keys.Add(inventoryItem);
                    SortKeys();
                }
            }

            return true;
        }

        private void AddPowerup(EntityDefinition definition)
        {
            EntityDefinition? powerupDef = EntityDefinitionComposer.GetByName("Power" + definition.Properties.Powerup.Type);
            if (powerupDef == null)
                return;

            PowerupType powerupType = GetPowerupType(definition.Properties.Powerup.Type);
            if (powerupType == PowerupType.None)
                return;

            IPowerup? existingPowerup = GetPowerup(powerupType);
            if (existingPowerup != null)
            {
                existingPowerup.Reset();
                return;
            }

            Powerups.Add(new PowerupBase(Owner, powerupDef, powerupType));
            SetPriorityPowerupEffects();
        }

        private static PowerupType GetPowerupType(string type)
        {
            Array values = Enum.GetValues(typeof(PowerupType));        

            foreach (Enum value in values)
            {
                if (value.ToString().Equals(type, StringComparison.OrdinalIgnoreCase))
                    return (PowerupType)value;
            }

            return PowerupType.None;
        }

        private void SetPriorityPowerupEffects()
        {
            PowerupEffectColor = Powerups.Where(x => x.EffectType == PowerupEffectType.Color && x.DrawEffectActive).OrderBy(y => (int)y.PowerupType).FirstOrDefault();
            PowerupEffectColorMap = Powerups.Where(x => x.EffectType == PowerupEffectType.ColorMap).OrderBy(y => (int)y.PowerupType).FirstOrDefault();
        }

        public void AddBackPackAmmo(EntityDefinitionComposer definitionComposer)
        {
            HashSet<CIString> addedBaseNames = new HashSet<CIString>();
            List<EntityDefinition> ammoDefinitions = GetAmmoTypes(definitionComposer).Where(x => x.Properties.Ammo.BackpackAmount > 0).ToList();
            foreach (EntityDefinition ammo in ammoDefinitions)
            {
                CIString baseName = GetBaseInventoryName(ammo);
                if (addedBaseNames.Contains(baseName))
                    continue;

                Add(ammo, ammo.Properties.Ammo.BackpackAmount);
                addedBaseNames.Add(baseName);
            }
        }

        public void GiveAllAmmo(EntityDefinitionComposer definitionComposer)
        {
            EntityDefinition? backpackDef = definitionComposer.GetByName(BackPackBaseClassName);
            if (backpackDef != null)
                Add(backpackDef, 1);
            List<EntityDefinition> ammoDefinitions = GetAmmoTypes(definitionComposer).ToList();
            foreach (EntityDefinition ammo in ammoDefinitions)
                Add(ammo, Math.Max(ammo.Properties.Ammo.BackpackMaxAmount, ammo.Properties.Inventory.Amount));
        }

        public void GiveAllKeys(EntityDefinitionComposer definitionComposer)
        {
            List<EntityDefinition> keys = definitionComposer.GetEntityDefinitions().Where(x => x.IsType(KeyClassName) && x.EditorId.HasValue).ToList();
            keys.ForEach(x => Add(x, 1));
        }

        public void ClearKeys()
        {
            Keys.ForEach(x => Items.Remove(x.Definition.Name));
            Keys.Clear();
        }

        public void Clear()
        {
            Items.Clear();
            Keys.Clear();
        }

        public bool HasItem(CIString name) => Items.ContainsKey(name);

        public bool HasAnyItem(IEnumerable<CIString> names) => names.Any(x => HasItem(x));

        public bool HasItemOfClass(CIString name) => Items.Any(x => x.Value.Definition.IsType(name));
        
        public int Amount(CIString name) => Items.TryGetValue(name, out var item) ? item.Amount : 0;

        public void Remove(CIString name, int amount)
        {
            if (amount <= 0)
                return;
            
            if (Items.TryGetValue(name, out InventoryItem? item))
            {
                if (amount < item.Amount)
                    item.Amount -= amount;
                else
                    Items.Remove(name);

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

        public List<InventoryItem> GetInventoryItems() => Items.Values.ToList();
        public List<InventoryItem> GetKeys() => Keys;

        public void RemoveAll(CIString name)
        {
            Items.Remove(name);
        }

        private static IEnumerable<EntityDefinition> GetAmmoTypes(EntityDefinitionComposer definitionComposer)
        {
            return definitionComposer.GetEntityDefinitions().Where(x => x.IsType(AmmoClassName));
        }

        private void SortKeys()
        {
            Keys.Sort((i1, i2) => i1.Definition.EditorId.Value.CompareTo(i2.Definition.EditorId.Value));
        }
    }
}