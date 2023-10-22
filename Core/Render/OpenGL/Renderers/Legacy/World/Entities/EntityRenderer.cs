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
using System.Diagnostics.CodeAnalysis;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;
using Helion.Util;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities;

public class EntityRenderer : IDisposable
{
    private readonly IConfig m_config;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly EntityProgram m_program = new();
    private readonly RenderDataManager<EntityVertex> m_dataManager;
    private readonly Dictionary<Vec2D, int> m_renderPositions = new(1024, new Vec2DCompararer());
    private double m_tickFraction;
    private Vec2F m_viewRightNormal;
    private bool m_spriteAlpha;
    private bool m_spriteClip;
    private bool m_spriteZCheck;
    private int m_spriteClipMin;
    private float m_spriteClipFactorMax;
    private bool m_disposed;

    public EntityRenderer(IConfig config, LegacyGLTextureManager textureManager)
    {
        m_config = config;
        m_textureManager = textureManager;
        m_dataManager = new(m_program);
        m_spriteAlpha = m_config.Render.SpriteTransparency;
        m_spriteClip = m_config.Render.SpriteClip;
        m_spriteZCheck = m_config.Render.SpriteZCheck;
        m_spriteClipMin = m_config.Render.SpriteClipMin;
        m_spriteClipFactorMax = (float)m_config.Render.SpriteClipFactorMax;
    }

    ~EntityRenderer()
    {
        PerformDispose();
    }
    
    public void Clear(IWorld world)
    {
        m_dataManager.Clear();
        m_renderPositions.Clear();
        m_spriteAlpha = m_config.Render.SpriteTransparency;
        m_spriteClip = m_config.Render.SpriteClip;
        m_spriteZCheck = m_config.Render.SpriteZCheck;
        m_spriteClipMin = m_config.Render.SpriteClipMin;
        m_spriteClipFactorMax = (float)m_config.Render.SpriteClipFactorMax;
    }

    public void SetTickFraction(double tickFraction) =>
        m_tickFraction = tickFraction;

