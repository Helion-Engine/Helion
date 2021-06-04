using System;
using System.Collections.Generic;
using System.Drawing;
using Helion.Geometry.Vectors;
using Helion.Render.Legacy.Renderers.Legacy.World.Data;
using Helion.Render.Legacy.Shared.World.ViewClipping;
using Helion.Render.Legacy.Texture.Legacy;
using Helion.Resources;
using Helion.Util;
using Helion.Util.Configs;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Geometry.Subsectors;

namespace Helion.Render.Legacy.Renderers.Legacy.World.Entities
{
    public class EntityRenderer
    {
        /// <summary>
        /// The rotation angle in diamond angle format. This is equal to 180
        /// degrees + 22.5 degrees. See <see cref="CalculateRotation"/> docs
        /// for more information.
        /// </summary>
        private const uint SpriteFrameRotationAngle = 9 * (uint.MaxValue / 16);
        private static readonly Color ShadowColor = Color.FromArgb(32, 32, 32);

        private readonly Config m_config;
        private readonly LegacyGLTextureManager m_textureManager;
        private readonly RenderWorldDataManager m_worldDataManager;
        private readonly EntityDrawnTracker m_EntityDrawnTracker = new();
        private bool m_drawDebugBox;
        private double m_tickFraction;
        private Entity? m_cameraEntity;
        private GLLegacyTexture m_debugBoxTexture;
        private RenderWorldData m_debugBoxRenderWorldData;

        private readonly List<Entity> m_alphaEntities = new();
        private readonly List<GLLegacyTexture> m_alphaEntityTextures = new();

        public EntityRenderer(Config config, LegacyGLTextureManager textureManager, RenderWorldDataManager worldDataManager)
        {
            m_config = config;
            m_textureManager = textureManager;
            m_worldDataManager = worldDataManager;
            m_debugBoxTexture = m_textureManager.NullTexture;
            m_debugBoxRenderWorldData = m_worldDataManager.GetRenderData(m_debugBoxTexture);
        }

        public void UpdateTo(WorldBase world)
        {
            PreloadAllTextures(world);
        }

        private HashSet<Vec2D> m_renderPositions = new();

        public void Clear(WorldBase world, double tickFraction, Entity cameraEntity)
        {
            // I'm hitching a ride here so we don't keep making a bunch of
            // invocations to this for every single sprite to avoid overhead
            // of asking the config for a new value every time.
            m_drawDebugBox = m_config.Developer.RenderDebug;
            m_textureManager.TryGet(Constants.DebugBoxTexture, ResourceNamespace.Graphics, out m_debugBoxTexture);
            m_debugBoxRenderWorldData = m_worldDataManager.GetRenderData(m_debugBoxTexture);

            m_tickFraction = tickFraction;
            m_cameraEntity = cameraEntity;
            m_EntityDrawnTracker.Reset(world);
            m_alphaEntities.Clear();
            m_renderPositions.Clear();
        }

        public void RenderSubsector(Subsector subsector, in Vec2D position, in Vec2D viewDirection)
        {
            foreach (Entity entity in subsector.Entities)
            {
                if (m_drawDebugBox)
                    AddSpriteDebugBox(entity);

                if (ShouldNotDraw(entity))
                    continue;

                if (entity.Definition.Properties.Alpha < 1)
                {
                    entity.RenderDistance = entity.Position.XY.Distance(position);
                    m_alphaEntities.Add(entity);
                    continue;
                }

                RenderEntity(entity, position, viewDirection);
                m_EntityDrawnTracker.MarkDrawn(entity);
            }
        }

        public void RenderAlphaEntities(in Vec2D position, in Vec2D viewDirection)
        {
            // Entities with alpha need to be drawn last to draw correctly
            // Sort from farthest to nearest
            // This should work well enough since we are only dealing with sprites
            m_alphaEntities.Sort((i1, i2) => i2.RenderDistance.CompareTo(i1.RenderDistance));
            for (int i = 0; i < m_alphaEntities.Count; i++)
                RenderEntity(m_alphaEntities[i], position, viewDirection);
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
            return unchecked((viewAngle - entityAngle + SpriteFrameRotationAngle) >> 29);
        }

        private static short CalculateLightLevel(Entity entity, short sectorLightLevel)
        {
            if (entity.Flags.Bright || entity.Frame.Properties.Bright)
                return 255;
            return sectorLightLevel;
        }

        private void PreloadAllTextures(WorldBase world)
        {
            // TODO
        }

        private bool ShouldNotDraw(Entity entity)
        {
            return entity.Frame.IsInvisible ||
                    m_EntityDrawnTracker.HasDrawn(entity) ||
                    ReferenceEquals(m_cameraEntity, entity);
        }

