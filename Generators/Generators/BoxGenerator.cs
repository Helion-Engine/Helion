using System;
using CodegenCS;

namespace Generators.Generators
{
    public class BoxGenerator
    { 
        private readonly Types m_type;
        private readonly int m_dimension;
        private readonly bool m_isStruct;

        private string ClassName => m_isStruct ? StructType : InstanceType;
        private string PrimitiveType => m_type.PrimitiveType();
        private string StructType => $"Box{m_dimension}{m_type.GetShorthand()}";
        private string InstanceType => $"BoundingBox{m_dimension}{m_type.GetShorthand()}<T>";
        private string VecStruct => $"Vec{m_dimension}{m_type.GetShorthand()}";
        private string VecClass => $"Vector{m_dimension}{m_type.GetShorthand()}";
        
        public BoxGenerator(Types type, int dimension, bool isStruct)
        {
            m_type = type;
            m_dimension = dimension;
            m_isStruct = isStruct;
        }

        private void PerformGeneration()
        {
            var w = new CodegenTextWriter();
            
            w.WriteCommentHeader();
            w.WriteLine("using System;");
            w.WriteLine("using System.Collections.Generic;");
            w.WriteLine("using System.Linq;");
            w.WriteLine("using Helion.Geometry.Segments;");
            w.WriteLine("using Helion.Geometry.Vectors;");
            w.WriteLine("using Helion.Util.Extensions;");
            w.WriteLine("using static Helion.Util.Assertion.Assert;");
            w.WriteLine();
            w.WriteNamespaceBlock("Geometry.Boxes", () =>
            {
                string classOrStruct = m_isStruct ? "readonly struct" : "class";
                w.WithCBlock($"public {classOrStruct} {ClassName}", () =>
                {
                    WriteFieldsAndProperties(w);
                    WriteConstructors(w);
                    WriteDeconstructions(w);
                    WriteOperators(w);
                    WriteMethods(w);
                });
            });

            string path = $"Geometry/Boxes/{ClassName}.cs";
            Console.WriteLine($"Generating {path}");
            w.WriteToCoreProject(path);
        }

        private void WriteFieldsAndProperties(CodegenTextWriter w)
        {
            w.WriteLine($"public readonly {VecStruct} Min;");
            w.WriteLine($"public readonly {VecStruct} Max;");
            w.WriteLine();

            if (m_dimension == 2)
            {
                w.WriteLine($"public {VecStruct} TopLeft => new(Min.X, Max.Y);");
                w.WriteLine($"public {VecStruct} BottomLeft => Min");
                w.WriteLine($"public {VecStruct} BottomRight => new(Max.X, Min.Y)");
                w.WriteLine($"public {VecStruct} TopRight => Max");
                w.WriteLine($"public {PrimitiveType} Top => Max.Y");
                w.WriteLine($"public {PrimitiveType} Bottom => Min.Y");
                w.WriteLine($"public {PrimitiveType} Left => Min.X");
                w.WriteLine($"public {PrimitiveType} Right => Max.X");
                w.WriteLine($"public {PrimitiveType} Width => Max.X - Min.X");
                w.WriteLine($"public {PrimitiveType} Height => Max.Y - Min.Y");
            }
            
            if (!m_isStruct)
                w.WriteLine($"public {StructType} Struct => new(Min, Max);");

            w.WriteLine($"public {VecStruct} Sides => Max - Min;");
            w.WriteLine();
        }

        private void WriteConstructors(CodegenTextWriter w)
        {
            var classStructPairs = new[] { (VecStruct, VecStruct), (VecStruct, VecClass), (VecClass, VecStruct), (VecClass, VecClass) };
            
            foreach ((string first, string second) in classStructPairs)
            {
                w.WithCBlock($"public {ClassName}({first} min, {second} max)", () =>
                {
                    w.WriteLine($@"Precondition(min.X <= max.X, ""Bounding box min X > max X"");");
                    w.WriteLine($@"Precondition(min.Y <= max.Y, ""Bounding box min Y > max Y"");");
                    w.WriteLine($"Min = min;");
                    w.WriteLine($"Max = max;");
                });
                w.WriteLine();
            }

            foreach (string vecType in new[] {VecStruct, VecClass})
            {
                w.WithCBlock($"public {ClassName}({vecType} center, {PrimitiveType} radius)", () =>
                {
                    w.WriteLine($@"Precondition(radius >= 0, ""Bounding box radius yields min X > max X"");");
                    w.WriteLine($"Min = new(center.X - radius, center.Y - radius);");
                    w.WriteLine($"Max = new(center.X + radius, center.Y + radius);");
                });
                w.WriteLine();
            }
            
            foreach ((string first, string second) in classStructPairs)
            {
                w.WithCBlock($"public static implicit operator Box2D(ValueTuple<{first}, {second}> tuple)", () =>
                {
                    w.WriteLine("return new(tuple.Item1, tuple.Item2);");
                });
                w.WriteLine();
            }
        }

        private void WriteDeconstructions(CodegenTextWriter w)
        {
            w.WithCBlock($"public void Deconstruct(out {VecStruct} min, out {VecStruct} max)", () =>
            {
                w.WriteLine($"min = Min;");
                w.WriteLine($"max = Max;");
            });
            
            w.WriteLine();
        }

        private void WriteOperators(CodegenTextWriter w)
        {
            w.WriteLine($"public static {StructType} operator +({ClassName} self, {VecStruct} offset) => new(self.Min + offset, self.Max + offset);");
            w.WriteLine($"public static {StructType} operator +({ClassName} self, {VecClass} offset) => new(self.Min + offset, self.Max + offset);");
            w.WriteLine($"public static {StructType} operator -({ClassName} self, {VecStruct} offset) => new(self.Min - offset, self.Max - offset);");
            w.WriteLine($"public static {StructType} operator -({ClassName} self, {VecClass} offset) => new(self.Min - offset, self.Max - offset);");
            w.WriteLine();
        }

        private void WriteMethods(CodegenTextWriter w)
        {
            w.WriteLine($@"public override string ToString() => $""({{Min}}), ({{Max}})"";");
        }

        public static void Generate()
        {
            foreach (Types type in new[] { Types.Float, Types.Double, Types.Fixed, Types.Int })
            {
                foreach (int dimension in new[] { 2, 3 })
                {
                    new BoxGenerator(type, dimension, true).PerformGeneration();
                    new BoxGenerator(type, dimension, false).PerformGeneration();
                }
            }
        }
    }
}
