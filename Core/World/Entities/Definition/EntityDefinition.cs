using System;
using System.Collections.Generic;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Entities.Definition.Properties;
using Helion.World.Entities.Definition.States;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities.Definition;

public class EntityDefinition
{
    private static readonly Dictionary<string, EntityType> ClassToType = new(StringComparer.OrdinalIgnoreCase)
    {
        { "BulletPuff", EntityType.BulletPuff },
        { "Blood", EntityType.Blood },
        { "AmbientSound", EntityType.AmbientSound },
    };

    public static readonly EntityDefinition Default = new(-1, "DEFAULT-NULL", null, []);

    public int Id;
    public int? EditorId;
    public string Name;
    public EntityFlags Flags;
    public EntityProperties Properties;
    public readonly EntityStates States;
    public readonly IList<string> ParentClassNames;
    public bool IsInventory;
    public EntityType Type;
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
    public bool IgnoreVanillaSpriteLookup;

    public EntityFrame? HealFrame;

    public EntityDefinition? MonsterSpeciesDefinition;
    public EntityDefinition BloodDefinition = Default;

    private readonly HashSet<string> ParentClassLookup = new(StringComparer.OrdinalIgnoreCase);

    public EntityDefinition(int id, string name, int? editorId, IList<string> parentClassNames)
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
        IsInventory = IsType(EntityDefinitionType.Inventory);
        if (ClassToType.TryGetValue(Name, out var type))
            Type = type;
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
