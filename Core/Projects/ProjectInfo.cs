using System;

namespace Helion.Projects
{
    /// <summary>
    /// A container of the project metadata.
    /// </summary>
    public class ProjectInfo
    {
        /// <summary>
        /// The name of this project. This does not need to be unique but it
        /// is recommended to strive for that.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The version this project is at.
        /// </summary>
        public Version Version { get; }

        public ProjectInfo(string name, Version version)
        {
            Name = name;
            Version = version;
        }
    }
}
