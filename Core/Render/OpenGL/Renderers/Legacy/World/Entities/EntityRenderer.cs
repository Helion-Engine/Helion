using System;
using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World.ViewClipping;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources;
using Helion.Util.Configs;
using Helion.Util.Container;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities;

public class EntityRenderer : IDisposable
{
    const int MinBarWidth = 20;
    const int MaxBarWidth = 80;
    const int MinHealth = 20;
    const int MaxHealth = 4000;

    private readonly IConfig m_config;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly EntityProgram m_program = new("Main");
    private readonly EntityTransparentProgram m_programTransparent = new();
    private readonly EntityCompositeProgram m_programComposite = new();
    private readonly EntityFuzzRefractionProgram m_programFuzzRefraction = new();
    private readonly RenderDataManager<EntityVertex> m_dataManager;
    private readonly Dictionary<Vec2D, int> m_renderPositions = new(1024, new Vec2DCompararer());
    private readonly HashSet<SpritePosKey> m_spriteRenderPositions = new(1024);
    private readonly DynamicArray<SpriteDefinition?> m_spriteDefs = new(1024);
    private readonly SpriteRotation m_nullSpriteRotation;
    private Vec2F m_viewRightNormal;
    private Vec2F m_prevViewRightNormal;
    private TransferHeightView m_transferHeightView = TransferHeightView.Middle;
    private bool m_spriteAlpha;
    private bool m_spriteClip;
    private bool m_spriteZCheck;
    private bool m_vanillaRender;
    private bool m_healthBars;
    private bool m_attackIndicator;
    private int m_healthBarLimit;
    private int m_spriteClipMin;
    private float m_spriteClipFactorMax;
    private bool m_disposed;
    private int m_lastViewerEntityId;

