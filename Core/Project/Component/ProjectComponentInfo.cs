using System;

namespace Helion.Project
{
    /// <summary>
    /// A container of the project component metadata.
    /// </summary>
    public class ProjectComponentInfo
    {
        /// <summary>
        /// The name of this project component. This need not be unique, but is
        /// recommended.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The version this project component is at.
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// The location of where this data was retrieved from.
        /// </summary>
        public string Uri { get; }

        public ProjectComponentInfo(string name, Version version, string uri)
        {
            Name = name;
            Version = version;
            Uri = uri;
        }
    }
}
