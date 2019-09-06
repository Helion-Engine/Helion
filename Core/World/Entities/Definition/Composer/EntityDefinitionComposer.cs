using System.Collections.Generic;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Decorate;
using Helion.Util;
using Helion.Util.Container;
using MoreLinq;
using NLog;

namespace Helion.World.Entities.Definition.Composer
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

        private readonly ArchiveCollection m_archiveCollection;
        private readonly AvailableIndexTracker m_indexTracker = new AvailableIndexTracker();
        private readonly Dictionary<CIString, EntityDefinition> m_definitions = new Dictionary<CIString, EntityDefinition>();
        private readonly Dictionary<int, EntityDefinition> m_editorNumToDefinition = new Dictionary<int, EntityDefinition>();

        public EntityDefinitionComposer(ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;
        }

        public EntityDefinition? this[CIString name] => GetByName(name);
        public EntityDefinition? this[int editorId] => GetByID(editorId);

        private static void ApplyActorFlagsAndProperties(EntityDefinition definition, ActorDefinition actorDefinition)
        {
            DefinitionFlagApplier.Apply(definition, actorDefinition.Flags, actorDefinition.FlagProperties);
            DefinitionPropertyApplier.Apply(definition, actorDefinition.Properties);
        }

        private EntityDefinition? GetByName(CIString name)
        {
            if (m_definitions.TryGetValue(name, out EntityDefinition definition))
                return definition;

            ActorDefinition? actorDefinition = m_archiveCollection.Definitions.Decorate[name];
            return actorDefinition != null ? ComposeNewDefinition(actorDefinition) : null;
        }
        
        private EntityDefinition? GetByID(int id)
        {
            if (m_editorNumToDefinition.TryGetValue(id, out EntityDefinition definition))
                return definition;
            
            ActorDefinition? actorDefinition = m_archiveCollection.Definitions.Decorate[id];
            return actorDefinition != null ? ComposeNewDefinition(actorDefinition) : null;
        }

        private bool CreateInheritanceOrderedList(ActorDefinition actorDef, out LinkedList<ActorDefinition> definitions)
        {
            definitions = new LinkedList<ActorDefinition>();
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
            definitions.AddFirst(m_archiveCollection.Definitions.Decorate[Constants.BaseActorClass]);
            
            return true;
        }

        private EntityDefinition? ComposeNewDefinition(ActorDefinition actorDefinition)
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
            EntityDefinition definition = new EntityDefinition(id, actorDefinition.Name, actorDefinition.EditorNumber);

            // While we can apply properties and flags sequentially without
            // remembering what came before it, that is not the case for the
            // states.
            definitions.ForEach(actorDef => ApplyActorFlagsAndProperties(definition, actorDef));
            DefinitionStateApplier.Apply(definition, definitions);

            // TODO: Check if well formed after everything was added.

            // TODO: Handle 'replaces'.

            m_definitions[definition.Name] = definition;
            if (definition.EditorId != null)
                m_editorNumToDefinition[definition.EditorId.Value] = definition;
            
            return definition;
        }
    }
}