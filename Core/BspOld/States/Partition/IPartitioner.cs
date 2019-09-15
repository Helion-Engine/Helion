using System.Collections.Generic;
using Helion.BspOld.Geometry;

namespace Helion.BspOld.States.Partition
{
    /// <summary>
    /// The interface for an object that performs line splitting.
    /// </summary>
    public interface IPartitioner
    {
        /// <summary>
        /// The states for this partitioner.
        /// </summary>
        PartitionStates States { get; }

        /// <summary>
        /// Loads the segments and the splitter.
        /// </summary>
        /// <param name="splitter">The splitter segment.</param>
        /// <param name="segments">The segments to split, which also should
        /// contain the splitter as well.</param>
        void Load(BspSegment? splitter, List<BspSegment> segments);

        /// <summary>
        /// Performs an splitting operation.
        /// </summary>
        void Execute();
    }
}