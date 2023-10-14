using System;
using System.Collections.Generic;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Entities.Definition.Properties;
using Helion.World.Entities.Definition.States;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities.Definition;

public class EntityDefinition
{
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

    public EntityFrame? HealFrame;

    public EntityDefinition? MonsterSpeciesDefinition;
    public EntityDefinition? BloodDefinition;

    private readonly HashSet<string> ParentClassLookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public EntityDefinition(int id, string name, int? editorId, List<string> parentClassNames)
    {
        Precondition(!string.IsNullOrEmpty(name), "Cannot have an entity definition with an empty name");

        Id = id;
        Name = name;
        EditorId = editorId;
        Properties = new EntityProperties();
        States = new EntityStates();
        ParentClassNames = parentClassNames;
        parentClassNames.ForEach(x => ParentClassLookup.Add(x));
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
}
