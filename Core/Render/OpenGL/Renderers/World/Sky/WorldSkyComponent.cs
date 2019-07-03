using Helion.Render.OpenGL.Buffer.Vao;
using Helion.Render.OpenGL.Buffer.Vbo;
using Helion.Render.OpenGL.Shader;
using Helion.Render.OpenGL.Texture;
using Helion.Render.Shared;
using MoreLinq;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Renderers.World.Sky
{
    public class WorldSkyComponent
    {
        private readonly StreamVertexBuffer<WorldSkyStencilVertex> skyGeometryVbo = new StreamVertexBuffer<WorldSkyStencilVertex>();
        private readonly StaticVertexBuffer<WorldSkyVertex> skyboxVbo = new StaticVertexBuffer<WorldSkyVertex>();
        private readonly ShaderProgram skyGeometryShaderProgram;
        private readonly ShaderProgram skyboxShaderProgram;
        private readonly VertexArrayObject skyGeometryVao = new VertexArrayObject(
            new VaoAttributeF("pos", 0, 3, VertexAttribPointerType.Float)
        );
        private readonly VertexArrayObject skyboxVao = new VertexArrayObject(
            new VaoAttributeF("pos", 0, 3, VertexAttribPointerType.Float),
            new VaoAttributeF("uv", 1, 2, VertexAttribPointerType.Float)
        );

        public WorldSkyComponent(int spanCount = 2, int verticesPerSpan = 16)
        {
            Precondition(spanCount > 0, "Sky cylinder needs a positive span amount");
            Precondition(verticesPerSpan > 0, "Sky cylinder needs a positive amount of vertices per span");

            skyGeometryVao.BindAttributesTo(skyGeometryVbo);
            skyGeometryShaderProgram = WorldSkyShader.CreateSkyGeometryShaderProgramOrThrow(skyGeometryVao);

            skyboxVao.BindAttributesTo(skyboxVbo);
            skyboxShaderProgram = WorldSkyShader.CreateSkyboxShaderProgramOrThrow(skyboxVao);

            CreateSkyboxCylinderVertices(spanCount, verticesPerSpan);
            CreateTopAndBottomPlanes();
        }

        private List<System.Numerics.Vector2> CreateCylinderVertices(int spanCount, int verticesPerSpan)
        {
            List<System.Numerics.Vector2> vertices = new List<System.Numerics.Vector2>();
            
            int totalVertices = spanCount * verticesPerSpan;
            double radianDelta = MathHelper.TwoPi / totalVertices;
            double currentRadians = 0.0;
            
            for (int i = 0; i < totalVertices; i++)
            {
                double x = Math.Cos(currentRadians);
                double y = Math.Sin(currentRadians);
                vertices.Add(new System.Numerics.Vector2((float)x, (float)y));

                currentRadians += radianDelta;
            }

            return vertices;
        }
        
        private void CreateSkyboxCylinderVertices(int spanCount, int verticesPerSpan)
        {
            // Creates a list of vertices in counter-clockwise order starting
            // from the right side.
            //
            //       v3
            //   v4 .-`-. v2
            //     /     \
            // v5 |       | v1
            //     \     /
            //   v6 `-.-` vN
            //       v...
            List<System.Numerics.Vector2> vertices = CreateCylinderVertices(spanCount, verticesPerSpan);
            
            // We have a list of vertices like [v1, v2, v3, ... vN] and we want
            // to make a line segment from all the pairs. However we also need
            // a segment for [vN, v1]. We do this by adding the front vertex to
            // the back, so when we iterate over pairs of elements it will make
            // the final segment.
            vertices.Add(vertices.First());
            vertices.Reverse();

            // Proceed over each pair of vertices [[vA, vB], [vB, vC], ...] and
            // break the pairs into spans and make the geometry out of them.
            // After doing so, submit the spans to the VBO.
            //
            // Note that we also reverse the list because we make the elements
            // in a counter-clockwise direction, but WorldSkyWallSpan sets the
            // UV values by forward iteration. We could either do the reversal
            // here, or do it inside of WorldSkyWallSpan, so I decided it was
            // best to do it here.
            vertices.Zip(vertices.Skip(1), (start, end) => (start, end))
                    .Batch(verticesPerSpan)
                    .Select(vertexPairs => new WorldSkyWallSpan(vertexPairs))
                    .ForEach(UploadWallVertices);
        }

        private void UploadWallVertices(WorldSkyWallSpan wallSpan)
        {
            wallSpan.Walls.ForEach(wall =>
            {
                foreach (WorldSkyTriangle triangle in wall.GetTriangles())
                {
                    skyboxVbo.Add(triangle.First);
                    skyboxVbo.Add(triangle.Second);
                    skyboxVbo.Add(triangle.Third);
                }
            });
            
            skyboxVbo.BindAnd(() =>
            {
                skyboxVbo.Upload();
            });
        }

        private void CreateTopAndBottomPlanes()
        {
            // TODO
        }

        private void SetSkyGeometryUniforms(RenderInfo renderInfo)
        {
            skyGeometryShaderProgram.SetMatrix("mvp", GLRenderer.CreateMVP(renderInfo));
        }
        
        private void SetSkyboxUniforms(RenderInfo renderInfo)
        {
            Matrix4 yawRotate = Matrix4.CreateRotationY(renderInfo.CameraInfo.Yaw);
            Matrix4 pitchRotate = Matrix4.CreateRotationX(renderInfo.CameraInfo.Pitch);
            skyboxShaderProgram.SetMatrix("mvp", yawRotate * pitchRotate);
        }
        
        public void Clear()
        {
            skyGeometryVbo.Clear();
        }
        
        public void AddTriangle(WorldVertex first, WorldVertex second, WorldVertex third)
        {
            skyGeometryVbo.Add(new WorldSkyStencilVertex(first));
            skyGeometryVbo.Add(new WorldSkyStencilVertex(second));
            skyGeometryVbo.Add(new WorldSkyStencilVertex(third));
        }
        
        public void RenderSkyGeometry(RenderInfo renderInfo)
        {
            skyGeometryVao.BindAnd(() =>
            {
                skyGeometryVbo.BindAnd(() => 
                { 
                    skyGeometryShaderProgram.BindAnd(() =>
                    {
                        SetSkyGeometryUniforms(renderInfo);
                        skyGeometryVbo.DrawArrays(skyGeometryVbo.Count);
                    });
                });
            });
        }
        
        public void RenderSkybox(GLTexture skyTexture, RenderInfo renderInfo)
        {
            skyboxVao.BindAnd(() =>
            {
                skyboxVbo.BindAnd(() => 
                { 
                    skyboxShaderProgram.BindAnd(() =>
                    {
                        SetSkyboxUniforms(renderInfo);
                        skyTexture.BindAnd(() =>
                        {
                            skyboxVbo.DrawArrays(skyboxVbo.Count);
                        });
                    });
                });
            });
        }
    }
}