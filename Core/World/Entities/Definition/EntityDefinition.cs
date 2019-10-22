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
        public readonly EntityFlags Flags;
        public readonly EntityProperties Properties;
        public readonly EntityStates States;

        public EntityDefinition(int id, CIString name, int? editorId)
        {
            Precondition(!name.Empty, "Cannot have an entity definition with an empty name");
            
            Id = id;
            Name = name;
            EditorId = editorId;

            Flags = new EntityFlags();
            Properties = new EntityProperties();
            States = new EntityStates();
        }

        //TODO hopefully temporary
        public EntityDefinition(EntityDefinition def)
        {
            Id = def.Id;
            Name = def.Name;
            EditorId = def.EditorId;

            Flags = new EntityFlags(def.Flags);
            Properties = def.Properties;
            States = def.States;
        }
        
        public override string ToString() => $"{Name} (id = {Id}, editorId = {EditorId})";
    }
}