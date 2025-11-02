using System;
using System.Collections.Generic;
using GlmSharp;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Maps.Specials.ZDoom;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Vertex;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Container;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Impl.SinglePlayer;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Automap;

public class LegacyAutomapRenderer : IDisposable
{
    readonly record struct KeyColors(Color Color, Color ImageColor);
    readonly record struct ColorRange(int Start, Vec3F Color);
    private readonly ArchiveCollection m_archiveCollection;
    private readonly StreamVertexBuffer<AutomapVertex> m_vbo;
    private readonly VertexArrayObject m_vao;
    private readonly AutomapShader m_shader;
    private readonly List<ColorRange> m_vboRanges = [];
    private readonly List<ColorRange> m_highlightVboRanges = [];
    private readonly DynamicArray<vec2> m_points = [];
    private readonly AutomapColorPoints m_colorPoints = new();
    private readonly AutomapColorPoints m_highlightColorPoints = new();
    private readonly HashSet<int> m_teleportLines = [];
    private readonly HashSet<int> m_exitLines = [];
    private readonly List<Color> m_transferColors = [];
    private readonly DynamicArray<Entity> m_mapMarkers = [];

    private float m_offsetX;
    private float m_offsetY;
    private int m_lastOffsetX;
    private int m_lastOffsetY;
    private bool m_disposed;
    private bool m_rotate;
    private bool m_keyImageColor;
    private bool m_flashTime;
    private Box2D m_boundingBox;

    private readonly Dictionary<string, KeyColors> m_keysByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<ZDoomKeyType, KeyColors> m_keysByNumber = [];

    private Color m_wallColor;
    private Color m_twoSidedWallColor;
    private Color m_unseenWallColor;
    private Color m_teleportLineColor;
    private Color m_exitLineColor;

    private Color m_playerColor;
    private Color m_thingColor;
    private Color m_pickupColor;
    private Color m_monsterColor;
    private Color m_deadMonsterColor;

    private Color m_markerColor;
    private Color m_markerColorAlt;

    private void SetColors()
    {
        var automap = m_archiveCollection.Config.Hud.AutoMap;
        var colors = automap.Overlay ? automap.OverlayColors : automap.DefaultColors;
        m_wallColor = new(colors.WallColor.Value);
        m_twoSidedWallColor = new(colors.TwoSidedWallColor.Value);
        m_unseenWallColor = new(colors.UnseenWallColor.Value);
        m_teleportLineColor = new(colors.TeleportLineColor.Value);
        m_exitLineColor = new(colors.ExitLineColor.Value);
        m_playerColor = new(colors.PlayerColor.Value);
        m_thingColor = new(colors.ThingColor.Value);
        m_pickupColor = new(colors.PickupColor.Value);
        m_monsterColor = new(colors.MonsterColor.Value);
        m_deadMonsterColor = new(colors.DeadMonsterColor.Value);
        m_markerColor = new(colors.MakerColor.Value);
        m_markerColorAlt = new(colors.AltMakerColor.Value);
        m_keyImageColor = automap.ImageKeyColor;
    }

    public LegacyAutomapRenderer(ArchiveCollection archiveCollection)
    {
        m_archiveCollection = archiveCollection;
        m_vao = new("Automap");
        m_vbo = new("Automap");
        m_shader = new();

        Attributes.BindAndApply(m_vbo, m_vao, m_shader.Attributes);
    }

    ~LegacyAutomapRenderer()
    {
        PerformDispose();
    }

