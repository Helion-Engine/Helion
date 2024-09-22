using System;
using System.Collections.Generic;
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
using Helion.World.Entities.Definition.Flags;
using Helion.World.Geometry.Sectors;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities;

public class EntityRenderer : IDisposable
{
    private readonly IConfig m_config;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly EntityProgram m_program = new();
    private readonly RenderDataManager<EntityVertex> m_dataManager;
    private readonly Dictionary<Vec2D, int> m_renderPositions = new(1024, new Vec2DCompararer());
    private DynamicArray<SpriteDefinition?> m_spriteDefs = new(1024);
    private SpriteRotation m_nullSpriteRotation;
    private Vec2F m_viewRightNormal;
    private Vec2F m_prevViewRightNormal;
    private TransferHeightView m_transferHeightView = TransferHeightView.Middle;
    private bool m_spriteAlpha;
    private bool m_spriteClip;
    private bool m_spriteZCheck;
    private bool m_vanillaRender;
    private int m_spriteClipMin;
    private float m_spriteClipFactorMax;
    private bool m_disposed;
    private int m_lastViewerEntityId;

    public EntityRenderer(IConfig config, LegacyGLTextureManager textureManager)
    {
        m_config = config;
        m_textureManager = textureManager;
        m_nullSpriteRotation = m_textureManager.NullSpriteRotation;
        m_dataManager = new(m_program);
        m_spriteAlpha = m_config.Render.SpriteTransparency;
        m_spriteClip = m_config.Render.SpriteClip;
        m_spriteZCheck = m_config.Render.SpriteZCheck;
        m_spriteClipMin = m_config.Render.SpriteClipMin;
        m_vanillaRender = m_config.Render.VanillaRender;
        m_spriteClipFactorMax = (float)m_config.Render.SpriteClipFactorMax;
    }

    ~EntityRenderer()
    {
        PerformDispose();
    }

    public void UpdateTo(IWorld world)
    {
        m_vanillaRender = world.Config.Render.VanillaRender;
        m_lastViewerEntityId = -1;
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
        if (m_vanillaRender)
            return offsetAmount;

        if (offsetAmount >= 0 || entity.Definition.Flags.Missile)
            return offsetAmount;

        if (entity.Sector.Flood || entity.Sector.Floor.NoRender)
            return offsetAmount;

        if (!m_spriteClip)
            return 0;

        if (texture.Height < m_spriteClipMin || entity.Definition.IsInventory)
            return 0;

        if (entity.Position.Z - entity.HighestFloorSector.Floor.Z < texture.Offset.Y)
        {
            float maxHeight = (texture.Height - texture.BlankRowsFromBottom) * m_spriteClipFactorMax;
            if (-offsetAmount > maxHeight)
                offsetAmount = -maxHeight - texture.BlankRowsFromBottom;
            // Truncate to integer pixel amount. This helps the jumpiness for the stock large torches.
            return (int)offsetAmount;
        }

        return offsetAmount;
    }

    private SpriteRotation GetSpriteRotation(SpriteDefinition spriteDefinition, int frame, uint rotation)
    {
        var spriteRotation = spriteDefinition.Rotations[frame, rotation];
        if (spriteRotation == null)
            return m_nullSpriteRotation;

        if (spriteRotation.RenderStore != null)
            return spriteRotation;

        return m_textureManager.GetSpriteRotation(spriteDefinition, frame, rotation);
    }

