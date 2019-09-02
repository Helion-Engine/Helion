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
        public readonly EntityFlags Flags = new EntityFlags();
        public readonly EntityProperties Properties = new EntityProperties();
        public readonly EntityStates States = new EntityStates();

        public EntityDefinition(int id, CIString name, int? editorId)
        {
            Precondition(!name.Empty, "Cannot have an entity definition with an empty name");
            
            Id = id;
            Name = name;
            EditorId = editorId;
        }
        
        public override string ToString() => $"{Name} (id = {Id}, editorId = {EditorId})";
    }
}