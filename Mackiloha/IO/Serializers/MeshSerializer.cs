﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mackiloha.Render;

namespace Mackiloha.IO.Serializers
{
    public class MeshSerializer : AbstractSerializer
    {
        public MeshSerializer(MiloSerializer miloSerializer) : base(miloSerializer) { }
        
        public override void ReadFromStream(AwesomeReader ar, ISerializable data)
        {
            var mesh = data as Mesh;
            int version = ReadMagic(ar, data);

            MiloSerializer.ReadFromStream(ar.BaseStream, mesh.Trans);
            MiloSerializer.ReadFromStream(ar.BaseStream, mesh.Draw);

            mesh.Material = ar.ReadString();
            mesh.MainMesh = ar.ReadString();

            mesh.Unknown = ar.ReadInt32();
            switch(mesh.Unknown)
            {
                case 0:
                case 31:
                case 33:
                case 37:
                case 63:
                    break;
                default:
                    throw new Exception($"Unexpected number, got {mesh.Unknown}");
            }                

            var num = ar.ReadInt32();
            if (num != 1)
                throw new Exception($"This should be 1, got {num}");

            num = ar.ReadByte();
            if (num != 0)
                throw new Exception($"This should be 0, got {num}");

            // Read vertices
            var count = ar.ReadInt32();
            mesh.Vertices.Clear();
            mesh.Vertices.AddRange(RepeatFor(count, () =>
            {
                // TODO: Add switch statement for milo version
                return new Vertex3()
                {
                    X = ar.ReadSingle(),
                    Y = ar.ReadSingle(),
                    Z = ar.ReadSingle(),
                    NormalX = ar.ReadSingle(),
                    NormalY = ar.ReadSingle(),
                    NormalZ = ar.ReadSingle(),
                    ColorR = ar.ReadSingle(),
                    ColorG = ar.ReadSingle(),
                    ColorB = ar.ReadSingle(),
                    ColorA = ar.ReadSingle(),
                    U = ar.ReadSingle(),
                    V = ar.ReadSingle()
                };
            }));

            // Read face indicies
            count = ar.ReadInt32();
            mesh.Faces.Clear();
            mesh.Faces.AddRange(RepeatFor(count, () => new Face()
            {
                V1 = ar.ReadUInt16(),
                V2 = ar.ReadUInt16(),
                V3 = ar.ReadUInt16(),
            }));

            // Read groups
            count = ar.ReadInt32();
            var groupSizes = RepeatFor(count, () => ar.ReadByte()).ToArray();

            if (groupSizes.Select(x => (int)x).Sum() != mesh.Faces.Count)
                throw new Exception("Sum should equal count of faces");
            
            var charCount = ar.ReadInt32();
            ar.BaseStream.Position -= 4;

            // Read bones
            mesh.Bones.Clear();
            if (charCount > 0)
            {
                const int boneCount = 4; // Always 4?
                var boneNames = RepeatFor(boneCount, () => (MiloString)ar.ReadString()).ToArray(); // Either 3 or none (Last one is always empty?)
                var boneMats = RepeatFor(boneCount, () => ReadMatrix(ar)).ToArray();
                                
                for (int i = 0; i < boneCount; i++)
                {
                    mesh.Bones.Add(new Bone()
                    {
                        Name = boneNames[i],
                        Mat = boneMats[i]
                    });
                }
            }
            else
            {
                // Skips zero
                ar.BaseStream.Position += 4;
            }

            mesh.Groups.Clear();
            if (count <= 0 || groupSizes[0] <= 0 || ar.BaseStream.Length == ar.BaseStream.Position)
            {
                mesh.Groups.AddRange(groupSizes
                    .Select(x => new FaceGroup()
                    {
                        Size = x,
                        Sections = new List<int>(),
                        VertexIndicies = new List<int>()
                    }));
                return;
            }

            // Read face groups
            mesh.Groups.AddRange(Enumerable.Range(0, count).Select(x =>
            {
                var sectionCount = ar.ReadInt32();
                var vertCount = ar.ReadInt32();

                return new FaceGroup()
                {
                    Size = groupSizes[x],
                    Sections = Enumerable.Range(0, sectionCount)
                        .Select(y => ar.ReadInt32())
                        .ToList(),
                    VertexIndicies = Enumerable.Range(0, vertCount)
                        .Select(y => (int)ar.ReadUInt16())
                        .ToList(),
                };
            }));
        }

