namespace Helion.Project.Component
{
    /// <summary>
    /// A unique ID that is specific to the project component. No other 
    /// project should have an identical ID.
    /// </summary>
    public struct ProjectComponentId
    {
        public uint Value { get; }

        public ProjectComponentId(uint value) => Value = value;
    }
}
