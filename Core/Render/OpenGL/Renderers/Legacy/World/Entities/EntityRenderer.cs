using System.Linq;
using System.Numerics;
using Helion.Maps.Geometry;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.Shared.World.ViewClipping;
using Helion.Util;
using Helion.Util.Geometry;
using Helion.World;
using Helion.World.Bsp;
using Helion.World.Entities;
using MoreLinq.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities
{
    public class EntityRenderer
    {
        /// <summary>
        /// The rotation angle in diamond angle format. This is equal to 180
        /// degrees + 22.5 degrees. See <see cref="CalculateRotation"/> docs
        /// for more information.
        /// </summary>
        private const uint SpriteFrameRotationAngle = 9 * (uint.MaxValue / 16);

        private readonly LegacyGLTextureManager m_textureManager;
        private readonly RenderWorldDataManager m_worldDataManager;
        private readonly SectorDrawnTracker m_SectorDrawnTracker = new SectorDrawnTracker();
        private readonly EntityDrawnTracker m_EntityDrawnTracker = new EntityDrawnTracker();
        private double m_tickFraction;
        private Entity? m_cameraEntity;

        public EntityRenderer(LegacyGLTextureManager textureManager, RenderWorldDataManager worldDataManager)
        {
            m_textureManager = textureManager;
            m_worldDataManager = worldDataManager;
        }

        public void UpdateTo(WorldBase world)
        {
            m_SectorDrawnTracker.UpdateTo(world);
            PreloadAllTextures(world);
        }

        public void Clear(WorldBase world, double tickFraction, Entity cameraEntity)
        {
            m_tickFraction = tickFraction;
            m_cameraEntity = cameraEntity;
            m_EntityDrawnTracker.Reset(world);
            m_SectorDrawnTracker.Reset();
        }

        public void RenderSubsector(Subsector subsector, Vec2D position)
        {
            Sector sector = subsector.Sector;
            
            // TODO: Track all the subsectors the entity is in, assuming that is faster.
            if (m_SectorDrawnTracker.HasDrawn(sector))
                return;
            
            foreach (Entity entity in sector.Entities)
            {
                if (ShouldNotDraw(entity))
                    continue;

                RenderEntity(entity, position);
                m_EntityDrawnTracker.MarkDrawn(entity);
            }
            
            m_SectorDrawnTracker.MarkDrawn(sector);
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
            unchecked
            {
                return (viewAngle - entityAngle + SpriteFrameRotationAngle) >> 29;
            }
        }

        private static string GetRotationSprite(string fullFrame, uint rotation)
        {
            Precondition(fullFrame.Length == 5, "The 'AAAA B' sprite/frame should be 5 letters long");

            char frame = fullFrame[4];
            switch (rotation)
            {
            case 0:
                return fullFrame + '1';
            case 1:
            case 7:
                return fullFrame + '2' + frame + '8';
            case 2:
            case 6:
                return fullFrame + '3' + frame + '7';
            case 3:
            case 5:
                return fullFrame + '4' + frame + '6';
            case 4:
                return fullFrame + '5';
            }

            Fail("Rotation sprite was somehow not between 0 - 7");
            return fullFrame + rotation;
        }
        
        private void PreloadAllTextures(WorldBase world)
        {
            world.Entities.Select(entity => entity.Definition)
                          .DistinctBy(def => def.Id)
                          .SelectMany(def => def.States.Frames)
                          .Select(frame => frame.FullSpriteFrame)
                          .Distinct()
                          .ForEach(LoadSprite);

            void LoadSprite(string spriteFrame)
            {
                Precondition(spriteFrame.Length == 5, "Expected frame in the form of 'AAAA B', length was wrong");
                
                char frame = spriteFrame[4];
                string[] allPossibleFrames =
                {
                    spriteFrame + '0', 
                    spriteFrame + '1', 
                    spriteFrame + '2', 
                    spriteFrame + '3', 
                    spriteFrame + '4', 
                    spriteFrame + '5', 
                    spriteFrame + '6', 
                    spriteFrame + '7', 
                    spriteFrame + '8',
                    spriteFrame + '2' + frame + '8',
                    spriteFrame + '3' + frame + '7',
                    spriteFrame + '4' + frame + '6',
                };
                
                allPossibleFrames.ForEach(sprite => m_textureManager.TryGetSprite(sprite, out _));
            }
        }

        private bool ShouldNotDraw(Entity entity)
        {
            return m_EntityDrawnTracker.HasDrawn(entity) || 
                   ReferenceEquals(m_cameraEntity, entity) ||
                   entity.Frame.Sprite == Constants.InvisibleSprite;
        }

        private (GLLegacyTexture, bool) FindSpriteTexture(Entity entity, uint rotation)
        {
            Precondition(rotation < 8, "Bad rotation index, rotation should be between 0 - 7.");

            string fullFrame = entity.Frame.FullSpriteFrame;
            
            // We figure we'll run into the rotations the most, followed by the
            // non-rotation, and then rarely the octet rotation form.
            string rotationSprite = GetRotationSprite(fullFrame, rotation);
            if (m_textureManager.TryGetSprite(rotationSprite, out GLLegacyTexture mirrorTexture))
            {
                // Rotations 0 - 4 are normal, and 5 - 7 are the A2A8, A3A7, and
                // A4A6 rotations which we do need to mirror.
                bool isMirror = rotation >= 5;
                return (mirrorTexture, isMirror);                
            }

            if (m_textureManager.TryGetSprite(fullFrame + '0', out GLLegacyTexture? noRotationSprite))
                return (noRotationSprite, false);
            
            if (m_textureManager.TryGetSprite(fullFrame + rotation, out GLLegacyTexture? octetSprite))
                return (octetSprite, false);

            return (m_textureManager.NullTexture, false);
        }

        private void AddSpriteQuad(Vec2D vectorToEntity, Vec3D entityCenterBottom, Entity entity, 
            GLLegacyTexture texture, bool mirror)
        {
            // We need to find the perpendicular vector from the entity so we
            // know where to place the quad vertices.
            Vector2 rightNormal = vectorToEntity.OriginRightRotate90().Unit().ToFloat();

            Vector2 entityCenterXY = entityCenterBottom.To2D().ToFloat();
            Vector2 halfWidth = rightNormal * texture.Dimension.Width / 2;
            Vector2 left = entityCenterXY - halfWidth;
            Vector2 right = entityCenterXY + halfWidth;

            float bottomZ = (float)entityCenterBottom.Z;
            float topZ = bottomZ + texture.Height;
            float leftU = mirror ? 1.0f : 0.0f;
            float rightU = mirror ? 0.0f : 1.0f;
            short lightLevel = entity.Sector.LightLevel;

            LegacyVertex topLeft = new LegacyVertex(left.X, left.Y, topZ, leftU, 0.0f, lightLevel);
            LegacyVertex topRight = new LegacyVertex(right.X, right.Y, topZ, rightU, 0.0f, lightLevel);
            LegacyVertex bottomLeft = new LegacyVertex(left.X, left.Y, bottomZ, leftU, 1.0f, lightLevel);
            LegacyVertex bottomRight = new LegacyVertex(right.X, right.Y, bottomZ, rightU, 1.0f, lightLevel);
                
            RenderWorldData renderWorldData = m_worldDataManager[texture];
            renderWorldData.Vbo.Add(topLeft);
            renderWorldData.Vbo.Add(bottomLeft);
            renderWorldData.Vbo.Add(topRight);
            renderWorldData.Vbo.Add(topRight);
            renderWorldData.Vbo.Add(bottomLeft);
            renderWorldData.Vbo.Add(bottomRight);
        }

        private void RenderEntity(Entity entity, Vec2D position)
        {
            Vec3D centerBottom = entity.Position.Interpolate(entity.PrevPosition, m_tickFraction);
            Vec2D entityPos = centerBottom.To2D();

            uint viewAngle = ViewClipper.DiamondAngleFromRadians(m_cameraEntity.AngleRadians);
            uint entityAngle = ViewClipper.DiamondAngleFromRadians(entity.AngleRadians);
            uint rotation = CalculateRotation(viewAngle, entityAngle);

            (GLLegacyTexture texture, bool mirror) = FindSpriteTexture(entity, rotation);
            AddSpriteQuad(entityPos - position, centerBottom, entity, texture, mirror);
        }
    }
}