    public unsafe void RenderEntity(Entity entity, in Vec2D position)
    {
        const double NudgeFactor = 0.0001;
        
        Vec3D centerBottom = entity.Position;
        Vec2D entityPos = new(centerBottom.X, centerBottom.Y);
        Vec2D nudgeAmount = default;

        SpriteDefinition? spriteDef = null;
        int spriteIndex = entity.Frame.SpriteIndex;
        if (spriteIndex >= m_spriteDefs.Capacity)
        {
            m_spriteDefs.EnsureCapacity(spriteIndex);
            spriteDef = m_textureManager.GetSpriteDefinition(entity.Frame.SpriteIndex);
            m_spriteDefs.Data[spriteIndex] = spriteDef;
        }
        else
        {
            spriteDef = m_spriteDefs.Data[spriteIndex];
            if (spriteDef == null)
            {
                spriteDef = m_textureManager.GetSpriteDefinition(entity.Frame.SpriteIndex);
                m_spriteDefs.Data[spriteIndex] = spriteDef;
            }
        }

        uint rotation = 0;
        if (spriteDef != null && spriteDef.HasRotations)
        {
            uint viewAngle = ViewClipper.ToDiamondAngle(position, entityPos);
            uint entityAngle = ViewClipper.DiamondAngleFromRadians(entity.AngleRadians);
            rotation = CalculateRotation(viewAngle, entityAngle);
        }

        if (m_spriteZCheck)
        {
            if (m_renderPositions.TryGetValue(entityPos, out int count))
            {
                double nudge = NudgeFactor * count * Math.Sqrt(entity.RenderDistanceSquared);
                double angle = Math.Atan2(centerBottom.Y - position.Y, centerBottom.X - position.X);
                nudgeAmount.X = Math.Cos(angle) * nudge;
                nudgeAmount.Y = Math.Sin(angle) * nudge;
                m_renderPositions[entityPos] = count + 1;      
            }
            else
            {
                m_renderPositions[entityPos] = 1;
            }
        }
        
        SpriteRotation spriteRotation = spriteDef == null ? m_nullSpriteRotation : GetSpriteRotation(spriteDef, entity.Frame.Frame, rotation);
        GLLegacyTexture texture = (spriteRotation.RenderStore as GLLegacyTexture) ?? m_textureManager.NullTexture;
        Sector sector = entity.Sector.GetRenderSector(m_transferHeightView);

        int halfTexWidth = texture.Dimension.Width / 2;
        float offsetZ = GetOffsetZ(entity, texture);

        bool useAlpha = entity.Flags.Shadow || (m_spriteAlpha && entity.Alpha < 1.0f);
        RenderData<EntityVertex> renderData = useAlpha ? m_dataManager.GetAlpha(texture) : m_dataManager.GetNonAlpha(texture);
        float alpha = useAlpha ? entity.Alpha : 1.0f;
        float fuzz = 0.0f;
        if (entity.Flags.Shadow)
        {
            fuzz = 1.0f;
            alpha = 0.99f;
        }

        var arrayData = renderData.ArrayData;
        int length = arrayData.Length;
        if (arrayData.Capacity < length + 1)
            arrayData.EnsureCapacity(length + 1);

        int colorMapIndex = entity.Properties.ColormapIndex ?? entity.GetTranslationColorMap();
        fixed (EntityVertex* vertex = &arrayData.Data[length])
        {
            // Multiply the X offset by the rightNormal X/Y to move the sprite according to the player's view
            // Doom graphics are drawn left to right and not centered
            vertex->Pos = new Vec3F(
                (float)(entity.Position.X - nudgeAmount.X) - (m_viewRightNormal.X * texture.Offset.X) + (m_viewRightNormal.X * halfTexWidth),
                (float)(entity.Position.Y - nudgeAmount.Y) - (m_viewRightNormal.Y * texture.Offset.X) + (m_viewRightNormal.Y * halfTexWidth),
                (float)entity.Position.Z + offsetZ);
            vertex->PrevPos = new Vec3F(
                (float)(entity.PrevPosition.X - nudgeAmount.X) - (m_prevViewRightNormal.X * texture.Offset.X) + (m_prevViewRightNormal.X * halfTexWidth),
                (float)(entity.PrevPosition.Y - nudgeAmount.Y) - (m_prevViewRightNormal.Y * texture.Offset.X) + (m_prevViewRightNormal.Y * halfTexWidth),
                (float)entity.PrevPosition.Z + offsetZ);
            vertex->LightLevel = entity.Flags.Bright || entity.Frame.Properties.Bright ? 255 :
                ((sector.TransferFloorLightSector.LightLevel + sector.TransferCeilingLightSector.LightLevel) / 2);
            vertex->Options = VertexOptions.Entity(alpha, fuzz, spriteRotation.FlipU, colorMapIndex);
            vertex->SectorIndex = sector.Id + 1;
        }
        arrayData.Length = length + 1;
    }

    public void Start(RenderInfo renderInfo)
    {
        m_transferHeightView = renderInfo.TransferHeightView;
        m_prevViewRightNormal = m_viewRightNormal;
        m_viewRightNormal = renderInfo.Camera.Direction.XY.RotateRight90().Unit();
        if (m_lastViewerEntityId != renderInfo.ViewerEntity.Id)
            m_prevViewRightNormal = m_viewRightNormal;

        m_program.ViewRightNormal(m_viewRightNormal);
        m_program.PrevViewRightNormal(m_prevViewRightNormal);
        m_lastViewerEntityId = renderInfo.ViewerEntity.Id;
    }

    private void SetUniforms(RenderInfo renderInfo)
    {
        m_program.BoundTexture(TextureUnit.Texture0);
        m_program.ColormapTexture(TextureUnit.Texture2);
        m_program.SectorColormapTexture(TextureUnit.Texture3);
        m_program.ExtraLight(renderInfo.Uniforms.ExtraLight);
        m_program.HasInvulnerability(renderInfo.Uniforms.DrawInvulnerability);
        m_program.LightLevelMix(renderInfo.Uniforms.Mix);
        m_program.Mvp(renderInfo.Uniforms.Mvp);
        m_program.MvpNoPitch(renderInfo.Uniforms.MvpNoPitch);
        m_program.FuzzFrac(renderInfo.Uniforms.TimeFrac);
        m_program.TimeFrac(renderInfo.TickFraction);
        m_program.ViewRightNormal(m_viewRightNormal);
        m_program.PrevViewRightNormal(m_prevViewRightNormal);
        m_program.DistanceOffset(Renderer.GetDistanceOffset(renderInfo));
        m_program.ColorMix(renderInfo.Uniforms.ColorMix.Global);
        m_program.FuzzDiv(renderInfo.Uniforms.FuzzDiv);
        m_program.PaletteIndex((int)renderInfo.Uniforms.PaletteIndex);
        m_program.ColorMapIndex(renderInfo.Uniforms.ColorMapUniforms.GlobalIndex);
        m_program.LightMode(renderInfo.Uniforms.LightMode);
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
