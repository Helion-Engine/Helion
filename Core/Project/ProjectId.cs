namespace Helion.Project
{
    /// <summary>
    /// A unique ID that is specific to the project. No other project should
    /// have an identical ID.
    /// </summary>
    public struct ProjectId
    {
        public uint Value { get; }

        public ProjectId(uint value) => Value = value;
    }
}
