using System;
using System.Linq;
using Helion.Maps;
using Helion.Util.Container.Linkable;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using Helion.World.Entities;
using MoreLinq.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.World.Blockmaps
{
    /// <summary>
    /// A conversion of a map into a grid structure whereby things in certain
    /// blocks only will check the blocks they are in for collision detection
    /// or line intersections to optimize computational cost.
    /// </summary>
    public class Blockmap
    {
        public readonly Box2D Bounds;
        private readonly UniformGrid<Block> m_blocks;
        
        /// <summary>
        /// Creates a blockmap grid for the map provided.
        /// </summary>
        /// <param name="map">The map to make the grid for.</param>
        public Blockmap(IMap map)
        {
            Bounds = FindMapBoundingBox(map);
            m_blocks = new UniformGrid<Block>(Bounds);
            SetBlockCoordinates();
            AddLinesToBlocks(map);
        }
        
        /// <summary>
        /// Performs iteration over the blocks for some line.
        /// </summary>
        /// <param name="seg">The line segment to check.</param>
        /// <param name="func">The callback for whether iteration should be
        /// continued or not.</param>
        /// <returns>True if iteration was halted due to the return value of
        /// the provided function, false if not.</returns>
        public bool Iterate(Seg2DBase seg, Func<Block, GridIterationStatus> func) 
        {
            return m_blocks.Iterate(seg, func);
        }

        /// <summary>
        /// Performs iteration over the blocks at the entity position.
        /// </summary>
        /// <param name="entity">The entity to iterate from.</param>
        /// <param name="func">The callback for whether iteration should be
        /// continued or not.</param>
        /// <returns>True if iteration was halted due to the return value of
        /// the provided function, false if not.</returns>
        public bool Iterate(Entity entity, Func<Block, GridIterationStatus> func)
        {
            // TODO: Why not store the blocks with the entity in the internal
            //       list and just iterate over that? May be faster...
            return Iterate(entity.Box.To2D(), func);
        }

        /// <summary>
        /// Performs iteration over the blocks at the box position.
        /// </summary>
        /// <param name="box">The box area to iterate with.</param>
        /// <param name="func">The callback for whether iteration should be
        /// continued or not.</param>
        /// <returns>True if iteration was halted due to the return value of
        /// the provided function, false if not.</returns>
        public bool Iterate(Box2D box, Func<Block, GridIterationStatus> func)
        {
            return m_blocks.Iterate(box, func);
        }

        /// <summary>
        /// Links an entity to the grid.
        /// </summary>
        /// <param name="entity">The entity to link. Should be inside the map.
        /// </param>
        public void Link(Entity entity)
        {
            Precondition(entity.BlockmapNodes.Empty(), "Forgot to unlink entity from blockmap");

            m_blocks.Iterate(entity.Box.To2D(), BlockLinkFunc);
            
            GridIterationStatus BlockLinkFunc(Block block)
            {
                LinkableNode<Entity> blockEntityNode = block.Entities.Add(entity);
                entity.BlockmapNodes.Add(blockEntityNode);
                return GridIterationStatus.Continue;
            }
        }

        private static Box2D FindMapBoundingBox(IMap map)
        {
            Box2D startBox = map.Lines.First().Segment.Box;
            return map.Lines.Select(line => line.Segment.Box)
                            .Aggregate(startBox, (accumBox, lineBox) => Box2D.Combine(accumBox, lineBox));
        }

        private void SetBlockCoordinates()
        {
            // Unfortunately we have to do it this way because we can't get
            // constraining for generic parameters, so the UniformGrid will
            // not be able to do this for us via it's constructor. 
            int index = 0;
            for (int y = 0; y < m_blocks.Height; y++)
                for (int x = 0; x < m_blocks.Width; x++)
                    m_blocks[index++].SetCoordinate(x, y);
        }

        private void AddLinesToBlocks(IMap map)
        {
            map.Lines.ForEach(line =>
            {
                m_blocks.Iterate(line.Segment, block =>
                {
                    block.Lines.Add(line);
                    return GridIterationStatus.Continue;
                });
            });
        }
    }
}