    public void Render(IWorld world, RenderInfo renderInfo)
    {
        m_flashTime = WorldStatic.World.GameTicker / (int)(Constants.TicksPerSecond / 3) % 2 == 0;
        SetColors();

        // Consider both offsets at zero a reset
        if (renderInfo.AutomapOffset.X == 0 && renderInfo.AutomapOffset.Y == 0)
        {
            m_offsetX = 0;
            m_offsetY = 0;
            m_lastOffsetX = 0;
            m_lastOffsetY = 0;
        }

        if (m_lastOffsetX != renderInfo.AutomapOffset.X || m_lastOffsetY != renderInfo.AutomapOffset.Y)
        {
            m_offsetX += (renderInfo.AutomapOffset.X - m_lastOffsetX) * 64 * 1 / (float)renderInfo.AutomapScale;
            m_offsetY += (renderInfo.AutomapOffset.Y - m_lastOffsetY) * 64 * 1 / (float)renderInfo.AutomapScale;
            RenderInfo.LastAutomapOffset = (m_offsetX, m_offsetY);
            m_lastOffsetX = renderInfo.AutomapOffset.X;
            m_lastOffsetY = renderInfo.AutomapOffset.Y;
        }

        // Don't rotate if the user has applied offsets
        m_rotate = m_offsetX == 0 && m_offsetY == 0 && m_archiveCollection.Config.Hud.AutoMap.Rotate;

        SetBoundingBox(renderInfo);
        PopulateData(world, renderInfo, out _);

        GL.LineWidth(1);
        RenderElements(world, renderInfo, m_vboRanges);

        GL.LineWidth(1.25f);
        RenderElements(world, renderInfo, m_highlightVboRanges, m_flashTime ? 0.25f : 0);
    }

    private void RenderElements(IWorld world, RenderInfo renderInfo, List<ColorRange> vboRanges, float changeColorAmount = 0)
    {
        m_shader.Bind();
        m_shader.Mvp(CalculateMvp(renderInfo, world.Config));
        for (int i = 0; i < vboRanges.Count; i++)
        {
            (int first, Vec3F color) = vboRanges[i];
            int count = i == vboRanges.Count - 1 ? m_vbo.Count - first : vboRanges[i + 1].Start - first;

            if (changeColorAmount > 0)
            {
                color.X = ChangeColor(color.X, 0.25f);
                color.Y = ChangeColor(color.Y, 0.25f);
                color.Z = ChangeColor(color.Z, 0.25f);
            }

            m_shader.Color(color);
            m_vao.Bind();
            GL.DrawArrays(PrimitiveType.Lines, first, count);
            m_vao.Unbind();
        }

        m_shader.Unbind();
    }

    private static float ChangeColor(float color, float amount)
    {
        var changeAmount = color + amount;
        if (changeAmount > 1 + amount / 2)
            return color - amount;
        return changeAmount;
    }

    public void UpdateTo(IWorld world)
    {
        m_teleportLines.Clear();
        m_exitLines.Clear();

        foreach (var line in world.Lines)
        {
            if (line.Special.IsTeleport())
                m_teleportLines.Add(line.Id);
            if (line.Special.IsExitSpecial())
                m_exitLines.Add(line.Id);
        }

        foreach (var lockDef in m_archiveCollection.Definitions.LockDefinitions.LockDefs)
        {
            m_keysByNumber[lockDef.KeyNumber] = new(lockDef.MapColor, lockDef.KeyImageColor);
            foreach (var item in lockDef.KeyDefinitionNames)
                m_keysByName[item] = new(lockDef.MapColor, lockDef.KeyImageColor);
        }
    }

    private void SetBoundingBox(RenderInfo renderInfo)
    {
        // Not optimally correct but works well enough. Would be best if this used the same method as static rendering.
        var center = new Vec2D(renderInfo.Camera.PositionInterpolated.X + m_offsetX, renderInfo.Camera.PositionInterpolated.Y + m_offsetY);
        var scale = m_archiveCollection.Config.Hud.AutoMap.Scale;
        double BoxScale = m_rotate ? 2.2 / scale : 2 / scale;
        var width = (renderInfo.Viewport.Width * BoxScale) / 2;
        var height = (renderInfo.Viewport.Height * BoxScale) / 2;

        if (m_rotate)
        {
            width = Math.Max(width, height);
            height = Math.Max(width, height);
        }

        m_boundingBox = new Box2D((center.X - width, center.Y - height), (center.X + width, center.Y + height));
    }