    public EntityRenderer(IConfig config, LegacyGLTextureManager textureManager)
    {
        m_config = config;
        m_textureManager = textureManager;
        m_nullSpriteRotation = m_textureManager.NullSpriteRotation;
        m_dataManager = new(m_program, textureManager.BlackTexture);
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

    public bool HasFuzz() => m_dataManager.HasFuzz();
    public bool HasAlpha() => m_dataManager.HasAlpha();

    public void HealthBarMode(bool set) => m_program.HealthBarMode(set);

    public void UpdateTo(IWorld world)
    {
        m_vanillaRender = world.Config.Render.VanillaRender;
        m_lastViewerEntityId = -1;
    }
    
    public void Clear(IWorld world)
    {
        m_dataManager.Clear();
        m_renderPositions.Clear();
        m_spriteRenderPositions.Clear();
        m_spriteAlpha = m_config.Render.SpriteTransparency;
        m_spriteClip = m_config.Render.SpriteClip;
        m_spriteZCheck = m_config.Render.SpriteZCheck;
        m_spriteClipMin = m_config.Render.SpriteClipMin;
        m_spriteClipFactorMax = (float)m_config.Render.SpriteClipFactorMax;
        m_healthBars = m_config.Render.HealthBar.Enable;
        m_attackIndicator = m_config.Render.HealthBar.AttackIndicator;
        m_healthBarLimit = m_config.Render.HealthBar.HealthLimit;
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

    private SpriteRotation GetSpriteRotation(SpriteDefinition spriteDefinition, int frame, uint rotation, int colorMapIndex)
    {
        var spriteRotation = spriteDefinition.Rotations[frame, rotation];
        if (spriteRotation == null)
            return m_nullSpriteRotation;

        if (colorMapIndex <= 0 && spriteRotation.RenderStore != null)
            return spriteRotation;

        return m_textureManager.GetSpriteRotation(spriteDefinition, frame, rotation, colorMapIndex);
    }

    const double NudgeFactor = 0.0001;

    public void RenderEntity(Entity entity, in Vec2D position)
    {        
        Vec3D centerBottom = entity.Position;
        Vec2D entityPos = new(centerBottom.X, centerBottom.Y);
        Vec2D nudgeAmount = default;

        SpriteDefinition? spriteDef = null;
        int spriteIndex = entity.FrameState.Frame.SpriteIndex;
        if (spriteIndex >= m_spriteDefs.Capacity)
        {
            m_spriteDefs.EnsureCapacity(spriteIndex);
            spriteDef = m_textureManager.GetSpriteDefinition(entity.FrameState.Frame.SpriteIndex);
            m_spriteDefs.Data[spriteIndex] = spriteDef;
        }
        else
        {
            spriteDef = m_spriteDefs.Data[spriteIndex];
            if (spriteDef == null)
            {
                spriteDef = m_textureManager.GetSpriteDefinition(entity.FrameState.Frame.SpriteIndex);
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
            var spritePosKey = new SpritePosKey(entityPos, spriteIndex);
            if (m_spriteRenderPositions.Add(spritePosKey))
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
        }

        int colorMapIndex = entity.Properties.ColormapIndex ?? entity.GetTranslationColorMap();
        SpriteRotation spriteRotation = spriteDef == null ? m_nullSpriteRotation : GetSpriteRotation(spriteDef, entity.FrameState.Frame.Frame, rotation, colorMapIndex);
        GLLegacyTexture texture = (spriteRotation.RenderStore as GLLegacyTexture) ?? m_textureManager.NullTexture;
        Sector sector = entity.Sector.GetRenderSector(m_transferHeightView);

        float offsetZ = GetOffsetZ(entity, texture);

        bool shadow = entity.Flags.Shadow;
        bool useAlpha = m_spriteAlpha && entity.Alpha < 1.0f;
        RenderData<EntityVertex> renderData;
        if (shadow)
            renderData = m_dataManager.GetFuzz(texture);
        else
            renderData = useAlpha ? m_dataManager.GetAlpha(texture) : m_dataManager.GetNonAlpha(texture);

        float alpha = useAlpha ? entity.Alpha : 1.0f;
        float fuzz = shadow ? 1.0f : 0.0f;

        var arrayData = renderData.ArrayData;
        int length = arrayData.Length;
        if (arrayData.Capacity < length + 1)
            arrayData.EnsureCapacity(length + 1);

        ref var vertex = ref arrayData.Data[length];
        // Multiply the X offset by the rightNormal X/Y to move the sprite according to the player's view
        // Doom graphics are drawn left to right and not centered
        vertex.Pos = new Vec3F(
            (float)(entity.Position.X - nudgeAmount.X),
            (float)(entity.Position.Y - nudgeAmount.Y),
            (float)entity.Position.Z);
        vertex.PrevPos = new Vec3F(
            (float)(entity.PrevPosition.X - nudgeAmount.X),
            (float)(entity.PrevPosition.Y - nudgeAmount.Y),
            (float)entity.PrevPosition.Z);
        vertex.OffsetZ = offsetZ;
        vertex.OffsetXY = texture.Offset.X;
        vertex.LightLevel = entity.Flags.Bright || entity.FrameState.Frame.Properties.Bright ? 255 :
            ((sector.TransferFloorLightSector.LightLevel + sector.TransferCeilingLightSector.LightLevel) / 2);
        vertex.Options = VertexOptions.Entity(alpha, fuzz, spriteRotation.FlipU, colorMapIndex);
        vertex.SectorIndex = Renderer.GetColorMapBufferIndex(sector, LightBufferType.Floor);
        
        arrayData.Length = length + 1;

        if (m_healthBars && entity.Flags.Shootable && (m_healthBarLimit <= 0 || m_healthBarLimit <= entity.Properties.Health))
            RenderHealthBar(entity, texture, offsetZ, vertex);
    }

    private void RenderHealthBar(Entity entity, GLLegacyTexture texture, float offsetZ, in EntityVertex entityVertex)
    {
        // Don't let the bar bounce back and forth in height (eg Lost Soul)
        var offset = (int)offsetZ + texture.Height - texture.BlankRowsFromTop + 4;
        if (offset > entity.Properties.HealthBarOffset)
            entity.Properties.HealthBarOffset = offset;
        else
            offset = entity.Properties.HealthBarOffset;

        if (entity.Properties.HealthBarWidth == -1)
            entity.Properties.HealthBarWidth = ScaleHealthBarWidth(entity.Properties.Health);

        var attackFlash = m_attackIndicator && entity.Flags.Attacking && ((entity.World.GameTicker / 3) & 3) == 0;
        var healthBarData = m_dataManager.GetHealthBarData();
        var array = healthBarData.ArrayData;
        array.EnsureCapacity(array.Length + 1);
        ref var vertex = ref array.Data[array.Length];
        // Prevent small health values from rendering zero pixels
        float min = 1f / (entity.Properties.HealthBarWidth + MinBarWidth - 5);
        // Normalized health percent
        vertex.LightLevel = Math.Max(min, entity.Health / (float)entity.Properties.Health);
        vertex.Options = VertexOptions.Entity(1, attackFlash ? 1 : 0, 0, entity.Properties.HealthBarWidth);
        vertex.Pos = entityVertex.Pos;
        vertex.PrevPos = entityVertex.PrevPos;
        vertex.OffsetZ = offset;
        vertex.OffsetXY = 0;

        array.SetLength(array.Length + 1);
    }

    private static int ScaleHealthBarWidth(int health) =>
        (int)((MaxBarWidth - MinBarWidth) * (Math.Sqrt(health - MinHealth) / Math.Sqrt(MaxHealth - MinHealth)));

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

    private void SetUniforms(EntityProgram program, RenderInfo renderInfo)
    {
        program.BoundTexture(TextureUnit.Texture0);
        program.ColormapTexture(TextureUnit.Texture2);
        program.SectorColormapTexture(TextureUnit.Texture3);
        program.ExtraLight(renderInfo.Uniforms.ExtraLight);
        program.HasInvulnerability(renderInfo.Uniforms.DrawInvulnerability);
        program.LightLevelMix(renderInfo.Uniforms.Mix);
        program.Mvp(renderInfo.Uniforms.Mvp);
        program.MvpNoPitch(renderInfo.Uniforms.MvpNoPitch);
        program.FuzzFrac(renderInfo.Uniforms.TimeFrac);
        program.TimeFrac(renderInfo.TickFraction);
        program.ViewRightNormal(m_viewRightNormal);
        program.PrevViewRightNormal(m_prevViewRightNormal);
        program.DistanceOffset(Renderer.GetDistanceOffset(renderInfo));
        program.ColorMix(renderInfo.Uniforms.ColorMix.Global);
        program.FuzzDiv(renderInfo.Uniforms.FuzzDiv);
        program.PaletteIndex((int)renderInfo.Uniforms.PaletteIndex);
        program.ColorMapIndex(renderInfo.Uniforms.ColorMapUniforms.GlobalIndex);
        program.LightMode(renderInfo.Uniforms.LightMode);
        program.GammaCorrection(renderInfo.Uniforms.GammaCorrection);
        program.ViewPos(renderInfo.Camera.Position);
        program.ScreenBounds((renderInfo.Viewport.Width, renderInfo.Viewport.Height));
        program.CheckPlaneClip(m_vanillaRender);

        // The fade distance calculations work using squared distances
        float maxDistanceSquared = renderInfo.Uniforms.MaxDistance * renderInfo.Uniforms.MaxDistance;
        program.MaxDistanceSquared(maxDistanceSquared);
        program.FadeDistance(maxDistanceSquared / 2);

        if (program is EntityCompositeProgram)
        {
            program.AccumTexture(TextureUnit.Texture4);
            program.AccumCountTextre(TextureUnit.Texture5);
        }

        if (program is EntityFuzzRefractionProgram)
        {
            program.AccumTexture(TextureUnit.Texture4);
            program.AccumCountTextre(TextureUnit.Texture5);
            program.FuzzTexture(TextureUnit.Texture6);
            program.OpaqueTexture(TextureUnit.Texture7);
        }

        program.WallClipTexture(BindTextures.WallClipTexture);
        program.MapDataTexture(BindTextures.MapLineData);
    }

    public void RenderOpaque(RenderInfo renderInfo)
    {
        m_program.Bind();
        GL.ActiveTexture(TextureUnit.Texture0);
        SetUniforms(m_program, renderInfo);
        m_program.HealthBarMode(false);
        m_dataManager.RenderNonAlpha(PrimitiveType.Points);

        if (m_healthBars)
        {
            m_program.HealthBarMode(true);
            m_dataManager.RenderHealthBars();
        }

        m_program.Unbind();
    }

    public void RenderOitTransparentPass(RenderInfo renderInfo)
    {
        m_programTransparent.Bind();
        m_programTransparent.RenderFuzz(false);
        GL.ActiveTexture(TextureUnit.Texture0);
        SetUniforms(m_programTransparent, renderInfo);
        m_dataManager.RenderAlpha(PrimitiveType.Points);
        m_dataManager.RenderFuzz(PrimitiveType.Points);
        m_programTransparent.Unbind();
    }

    public void RenderOitTransparentFuzzPass(RenderInfo renderInfo)
    {
        m_programTransparent.Bind();
        m_programTransparent.RenderFuzz(true);
        GL.ActiveTexture(TextureUnit.Texture0);
        SetUniforms(m_programTransparent, renderInfo);
        m_dataManager.RenderFuzz(PrimitiveType.Points);
        m_programTransparent.Unbind();
    }

    public void RenderOitCompositePass(RenderInfo renderInfo)
    {
        m_programComposite.Bind();
        GL.ActiveTexture(TextureUnit.Texture0);
        SetUniforms(m_programComposite, renderInfo);
        m_dataManager.RenderAlpha(PrimitiveType.Points);
        m_programComposite.Unbind();
    }

    public void RenderOitFuzzRefractionPass(RenderInfo renderInfo, bool renderColor)
    {
        m_programFuzzRefraction.Bind();
        GL.ActiveTexture(TextureUnit.Texture0);
        m_programFuzzRefraction.RenderFuzzRefractionColor(renderColor);
        SetUniforms(m_programFuzzRefraction, renderInfo);
        m_dataManager.RenderFuzz(PrimitiveType.Points);
        m_programFuzzRefraction.Unbind();
    }

    public void RenderTransparent(RenderInfo renderInfo)
    {
        m_program.Bind();
        GL.ActiveTexture(TextureUnit.Texture0);
        SetUniforms(m_program, renderInfo);
        m_dataManager.RenderAlpha(PrimitiveType.Points);
        m_dataManager.RenderFuzz(PrimitiveType.Points);
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
