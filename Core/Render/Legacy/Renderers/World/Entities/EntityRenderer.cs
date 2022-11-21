using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Helion;
using Helion.Geometry.Vectors;
using Helion.Render;
using Helion.Render.Legacy;
using Helion.Render.Legacy.Renderers;
using Helion.Render.Legacy.Renderers.World;
using Helion.Render.Legacy.Renderers.World.Data;
using Helion.Render.Legacy.Renderers.World.Entities;
using Helion.Render.Legacy.Shared.World.ViewClipping;
using Helion.Render.Legacy.Texture.Legacy;
using Helion.Resources;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Container;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;

namespace Helion.Render.Legacy.Renderers.World.Entities;

public class EntityRenderer
{
    /// <summary>
    /// The rotation angle in diamond angle format. This is equal to 180
    /// degrees + 22.5 degrees. See <see cref="CalculateRotation"/> docs
    /// for more information.
    /// </summary>
    private const uint SpriteFrameRotationAngle = 9 * (uint.MaxValue / 16);
    private static readonly Color ShadowColor = Color.FromArgb(32, 32, 32);

    private readonly IConfig m_config;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly RenderWorldDataManager m_worldDataManager;
    private readonly EntityDrawnTracker m_EntityDrawnTracker = new();
    private readonly HashSet<Vec2D> m_renderPositions = new();
    private bool m_drawDebugBox;
    private double m_tickFraction;
    private Entity? m_cameraEntity;
    private GLLegacyTexture m_debugBoxTexture;
    private RenderWorldData m_debugBoxRenderWorldData;
    private Vec2F m_viewRightNormal;

    public readonly List<IRenderObject> AlphaEntities = new();

    public EntityRenderer(IConfig config, LegacyGLTextureManager textureManager, RenderWorldDataManager worldDataManager)
    {
        m_config = config;
        m_textureManager = textureManager;
        m_worldDataManager = worldDataManager;
        m_debugBoxTexture = m_textureManager.NullTexture;
        m_debugBoxRenderWorldData = m_worldDataManager.GetRenderData(m_debugBoxTexture);
    }

    public void UpdateTo(IWorld world)
    {

    }

    public void Clear(IWorld world, double tickFraction, Entity cameraEntity)
    {
        // I'm hitching a ride here so we don't keep making a bunch of
        // invocations to this for every single sprite to avoid overhead
        // of asking the config for a new value every time.
        m_drawDebugBox = m_config.Developer.Render.Debug;
        m_textureManager.TryGet(Constants.DebugBoxTexture, ResourceNamespace.Graphics, out m_debugBoxTexture);
        m_debugBoxRenderWorldData = m_worldDataManager.GetRenderData(m_debugBoxTexture);

        m_tickFraction = tickFraction;
        m_cameraEntity = cameraEntity;
        m_EntityDrawnTracker.Reset(world);
        AlphaEntities.Clear();
        m_renderPositions.Clear();
    }

    public void SetViewDirection(Vec2D viewDirection)
    {
        m_viewRightNormal = viewDirection.RotateRight90().Unit().Float;
    }

    public void RenderSubsector(Sector viewSector, in Subsector subsector, in Vec3D position)
    {
        LinkableNode<Entity>? node = subsector.Sector.Entities.Head;
        while (node != null)
        {
            Entity entity = node.Value;
            node = node.Next;
            if (m_drawDebugBox)
                AddSpriteDebugBox(entity);

            if (ShouldNotDraw(entity))
                continue;

            if (entity.Definition.Properties.Alpha < 1)
            {
                entity.RenderDistance = entity.Position.XY.Distance(position.XY);
                AlphaEntities.Add(entity);
                continue;
            }

            RenderEntity(viewSector, entity, position);
        }
    }

    private static uint CalculateRotation(uint viewAngle, uint entityAngle)
    {
        // This works as follows:
        //
        // First we find the angle that we have to the entity. Since
        // facing along with the actor (ex: looking at their back) wants to
        // give us the opposite rotation side, we add 180 degrees to our
        // angle delta.
        //
        // Then we add 22.5 degrees to that as well because we don't want
        // a transition when we hit 180 degrees... we'd rather have ranges
        // of [180 - 22.5, 180 + 22.5] be the angle rather than the range
        // [180 - 45, 180].
        //
        // Then we can do a bit shift trick which converts the higher order
        // three bits into the angle rotation between 0 - 7.
        return unchecked(viewAngle - entityAngle + SpriteFrameRotationAngle >> 29);
    }

    private static short CalculateLightLevel(Entity entity, short sectorLightLevel)
    {
        if (entity.Flags.Bright || entity.Frame.Properties.Bright)
            return 255;
        return sectorLightLevel;
    }

