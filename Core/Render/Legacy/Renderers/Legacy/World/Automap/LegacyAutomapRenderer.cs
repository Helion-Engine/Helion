using System;
using System.Collections.Generic;
using System.Drawing;
using GlmSharp;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials.ZDoom;
using Helion.Render.Legacy.Buffer.Array.Vertex;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Shared;
using Helion.Render.Legacy.Vertex;
using Helion.Render.Legacy.Vertex.Attribute;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.Locks;
using Helion.Util;
using Helion.Util.Container;
using Helion.Util.Extensions;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Legacy.Renderers.Legacy.World.Automap
{
    public class LegacyAutomapRenderer : IDisposable
    {
        private static readonly VertexArrayAttributes Attributes = new(new VertexPointerFloatAttribute("pos", 0, 2));

        private readonly IGLFunctions gl;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly LegacyAutomapShader m_shader;
        private readonly StreamVertexBuffer<vec2> m_vbo;
        private readonly VertexArrayObject m_vao;
        private readonly List<DynamicArray<vec2>> m_colorEnumToLines = new();
        private readonly List<(int start, vec3 color)> m_vboRanges = new();
        private readonly DynamicArray<vec2> m_entityPoints = new(); 
        private bool m_disposed;

        private float m_offsetX;
        private float m_offsetY;

        private int m_lastOffsetX;
        private int m_lastOffsetY;

        private readonly Dictionary<string, AutomapColor> m_keys = new(StringComparer.OrdinalIgnoreCase);
        
        public LegacyAutomapRenderer(GLCapabilities capabilities, IGLFunctions glFunctions, ArchiveCollection archiveCollection)
        {
            gl = glFunctions;
            m_archiveCollection = archiveCollection;
            m_vao = new VertexArrayObject(capabilities, gl, Attributes, "VAO: Attributes for Automap");
            m_vbo = new StreamVertexBuffer<vec2>(capabilities, gl, m_vao, "VBO: Geometry for Automap");
            
            foreach (AutomapColor _ in Enum.GetValues<AutomapColor>())
                m_colorEnumToLines.Add(new DynamicArray<vec2>());
            
            using (var shaderBuilder = LegacyAutomapShader.MakeBuilder(gl))
                m_shader = new LegacyAutomapShader(gl, shaderBuilder, Attributes);

            foreach (var lockDef in m_archiveCollection.Definitions.LockDefininitions.LockDefs)
            {
                foreach (var item in lockDef.KeyDefinitionNames)
                {
                    // TODO support any color
                    if (FromColor(lockDef.MapColor, out AutomapColor? color))
                        m_keys[item] = color!.Value;
                    else
                        m_keys[item] = AutomapColor.Purple;
                }
            }
        }

        ~LegacyAutomapRenderer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void Render(IWorld world, RenderInfo renderInfo)
        {
            // Consider both offsets at zero a reset
            if (world.Config.Hud.AutoMapOffsetX == 0 && world.Config.Hud.AutoMapOffsetY == 0)
            {
                m_offsetX = 0;
                m_offsetY = 0;
                m_lastOffsetX = 0;
                m_lastOffsetY = 0;
            }

            if (m_lastOffsetX != world.Config.Hud.AutoMapOffsetX || m_lastOffsetY != world.Config.Hud.AutoMapOffsetY)
            {
                m_offsetX += (world.Config.Hud.AutoMapOffsetX - m_lastOffsetX) * 64 * 1 / (float)world.Config.Hud.AutoMapScale;
                m_offsetY += (world.Config.Hud.AutoMapOffsetY - m_lastOffsetY) * 64 * 1 / (float)world.Config.Hud.AutoMapScale;
                m_lastOffsetX = world.Config.Hud.AutoMapOffsetX;
                m_lastOffsetY = world.Config.Hud.AutoMapOffsetY;
            }

            PopulateData(world, renderInfo, out Box2F worldBounds);

            m_shader.BindAnd(() =>
            {
                m_shader.Mvp.Set(gl, CalculateMvp(renderInfo, worldBounds, world));

                for (int i = 0; i < m_vboRanges.Count; i++)
                {
                    (int first, vec3 color) = m_vboRanges[i];
                    int count = i == m_vboRanges.Count - 1 ? m_vbo.Count - first : m_vboRanges[i + 1].start - first;
                    
                    m_shader.Color.Set(gl, color);
                    m_vao.BindAnd(() =>
                    {
                        GL.DrawArrays(PrimitiveType.Lines, first, count);
                    });
                }
            });
        }

        private mat4 CalculateMvp(RenderInfo renderInfo, Box2F worldBounds, IWorld world)
        {
            vec2 scale = CalculateScale(renderInfo, worldBounds, world);
            vec3 camera = renderInfo.Camera.Position.GlmVector;

            float offsetX = (m_offsetX - m_lastOffsetY) * renderInfo.TickFraction;
            float offsetY = (m_offsetY - m_lastOffsetY) * renderInfo.TickFraction;

            mat4 model = mat4.Scale(scale.x, scale.y, 1.0f);
            mat4 view = mat4.Translate(-camera.x - m_offsetX, -camera.y - m_offsetY, 0);
            mat4 proj = mat4.Identity;

            return model * view * proj;
        }

        private static vec2 CalculateScale(RenderInfo renderInfo, Box2F worldBounds, IWorld world)
        {
            // Note: we're translating to NDC coordinates, so everything should
            // end up between [-1.0, 1.0].
            (float w, float h) = worldBounds.Sides;
            (float vW, float vH) = (renderInfo.Viewport.Width, renderInfo.Viewport.Height);
            float aspect = vW / vH;

            // TODO: Do this properly...
            float scale = (float)world.Config.Hud.AutoMapScale.Value;
            return new vec2(1 / vW * scale, 1 / vH * scale);
        }

        private void PopulateData(IWorld world, RenderInfo renderInfo, out Box2F box2F)
        {
            Player? player = renderInfo.ViewerEntity as Player;
            
            m_vbo.Clear();
            PopulateColoredLines(world, player);
            PopulateThings(world, player, renderInfo);
            DrawEntity(player, renderInfo.TickFraction);

            if (player != null && (m_offsetX != 0 || m_offsetY != 0))
                DrawCenterCross(world, player, renderInfo);

            TransferLineDataIntoBuffer(out box2F);
            m_vbo.UploadIfNeeded();
        }

        private void DrawCenterCross(IWorld world, Player player, RenderInfo renderInfo)
        {
            const int VirtualLength = 17;
            var center = player.PrevPosition.Interpolate(player.Position, renderInfo.TickFraction);
            float x = (float)center.X + m_offsetX;
            float y = (float)center.Y + m_offsetY;
            float length = VirtualLength * 1 / (float)world.Config.Hud.AutoMapScale;
            // Center the cross
            float offset = length / VirtualLength / 2.0f;

            DynamicArray<vec2> array = m_colorEnumToLines[(int)AutomapColor.Purple];
            array.Add(new vec2(x - length, y - offset));
            array.Add(new vec2(x + length, y - offset));
            array.Add(new vec2(x - offset, y - length));
            array.Add(new vec2(x - offset, y + length));
        }

        private void PopulateThings(IWorld world, Player? player, RenderInfo renderInfo)
        {
            if (player == null || !player.Cheats.IsCheatActive(CheatType.AutoMapModeShowAllLinesAndThings))
                return;

            foreach (var entity in world.Entities)
                DrawEntity(entity, renderInfo.TickFraction);
        }

        private void PopulateColoredLines(IWorld world, Player? player)
        {
            foreach (DynamicArray<vec2> lineList in m_colorEnumToLines)
                lineList.Clear();

            bool allMap = false;
            if (player != null)
            {
                allMap = player.Inventory.IsPowerupActive(PowerupType.ComputerAreaMap) ||
                    player.Cheats.IsCheatActive(CheatType.AutoMapModeShowAllLines) ||
                    player.Cheats.IsCheatActive(CheatType.AutoMapModeShowAllLinesAndThings);
            }

            foreach (Line line in world.Lines)
            {
                if (!line.Flags.Automap.AlwaysDraw && (!allMap && !line.SeenForAutomap || line.Flags.Automap.NeverDraw))
                    continue;
                
                Vec2D start = line.StartPosition;
                Vec2D end = line.EndPosition;

                if (line.Special.LineSpecialType == ZDoomLineSpecialType.DoorLockedRaise)
                {
                    LockDef? lockDef = m_archiveCollection.Definitions.LockDefininitions.GetLockDef(line.Args.Arg3);
                    if (lockDef != null && FromColor(lockDef.MapColor, out AutomapColor? color))
                    {
                        AddLine(color!.Value, start, end);
                        continue;
                    }
                }
                
                if (line.Back == null || line.Flags.Secret || line.Flags.Automap.AlwaysDraw)
                {
                    AddLine(line.SeenForAutomap ? AutomapColor.White : AutomapColor.LightBlue, start, end);
                    continue;
                }

                // TODO: bool floorChanges = line.Front.Sector.Floor.Z != line.Back.Sector.Floor.Z;
                AddLine((line.HasSpecial && line.Special.IsTeleport()) ? AutomapColor.Green : AutomapColor.Gray, start, end);
            }

            void AddLine(AutomapColor color, Vec2D start, Vec2D end)
            {
                DynamicArray<vec2> array = m_colorEnumToLines[(int)color];
                array.Add(new vec2((float)start.X, (float)start.Y));
                array.Add(new vec2((float)end.X, (float)end.Y));
            }
        }

        private static bool FromColor(Color color, out AutomapColor? automapColor)
        {
            automapColor = null;
            if (color == Color.Red)
                automapColor = AutomapColor.Red;
            else if (color == Color.Blue)
                automapColor = AutomapColor.Blue;
            else if (color == Color.Yellow)
                automapColor = AutomapColor.Yellow;

            return automapColor != null;
        }
        
        private void DrawEntity(Entity? entity, float interpolateFrac)
        {
            if (entity == null)
                return;

            // Ignore player starts and deathmatch starts
            if (entity.Definition.EditorId < 4 || entity.Definition.EditorId == 11)
                return;

            m_entityPoints.Clear();

            // We start with the arrow facing along the positive X axis direction.
            // This way, our rotation can be easily done.
            var center = entity.PrevPosition.Interpolate(entity.Position, interpolateFrac);
            var (width, height) = entity.Box.To2D().Sides.Float;
            var (centerX, centerY) = center.XY.Float;
            float halfWidth = width / 2;
            float halfHeight = height / 2;
            float quarterWidth = width / 4;
            float quarterHeight = height / 4;
            
            mat4 rotate = mat4.Rotate((float)entity.AngleRadians, vec3.UnitZ);
            mat4 translate = mat4.Translate(centerX, centerY, 0);
            mat4 transform = translate * rotate;

            AutomapColor color = AutomapColor.Green;
            bool isKey = false;

            if (m_keys.TryGetValue(entity.Definition.Name, out AutomapColor keyColor))
            {
                isKey = true;
                color = keyColor;
            }
            else if (entity.Flags.IsMonster)
                color = entity.IsDead ? AutomapColor.Gray : AutomapColor.Red;
            else if (entity.Definition.IsType(EntityDefinitionType.Inventory))
                color = AutomapColor.Yellow;

            if (entity.Definition.EditorId == 14)
            {
                color = AutomapColor.Green;
                AddSquare(-quarterWidth, -quarterHeight, halfWidth, halfHeight);
            }
            else if (isKey)
            {
                // Draw a square for keys, make it flash
                if (entity.World.Gametick / (int)(Constants.TicksPerSecond / 3) % 2 == 0)
                    AddSquare(-quarterWidth, -quarterHeight, halfWidth, halfHeight);
            }
            else if (entity.IsPlayer)
            {
                color = AutomapColor.Green;
                // Main arrow from middle left to middle right
                AddLine(-halfWidth, 0, halfWidth, 0);

                // Arrow from the right tip to the top middle at 45 degrees. Same
                // for the bottom one.
                AddLine(halfWidth, 0, quarterWidth, quarterHeight);
                AddLine(halfWidth, 0, quarterWidth, -quarterHeight);
            }
            else
            {
                AddLine(-halfWidth, quarterHeight, halfWidth, 0);
                AddLine(-halfWidth, -quarterHeight, halfWidth, 0);
                AddLine(-halfWidth, -quarterHeight, -halfWidth, quarterHeight);
            }

            DynamicArray<vec2> array = m_colorEnumToLines[(int)color];
            foreach (vec2 point in m_entityPoints)
                array.Add(point);
            void AddSquare(float startX, float startY, float width, float height)
            {
                AddLine(startX, startY, startX, startY + height);
                AddLine(startX, startY + height, startX + width, startY + height);
                AddLine(startX + width, startY + height, startX + halfWidth, startY);
                AddLine(startX + width, startY, startX, startY);
            }

            void AddLine(float startX, float startY, float endX, float endY)
            {
                vec4 s = transform * new vec4(startX, startY, 0, 1); 
                vec4 e = transform * new vec4(endX, endY, 0, 1);

                m_entityPoints.Add(s.xy);
                m_entityPoints.Add(e.xy);
            }
        }
        
        private void TransferLineDataIntoBuffer(out Box2F box2F)
        {
            float minX = Single.PositiveInfinity;
            float minY = Single.PositiveInfinity;
            float maxX = Single.NegativeInfinity;
            float maxY = Single.NegativeInfinity;
            
            m_vboRanges.Clear();

            for (int i = 0; i < m_colorEnumToLines.Count; i++)
            {
                DynamicArray<vec2> lines = m_colorEnumToLines[i];
                if (lines.Empty())
                    continue;

                AutomapColor color = (AutomapColor)i;
                vec3 colorVec = color.ToColor();
                m_vboRanges.Add((m_vbo.Count, colorVec));
                
                foreach (vec2 line in lines)
                    AddLine(line);
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
            
            void AddLine(vec2 line)
            {
                m_vbo.Add(line);

                if (line.x < minX)
                    minX = line.x;
                if (line.y < minY)
                    minY = line.y;
                if (line.x > maxX)
                    maxX = line.x;
                if (line.y > maxY)
                    maxY = line.y;
            }
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
}