    private mat4 CalculateMvp(RenderInfo renderInfo, IConfig config, float scaleFactor = 1f)
    {
        vec2 scale = CalculateScale(renderInfo);
        vec3 camera = renderInfo.Camera.PositionInterpolated.GlmVector;

        mat4 model = mat4.Scale(scale.x * scaleFactor, scale.y * scaleFactor, 1.0f);
        if (m_rotate)
            model *= mat4.RotateZ(-renderInfo.Camera.YawRadians + MathF.PI / 2);
        mat4 view = mat4.Translate(-camera.x - m_offsetX, -camera.y - m_offsetY, 0);
        mat4 proj = mat4.Identity;

        return model * view * proj;
    }

    private static vec2 CalculateScale(RenderInfo renderInfo)
    {
        // Note: we're translating to NDC coordinates, so everything should
        // end up between [-1.0, 1.0].
        (float vW, float vH) = (renderInfo.Viewport.Width, renderInfo.Viewport.Height);

        // TODO: Do this properly...
        float scale = (float)renderInfo.AutomapScale;
        return new vec2(1 / vW * scale, 1 / vH * scale);
    }

    private void PopulateData(IWorld world, RenderInfo renderInfo, out Box2F box2F)
    {
        Player? player = renderInfo.ViewerEntity.PlayerObj;
        m_mapMarkers.Clear();
        m_vbo.Clear();
        PopulateColoredLines(world, player);
        PopulateThings(world, player, renderInfo);
        // Prevent the player arrow from being too small, perceptually targeting 1080p;
        // Since we're scaling based on that, the cube root prevents too-small minimums
        // for smaller resolutions and too-high minimums for higher resolutions
        float minPlayerScale = (float)Math.Pow(renderInfo.Viewport.Height / 1080f, 0.33);
        float playerScaleMultipler = Math.Max(1, minPlayerScale / (float)renderInfo.AutomapScale);
        DrawEntity(player, renderInfo.TickFraction, playerScaleMultipler);
        DrawHighlightAreas(world, renderInfo);

        if (world is SinglePlayerWorld singlePlayerWorld)
            DrawAutomapTracers(world, singlePlayerWorld.Player);

        if (player != null && (m_offsetX != 0 || m_offsetY != 0))
            DrawCenterCross(player, renderInfo);

        TransferLineDataIntoBuffer(m_colorPoints, m_vboRanges, out box2F);
        TransferLineDataIntoBuffer(m_highlightColorPoints, m_highlightVboRanges, out _);
        m_vbo.UploadIfNeeded();
    }

    private void DrawAutomapTracers(IWorld world, Player player)
    {
        var node = player.Tracers.Tracers.First;
        while (node != null)
        {
            if (node.Value.AutomapColor.HasValue)
            {
                var info = node.Value;
                for (int i = 0; i < info.Segs.Count; i++)
                {
                    var seg = info.Segs[i];
                    AddLine(node.Value.AutomapColor.Value, seg.Start.XY, seg.End.XY);
                }
            }
            node = node.Next;
        }
    }

    private void DrawCenterCross(Player player, RenderInfo renderInfo)
    {
        const int VirtualLength = 17;
        var center = player.PrevPosition.Interpolate(player.Position, renderInfo.TickFraction);
        float x = (float)center.X + m_offsetX;
        float y = (float)center.Y + m_offsetY;
        float length = VirtualLength * 1 / (float)renderInfo.AutomapScale;
        // Center the cross
        float offset = length / VirtualLength / 2.0f;

        DynamicArray<vec2> array = m_colorPoints.GetPoints(m_markerColor);
        array.Add(new vec2(x - length, y - offset));
        array.Add(new vec2(x + length, y - offset));
        array.Add(new vec2(x - offset, y - length));
        array.Add(new vec2(x - offset, y + length));
    }

    private void PopulateThings(IWorld world, Player? player, RenderInfo renderInfo)
    {
        if (player == null)
            return;

        for (var entity = world.EntityManager.Head; entity != null; entity = entity.Next)
        {
            if (!m_boundingBox.Contains(entity.Position))
                continue;

            if (entity.Definition.EditorId == (int)EditorId.MapMarker)
            {
                m_mapMarkers.Add(entity);
                continue;
            }

            if (!player.Cheats.IsCheatActive(CheatType.AutoMapModeShowAllLinesAndThings))
                continue;

            DrawEntity(entity, renderInfo.TickFraction);
        }
    }