    public bool ShouldNotDraw(Entity entity)
    {
        return entity.Frame.IsInvisible || entity.Flags.Invisible || entity.Flags.NoSector ||
                m_EntityDrawnTracker.HasDrawn(entity) ||
                ReferenceEquals(m_cameraEntity, entity);
    }

    private void AddSpriteQuad(in Vec3D entityCenterBottom, Entity entity,
        GLLegacyTexture texture, short lightLevel, bool mirror)
    {
        // We need to find the perpendicular vector from the entity so we
        // know where to place the quad vertices.
        Vec2F rightNormal = m_viewRightNormal;
        Vec2F entityCenterXY = entityCenterBottom.XY.Float;
        // Multiply the X offset by the rightNormal X/Y to move the sprite according to the player's view
        // Doom graphics are drawn left to right and not centered. Have to translate the offset.
        entityCenterXY += rightNormal * (texture.Width / 2 - texture.Offset.X);

        Vec2F halfWidth = rightNormal * texture.Dimension.Width / 2;
        Vec2F left = entityCenterXY - halfWidth;
        Vec2F right = entityCenterXY + halfWidth;

        float bottomZ = (float)entityCenterBottom.Z;

        if (ShouldApplyOffsetZ(entity, texture, out float offsetAmount))
            bottomZ += offsetAmount;

        float topZ = bottomZ + texture.Height;
        float alpha = m_config.Render.SpriteTransparency ? (float)entity.Definition.Properties.Alpha : 1.0f;
        float fuzz = entity.Flags.Shadow ? 1.0f : 0.0f;
        float leftU = 0.0f;
        float rightU = 1.0f;
        if (mirror)
        {
            leftU = 1.0f;
            rightU = 0.0f;
        }

        WorldVertex topLeft = new(left.X, left.Y, topZ, leftU, 0.0f, lightLevel, alpha, fuzz);
        WorldVertex topRight = new(right.X, right.Y, topZ, rightU, 0.0f, lightLevel, alpha, fuzz);
        WorldVertex bottomLeft = new(left.X, left.Y, bottomZ, leftU, 1.0f, lightLevel, alpha, fuzz);
        WorldVertex bottomRight = new(right.X, right.Y, bottomZ, rightU, 1.0f, lightLevel, alpha, fuzz);

        RenderWorldData renderWorldData = alpha < 1 ? m_worldDataManager.GetAlphaRenderData(texture) : m_worldDataManager.GetRenderData(texture);
        renderWorldData.Vbo.Add(topLeft);
        renderWorldData.Vbo.Add(bottomLeft);
        renderWorldData.Vbo.Add(topRight);
        renderWorldData.Vbo.Add(topRight);
        renderWorldData.Vbo.Add(bottomLeft);
        renderWorldData.Vbo.Add(bottomRight);
    }

    private bool ShouldApplyOffsetZ(Entity entity, GLLegacyTexture texture, out float offsetAmount)
    {
        offsetAmount = texture.Offset.Y - texture.Height;
        if (entity.Definition.Flags.Missile || offsetAmount >= 0)
            return true;

        if (!m_config.Render.SpriteClip && !m_config.Render.SpriteClipCorpse)
            return false;

        if (texture.Height < m_config.Render.SpriteClipMin || entity.Definition.IsType(EntityDefinitionType.Inventory))
            return false;

        if (entity.Position.Z - entity.HighestFloorSector.ToFloorZ(entity.Position) < texture.Offset.Y)
        {
            if (m_config.Render.SpriteClipCorpse && entity.Flags.Corpse)
            {
                if (-offsetAmount > texture.Height * m_config.Render.SpriteClipCorpseFactorMax)
                    offsetAmount = -texture.Height * (float)m_config.Render.SpriteClipCorpseFactorMax;
                return true;
            }
            else if (m_config.Render.SpriteClip)
            {
                if (-offsetAmount > texture.Height * m_config.Render.SpriteClipFactorMax)
                    offsetAmount = -texture.Height * (float)m_config.Render.SpriteClipFactorMax;
                return true;
            }
        }

        return false;
    }