        protected static Matrix4 ReadMatrix(AwesomeReader ar)
        {
            return new Matrix4()
            {
                M11 = ar.ReadSingle(), // M11
                M12 = ar.ReadSingle(), // M12
                M13 = ar.ReadSingle(), // M13

                M21 = ar.ReadSingle(), // M21
                M22 = ar.ReadSingle(), // M22
                M23 = ar.ReadSingle(), // M23

                M31 = ar.ReadSingle(), // M31
                M32 = ar.ReadSingle(), // M32
                M33 = ar.ReadSingle(), // M33

                M41 = ar.ReadSingle(), // M41
                M42 = ar.ReadSingle(), // M42
                M43 = ar.ReadSingle(), // M43
                M44 = 1.0f             // M44 - Implicit
            };
        }

        public override void WriteToStream(AwesomeWriter aw, ISerializable data)
        {
            var mesh = data as Mesh;

            // TODO: Add version check
            var version = Magic();
            aw.Write(version);

            MiloSerializer.WriteToStream(aw.BaseStream, mesh.Trans);
            MiloSerializer.WriteToStream(aw.BaseStream, mesh.Draw);

            aw.Write((string)mesh.Material);
            aw.Write((string)mesh.MainMesh);
            aw.Write((int)mesh.Unknown);
            aw.Write((int)1);
            aw.Write((byte)0);

            // Write vertices
            aw.Write((int)mesh.Vertices.Count);
            mesh.Vertices.ForEach(x =>
            {
                // TODO: Add switch statement for milo versions
                aw.Write((float)x.X);
                aw.Write((float)x.Y);
                aw.Write((float)x.Z);
                aw.Write((float)x.NormalX);
                aw.Write((float)x.NormalY);
                aw.Write((float)x.NormalZ);
                aw.Write((float)x.ColorR);
                aw.Write((float)x.ColorG);
                aw.Write((float)x.ColorB);
                aw.Write((float)x.ColorA);
                aw.Write((float)x.U);
                aw.Write((float)x.V);
            });

            // Write faces
            aw.Write((int)mesh.Faces.Count);
            mesh.Faces.ForEach(x =>
            {
                aw.Write((ushort)x.V1);
                aw.Write((ushort)x.V2);
                aw.Write((ushort)x.V3);
            });

            // Write group sizes
            aw.Write((int)mesh.Groups.Count);
            mesh.Groups.ForEach(x => aw.Write((byte)x.Size));
            
            const int boneCount = 4; // Always 4?
            var bones = mesh.Bones
                .Take(boneCount)
                .ToList();

            // Write bones
            if (bones.Count > 0)
            {
                bones.ForEach(x => aw.Write((string)x.Name));
                RepeatFor(boneCount - bones.Count, () => aw.Write((string)""));

                bones.ForEach(x => WriteMatrix(x.Mat, aw));
                RepeatFor(boneCount - bones.Count, () => WriteMatrix(Matrix4.Identity(), aw));
            }
            else
            {
                aw.Write((int)0);
            }

            if (mesh.Groups.Sum(x => x.Sections.Count) == 0
                && mesh.Groups.Sum(x => x.VertexIndicies.Count) == 0)
                return;

            // Write group sections + vertex indices
            mesh.Groups.ForEach(x =>
            {
                aw.Write((int)x.Sections.Count);
                aw.Write((int)x.VertexIndicies.Count);

                x.Sections.ForEach(y => aw.Write((int)y));
                x.VertexIndicies.ForEach(y => aw.Write((ushort)y));
            });
        }

        protected static void WriteMatrix(Matrix4 mat, AwesomeWriter aw)
        {
            aw.Write((float)mat.M11);
            aw.Write((float)mat.M12);
            aw.Write((float)mat.M13);

            aw.Write((float)mat.M21);
            aw.Write((float)mat.M22);
            aw.Write((float)mat.M23);

            aw.Write((float)mat.M31);
            aw.Write((float)mat.M32);
            aw.Write((float)mat.M33);

            aw.Write((float)mat.M41);
            aw.Write((float)mat.M42);
            aw.Write((float)mat.M43);
        }

        public override bool IsOfType(ISerializable data) => data is Mesh;

        public override int Magic()
        {
            switch (MiloSerializer.Info.Version)
            {
                case 10:
                    // GH1
                    return 25;
                default:
                    return -1;
            }
        }
    }
}
