using g3;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Buffer = Stride.Graphics.Buffer;
using Vector2 = Stride.Core.Mathematics.Vector2;
using Vector3 = Stride.Core.Mathematics.Vector3;

namespace VL.G3.Utils
{
    public class MeshUtils
    {
        public static Mesh ToStrideMesh(GraphicsDevice graphicsDevice, DMesh3 g3Mesh, Vector3 offset, float scaling = 1f)
        {
            if (g3Mesh is null)
                return null;

            return ToStrideMesh(graphicsDevice, new SimpleMesh(g3Mesh), offset, scaling);
        }

        public static Mesh ToStrideMesh(GraphicsDevice graphicsDevice, SimpleMesh g3Mesh, Vector3 offset, float scaling = 1f)
        {
            if (g3Mesh is null || g3Mesh.VertexCount == 0)
                return null;

            var vertexDeclaration = GetVertexDeclaration(g3Mesh);

            var vertices = new byte[g3Mesh.VertexCount * vertexDeclaration.VertexStride];
            var boundingBox = BoundingBox.Empty;
            BoundingSphere boundingSphere;
            unsafe
            {
                fixed (byte* ptr = vertices)
                {
                    byte* current = ptr;
                    for (int i = 0; i < g3Mesh.VertexCount; i++)
                    {
                        var vi = g3Mesh.GetVertexAll(i);
                        var p = (new Vector3((float)vi.v.x, (float)vi.v.y, (float)vi.v.z) + offset) * scaling;
                        BoundingBox.Merge(ref boundingBox, ref p, out boundingBox);
                        Unsafe.Write(current, p);

                        current += sizeof(Vector3);
                        if (vi.bHaveN)
                        {
                            Unsafe.Write(current, vi.n);
                            current += sizeof(Vector3);
                        }
                        if (vi.bHaveUV)
                        {
                            Unsafe.Write(current, vi.uv);
                            current += sizeof(Vector2);
                        }
                        if (vi.bHaveC)
                        {
                            Unsafe.Write(current, new Color(vi.c.x, vi.c.y, vi.c.z));
                            current += sizeof(Color);
                        }

                    }

                    BoundingSphere.FromPoints((IntPtr)ptr, 0, g3Mesh.VertexCount, vertexDeclaration.VertexStride, out boundingSphere);
                }
            }

            var vertexBuffer = Buffer.New(graphicsDevice, vertices, vertexDeclaration.VertexStride, BufferFlags.VertexBuffer);
            var indexBuffer = Buffer.Index.New(graphicsDevice, g3Mesh.Triangles.Reverse().ToArray());
            return new Mesh()
            {
                Draw = new MeshDraw()
                {
                    VertexBuffers = new VertexBufferBinding[]
                    {
                        new VertexBufferBinding(vertexBuffer, vertexDeclaration, g3Mesh.VertexCount)
                    },
                    IndexBuffer = new IndexBufferBinding(indexBuffer, is32Bit: true, g3Mesh.Triangles.Length),
                    DrawCount = g3Mesh.Triangles.Length,
                    PrimitiveType = PrimitiveType.TriangleList
                },
                BoundingBox = boundingBox,
                BoundingSphere = boundingSphere
            };
        }

        static VertexDeclaration GetVertexDeclaration(SimpleMesh mesh)
        {
            var elements = new List<VertexElement>();
            elements.Add(VertexElement.Position<Vector3>());
            if (mesh.HasVertexNormals)
                elements.Add(VertexElement.Normal<Vector3>());
            if (mesh.HasVertexUVs)
                elements.Add(VertexElement.TextureCoordinate<Vector2>());
            if (mesh.HasVertexColors)
                elements.Add(VertexElement.Color<Color>());
            return new VertexDeclaration(elements.ToArray());
        }
    }
}