    private void AddSpriteDebugBox(Entity entity)
    {
        Vec3D centerBottom = entity.PrevPosition.Interpolate(entity.Position, m_tickFraction);
        Vec3F min = new Vec3D(centerBottom.X - entity.Radius, centerBottom.Y - entity.Radius, centerBottom.Z).Float;
        Vec3F max = new Vec3D(centerBottom.X + entity.Radius, centerBottom.Y + entity.Radius, centerBottom.Z + entity.Height).Float;

        // These are the indices for the corners on the ASCII art further
        // down in the image.
        AddCubeFaces(2, 0, 3, 1);
        AddCubeFaces(3, 1, 7, 5);
        AddCubeFaces(7, 5, 6, 4);
        AddCubeFaces(6, 4, 2, 0);
        AddCubeFaces(0, 4, 1, 5);
        AddCubeFaces(6, 2, 7, 3);

        void AddCubeFaces(int topLeft, int bottomLeft, int topRight, int bottomRight)
        {
            // We want to draw it to both sides, not just the front.
            AddCubeFace(topLeft, bottomLeft, topRight, bottomRight);
            AddCubeFace(topRight, bottomRight, topLeft, bottomLeft);
        }

        void AddCubeFace(int topLeft, int bottomLeft, int topRight, int bottomRight)
        {
            WorldVertex topLeftVertex = MakeVertex(topLeft, 0.0f, 0.0f);
            WorldVertex bottomLeftVertex = MakeVertex(bottomLeft, 0.0f, 1.0f);
            WorldVertex topRightVertex = MakeVertex(topRight, 1.0f, 0.0f);
            WorldVertex bottomRightVertex = MakeVertex(bottomRight, 1.0f, 1.0f);

            m_debugBoxRenderWorldData.Vbo.Add(topLeftVertex);
            m_debugBoxRenderWorldData.Vbo.Add(bottomLeftVertex);
            m_debugBoxRenderWorldData.Vbo.Add(topRightVertex);
            m_debugBoxRenderWorldData.Vbo.Add(topRightVertex);
            m_debugBoxRenderWorldData.Vbo.Add(bottomLeftVertex);
            m_debugBoxRenderWorldData.Vbo.Add(bottomRightVertex);
        }

        WorldVertex MakeVertex(int cornerIndex, float u, float v)
        {
            // The vertices look like this:
            //
            //          6----7 (max)
            //         /.   /|
            //        2----3 |
            //        | 4..|.5          Z Y
            //        |.   |/           |/
            //  (min) 0----1            o--> X
            return cornerIndex switch
            {
                0 => new WorldVertex(min.X, min.Y, min.Z, u, v),
                1 => new WorldVertex(max.X, min.Y, min.Z, u, v),
                2 => new WorldVertex(min.X, min.Y, max.Z, u, v),
                3 => new WorldVertex(max.X, min.Y, max.Z, u, v),
                4 => new WorldVertex(min.X, max.Y, min.Z, u, v),
                5 => new WorldVertex(max.X, max.Y, min.Z, u, v),
                6 => new WorldVertex(min.X, max.Y, max.Z, u, v),
                7 => new WorldVertex(max.X, max.Y, max.Z, u, v),
                _ => throw new Exception("Out of bounds cube index when debugging entity bounding box")
            };
        }
    }

    public void RenderEntity(Sector viewSector, Entity entity, in Vec3D position)
    {
        const double NudgeFactor = 0.0001;
        Vec3D centerBottom = entity.PrevPosition.Interpolate(entity.Position, m_tickFraction);
        Vec2D entityPos = centerBottom.XY;
        Vec2D position2D = position.XY;

        var spriteDef = m_textureManager.GetSpriteDefinition(entity.Frame.SpriteIndex);
        uint rotation;

        if (spriteDef != null && spriteDef.HasRotations)
        {
            uint viewAngle = ViewClipper.ToDiamondAngle(position2D, entityPos);
            uint entityAngle = ViewClipper.DiamondAngleFromRadians(entity.AngleRadians);
            rotation = CalculateRotation(viewAngle, entityAngle);
        }
        else
        {
            rotation = 0;
        }

        if (m_config.Render.SpriteZCheck)
        {
            if (m_renderPositions.Contains(entityPos))
            {
                double nudge = Math.Clamp(NudgeFactor * entityPos.Distance(position2D), NudgeFactor, double.MaxValue);
                Vec2D nudgeAmount = Vec2D.UnitCircle(position.Angle(centerBottom)) * nudge;
                centerBottom.X -= nudgeAmount.X;
                centerBottom.Y -= nudgeAmount.Y;

                while (m_renderPositions.Contains(centerBottom.XY))
                {
                    centerBottom.X -= nudgeAmount.X;
                    centerBottom.Y -= nudgeAmount.Y;
                }

                m_renderPositions.Add(centerBottom.XY);
            }
            else
            {
                m_renderPositions.Add(entityPos);
            }
        }

        SpriteRotation spriteRotation;
        if (spriteDef != null)
            spriteRotation = m_textureManager.GetSpriteRotation(spriteDef, entity.Frame.Frame, rotation);
        else
            spriteRotation = m_textureManager.NullSpriteRotation;
        GLLegacyTexture texture = spriteRotation.Texture.RenderStore == null ? m_textureManager.NullTexture : (GLLegacyTexture)spriteRotation.Texture.RenderStore;

        short lightLevel = CalculateLightLevel(entity, entity.Sector.GetRenderSector(viewSector, position.Z).LightLevel);
        AddSpriteQuad(centerBottom, entity, texture, lightLevel, spriteRotation.Mirror);

        m_EntityDrawnTracker.MarkDrawn(entity);
    }
}
