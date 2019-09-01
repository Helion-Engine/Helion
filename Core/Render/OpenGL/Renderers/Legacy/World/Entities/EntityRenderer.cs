using Helion.Maps.Geometry;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.Shared.World.ViewClipping;
using Helion.Util.Geometry;
using Helion.World;
using Helion.World.Bsp;
using Helion.World.Entities;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities
{
    public class EntityRenderer
    {
        private const uint SpriteFrameRotationAngle = 9 * (uint.MaxValue / 16);

        private readonly LegacyGLTextureManager m_textureManager;
        private readonly SectorDrawnTracker m_SectorDrawnTracker = new SectorDrawnTracker();
        private readonly EntityDrawnTracker m_EntityDrawnTracker = new EntityDrawnTracker();
        private double m_tickFraction;

        public EntityRenderer(LegacyGLTextureManager textureManager)
        {
            m_textureManager = textureManager;
        }

        public void UpdateTo(WorldBase world)
        {
            m_SectorDrawnTracker.UpdateTo(world);
        }

        public void Clear(WorldBase world, double tickFraction)
        {
            m_tickFraction = tickFraction;
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
                if (m_EntityDrawnTracker.HasDrawn(entity))
                    continue;

                RenderEntity(entity, position);
                m_EntityDrawnTracker.MarkDrawn(entity);
            }
            
            m_SectorDrawnTracker.MarkDrawn(sector);
        }

        private static uint CalculateRotation(uint viewAngle, uint entityAngle)
        {
            // This works as follows: First we find the angle that we have to
            // the entity. Since facing along with the actor (ex: looking at
            // their back) wants to give us the opposite rotation side, we
            // add 180 degrees to our angle delta. Finally we add 22.5 degrees
            // to that as well because we don't want a transition when we hit
            // 180 degrees... we'd rather have [180 - 22.5, 180 + 22.5] be the
            // angle rather than [180 - 45, 180]. Then we can do a bit shift
            // trick which converts the higher order bits into the angle.
            unchecked
            {
                return (viewAngle - entityAngle + SpriteFrameRotationAngle) >> 29;
            }
        }

        private GLLegacyTexture FindSpriteTexture(Entity entity, uint rotation)
        {
            Precondition(rotation < 8, "Bad rotation index, rotation should be between 0 - 7.");
            
            string sprite = entity.Frame.FullSpriteFrame;
            // TODO: Look up the 3 possible rotations: "R", "R+mirror", "0".
            
            return m_textureManager.NullTexture;
        }

        private void AddSpriteQuad(Vec2D vectorToEntity, Vec3D entityCenterBottom, Entity entity, 
            GLLegacyTexture texture)
        {
            // We need to find the perpendicular vector from the entity so we
            // know where to place the quad vertices.
            Vec2D rightNormal = vectorToEntity.OriginRightRotate90().Unit();

            Vec2D entityCenterXY = entityCenterBottom.To2D();
            Vec2D halfWidth = rightNormal * texture.Dimension.Width;
            Vec2D left = entityCenterXY - halfWidth;
            Vec2D right = entityCenterXY + halfWidth;
            
            Vec3D topLeft = new Vec3D(left.X, left.Y, entityCenterBottom.Z + texture.Height);
            Vec3D bottomLeft = new Vec3D(left.X, left.Y, entityCenterBottom.Z);
            Vec3D topRight = new Vec3D(right.X, right.Y, entityCenterBottom.Z + texture.Height);
            Vec3D bottomRight = new Vec3D(right.X, right.Y, entityCenterBottom.Z);
            
            // TODO...
        }

        private void RenderEntity(Entity entity, Vec2D position)
        {
            Vec3D centerBottom = entity.Position.Interpolate(entity.PrevPosition, m_tickFraction);
            Vec2D entityPos = centerBottom.To2D();

            uint viewAngle = ViewClipper.ToDiamondAngle(position, entityPos);
            uint entityAngle = ViewClipper.DiamondAngleFromRadians(entity.AngleRadians);
            uint rotation = CalculateRotation(viewAngle, entityAngle);

            GLLegacyTexture texture = FindSpriteTexture(entity, rotation);
            AddSpriteQuad(entityPos - position, centerBottom, entity, texture);
        }
    }
}