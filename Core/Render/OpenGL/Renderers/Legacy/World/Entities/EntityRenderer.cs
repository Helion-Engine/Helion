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

public class EntityRenderer
{
    private class Vec2DCompararer : IEqualityComparer<Vec2D>
    {
        public bool Equals(Vec2D x, Vec2D y)
        {
            return x.X == y.X && x.Y == y.Y;
        }

        public int GetHashCode([DisallowNull] Vec2D obj)
        {
            return HashCode.Combine(obj.X, obj.Y);
        }
    }

    private readonly IConfig m_config;
    private readonly LegacyGLTextureManager m_textureManager;
    private readonly EntityProgram m_program = new();
    private readonly RenderDataManager<EntityVertex> m_dataManager;
    private readonly RenderWorldDataManager m_worldDataManager;
    private readonly Dictionary<Vec2D, int> m_renderPositions = new(1024, new Vec2DCompararer());
    private int m_renderCounter;
    private double m_tickFraction;
    private Vec2F m_viewRightNormal;
    private bool m_singleVertex;
    private bool m_spriteAlpha;
    private int m_viewerEntityId;

    public EntityRenderer(IConfig config, LegacyGLTextureManager textureManager, RenderWorldDataManager worldDataManager)
    {
        m_config = config;
        m_textureManager = textureManager;
        m_dataManager = new(m_program);
        m_worldDataManager = worldDataManager;
        m_singleVertex = m_config.Render.SingleVertexSprites;
        m_spriteAlpha = m_config.Render.SpriteTransparency;
    }
    
    public void Clear(IWorld world)
    {
        m_dataManager.Clear();
        m_renderPositions.Clear();
        m_renderCounter++;
        m_singleVertex = m_config.Render.SingleVertexSprites;
        m_spriteAlpha = m_config.Render.SpriteTransparency;
    }

    public void SetTickFraction(double tickFraction) =>
        m_tickFraction = tickFraction;

