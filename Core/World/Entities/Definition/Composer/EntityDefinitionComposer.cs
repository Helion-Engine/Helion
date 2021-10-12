using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Decorate;
using Helion.Util;
using Helion.Util.Container;
using NLog;

namespace Helion.World.Entities.Definition.Composer;

/// <summary>
/// Responsible for building up entity definitions from existing decorate
/// definitions.
/// </summary>
/// <remarks>
/// We cannot use the decorate definitions directly because they may be
/// missing important data we need, and the performance hit of checking
/// for nulls or missing values frequently would add up. Further, they
/// lack the information from parent to child (they only hold their own
/// data and leave it up to someone else to piece everything together).
/// All of the inheritance data needs to be compiled into one final class.
/// This type is the one responsible for that.
/// </remarks>
public class EntityDefinitionComposer
{
    private const int RecursiveDefinitionOverflow = 10000;
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly ArchiveCollection m_archiveCollection;
    private readonly AvailableIndexTracker m_indexTracker = new AvailableIndexTracker();
    private readonly Dictionary<string, EntityDefinition> m_definitions = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<EntityDefinition> m_listDefinitions = new List<EntityDefinition>();
    private readonly Dictionary<int, EntityDefinition> m_editorNumToDefinition = new Dictionary<int, EntityDefinition>();

    public EntityDefinitionComposer(ArchiveCollection archiveCollection)
    {
        m_archiveCollection = archiveCollection;
    }

    public void LoadAllDefinitions()
    {
        List<ActorDefinition> actorDefinitions = m_archiveCollection.Definitions.Decorate.GetActorDefinitions();
        var normalDefinitions = actorDefinitions.Where(x => string.IsNullOrEmpty(x.Replaces));
        var replaceDefinitions = actorDefinitions.Where(x => !string.IsNullOrEmpty(x.Replaces));

        foreach (ActorDefinition definition in normalDefinitions)
            GetByName(definition.Name);

        foreach (ActorDefinition definition in replaceDefinitions)
            GetByName(definition.Name);
    }

    public EntityDefinition? GetByName(string name)
    {
        if (m_definitions.TryGetValue(name, out EntityDefinition? definition))
            return definition;

        ActorDefinition? actorDefinition = m_archiveCollection.Definitions.Decorate[name];
        if (actorDefinition == null)
            return null;

        definition = ComposeNewDefinition(actorDefinition);
        if (definition == null)
            return null;

        m_listDefinitions.Add(definition);
        if (!string.IsNullOrEmpty(actorDefinition.Replaces))
            m_definitions[actorDefinition.Replaces] = definition;
        else
            m_definitions[definition.Name] = definition;

        if (definition.EditorId != null)
            m_editorNumToDefinition[definition.EditorId.Value] = definition;

        return definition;
    }

    public EntityDefinition? GetNewDefinition(string name)
    {
        ActorDefinition? actorDefinition = m_archiveCollection.Definitions.Decorate[name];
        return actorDefinition != null ? ComposeNewDefinition(actorDefinition) : null;
    }

    public void Add(EntityDefinition definition)
    {
        definition.Id = m_indexTracker.Next();
        m_definitions[definition.Name] = definition;
        if (definition.EditorId != null)
            m_editorNumToDefinition[definition.EditorId.Value] = definition;
    }

    public IList<EntityDefinition> GetEntityDefinitions() => m_listDefinitions.AsReadOnly();

    public EntityDefinition? GetByID(int id)
    {
        if (m_editorNumToDefinition.TryGetValue(id, out EntityDefinition? definition))
            return definition;

        ActorDefinition? actorDefinition = m_archiveCollection.Definitions.Decorate[id];
        return actorDefinition != null ? ComposeNewDefinition(actorDefinition) : null;
    }

    public void ChangeEntityEditorID(EntityDefinition definition, int newID)
    {
        definition.EditorId = newID;
        m_editorNumToDefinition[newID] = definition;
    }

    private static void ApplyFlagsAndPropertiesFrom(EntityDefinition definition, IList<ActorDefinition> parents)
    {
        // This entire function is needed to support Skip_Super. Thanks
        // ZDoom! In short, skip super is designed to ignore its parent
        // flags/properties, but keep its states. To do this, we create
        // a list of actors and then only take the elements onwards from
        // the latest actor that has Skip_Super on it (or if none have it,
        // then we have the entire list to apply as per normal).
        //
        // The state applier will take care of the states for us in another
        // function, while we take care of the property/flag stuff here to
        // accomodate this very annoying 'feature'.
        List<ActorDefinition> parentsToApply = new List<ActorDefinition>();
        foreach (ActorDefinition parent in parents)
        {
            if (parent.FlagProperties.SkipSuper ?? false)
                parentsToApply.Clear();

            parentsToApply.Add(parent);
        }

        parentsToApply.ForEach(parent => ApplyActorFlagsAndProperties(definition, parent));
    }

    private static void ApplyActorFlagsAndProperties(EntityDefinition definition, ActorDefinition actorDefinition)
    {
        DefinitionFlagApplier.Apply(definition, actorDefinition.Flags, actorDefinition.FlagProperties);
        DefinitionPropertyApplier.Apply(definition, actorDefinition.Properties);
    }

    private bool CreateInheritanceOrderedList(ActorDefinition actorDef, out IList<ActorDefinition> actorDefinitions)
    {
        actorDefinitions = Array.Empty<ActorDefinition>();
        LinkedList<ActorDefinition> definitions = new LinkedList<ActorDefinition>();
        definitions.AddLast(actorDef);

        ActorDefinition current = actorDef;
        while (current.Parent != null)
        {
            ActorDefinition? parent = m_archiveCollection.Definitions.Decorate[current.Parent];
            if (parent == null)
            {
                Log.Warn("Cannot find entity definition for parent class '{0}'", current.Parent);
                return false;
            }

            definitions.AddFirst(parent);
            current = parent;

            if (definitions.Count > RecursiveDefinitionOverflow)
            {
                Log.Warn("Infinite recursive parent cycle detected, possible offender: {0}", current.Name);
                return false;
            }
        }

        // The base actor must always come first, but we can do this at
        // the very end. It is a critical error for this not to exist.
        ActorDefinition? baseActorClass = m_archiveCollection.Definitions.Decorate[Constants.BaseActorClass];
        if (baseActorClass == null)
            throw new HelionException($"Missing base decorate actor definition {Constants.BaseActorClass}");

        definitions.AddFirst(baseActorClass);
        actorDefinitions = definitions.ToArray();
        return true;
    }

    private EntityDefinition? ComposeNewDefinition(ActorDefinition actorDefinition)
    {
        // We build it up where the front of the list corresponds to the
        // base definition, and each one after that is a child of the
        // previous until we reach the bottom level. This means if we have
        // A inherits from B inherits from C, the list is: [C, B, A]
        if (!CreateInheritanceOrderedList(actorDefinition, out IList<ActorDefinition> definitions))
        {
            Log.Warn("Unable to create entity '{0}' due to errors with the actor or parents", actorDefinition.Name);
            return null;
        }

        int id = m_indexTracker.Next();
        List<string> parentClassNames = definitions.Select(d => d.Name).ToList();
        EntityDefinition definition = new EntityDefinition(id, actorDefinition.Name, actorDefinition.EditorNumber, parentClassNames);

        ApplyFlagsAndPropertiesFrom(definition, definitions);
        DefinitionStateApplier.Apply(m_archiveCollection.Definitions.EntityFrameTable, definition, definitions);

        // TODO: Check if well formed after everything was added.

        // TODO: Handle 'replaces'.


        return definition;
    }
}
