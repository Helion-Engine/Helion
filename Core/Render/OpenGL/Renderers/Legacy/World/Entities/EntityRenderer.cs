using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Renderers.Legacy.World.Data;
using Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Static;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Shared.World.ViewClipping;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Decorate.Properties.Enums;
using Helion.Util.Configs;
using Helion.Util.Container;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Geometry.Sectors;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Entities;

public sealed class EntityRenderer : StyleRendererBase, IDisposable
{
    const int MinBarWidth = 20;
    const int MaxBarWidth = 80;
    const int MinHealth = 20;
    const int MaxHealth = 4000;
    const int RenderPoolSize = 2048;

    private readonly IConfig m_config;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly EntityProgram m_program = new("Main");
    private readonly EntityHealthBarProgram m_healthBarProgram = new();
    private readonly EntityTransparentProgram m_programTransparent = new();
    private readonly EntityCompositeProgram m_programComposite = new();
    private readonly EntityFuzzRefractionProgram m_programFuzzRefraction = new();
    private readonly RenderDataManager<EntityVertex> m_dataManager;
    private readonly DynamicArray<SpriteDefinition?> m_spriteDefs = new(1024);
    private readonly SpriteRotation m_nullSpriteRotation;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly RenderDataPool<EntityVertex> m_renderDataPool;
    private readonly bool m_vanillaRender;
    private Vec2F m_viewRightNormal;
    private Vec2F m_prevViewRightNormal;
    private TransferHeightView m_transferHeightView = TransferHeightView.Middle;
    private bool m_spriteAlpha;
    private bool m_spriteClip;
    private bool m_healthBars;
    private bool m_attackIndicator;
    private int m_healthBarLimit;
    private int m_spriteClipMin;
    private float m_spriteClipFactorMax;
    private bool m_disposed;
    private int m_lastViewerEntityId;

    public EntityRenderer(IConfig config, LegacyGLTextureManager textureManager, ArchiveCollection archiveCollection)
    {
        m_config = config;
        m_textureManager = textureManager;
        m_archiveCollection = archiveCollection;
        m_nullSpriteRotation = m_textureManager.NullSpriteRotation;
        m_renderDataPool = new(m_program, RenderPoolSize);
        m_dataManager = new(m_program, textureManager.BlackTexture, m_renderDataPool);
        m_spriteAlpha = m_config.Render.SpriteTransparency;
        m_spriteClip = m_config.Render.SpriteClip;
        m_spriteClipMin = m_config.Render.SpriteClipMin;
        m_vanillaRender = m_config.Render.VanillaRender;
        m_spriteClipFactorMax = (float)m_config.Render.SpriteClipFactorMax.Value;
    }

    ~EntityRenderer()
    {
        PerformDispose();
    }

    public bool HasDataToRenderByStyle(RenderDataStyle style) => m_dataManager.HasDataToRenderByStyle(style);

    public override bool HasStyleToRender(RenderDataStyle style)
    {
        return m_dataManager.HasDataToRenderByStyle(style);
    }

    public override void Render(RenderDataStyle style)
    {
        m_dataManager.RenderByRenderStyle(style, PrimitiveType.Points);
    }

    public void UpdateTo(IWorld world)
    {
        m_lastViewerEntityId = -1;
        m_renderDataPool.RefillPool(RenderPoolSize / 4);
    }
    
