using System;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Atlas;

/// <summary>
/// A binary tree node which either represents a cut (and thus has two
/// children) or is a leaf node which represents a used up space.
/// </summary>
/// <remarks>
/// It uses a binary tree structure to allow for O(lg n) insertion and
/// deletion times. Also note that this atlas grows from the origin as if
/// it were a cartesian plane, so `left` and `right` when there is a
/// vertical split is actually `bottom` and `top` (implying it grows up to
/// the right).
/// </remarks>
public class AtlasNode
{
    /// <summary>
    /// The location in the owning atlas.
    /// </summary>
    public Box2I Location;

    /// <summary>
    /// The parent owner of this node, if any. Used in recursive ascent
    /// when setting parent values.
    /// </summary>
    private readonly AtlasNode? m_parent;

    /// <summary>
    /// The left node (or the bottom node).
    /// </summary>
    private AtlasNode? m_left;

    /// <summary>
    /// The right node (or top node).
    /// </summary>
    private AtlasNode? m_right;

    /// <summary>
    /// A cached value which knows the maximum dimensions from either child
    /// for this node. This only contains the max values, so if the left is
    /// (5, 8) and the right is (6, 2) then this contains (6, 8). If this
    /// is a leaf node that is not occupied, it equals the dimension. If
    /// this is an occupied leaf node, it is (0, 0).
    /// </summary>
    private Dimension m_maxAvailableDimensions;

    /// <summary>
    /// True if this node is a leaf node that represents an allocated space
    /// or also true if this is a parent node and every child is occupied.
    /// </summary>
    private bool m_occupied;

    /// <summary>
    /// A convenience property for iteration over both children.
    /// </summary>
    private AtlasNode?[] Children => new[] { m_left, m_right };

    /// <summary>
    /// Checks if this node has any children.
    /// </summary>
    private bool HasChildren => m_left != null || m_right != null;

    /// <summary>
    /// Creates a root node from the dimension of the atlas.
    /// </summary>
    /// <param name="atlasDimension">The dimension of the entire atlas.
    /// </param>
    public AtlasNode(Dimension atlasDimension)
    {
        Location = new Box2I(new Vec2I(0, 0), atlasDimension.Vector);
        m_maxAvailableDimensions = atlasDimension;
    }

    private AtlasNode(Box2I location, AtlasNode parent)
    {
        Location = location;
        m_parent = parent;
        m_maxAvailableDimensions = location.Dimension;
    }

    /// <summary>
    /// Attempts to recursively add the space provided into the atlas. If
    /// it cannot be done, it will return null.
    /// </summary>
    /// <param name="dimension">The space to try to add.</param>
    /// <returns>The node that represents the allocated space, or null if
    /// there is no space in any child of this.</returns>
    internal AtlasNode? RecursivelyAdd(Dimension dimension)
    {
        if (m_occupied || dimension.Width > Location.Width || dimension.Height > Location.Height)
            return null;

        // This is a heuristic to quickly determine if we can place it. It
        // will lead to some holes in the map as it is not optimal, but in
        // practice this does a pretty nice job thus far. I am not opposed
        // to a better implementation getting in, however due to the simple
        // nature of this, it'd only be worth upgrading if truly needed.
        if (HasChildren)
        {
            foreach (AtlasNode? child in Children)
            {
                if (child == null || !child.CanPossiblyAdd(dimension))
                    continue;

                AtlasNode? node = child.RecursivelyAdd(dimension);
                if (node != null)
                    return node;
            }

            // If we have children but cannot add them, that means there is
            // likely not enough space for them, so we exit early as part
            // of the heuristic.
            return null;
        }

        bool fitsWidthExactly = (dimension.Width == Location.Width);
        bool fitsHeightExactly = (dimension.Height == Location.Height);

        if (fitsWidthExactly && fitsHeightExactly)
        {
            m_occupied = true;
            m_maxAvailableDimensions = new Dimension(0, 0);
            m_parent?.RecursivelyNotifySomeChildWasAdded();
            return this;
        }

        if (fitsWidthExactly)
            return AddAsWidthFit(dimension);
        if (fitsHeightExactly)
            return AddAsHeightFit(dimension);

        return AddToBottomLeftCornerRecursively(dimension);
    }

    /// <summary>
    /// Deletes the node by making its space available. This will invoke
    /// recursive traversal to clean up space.
    /// </summary>
    internal void Delete()
    {
        Precondition(!HasChildren, "Trying to delete a parent atlas node, we should only delete from leaves");
        Precondition(m_occupied, "Trying to delete an atlas node that has not been used");

        // TODO
        throw new NotImplementedException("AtlasNode.Delete() not implemented yet");
    }

    /// <summary>
    /// Make this current node be the tree from another atlas node.
    /// </summary>
    /// <remarks>
    /// Right now we do not support adding an existing tree to anywhere but
    /// the origin, so we can only add an existing tree to a new tree if it
    /// was the first one to be added. We could however fix this by doing a
    /// recursive traversal and adding the offset of this node to the child
    /// location coordinates. This unfortunately would break invariants for
    /// the class since things that depend on this (like texture managers)
    /// depend on the location staying the exact same, and is why we do not
    /// do this.
    /// </remarks>
    /// <param name="treeRoot">The root of another atlas tree.</param>
    internal void EmplaceExistingTree(AtlasNode treeRoot)
    {
        Precondition(!HasChildren, "Trying to fuse a child atlas tree into to a non-empty node");
        Precondition(Location.BottomLeft == Vec2I.Zero, "All the locations of the child nodes will be wrong");

        m_left = treeRoot.m_left;
        m_right = treeRoot.m_right;
        m_occupied = treeRoot.m_occupied;
        m_maxAvailableDimensions = treeRoot.m_maxAvailableDimensions;
    }

