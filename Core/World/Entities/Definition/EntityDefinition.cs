using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Dehacked;
using Helion.Util.Extensions;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Entities.Definition.Properties;
using Helion.World.Entities.Definition.States;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities.Definition;

public class EntityDefinition
{
    public static readonly EntityDefinition Default = new(-1, "DEFAULT-NULL", null, []);

    public int Id { get; set; }
    public int? EditorId { get; set; }
    public readonly string Name;
    public EntityFlags Flags;
    public readonly EntityProperties Properties;
    public readonly EntityStates States;
    public readonly List<string> ParentClassNames;
    public readonly bool IsBulletPuff;
    public bool IsInventory;
    public int? SpawnState;
    public int? MissileState;
    public int? MeleeState;
    public int? DeathState;
    public int? XDeathState;
    public int? RaiseState;
    public int? SeeState;
    public int? PainState;
    public int? HealState;
    public string? BaseInventoryName;
    public string DehackedName = string.Empty;
    public bool DefinitionSet;

    public EntityFrame? HealFrame;

    public EntityDefinition? MonsterSpeciesDefinition;
    public EntityDefinition BloodDefinition = Default;

    private readonly HashSet<string> ParentClassLookup = new(StringComparer.OrdinalIgnoreCase);

    public EntityDefinition(int id, string name, int? editorId, List<string> parentClassNames)
    {
        Precondition(!string.IsNullOrEmpty(name), "Cannot have an entity definition with an empty name");

        Id = id;
        Name = name;
        EditorId = editorId;
        Properties = new EntityProperties();
        States = new EntityStates();
        ParentClassNames = parentClassNames;
        foreach (var parentClass in ParentClassNames)
            ParentClassLookup.Add(parentClass);
        IsBulletPuff = Name.EqualsIgnoreCase("BulletPuff");
        IsInventory = IsType(EntityDefinitionType.Inventory);
    }

    /// <summary>
    /// Checks if the definition is a descendant or class of the type
    /// provided.
    /// </summary>
    /// <param name="className">The name of the class, which is case
    /// insensitive.</param>
    /// <returns>True if it is the type, false if not.</returns>
    public bool IsType(string className) => ParentClassLookup.Contains(className);

    public override string ToString() => $"{(string.IsNullOrEmpty(DehackedName) ? Name : DehackedName)} (id = {Id}, editorId = {EditorId})";

    public void CloneAmmo(EntityDefinition definition)
    {
        ParentClassLookup.Clear();
        ParentClassNames.Clear();

        ParentClassNames.AddRange(definition.ParentClassNames);

        foreach (var parentClass in definition.ParentClassNames)
            ParentClassLookup.Add(parentClass);

        Properties.Inventory.Amount = definition.Properties.Inventory.Amount;
        Properties.Inventory.MaxAmount = definition.Properties.Inventory.MaxAmount;
        Properties.Ammo = definition.Properties.Ammo;
    }
}