    private void PopulateColoredLines(IWorld world, Player? player)
    {
        m_colorPoints.Clear();
        m_highlightColorPoints.Clear();

        bool allMap = false;
        if (player != null)
        {
            allMap = player.Inventory.IsPowerupActive(PowerupType.ComputerAreaMap) ||
                player.Cheats.IsCheatActive(CheatType.AutoMapModeShowAllLines) ||
                player.Cheats.IsCheatActive(CheatType.AutoMapModeShowAllLinesAndThings);
        }

        bool forceDraw = !world.Config.Render.AutomapBspThread;
        bool markSecrets = world.Config.Game.MarkSecrets;
        bool markFlood = world.Config.Developer.MarkFlood;
        bool checkMarkedSectors = markSecrets || markFlood || world.Config.Game.MarkSpecials;

        int length = world.StructLines.Length;
        var lineArray = world.StructLines.Data;
        for (int i = 0; i < length; i++)
        {
            ref var line = ref lineArray[i];
            var start = line.Segment.Start;
            var end = line.Segment.End;
            if (!m_boundingBox.Contains(start) && !m_boundingBox.Contains(end))
                continue;

            bool markedLine = IsLineMarked(ref line, markSecrets, markFlood, checkMarkedSectors);
            if (!forceDraw && !line.AutomapFlags.AlwaysDraw && !markedLine && (!allMap && !line.SeenForAutomap || line.AutomapFlags.NeverDraw))
                continue;

            if (!markedLine && line.LockKey != -1)
            {
                AddLockedLine(line.LockKey, start, end);
                continue;
            }

            if (line.BackSector == null || line.Secret || line.AutomapFlags.AlwaysDraw)
            {
                AddLine(GetOneSidedColor(ref line, forceDraw, markedLine), start, end);
                continue;
            }

            var color = GetTwoSidedColor(ref line, forceDraw, markedLine, out var specialColor);
            if (!allMap && !specialColor && line.BackFloorPlane != null && line.BackCeilingPlane != null &&
                line.FrontFloorPlane.Z == line.BackFloorPlane.Z && line.FrontCeilingPlane.Z == line.BackCeilingPlane.Z)
                continue;

            AddLine(color, start, end);
        }
    }

    private Color GetOneSidedColor(ref StructLine line, bool forceDraw, bool marked)
    {
        if (marked)
            return GetMarkedColor();

        if (line.SeenForAutomap || forceDraw)
            if (m_exitLines.Contains(line.Id))
                return m_exitLineColor;
            else
                return m_wallColor;

        return m_unseenWallColor;
    }

    private Color GetTwoSidedColor(ref StructLine line, bool forceDraw, bool marked, out bool specialColor)
    {
        specialColor = false;

        if (marked)
        {
            specialColor = true;
            return GetMarkedColor();
        }

        if (line.SeenForAutomap || forceDraw)
        {
            if (m_teleportLines.Contains(line.Id))
            {
                specialColor = true;
                return m_teleportLineColor;
            }
            if (m_exitLines.Contains(line.Id))
            {
                specialColor = true;
                return m_exitLineColor;
            }

            return m_twoSidedWallColor;
        }

        return m_unseenWallColor;
    }

    private Color GetMarkedColor() => m_flashTime ? m_markerColor : m_markerColorAlt;

    private static bool IsLineMarked(ref StructLine line, bool markSecrets, bool markFlood, bool checkMarkedSectors)
    {
        if (line.MarkAutomap)
            return true;

        if (!checkMarkedSectors)
            return false;

        if (line.FrontSector.MarkAutomap || (line.BackSector != null && line.BackSector.MarkAutomap))
            return true;

        if (markSecrets && (line.FrontSector.Secret || line.BackSector != null && line.BackSector.Secret))
            return true;

        if (markFlood && (line.FrontSector.Flood || line.BackSector != null && line.BackSector.Flood))
            return true;

        return false;
    }

