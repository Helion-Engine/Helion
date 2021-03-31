using System.Collections.Generic;
using Helion.Util;
using Helion.World.Entities.Definition.Flags;
using Helion.World.Entities.Definition.Properties;
using Helion.World.Entities.Definition.States;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Entities.Definition
{
    public class EntityDefinition
    {
        public readonly int Id;
        public readonly int? EditorId;
        public readonly CIString Name;
        public EntityFlags Flags;
        public readonly EntityProperties Properties;
        public readonly EntityStates States;
        public readonly List<CIString> ParentClassNames;
        public EntityDefinition? MonsterSpeciesDefinition { get; set; }

        private readonly HashSet<CIString> ParentClassLookup = new HashSet<CIString>();

        public EntityDefinition(int id, CIString name, int? editorId, List<CIString> parentClassNames)
        {
            Precondition(!name.IsEmpty(), "Cannot have an entity definition with an empty name");
            
            Id = id;
            Name = name;
            EditorId = editorId;
            Properties = new EntityProperties();
            States = new EntityStates();
            ParentClassNames = parentClassNames;
            parentClassNames.ForEach(x => ParentClassLookup.Add(x));
        }

        /// <summary>
        /// Checks if the definition is a descendant or class of the type
        /// provided.
        /// </summary>
        /// <param name="className">The name of the class, which is case
        /// insensitive.</param>
        /// <returns>True if it is the type, false if not.</returns>
        public bool IsType(CIString className) => ParentClassLookup.Contains(className);
        
        /// <summary>
        /// Checks if the definition is a descendant or class of the type
        /// provided.
        /// </summary>
        /// <param name="definitionType">The enumeration type for common types.
        /// </param>
        /// <returns>True if it is the type, false if not.</returns>
        public bool IsType(EntityDefinitionType definitionType) => IsType(definitionType.ToString());
        
        public override string ToString() => $"{Name} (id = {Id}, editorId = {EditorId})";
    }
}