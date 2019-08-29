using Helion.Resources.Definitions.Decorate.Flags;
using Helion.Resources.Definitions.Decorate.Properties;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Definitions.Decorate
{
    public class ActorDefinition
    {
        public readonly CIString Name;
        public readonly CIString? Parent;
        public readonly CIString? ReplacesClass;
        public readonly int? EditorNumber;
        public readonly ActorFlags Flags = new ActorFlags();
        public readonly ActorProperties Properties = new ActorProperties();
        public readonly ActorStates States = new ActorStates();

        public ActorDefinition(CIString name, CIString? parent, CIString? replacesClass, int? editorNumber)
        {
            Precondition(name.Length > 0, "Cannot have an empty actor definition name");
            Precondition(parent == null || parent.Length > 0, "Cannot have an empty actor parent name");
            Precondition(replacesClass == null || replacesClass.Length > 0, "Cannot have an empty actor replaces name");
            
            Name = name;
            Parent = parent;
            ReplacesClass = replacesClass;
            EditorNumber = editorNumber;
        }

        public bool IsValid()
        {
            if (EditorNumber != null && EditorNumber < 0)
                return false;
            
            // TODO: Make sure the frames are fine.

            foreach ((CIString name, int offset) in States.Labels)
            {
                if (name.Empty)
                    return false;
                if (offset < 0 || offset >= States.Labels.Count)
                    return false;
            }
            
            return true;
        }
    }
}