using System.Collections.Generic;
using System.Linq;
using Helion.Resource;
using Helion.Resource.Definitions.Decorate;
using Helion.Util;
using Helion.Util.Container;
using NLog;

namespace Helion.Worlds.Entities.Definition.Composer
{
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

        private readonly Resources m_resources;
        private readonly AvailableIndexTracker m_indexTracker = new();
        private readonly Dictionary<CIString, EntityDefinition> m_definitions = new();
        private readonly List<EntityDefinition> m_listDefinitions = new();
        private readonly Dictionary<int, EntityDefinition> m_editorNumToDefinition = new();

        public EntityDefinitionComposer(Resources resources)
        {
            m_resources = resources;

            // Load all definitions - Even if a map doesn't load them there are cases where they are needed (backpack ammo etc)
            foreach (ActorDefinition definition in resources.Decorate.GetActorDefinitions())
                ComposeNewDefinition(m_resources, definition);
        }

        public EntityDefinition? GetByName(CIString name)
        {
            if (m_definitions.TryGetValue(name, out EntityDefinition? definition))
                return definition;

            ActorDefinition? actorDefinition = m_resources.Decorate[name];
            return actorDefinition != null ? ComposeNewDefinition(m_resources, actorDefinition) : null;
        }

        public IList<EntityDefinition> GetEntityDefinitions() => m_listDefinitions.AsReadOnly();

        public EntityDefinition? GetByID(int id)
        {
            if (m_editorNumToDefinition.TryGetValue(id, out EntityDefinition? definition))
                return definition;

            ActorDefinition? actorDefinition = m_resources.Decorate[id];
            return actorDefinition != null ? ComposeNewDefinition(m_resources, actorDefinition) : null;
        }

        private static void ApplyFlagsAndPropertiesFrom(EntityDefinition definition, LinkedList<ActorDefinition> parents)
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
            List<ActorDefinition> parentsToApply = new();
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

        private bool CreateInheritanceOrderedList(ActorDefinition actorDef, out LinkedList<ActorDefinition> definitions)
        {
            definitions = new LinkedList<ActorDefinition>();
            definitions.AddLast(actorDef);

            ActorDefinition current = actorDef;
            while (current.Parent != null)
            {
                ActorDefinition? parent = m_resources.Decorate[current.Parent];
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
            ActorDefinition? baseActorClass = m_resources.Decorate[Constants.BaseActorClass];
            if (baseActorClass == null)
                throw new HelionException($"Missing base decorate actor definition {Constants.BaseActorClass}");

            definitions.AddFirst(baseActorClass);

            return true;
        }

        private EntityDefinition? ComposeNewDefinition(Resources resources, ActorDefinition actorDefinition)
        {
            // We build it up where the front of the list corresponds to the
            // base definition, and each one after that is a child of the
            // previous until we reach the bottom level. This means if we have
            // A inherits from B inherits from C, the list is: [C, B, A]
            if (!CreateInheritanceOrderedList(actorDefinition, out LinkedList<ActorDefinition> definitions))
            {
                Log.Warn("Unable to create entity '{0}' due to errors with the actor or parents", actorDefinition.Name);
                return null;
            }

            int id = m_indexTracker.Next();
            List<CIString> parentClassNames = definitions.Select(d => d.Name).ToList();
            EntityDefinition definition = new(id, actorDefinition.Name, actorDefinition.EditorNumber, parentClassNames);

            ApplyFlagsAndPropertiesFrom(definition, definitions);
            DefinitionStateApplier.Apply(resources, definition, definitions);

            // TODO: Check if well formed after everything was added.

            // TODO: Handle 'replaces'.

            m_listDefinitions.Add(definition);
            m_definitions[definition.Name] = definition;
            if (definition.EditorId != null)
                m_editorNumToDefinition[definition.EditorId.Value] = definition;

            return definition;
        }
    }
}