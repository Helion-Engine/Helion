using System.Linq;
using Helion.Maps;
using Helion.Util.Container.Linkable;
using Helion.Util.Geometry;
using Helion.World.Entities;
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
        private readonly UniformGrid<Block> blocks;
        
        /// <summary>
        /// Creates a blockmap grid for the map provided.
        /// </summary>
        /// <param name="map">The map to make the grid for.</param>
        public Blockmap(Map map)
        {
            Box2D mapBounds = FindMapBoundingBox(map);
            blocks = new UniformGrid<Block>(mapBounds);
            AddLinesToBlocks(map);
        }

        /// <summary>
        /// Links an entity to the grid.
        /// </summary>
        /// <param name="entity">The entity to link. Should be inside the map.
        /// </param>
        public void Link(Entity entity)
        {
            Precondition(entity.BlockmapNodes.Count == 0, "Forgot to unlink entity from blockmap");

            blocks.Iterate(entity.Box.To2D(), BlockLinkFunc);
            
            GridIterationStatus BlockLinkFunc(Block block)
            {
                LinkableNode<Entity> blockEntityNode = block.Entities.Add(entity);
                entity.BlockmapNodes.Add(blockEntityNode);
                return GridIterationStatus.Continue;
            }
        }

        private static Box2D FindMapBoundingBox(Map map)
        {
            Box2D startBox = map.Lines.First().Segment.Box;
            return map.Lines.Select(line => line.Segment.Box)
                            .Aggregate(startBox, (accumBox, lineBox) => Box2D.Combine(accumBox, lineBox));
        }

        private void AddLinesToBlocks(Map map)
        {
            map.Lines.ForEach(line =>
            {
                blocks.Iterate(line.Segment, block =>
                {
                    block.Lines.Add(line);
                    return GridIterationStatus.Continue;
                });
            });
        }
    }
}