    public void Clear(IWorld world)
    {
        m_dataManager.Clear();
        m_spriteAlpha = m_config.Render.SpriteTransparency;
        m_spriteClip = m_config.Render.SpriteClip;
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

    private int GetOffsetZ(Entity entity, GLLegacyTexture texture)
    {
        int offsetAmount = texture.Offset.Y - texture.Height;
        if (m_vanillaRender)
            return offsetAmount;

        if (offsetAmount >= 0 || entity.Definition.Flags.Missile() || entity.Definition.Flags.NoGravity())
            return offsetAmount;

        if (entity.Sector.Flood || entity.Sector.Floor.NoRender)
            return offsetAmount;

        if (!m_spriteClip)
            return 0;

        if (texture.Height < m_spriteClipMin || entity.Definition.IsInventory)
            return 0;

        if (entity.Position.Z - entity.HighestFloorSector.Floor.Z < texture.Offset.Y)
        {
            // Truncate to integer pixel amount. This helps the jumpiness for the stock large torches.
            int maxHeight = (int)((texture.Height - texture.BlankRowsFromBottom) * m_spriteClipFactorMax);
            if (-offsetAmount > maxHeight)
                offsetAmount = -maxHeight - texture.BlankRowsFromBottom;
            return offsetAmount;
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

    public void RenderEntity(Entity entity, in Vec2D position, int renderIndex)
    {        
        Vec3D centerBottom = entity.Position;
        Vec2D entityPos = new(centerBottom.X, centerBottom.Y);
        Vec2D nudgeAmount = default;

        SpriteDefinition? spriteDef;
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

        var colorMapIndex = entity.Properties.ColormapIndex ?? entity.GetTranslationColorMap();
        if (WorldStatic.BloodColor && entity.Definition.Type == EntityType.Blood)
        {
            var owner = entity.Owner();
            if (owner != null && owner.Properties.BloodPaletteColor.HasValue)
                colorMapIndex = m_archiveCollection.Definitions.GetBloodColormap(owner.Properties.BloodPaletteColor.Value).Index;
        }

        var shouldMirror = entity.Flags.Mirror();
        if (shouldMirror)
            rotation = SpriteDefinition.MaxRotationIndex - rotation;

        var spriteRotation = spriteDef == null ? m_nullSpriteRotation : GetSpriteRotation(spriteDef, entity.FrameState.Frame.Frame, rotation, colorMapIndex);
        var texture = (spriteRotation.RenderStore as GLLegacyTexture) ?? m_textureManager.NullTexture;
        var brightmapTexture = spriteRotation.BrightmapRenderStore as GLLegacyTexture;
        var sector = entity.LightSector3D ?? entity.Sector.GetRenderSector(m_transferHeightView);

        int flipU;
        int offsetX = texture.Offset.X;
        if (shouldMirror)
        {
            flipU = spriteRotation.FlipU ^ 1;
            offsetX = texture.Width - offsetX;
        }
        else
        {
            flipU = spriteRotation.FlipU;
        }

        var disableFullbright = spriteRotation.BrightmapNoFullbright;
        var isFullBright = (entity.Flags.Bright() || entity.FrameState.Frame.Properties.Bright) && !disableFullbright;
        var offsetZ = GetOffsetZ(entity, texture);
        var shadow = entity.Flags.Shadow() || entity.RenderStyle == RenderStyle.Fuzzy;

        int fuzz;
        RenderStyle renderStyle;
        if (shadow)
        {
            renderStyle = RenderStyle.Fuzzy;
            fuzz = 1;
        }
        else
        {
            renderStyle = m_spriteAlpha ? entity.RenderStyle: RenderStyle.Normal;
            fuzz = 0;
        }

        // If fullbright and modified through dehacked then change render style to ColorAdd for better color rendering.
        var entityAlpha = entity.Alpha;
        if (m_spriteAlpha)
        {
            if (entity.RenderStyle == RenderStyle.ColorAddFullBright)
                renderStyle = isFullBright ? RenderStyle.ColorAdd : RenderStyle.Translucent;
            else if (entity.RenderStyle == RenderStyle.ColorAddExplosion)
                renderStyle = entity.Flags.Missile() ? RenderStyle.Normal : RenderStyle.ColorAdd;

            if (renderStyle == RenderStyle.Translucent && entityAlpha >= 1)
                renderStyle = RenderStyle.Normal;
        }

        if (renderStyle == RenderStyle.ColorAdd)
            entityAlpha = 1.0f;

        var renderData = m_dataManager.GetByRenderStyle(renderStyle, texture, brightmapTexture);
        var alpha = m_spriteAlpha && renderStyle != RenderStyle.Normal ? entityAlpha : 1.0f;

        var arrayData = renderData.ArrayData;
        int length = arrayData.Length;
        if (arrayData.Capacity < length + 1)
            arrayData.EnsureCapacity(length + 1);

        int lightLevel = isFullBright ? 255 : ((sector.TransferFloorLightSector.LightLevel + sector.TransferCeilingLightSector.LightLevel) / 2);

        ref var vertex = ref arrayData.Data[length];
        // Multiply the X offset by the rightNormal X/Y to move the sprite according to the player's view
        // Doom graphics are drawn left to right and not centered
        vertex.Pos.X = (float)(entity.Position.X - nudgeAmount.X);
        vertex.Pos.Y = (float)(entity.Position.Y - nudgeAmount.Y);
        vertex.Pos.Z = (float)entity.Position.Z;
        vertex.PrevPos.X = (float)(entity.PrevPosition.X - nudgeAmount.X);
        vertex.PrevPos.Y = (float)(entity.PrevPosition.Y - nudgeAmount.Y);
        vertex.PrevPos.Z = (float)entity.PrevPosition.Z;
        vertex.SurfaceOptions = VertexOptions.EntityPackSurface(alpha, fuzz, flipU, colorMapIndex, lightLevel);
        vertex.RenderOptions = VertexOptions.EntityPackRender(
            Renderer.GetLightBufferIndex(sector, WorldStatic.Sector3D && sector.Sectors3D.Length > 0 ? LightBufferType.Wall : LightBufferType.Floor), renderIndex);

        if (entity.Definition.Flags.SpawnCeiling() && m_vanillaRender)
        {
            // Set position and offset from ceiling to not clip to floors
            var ceilingZ = (float)entity.Sector.Ceiling.Z;
            float diff = 0;
            offsetZ = (int)(vertex.Pos.Z + offsetZ - ceilingZ);
            vertex.Pos.Z = ceilingZ + diff;
            vertex.PrevPos.Z = entity.PrevPosition.Z != entity.Position.Z ? (float)entity.Sector.Ceiling.PrevZ : ceilingZ;
        }
        
        vertex.OffsetXYZ = VertexOptions.EntityPackXYZ(offsetX, offsetZ);
        arrayData.Length = length + 1;

        if (m_healthBars && entity.Flags.Shootable() && (m_healthBarLimit <= 0 || m_healthBarLimit <= entity.Properties.Health))
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

        var attackFlash = m_attackIndicator && entity.Flags.Attacking() && ((entity.World.GameTicker / 3) & 3) == 0;
        var healthBarData = m_dataManager.GetHealthBarData();
        var array = healthBarData.ArrayData;
        array.EnsureCapacity(array.Length + 1);
        ref var vertex = ref array.Data[array.Length];
        // Prevent small health values from rendering zero pixels
        float min = 1f / (entity.Properties.HealthBarWidth + MinBarWidth - 5);
        // Normalized health percent (0-255)
        int health = (int)(Math.Max(min, entity.Health / (float)entity.Properties.Health) * 255f);
        vertex.SurfaceOptions = VertexOptions.EntityPackSurface(1, attackFlash ? 1 : 0, 0, entity.Properties.HealthBarWidth, health);
        vertex.Pos = entityVertex.Pos;
        vertex.PrevPos = entityVertex.PrevPos;
        vertex.OffsetXYZ = VertexOptions.EntityPackXYZ(0, offset);

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
        program.BoundTexture(BindTextures.BoundTexture);
        program.BrightmapTexture(BindTextures.BrightmapTexture);
        program.ColormapTexture(BindTextures.Colormap);
        program.SectorColormapTexture(BindTextures.SectorColormap);
        program.SectorFogTexture(BindTextures.SectorFog);
        program.ExtraLight(renderInfo.Uniforms.ExtraLightOrColorMapIndex);
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
        program.ScreenBounds((renderInfo.Viewport.Width - 1, renderInfo.Viewport.Height - 1));
        program.CheckPlaneClip(m_vanillaRender);
        program.UseBrightmaps(renderInfo.Uniforms.UseBrightmaps);
        program.UseSectorColor(renderInfo.Uniforms.SectorColor);
        program.UseSectorFog(renderInfo.Uniforms.SectorFogIndex);
        program.SetSpriteClipDownScaleAmount(Math.Max(renderInfo.Uniforms.DownScaleAmount, 1));
        program.ColorClamp(1f);

        // The fade distance calculations work using squared distances
        float maxDistanceSquared = renderInfo.Uniforms.MaxDistance * renderInfo.Uniforms.MaxDistance;
        program.MaxDistanceSquared(maxDistanceSquared);
        program.FadeDistance(maxDistanceSquared / 2);

        if (program is EntityCompositeProgram)
        {
            program.AccumTexture(BindTextures.AccumTexture);
            program.AccumCountTexture(BindTextures.AccumCountTexture);
        }

        if (program is EntityFuzzRefractionProgram)
        {
            program.AccumTexture(BindTextures.AccumTexture);
            program.AccumCountTexture(BindTextures.AccumCountTexture);
            program.FuzzTexture(BindTextures.FuzzTexture);
            program.OpaqueTexture(BindTextures.OpaqueTexture);
        }

        program.WallClipTexture(BindTextures.WallClipTexture);
        program.PlaneClipTexture(BindTextures.PlaneClipTexture);
        program.MapDataTexture(BindTextures.MapLineData);
        program.LineHeightsTexture(BindTextures.LineHeights);
    }

    public void RenderOpaque(RenderInfo renderInfo)
    {
        m_program.Bind();
        GL.ActiveTexture(BindTextures.BoundTexture);
        SetUniforms(m_program, renderInfo);
        m_dataManager.RenderByRenderStyle(RenderDataStyle.Normal, PrimitiveType.Points);

        if (m_healthBars)
        {
            m_healthBarProgram.Bind();
            SetUniforms(m_healthBarProgram, renderInfo);
            m_dataManager.RenderHealthBars();
        }
    }

    public void RenderOitTransparentPass(RenderInfo renderInfo)
    {
        m_programTransparent.Bind();
        m_programTransparent.RenderFuzz(false);
        GL.ActiveTexture(BindTextures.BoundTexture);
        SetUniforms(m_programTransparent, renderInfo);
        m_dataManager.RenderByRenderStyle(RenderDataStyle.Translucent, PrimitiveType.Points);
        m_dataManager.RenderByRenderStyle(RenderDataStyle.Add, PrimitiveType.Points);
        m_dataManager.RenderByRenderStyle(RenderDataStyle.Fuzzy, PrimitiveType.Points);
        m_programTransparent.ColorClamp(0.9f);
        m_dataManager.RenderByRenderStyle(RenderDataStyle.ColorAdd, PrimitiveType.Points);
        m_programTransparent.Unbind();
    }

    public void RenderOitTransparentFuzzPass(RenderInfo renderInfo)
    {
        m_programTransparent.Bind();
        m_programTransparent.RenderFuzz(true);
        GL.ActiveTexture(BindTextures.BoundTexture);
        SetUniforms(m_programTransparent, renderInfo);
        m_dataManager.RenderByRenderStyle(RenderDataStyle.Fuzzy, PrimitiveType.Points);
        m_programTransparent.Unbind();
    }

    public void StartRenderOitCompositePass(RenderInfo renderInfo)
    {
        m_programComposite.Bind();
        GL.ActiveTexture(BindTextures.BoundTexture);
        SetUniforms(m_programComposite, renderInfo);
    }

    public void RenderOitFuzzRefractionPass(RenderInfo renderInfo, bool renderColor)
    {
        m_programFuzzRefraction.Bind();
        GL.ActiveTexture(BindTextures.BoundTexture);
        m_programFuzzRefraction.RenderFuzzRefractionColor(renderColor);
        SetUniforms(m_programFuzzRefraction, renderInfo);
        m_dataManager.RenderByRenderStyle(RenderDataStyle.Fuzzy, PrimitiveType.Points);
        m_programFuzzRefraction.Unbind();
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
    
    private sealed class Vec2DComparer : IEqualityComparer<Vec2D>
    {
        public bool Equals(Vec2D x, Vec2D y) => x.X == y.X && x.Y == y.Y;
        public int GetHashCode(Vec2D obj) => HashCode.Combine(obj.X, obj.Y);
    }
}