    public void SetViewDirection(Vec2D viewDirection)
    {
        m_viewRightNormal = viewDirection.RotateRight90().Unit().Float;
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

    private float GetOffsetZ(Entity entity, GLLegacyTexture texture)
    {
        float offsetAmount = texture.Offset.Y - texture.Height;
        if (offsetAmount >= 0 || entity.Definition.Flags.Missile)
            return offsetAmount;

        if (!m_spriteClip)
            return 0;

        if (texture.Height < m_spriteClipMin || entity.Definition.IsInventory)
            return 0;

        if (entity.Position.Z - entity.HighestFloorSector.Floor.Z < texture.Offset.Y)
        {
            float maxHeight = texture.Height * m_spriteClipFactorMax;
            if (-offsetAmount > maxHeight)
                offsetAmount = -maxHeight;
            return offsetAmount;
        }

        return 0;
    }

    public unsafe void RenderEntity(Sector viewSector, Entity entity, in Vec3D position)
    {
        const double NudgeFactor = 0.0001;
        
        Vec3D centerBottom = entity.Position;
        Vec2D entityPos = centerBottom.XY;
        Vec2D position2D = position.XY;
        Vec2D nudgeAmount = Vec2D.Zero;

        SpriteDefinition? spriteDef = m_textureManager.GetSpriteDefinition(entity.Frame.SpriteIndex);
        uint rotation = 0;
        if (spriteDef != null && spriteDef.HasRotations)
        {
            uint viewAngle = ViewClipper.ToDiamondAngle(position2D, entityPos);
            uint entityAngle = ViewClipper.DiamondAngleFromRadians(entity.AngleRadians);
            rotation = CalculateRotation(viewAngle, entityAngle);
        }

        if (m_spriteZCheck)
        {
            Vec2D positionLookup = centerBottom.XY;
            if (m_renderPositions.TryGetValue(positionLookup, out int count))
            {
                double nudge = Math.Clamp(NudgeFactor * entityPos.Distance(position2D), NudgeFactor, double.MaxValue);
                nudgeAmount = Vec2D.UnitCircle(position.Angle(centerBottom)) * nudge * count;
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
        Sector sector = entity.Sector.GetRenderSector(viewSector, position.Z);

        short lightLevel = entity.Flags.Bright || entity.Frame.Properties.Bright ? (short)255 :
            (short)((sector.TransferFloorLightSector.LightLevel + sector.TransferCeilingLightSector.LightLevel) / 2);

        int halfTexWidth = texture.Dimension.Width / 2;
        float offsetZ = GetOffsetZ(entity, texture);
        // Multiply the X offset by the rightNormal X/Y to move the sprite according to the player's view
        // Doom graphics are drawn left to right and not centered
        var pos = new Vec3F((float)(entity.Position.X - nudgeAmount.X) - (m_viewRightNormal.X * texture.Offset.X) + (m_viewRightNormal.X * halfTexWidth), 
            (float)(entity.Position.Y - nudgeAmount.Y) - (m_viewRightNormal.Y * texture.Offset.X) + (m_viewRightNormal.Y * halfTexWidth), (float)entity.Position.Z + offsetZ);

        var prevPos = entity.Position == entity.PrevPosition ? pos : 
            new Vec3F(pos.X - (float)(entity.Position.X - entity.PrevPosition.X), 
            pos.Y - (float)(entity.Position.Y - entity.PrevPosition.Y),
            pos.Z - (float)(entity.Position.Z - entity.PrevPosition.Z));

        bool useAlpha = entity.Flags.Shadow || (m_spriteAlpha && entity.Alpha < 1.0f);
        RenderData<EntityVertex> renderData = useAlpha ? m_dataManager.GetAlpha(texture) : m_dataManager.GetNonAlpha(texture);
        float alpha = useAlpha ? entity.Alpha : 1.0f;
        float flipU = spriteRotation.Mirror ? 1.0f : 0.0f;
        float fuzz = 0.0f;
        if (entity.Flags.Shadow)
        {
            fuzz = 1.0f;
            alpha = 0.99f;
        }

        int newLength = renderData.Vbo.Data.Length + 1;
        renderData.Vbo.Data.EnsureCapacity(newLength);
        fixed (EntityVertex* vertex = &renderData.Vbo.Data.Data[renderData.Vbo.Data.Length])
        {
            vertex->Pos = pos;
            vertex->PrevPos = prevPos;
            vertex->LightLevel = lightLevel;
            vertex->Alpha = alpha;
            vertex->Fuzz = fuzz;
            vertex->FlipU = flipU;
        }
        renderData.Vbo.Data.SetLength(newLength);
    }

    private void SetUniforms(RenderInfo renderInfo)
    {
        const int TicksPerFrame = 4;
        const int DifferentFrames = 8;
        
        mat4 mvp = Renderer.CalculateMvpMatrix(renderInfo);
        mat4 mvpNoPitch = Renderer.CalculateMvpMatrix(renderInfo, true);
        float fuzzFrac = (((WorldStatic.World.GameTicker / TicksPerFrame) % DifferentFrames)) + 1;
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
        m_program.FuzzFrac(fuzzFrac);
        m_program.TimeFrac(renderInfo.TickFraction);
        m_program.ViewRightNormal(m_viewRightNormal);
    }

    public void RenderAlpha(RenderInfo renderInfo)
    {
        Render(renderInfo, true);
    }

    public void RenderNonAlpha(RenderInfo renderInfo)
    {
        Render(renderInfo, false);
    }
    
    private void Render(RenderInfo renderInfo, bool alpha)
    {
        m_tickFraction = renderInfo.TickFraction;
        m_program.Bind();
        GL.ActiveTexture(TextureUnit.Texture0);
        SetUniforms(renderInfo);

        if (alpha)
            m_dataManager.RenderAlpha(PrimitiveType.Points);
        else
            m_dataManager.RenderNonAlpha(PrimitiveType.Points);

        m_program.Unbind();
    }

    public void ResetInterpolation(IWorld world)
    {
        Clear(world);
    }
    
    private void PerformDispose()
    {
        if (m_disposed)
            return;
        
        m_program.Dispose();
        m_dataManager.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        PerformDispose();
        GC.SuppressFinalize(this);
    }
    
    private class Vec2DCompararer : IEqualityComparer<Vec2D>
    {
        public bool Equals(Vec2D x, Vec2D y) => x.X == y.X && x.Y == y.Y;
        public int GetHashCode(Vec2D obj) => HashCode.Combine(obj.X, obj.Y);
    }
}
