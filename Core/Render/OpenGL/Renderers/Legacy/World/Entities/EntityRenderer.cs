using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World.ViewClipping;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources;
using Helion.Util.Configs;
using Helion.Util.Container;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using Helion.World.Geometry.Subsectors;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities;

public class EntityRenderer
{
    private const byte MaxAlpha = 255;
    
    private readonly IConfig m_config;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly EntityProgram m_program = new();
    private readonly RenderDataManager<EntityVertex> m_dataManager;
    private readonly Dictionary<Vec2D, int> m_renderPositions = new();
    private int m_renderCounter;
    private double m_tickFraction;
    private Vec2F m_viewRightNormal;

    public EntityRenderer(IConfig config, LegacyGLTextureManager textureManager)
    {
        m_config = config;
        m_textureManager = textureManager;
        m_dataManager = new(m_program);
    }
    
    public void Clear(IWorld world, double tickFraction)
    {
        m_tickFraction = tickFraction;
        m_dataManager.Clear();
        m_renderPositions.Clear();
        m_renderCounter++;
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

            if (ShouldNotDraw(entity))
                continue;

            // if (entity.Definition.Properties.Alpha < 1)
            // {
            //     entity.RenderDistance = entity.Position.XY.Distance(position.XY);
            //     AlphaEntities.Add(entity);
            //     continue;
            // }

            RenderEntity(viewSector, entity, position);
        }
    }

    private static uint CalculateRotation(uint viewAngle, uint entityAngle)
    {
        // The rotation angle in diamond angle format. This is equal to 180
        // degrees + 22.5 degrees. See <see cref="CalculateRotation"/> docs
        // for more information.
        const uint SpriteFrameRotationAngle = 9 * (uint.MaxValue / 16);
        
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

    public bool ShouldNotDraw(Entity entity)
    {
        return entity.Frame.IsInvisible || entity.Flags.Invisible || entity.Flags.NoSector || entity.RenderedCounter == m_renderCounter;
    }

    private void AddSpriteQuad(in Vec3D entityCenterBottom, Entity entity, GLLegacyTexture texture, short lightLevel, bool mirror)
    {
        float bottomZ = (float)entityCenterBottom.Z;
        if (ShouldApplyOffsetZ(entity, texture, out float offsetAmount))
            bottomZ += offsetAmount;
        
        Vec3F pos = entityCenterBottom.Float.WithZ(bottomZ);
        EntityVertex vertex = new(pos, lightLevel, MaxAlpha, entity.Flags.Shadow, mirror);

        RenderData<EntityVertex> renderData = m_dataManager.Get(texture);
        renderData.Vbo.Add(vertex);
    }

    private bool ShouldApplyOffsetZ(Entity entity, GLLegacyTexture texture, out float offsetAmount)
    {
        offsetAmount = texture.Offset.Y - texture.Height;
        if (offsetAmount >= 0 || entity.Definition.Flags.Missile)
            return true;

        if (!m_config.Render.SpriteClip)
            return false;

        if (texture.Height < m_config.Render.SpriteClipMin || entity.Definition.IsInventory)
            return false;

        if (entity.Position.Z - entity.HighestFloorSector.Floor.Z < texture.Offset.Y)
        {
            if (-offsetAmount > texture.Height * m_config.Render.SpriteClipFactorMax)
                offsetAmount = -texture.Height * (float)m_config.Render.SpriteClipFactorMax;
            return true;
        }

        return false;
    }

    public void RenderEntity(Sector viewSector, Entity entity, in Vec3D position)
    {
        const double NudgeFactor = 0.0001;
        
        Vec3D centerBottom = entity.PrevPosition.Interpolate(entity.Position, m_tickFraction);
        Vec2D entityPos = centerBottom.XY;
        Vec2D position2D = position.XY;

        SpriteDefinition? spriteDef = m_textureManager.GetSpriteDefinition(entity.Frame.SpriteIndex);
        uint rotation = 0;
        if (spriteDef != null && spriteDef.HasRotations)
        {
            uint viewAngle = ViewClipper.ToDiamondAngle(position2D, entityPos);
            uint entityAngle = ViewClipper.DiamondAngleFromRadians(entity.AngleRadians);
            rotation = CalculateRotation(viewAngle, entityAngle);
        }

        if (m_config.Render.SpriteZCheck)
        {
            Vec2D positionLookup = centerBottom.XY;
            if (m_renderPositions.TryGetValue(positionLookup, out int count))
            {
                double nudge = Math.Clamp(NudgeFactor * entityPos.Distance(position2D), NudgeFactor, double.MaxValue);
                Vec2D nudgeAmount = Vec2D.UnitCircle(position.Angle(centerBottom)) * nudge * count;
                centerBottom.X -= nudgeAmount.X;
                centerBottom.Y -= nudgeAmount.Y;
                m_renderPositions[positionLookup] = count + 1;
            }
            else
            {
                m_renderPositions[positionLookup] = 1;
            }
        }

        SpriteRotation spriteRotation = m_textureManager.NullSpriteRotation;
        if (spriteDef != null)
            spriteRotation = m_textureManager.GetSpriteRotation(spriteDef, entity.Frame.Frame, rotation);
        GLLegacyTexture texture = (spriteRotation.Texture.RenderStore as GLLegacyTexture) ?? m_textureManager.NullTexture; 

        short lightLevel = CalculateLightLevel(entity, entity.Sector.GetRenderSector(viewSector, position.Z).LightLevel);
        AddSpriteQuad(centerBottom, entity, texture, lightLevel, spriteRotation.Mirror);
        entity.RenderedCounter = m_renderCounter;
    }

    private void SetUniforms(RenderInfo renderInfo)
    {
        const int TicksPerFrame = 4;
        const int DifferentFrames = 8;
        
        mat4 mvp = Renderer.CalculateMvpMatrix(renderInfo);
        mat4 mvpNoPitch = Renderer.CalculateMvpMatrix(renderInfo, true);
        float timeFrac = ((renderInfo.ViewerEntity.World.Gametick / TicksPerFrame) % DifferentFrames) + 1;
        bool drawInvulnerability = false;
        int extraLight = 0;
        float mix = 0.0f;

        if (renderInfo.ViewerEntity.PlayerObj != null)
        {
            if (renderInfo.ViewerEntity.PlayerObj.DrawFullBright())
                mix = 1.0f;
            if (renderInfo.ViewerEntity.PlayerObj.DrawInvulnerableColorMap())
                drawInvulnerability = true;

            extraLight = renderInfo.ViewerEntity.PlayerObj.GetExtraLightRender();
        }
        
        m_program.BoundTexture(TextureUnit.Texture0);
        m_program.ExtraLight(extraLight);
        m_program.HasInvulnerability(drawInvulnerability);
        m_program.LightLevelMix(mix);
        m_program.Mvp(mvp);
        m_program.MvpNoPitch(mvpNoPitch);
        m_program.TimeFrac(timeFrac);
        m_program.ViewRightNormal(m_viewRightNormal);
    }
    
    public void Render(RenderInfo renderInfo)
    {
        m_program.Bind();
        SetUniforms(renderInfo);

        m_dataManager.Render();
        
        m_program.Unbind();
    }
}