    private bool AddLockedLine(int keyNumber, in Vec2D start, in Vec2D end)
    {
        if (keyNumber == 0)
            return false;

        if (m_keysByNumber.TryGetValue((ZDoomKeyType)keyNumber, out var color))
        {
            AddKeyLine(m_keyImageColor ? color.ImageColor : color.Color, start, end);
            return true;
        }

        return false;
    }

    void AddKeyLine(Color color, Vec2D start, Vec2D end)
    {
        DynamicArray<vec2> array = m_highlightColorPoints.GetPoints(color);
        array.Add(new vec2((float)start.X, (float)start.Y));
        array.Add(new vec2((float)end.X, (float)end.Y));
    }

    void AddLine(Color color, Vec2D start, Vec2D end)
    {
        DynamicArray<vec2> array = m_colorPoints.GetPoints(color);
        array.Add(new vec2((float)start.X, (float)start.Y));
        array.Add(new vec2((float)end.X, (float)end.Y));
    }

    private void DrawEntity(Entity? entity, float interpolateFrac, float scaleMultiplier = 1)
    {
        if (entity == null)
            return;

        // Ignore player starts and deathmatch starts
        if (EditorIds.IsPlayerStart(entity.Definition.EditorId) || entity.Definition.EditorId == (int)EditorId.DeathmatchStart)
            return;

        m_points.Clear();

        // We start with the arrow facing along the positive X axis direction.
        // This way, our rotation can be easily done.
        var center = entity.PrevPosition.Interpolate(entity.Position, interpolateFrac);
        var radius = (float)entity.Radius * scaleMultiplier;
        var (centerX, centerY) = center.XY.Float;
        float halfWidth = radius / 2;
        float halfHeight = radius / 2;
        float quarterWidth = radius / 4;
        float quarterHeight = radius / 4;

        mat4 transform = CreateTransform((float)entity.AngleRadians, centerX, centerY);
        Color color = m_thingColor;
        bool flash = false;

        if (m_keysByName.TryGetValue(entity.Definition.Name, out var keyColors))
        {
            flash = true;
            color = m_keyImageColor ? keyColors.ImageColor : keyColors.Color;
        }
        else if (entity.Flags.CountKill())
        {
            color = entity.IsDead() ? m_deadMonsterColor : m_monsterColor;
        }
        else if (entity.Definition.IsType(EntityDefinitionType.Inventory))
        {
            color = m_pickupColor;
        }
        else if (entity.Definition.EditorId == (int)EditorId.MapMarker)
        {
            color = m_thingColor;
            flash = true;
        }

        if (entity.Definition.EditorId == (int)EditorId.TeleportLanding)
        {
            color = m_thingColor;
            AddSquare(-quarterWidth, -quarterHeight, halfWidth, halfHeight, transform);
        }
        else if (flash)
        {
            // Draw a square for keys, make it flash
            if (m_flashTime)
                AddSquare(-quarterWidth, -quarterHeight, halfWidth, halfHeight, transform);
        }
        else if (entity.IsPlayer)
        {
            color = m_playerColor;
            // Main arrow from middle left to middle right
            AddLine(-halfWidth, 0, halfWidth, 0, transform);

            // Arrow from the right tip to the top middle at 45 degrees. Same
            // for the bottom one.
            AddLine(halfWidth, 0, quarterWidth, quarterHeight, transform);
            AddLine(halfWidth, 0, quarterWidth, -quarterHeight, transform);
        }
        else
        {
            AddLine(-halfWidth, quarterHeight, halfWidth, 0, transform);
            AddLine(-halfWidth, -quarterHeight, halfWidth, 0, transform);
            AddLine(-halfWidth, -quarterHeight, -halfWidth, quarterHeight, transform);
        }

        DynamicArray<vec2> array = m_colorPoints.GetPoints(color);
        for (int i = 0; i < m_points.Length; i++)
            array.Add(m_points[i]);
    }