    public void SetViewDirection(Entity viewerEntity, Vec2D viewDirection)
    {
        m_viewerEntityId = viewerEntity.Id;
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

    public bool ShouldNotDraw(Entity entity)
    {
        return entity.Frame.IsInvisible || entity.Flags.Invisible || entity.Flags.NoSector || entity.RenderedCounter == m_renderCounter || entity.Id == m_viewerEntityId;
    }

    private void AddSpriteQuadSingleVertex(Entity entity, GLLegacyTexture texture, short lightLevel, bool mirror, in Vec2D nudgeAmount)
    {
        const byte MaxAlpha = 255;

        bool useAlpha = m_spriteAlpha && entity.Definition.Properties.Alpha < 1;
        RenderData<EntityVertex> renderData = useAlpha ? m_dataManager.GetAlpha(texture) : m_dataManager.GetNonAlpha(texture);

        Vec3D position = entity.Position;
        Vec3D prevPosition = entity.PrevPosition; position.X -= nudgeAmount.X;
        position.Y -= nudgeAmount.Y;
        prevPosition.X -= nudgeAmount.X;
        prevPosition.Y -= nudgeAmount.Y;

        float bottomZ = (float)position.Z;
        float offsetZ = GetOffsetZ(entity, texture);

        Vec3F pos = position.Float;
        Vec3F prevPos = prevPosition.Float;
        byte alpha = useAlpha ? (byte)(entity.Definition.Properties.Alpha * MaxAlpha) : MaxAlpha;
        EntityVertex vertex = new(pos, prevPos, offsetZ, lightLevel, alpha, entity.Flags.Shadow, mirror);
        renderData.Vbo.Add(vertex);
    }

    private void AddSpriteQuad(Entity entity, GLLegacyTexture texture, bool mirror, in Vec2D nudgeAmount)
    {
        float offsetZ = GetOffsetZ(entity, texture);
        Vec3D position = entity.Position;
        Vec3D prevPosition = entity.PrevPosition;
        position.X -= nudgeAmount.X;
        position.Y -= nudgeAmount.Y;
        prevPosition.X -= nudgeAmount.X;
        prevPosition.Y -= nudgeAmount.Y;

        SpriteQuad pos = CalculateQuad(position, offsetZ, entity, texture);
        SpriteQuad prevPos = CalculateQuad(prevPosition, offsetZ, entity, texture);
        float alpha = m_spriteAlpha ? (float)entity.Definition.Properties.Alpha : 1.0f;
        float fuzz = entity.Flags.Shadow ? 1.0f : 0.0f;
        float leftU = 0.0f;
        float rightU = 1.0f;
        if (mirror)
        {
            leftU = 1.0f;
            rightU = 0.0f;
        }

        if (entity.Flags.Shadow)
            alpha = 0.99f;

        int lightBuffer = entity.Flags.Bright || entity.Frame.Properties.Bright ? Constants.Render.FullBrightLightBufferIndex :
            StaticCacheGeometryRenderer.GetLightBufferIndex(entity.Sector.Id, LightBufferType.Wall);
        
        LegacyVertex topLeft = new(pos.Left.X, pos.Left.Y, pos.TopZ, prevPos.Left.X, prevPos.Left.Y, prevPos.TopZ, leftU, 0.0f, 0, alpha, fuzz,
            lightLevelBufferIndex: lightBuffer);
        LegacyVertex topRight = new(pos.Right.X, pos.Right.Y, pos.TopZ, prevPos.Right.X, prevPos.Right.Y, prevPos.TopZ, rightU, 0.0f, 0, alpha, fuzz,
            lightLevelBufferIndex: lightBuffer);
        LegacyVertex bottomLeft = new(pos.Left.X, pos.Left.Y, pos.BottomZ, prevPos.Left.X, prevPos.Left.Y, prevPos.BottomZ, leftU, 1.0f, 0, alpha, fuzz,
            lightLevelBufferIndex: lightBuffer);
        LegacyVertex bottomRight = new(pos.Right.X, pos.Right.Y, pos.BottomZ, prevPos.Right.X, prevPos.Right.Y, prevPos.BottomZ, rightU, 1.0f, 0, alpha, fuzz,
            lightLevelBufferIndex: lightBuffer);

        RenderWorldData renderWorldData = alpha < 1 ?
            m_worldDataManager.GetAlphaRenderData(texture, m_program) :
            m_worldDataManager.GetRenderData(texture, m_program);

        renderWorldData.Vbo.Add(topLeft);
        renderWorldData.Vbo.Add(bottomLeft);
        renderWorldData.Vbo.Add(topRight);
        renderWorldData.Vbo.Add(topRight);
        renderWorldData.Vbo.Add(bottomLeft);
        renderWorldData.Vbo.Add(bottomRight);
    }

    private SpriteQuad CalculateQuad(in Vec3D entityCenterBottom, float offsetZ, Entity entity, GLLegacyTexture texture)
    {
        SpriteQuad spriteQuad = new();
        // We need to find the perpendicular vector from the entity so we
        // know where to place the quad vertices.
        Vec2F rightNormal = m_viewRightNormal;
        Vec2F entityCenterXY = entityCenterBottom.XY.Float;
        // Multiply the X offset by the rightNormal X/Y to move the sprite according to the player's view
        // Doom graphics are drawn left to right and not centered
        entityCenterXY -= rightNormal * texture.Offset.X;

        spriteQuad.Left = entityCenterXY;
        spriteQuad.Right = entityCenterXY + (rightNormal * texture.Dimension.Width);

        spriteQuad.BottomZ = (float)entityCenterBottom.Z + offsetZ;
        spriteQuad.TopZ = spriteQuad.BottomZ + texture.Height;
        return spriteQuad;
    }

    private float GetOffsetZ(Entity entity, GLLegacyTexture texture)
    {
        float offsetAmount = texture.Offset.Y - texture.Height;
        if (offsetAmount >= 0 || entity.Definition.Flags.Missile)
            return offsetAmount;

        if (!m_config.Render.SpriteClip)
            return 0;

        if (texture.Height < m_config.Render.SpriteClipMin || entity.Definition.IsInventory)
            return 0;

        if (entity.Position.Z - entity.HighestFloorSector.Floor.Z < texture.Offset.Y)
        {
            if (-offsetAmount > texture.Height * m_config.Render.SpriteClipFactorMax)
                offsetAmount = -texture.Height * (float)m_config.Render.SpriteClipFactorMax;
            return offsetAmount;
        }

        return 0;
    }

    public void RenderEntity(Sector viewSector, Entity entity, in Vec3D position)
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

        if (m_config.Render.SpriteZCheck)
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

        if (m_singleVertex)
            AddSpriteQuadSingleVertex(entity, texture, 0, spriteRotation.Mirror, nudgeAmount);
        else
            AddSpriteQuad(entity, texture, spriteRotation.Mirror, nudgeAmount);
        entity.RenderedCounter = m_renderCounter;
    }

    private void SetUniforms(RenderInfo renderInfo)
    {
        const int TicksPerFrame = 4;
        const int DifferentFrames = 8;
        
        mat4 mvp = Renderer.CalculateMvpMatrix(renderInfo);
        mat4 mvpNoPitch = Renderer.CalculateMvpMatrix(renderInfo, true);
        float fuzzFrac = (((renderInfo.ViewerEntity.World.GameTicker / TicksPerFrame) % DifferentFrames)) + 1;
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
}