        private void AddSpriteQuad(in Vec2D viewDirection, in Vec3D entityCenterBottom, Entity entity,
            GLLegacyTexture texture, bool mirror)
        {
            // We need to find the perpendicular vector from the entity so we
            // know where to place the quad vertices.
            Vec2F rightNormal = viewDirection.RotateRight90().Unit().Float;
            Vec2F entityCenterXY = entityCenterBottom.XY.Float;

            // Multiply the X offset by the rightNormal X/Y to move the sprite according to the player's view
            entityCenterXY.X += rightNormal.X * texture.Metadata.Offset.X;
            entityCenterXY.Y += rightNormal.Y * texture.Metadata.Offset.X;

            Vec2F halfWidth = rightNormal * texture.Dimension.Width / 2;
            Vec2F left = entityCenterXY - halfWidth;
            Vec2F right = entityCenterXY + halfWidth;

            float bottomZ = (float)entityCenterBottom.Z;

            if (ShouldApplyOffsetZ(entity, texture, out float offsetAmount))
                bottomZ += offsetAmount;

            float topZ = bottomZ + texture.Height;
            float leftU = mirror ? 1.0f : 0.0f;
            float rightU = mirror ? 0.0f : 1.0f;
            short lightLevel = CalculateLightLevel(entity, entity.Sector.LightLevel);
            float alpha = (float)entity.Definition.Properties.Alpha;
            Color color = entity.Definition.Flags.Shadow ? ShadowColor : Color.White;
            bool fuzz = entity.Definition.Flags.Shadow;

            LegacyVertex topLeft = new LegacyVertex(left.X, left.Y, topZ, leftU, 0.0f, color, lightLevel, alpha, fuzz);
            LegacyVertex topRight = new LegacyVertex(right.X, right.Y, topZ, rightU, 0.0f, color, lightLevel, alpha, fuzz);
            LegacyVertex bottomLeft = new LegacyVertex(left.X, left.Y, bottomZ, leftU, 1.0f, color, lightLevel, alpha, fuzz);
            LegacyVertex bottomRight = new LegacyVertex(right.X, right.Y, bottomZ, rightU, 1.0f, color, lightLevel, alpha, fuzz);

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
            offsetAmount = texture.Metadata.Offset.Y;
            if (entity.Flags.Projectile || texture.Metadata.Offset.Y >= 0)
                return true;

            if (!m_config.Render.SpriteClip && !m_config.Render.SpriteClipCorpse)
                return false;

            if (texture.Height < m_config.Render.SpriteClipMin || entity.Definition.IsType(EntityDefinitionType.Inventory) ||
                (entity.Flags.Monster && !entity.Flags.Corpse))
                return false;

            if (entity.Position.Z - entity.HighestFloorSector.ToFloorZ(entity.Position) < -texture.Metadata.Offset.Y)
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
                LegacyVertex topLeftVertex = MakeVertex(topLeft, 0.0f, 0.0f);
                LegacyVertex bottomLeftVertex = MakeVertex(bottomLeft, 0.0f, 1.0f);
                LegacyVertex topRightVertex = MakeVertex(topRight, 1.0f, 0.0f);
                LegacyVertex bottomRightVertex = MakeVertex(bottomRight, 1.0f, 1.0f);

                m_debugBoxRenderWorldData.Vbo.Add(topLeftVertex);
                m_debugBoxRenderWorldData.Vbo.Add(bottomLeftVertex);
                m_debugBoxRenderWorldData.Vbo.Add(topRightVertex);
                m_debugBoxRenderWorldData.Vbo.Add(topRightVertex);
                m_debugBoxRenderWorldData.Vbo.Add(bottomLeftVertex);
                m_debugBoxRenderWorldData.Vbo.Add(bottomRightVertex);
            }

            LegacyVertex MakeVertex(int cornerIndex, float u, float v)
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
                    0 => new LegacyVertex(min.X, min.Y, min.Z, u, v),
                    1 => new LegacyVertex(max.X, min.Y, min.Z, u, v),
                    2 => new LegacyVertex(min.X, min.Y, max.Z, u, v),
                    3 => new LegacyVertex(max.X, min.Y, max.Z, u, v),
                    4 => new LegacyVertex(min.X, max.Y, min.Z, u, v),
                    5 => new LegacyVertex(max.X, max.Y, min.Z, u, v),
                    6 => new LegacyVertex(min.X, max.Y, max.Z, u, v),
                    7 => new LegacyVertex(max.X, max.Y, max.Z, u, v),
                    _ => throw new Exception("Out of bounds cube index when debugging entity bounding box")
                };
            }
        }

        private void RenderEntity(Entity entity, in Vec2D position, in Vec2D viewDirection)
        {
            const double NudgeFactor = 0.0001;
            Vec3D centerBottom = entity.PrevPosition.Interpolate(entity.Position, m_tickFraction);
            Vec2D entityPos = centerBottom.XY;

            var spriteDef = m_textureManager.GetSpriteDefinition(entity.Frame.Sprite);
            uint rotation;

            if (spriteDef != null && spriteDef.HasRotations)
            {
                uint viewAngle = ViewClipper.ToDiamondAngle(position, entityPos);
                uint entityAngle = ViewClipper.DiamondAngleFromRadians(entity.AngleRadians);
                rotation = CalculateRotation(viewAngle, entityAngle);
            }
            else
            {
                rotation = 0;
            }

            if (m_renderPositions.Contains(entityPos))
            {
                double nudge = Math.Clamp(NudgeFactor * entityPos.Distance(position), NudgeFactor, double.MaxValue);
                Vec2D nudgeAmount = Vec2D.UnitCircle(position.Angle(centerBottom)) * nudge;
                centerBottom.X += nudgeAmount.X;
                centerBottom.Y += nudgeAmount.Y;

                while (m_renderPositions.Contains(centerBottom.XY))
                {
                    centerBottom.X += nudgeAmount.X;
                    centerBottom.Y += nudgeAmount.Y;
                }

                m_renderPositions.Add(centerBottom.XY);

            }
            else
            {
                m_renderPositions.Add(entityPos);
            }

            SpriteRotation spriteRotation;
            if (spriteDef != null)
                spriteRotation = m_textureManager.GetSpriteRotation(spriteDef, entity.Frame.Frame, rotation);
            else
                spriteRotation = m_textureManager.NullSpriteRotation;
            GLLegacyTexture texture = spriteRotation.Texture.RenderStore == null ? m_textureManager.NullTexture : (GLLegacyTexture)spriteRotation.Texture.RenderStore;
            
            AddSpriteQuad(viewDirection, centerBottom, entity, texture, spriteRotation.Mirror);
        }
    }
}