    private void DrawHighlightAreas(IWorld world, RenderInfo renderInfo)
    {
        m_points.Clear();
        foreach (var highlightArea in world.HighlightAreas)
            DrawHighlightArea(world, renderInfo, highlightArea.Position.X, highlightArea.Position.Y, highlightArea.Area);

        var array = m_colorPoints.GetPoints(m_markerColor);
        for (int i = 0; i < m_points.Length; i++)
            array.Add(m_points[i]);

        m_points.Clear();
        for (int i = 0; i < m_mapMarkers.Length; i++)
        {
            var entity = m_mapMarkers.Data[i];
            DrawHighlightArea(world, renderInfo, entity.Position.X, entity.Position.Y, 64);
        }

        array = m_colorPoints.GetPoints(m_thingColor);
        for (int i = 0; i < m_points.Length; i++)
            array.Add(m_points[i]);
    }

    private void DrawHighlightArea(IWorld world, RenderInfo renderInfo, double x, double y, int area)
    {
        var scaleArea = (float)area * 1 / (float)renderInfo.AutomapScale;
        var angle = (float)((world.GameTicker / 4) % MathHelper.HalfPi);
        var halfWidth = scaleArea / 2;
        AddSquare(-halfWidth, -halfWidth, scaleArea, scaleArea, CreateTransform(angle, (float)x, (float)y));
    }

    private static mat4 CreateTransform(float angleRadians, float centerX, float centerY)
    {
        mat4 rotate = mat4.Rotate(angleRadians, vec3.UnitZ);
        mat4 translate = mat4.Translate(centerX, centerY, 0);
        mat4 transform = translate * rotate;
        return transform;
    }

    void AddSquare(float startX, float startY, float halfWidth, float height, mat4 transform)
    {
        AddLine(startX, startY, startX, startY + height, transform);
        AddLine(startX, startY + height, startX + halfWidth, startY + height, transform);
        AddLine(startX + halfWidth, startY + height, startX + halfWidth, startY, transform);
        AddLine(startX + halfWidth, startY, startX, startY, transform);
    }

    void AddLine(float startX, float startY, float endX, float endY, mat4 transform)
    {
        vec4 s = transform * new vec4(startX, startY, 0, 1);
        vec4 e = transform * new vec4(endX, endY, 0, 1);
        m_points.Add(s.xy);
        m_points.Add(e.xy);
    }

    private void TransferLineDataIntoBuffer(AutomapColorPoints colorPoints, List<ColorRange> vboRanges, out Box2F box2F)
    {
        float minX = float.PositiveInfinity;
        float minY = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float maxY = float.NegativeInfinity;

        vboRanges.Clear();
        m_transferColors.Clear();

        colorPoints.GetColors(m_transferColors);

        for (int i = 0; i < m_transferColors.Count; i++)
        {
            var color = m_transferColors[i];
            DynamicArray<vec2> lines = colorPoints.GetPoints(m_transferColors[i]);
            if (lines.Length == 0)
                continue;

            Vec3F colorVec = new(color.R / 255f, color.G / 255f, color.B / 255f);
            vboRanges.Add(new(m_vbo.Count, colorVec));

            for (int j = 0; j < lines.Length; j++)
                AddLineToVbo(lines[j], ref minX, ref minY, ref maxX, ref maxY);
        }

        // This is a backup case in the event there are no lines.
        if (float.IsPositiveInfinity(minX))
        {
            minX = 0;
            minY = 0;
            maxX = 1;
            maxY = 1;
        }

        box2F = ((minX, minY), (maxX, maxY));
    }

    void AddLineToVbo(vec2 line, ref float minX, ref float minY, ref float maxX, ref float maxY)
    {
        m_vbo.Add(new AutomapVertex(line.x, line.y));

        if (line.x < minX)
            minX = line.x;
        if (line.y < minY)
            minY = line.y;
        if (line.x > maxX)
            maxX = line.x;
        if (line.y > maxY)
            maxY = line.y;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        PerformDispose();
    }

    private void PerformDispose()
    {
        if (m_disposed)
            return;

        m_shader.Dispose();
        m_vbo.Dispose();
        m_vao.Dispose();

        m_disposed = true;
    }
}