    /// <summary>
    /// A recursive call that bubbles upwards to the parent.
    /// </summary>
    private void RecursivelyNotifySomeChildWasAdded()
    {
        if (m_left == null || m_right == null)
        {
            Fail("Propagating child addition should never encounter null children");
            return;
        }

        // We have to do it this way because being occupied means the value
        // of the min dimensions for that will be zero, which will mess up
        // our tracking. We want the minimum values on each dimension which
        // are *not* occupied.

        if (m_left.m_occupied && m_right.m_occupied)
        {
            m_occupied = true;
            m_maxAvailableDimensions = new Dimension(0, 0);
        }

        if (!m_left.m_occupied)
        {
            m_maxAvailableDimensions.Width = Math.Max(m_maxAvailableDimensions.Width, m_left.m_maxAvailableDimensions.Width);
            m_maxAvailableDimensions.Height = Math.Max(m_maxAvailableDimensions.Height, m_left.m_maxAvailableDimensions.Height);
        }

        if (!m_right.m_occupied)
        {
            m_maxAvailableDimensions.Width = Math.Max(m_maxAvailableDimensions.Width, m_right.m_maxAvailableDimensions.Width);
            m_maxAvailableDimensions.Height = Math.Max(m_maxAvailableDimensions.Height, m_right.m_maxAvailableDimensions.Height);
        }

        m_parent?.RecursivelyNotifySomeChildWasAdded();
    }

    /// <summary>
    /// A heuristic function for checking if this node can possibly hold
    /// the dimension provided. This may return a false negative, but that
    /// is the nature of this heuristic.
    /// </summary>
    /// <param name="dimension">The dimension to try to be added.</param>
    /// <returns>True if it definitely can add it, false if it is not safe
    /// performance-wise to try to evaluate if it can be added.</returns>
    private bool CanPossiblyAdd(Dimension dimension)
    {
        return !m_occupied &&
               dimension.Width <= m_maxAvailableDimensions.Width &&
               dimension.Height <= m_maxAvailableDimensions.Height;
    }

    private AtlasNode? AddAsWidthFit(Dimension dimension)
    {
        Box2I bottom = new Box2I(Location.Min, Location.Min + dimension.Vector);
        m_left = new AtlasNode(bottom, this);

        Box2I top = new Box2I(bottom.TopLeft, Location.TopRight);
        m_right = new AtlasNode(top, this);

        return m_left.RecursivelyAdd(dimension);
    }

    private AtlasNode? AddAsHeightFit(Dimension dimension)
    {
        Box2I left = new Box2I(Location.Min, Location.Min + dimension.Vector);
        m_left = new AtlasNode(left, this);

        Box2I right = new Box2I(left.BottomRight, Location.TopRight);
        m_right = new AtlasNode(right, this);

        return m_left.RecursivelyAdd(dimension);
    }

    private AtlasNode? AddToBottomLeftCornerRecursively(Dimension dimension)
    {
        Box2I first;
        Box2I second;

        // We want to split along the bigger axis. If the width is bigger,
        // then we will partition this area with a vertical line. Similarly
        // if the height is larger then cut across with a horizontal line.
        //
        // The cut that is done needs to make it so that the space we want
        // to consume tightly fits along one axis. This helps the algorithm
        // move towards completion as it solves an axis, then another axis,
        // and then places it in the area it sliced out.
        if (Location.Width >= Location.Height)
        {
            // This is represented by the following:
            //
            //         Middle
            //          Top      Max
            //    +------+------+
            //    |      .      |
            //    |      .      |
            //    +------+------+
            // Min     Middle
            //         Bottom
            //
            // Note that the dots are where we want to cut, and the width
            // from Min -> MiddleBottom is equal to the dimension width
            // provided since we are cutting vertically.
            Vec2I middleBottom = new Vec2I(Location.Min.X + dimension.Width, Location.Min.Y);
            Vec2I middleTop = new Vec2I(Location.Min.X + dimension.Width, Location.Max.Y);
            first = new Box2I(Location.BottomLeft, middleTop);
            second = new Box2I(middleBottom, Location.TopRight);
        }
        else
        {
            // This is represented by the following:
            //
            //              +-------+ Max
            //              |       |
            //  Left Middle + . . . + Right Middle
            //              |       |
            //          Min +-------+
            //
            // Note that the dots are where we want to cut, and the width
            // from Min -> Left Middle is equal to the dimension height
            // provided since we are cutting horizontally.
            Vec2I leftMiddle = new Vec2I(Location.Min.X, Location.Min.Y + dimension.Height);
            Vec2I rightMiddle = new Vec2I(Location.Max.X, Location.Min.Y + dimension.Height);
            first = new Box2I(Location.BottomLeft, rightMiddle);
            second = new Box2I(leftMiddle, Location.TopRight);
        }

        m_left = new AtlasNode(first, this);
        m_right = new AtlasNode(second, this);

        return m_left.RecursivelyAdd(dimension);
    }
}

