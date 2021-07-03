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
        private string InstanceType => $"BoundingBox{m_dimension}{m_type.GetShorthand()}";
        private string VecStruct => $"Vec{m_dimension}{m_type.GetShorthand()}";
        private string VecClass => $"Vector{m_dimension}{m_type.GetShorthand()}";
        private string Vec3Struct => $"Vec3{m_type.GetShorthand()}";
        private string Vec3Class => $"Vector3{m_type.GetShorthand()}";
        private string SegStruct => $"Seg{m_dimension}{m_type.GetShorthand()}";
        private string SegClassStruct => $"Segment{m_dimension}{m_type.GetShorthand()}";
        private string SegClassT => $"SegmentT{m_dimension}{m_type.GetShorthand()}<T>";
        private string WhereVec => $"where T : {VecClass}";

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
            if (m_isStruct && m_type != Types.Fixed)
            {
                if (m_dimension == 2)
                    w.WriteLine($"public static readonly {ClassName} UnitBox = ((0, 0), (1, 1));");
                else if (m_dimension == 3)
                    w.WriteLine($"public static readonly {ClassName} UnitBox = ((0, 0, 0), (1, 1, 1));");
                w.WriteLine();   
            }

            if (m_isStruct)
            {
                w.WriteLine($"public readonly {VecStruct} Min;");
                w.WriteLine($"public readonly {VecStruct} Max;");
            }
            else
            {
                w.WriteLine($"protected {VecStruct} m_Min;");
                w.WriteLine($"protected {VecStruct} m_Max;");
            }
            
            w.WriteLine();

            if (!m_isStruct)
            {
                w.WriteLine($"public {VecStruct} Min => m_Min;");
                w.WriteLine($"public {VecStruct} Max => m_Max;");
            }
            
            if (m_dimension == 2)
            {
                w.WriteLine($"public {VecStruct} TopLeft => new(Min.X, Max.Y);");
                w.WriteLine($"public {VecStruct} BottomLeft => Min;");
                w.WriteLine($"public {VecStruct} BottomRight => new(Max.X, Min.Y);");
                w.WriteLine($"public {VecStruct} TopRight => Max;");

                w.WriteLine($"public {PrimitiveType} Top => Max.Y;");
                w.WriteLine($"public {PrimitiveType} Bottom => Min.Y;");
                w.WriteLine($"public {PrimitiveType} Left => Min.X;");
                w.WriteLine($"public {PrimitiveType} Right => Max.X;");
                w.WriteLine($"public {PrimitiveType} Width => Max.X - Min.X;");
                w.WriteLine($"public {PrimitiveType} Height => Max.Y - Min.Y;");
            }

            if (!m_isStruct)
                w.WriteLine($"public {StructType} Struct => new(Min, Max);");
            
            if (m_dimension == 2 && m_type == Types.Int)
                w.WriteLine($"public Dimension Dimension => new(Width, Height);");
            
            w.WriteLine($"public {VecStruct} Sides => Max - Min;");
            
            w.WriteLine();
        }

        private void WriteConstructors(CodegenTextWriter w)
        {
            string protectedPrefix = m_isStruct ? "" : "m_";
            var classStructPairs = new[]
                {(VecStruct, VecStruct), (VecStruct, VecClass), (VecClass, VecStruct), (VecClass, VecClass)};

            foreach ((string first, string second) in classStructPairs)
            {
                string firstStructExt = first == VecClass ? ".Struct" : "";
                string secondStructExt = second == VecClass ? ".Struct" : "";
                w.WithCBlock($"public {ClassName}({first} min, {second} max)", () =>
                {
                    w.WriteLine($@"Precondition(min.X <= max.X, ""Bounding box min X > max X"");");
                    w.WriteLine($@"Precondition(min.Y <= max.Y, ""Bounding box min Y > max Y"");");
                    w.WriteLine();
                    
                    w.WriteLine($"{protectedPrefix}Min = min{firstStructExt};");
                    w.WriteLine($"{protectedPrefix}Max = max{secondStructExt};");
                });
                w.WriteLine();
            }

            if (m_dimension == 2)
            {
                foreach (string vecType in new[] {VecStruct, VecClass})
                {
                    w.WithCBlock($"public {ClassName}({vecType} center, {PrimitiveType} radius)", () =>
                    {
                        w.WriteLine($@"Precondition(radius >= 0, ""Bounding box radius yields min X > max X"");");
                        w.WriteLine();
                        
                        w.WriteLine($"{protectedPrefix}Min = new(center.X - radius, center.Y - radius);");
                        w.WriteLine($"{protectedPrefix}Max = new(center.X + radius, center.Y + radius);");
                    });
                    w.WriteLine();
                }
            }

            if (m_isStruct)
            {
                if (m_dimension == 2)
                {
                    w.WithCBlock($"public static implicit operator {ClassName}(ValueTuple<{PrimitiveType}, {PrimitiveType}, {PrimitiveType}, {PrimitiveType}> tuple)",
                        () => { w.WriteLine("return new((tuple.Item1, tuple.Item2), (tuple.Item3, tuple.Item4));"); });
                    w.WriteLine();    
                }
                
                foreach ((string first, string second) in classStructPairs)
                {
                    w.WithCBlock($"public static implicit operator {ClassName}(ValueTuple<{first}, {second}> tuple)",
                        () => { w.WriteLine("return new(tuple.Item1, tuple.Item2);"); });
                    w.WriteLine();
                }
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
            string containsFunc = m_dimension == 2
                ? "point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y"
                : "point.X > Min.X && point.X < Max.X && point.Y > Min.Y && point.Y < Max.Y && point.Z > Min.Z && point.Z < Max.Z";
            w.WriteLine($"public bool Contains({VecStruct} point) => {containsFunc};");
            w.WriteLine($"public bool Contains({VecClass} point) => {containsFunc};");
            if (m_dimension == 2)
            {
                w.WriteLine($"public bool Contains({Vec3Struct} point) => {containsFunc};");
                w.WriteLine($"public bool Contains({Vec3Class} point) => {containsFunc};");
            }

            if (m_dimension == 3)
            {
                w.WriteLine($"public bool Overlaps2D(in Box2{m_type.GetShorthand()} other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);");
                w.WriteLine($"public bool Overlaps2D(in Box3{m_type.GetShorthand()} other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);");
                w.WriteLine($"public bool Overlaps2D(BoundingBox2{m_type.GetShorthand()} other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);");
                w.WriteLine($"public bool Overlaps2D(BoundingBox3{m_type.GetShorthand()} other) => !(Min.X >= other.Max.X || Max.X <= other.Min.X || Min.Y >= other.Max.Y || Max.Y <= other.Min.Y);");
            }

            string overlapsFunc = m_dimension == 2
                ? "!(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y)"
                : "!(Min.X >= box.Max.X || Max.X <= box.Min.X || Min.Y >= box.Max.Y || Max.Y <= box.Min.Y || Min.Z >= box.Max.Z || Max.Z <= box.Min.Z)";
            w.WriteLine($"public bool Overlaps(in {StructType} box) => {overlapsFunc};");
            w.WriteLine($"public bool Overlaps({InstanceType} box) => {overlapsFunc};");

            if (m_dimension == 2)
            {
                if (m_type.IsFloatingPointPrimitive())
                {
                    w.WriteLine($"public bool Intersects({SegStruct} seg) => seg.Intersects(this);");
                    w.WriteLine($"public bool Intersects({SegClassStruct} seg) => seg.Intersects(this);");
                    w.WriteLine($"public bool Intersects<T>({SegClassT} seg) {WhereVec} => seg.Intersects(this);");
                    w.WriteLine($"public {SegStruct} GetSpanningEdge({VecClass} position) => GetSpanningEdge(position.Struct);");
                    w.WriteLine($@"public {SegStruct} GetSpanningEdge({VecStruct} position)
    {{
        // This is best understood by asking ourselves how we'd classify
        // where we are along a 1D line. Suppose we want to find out which
        // one of the spans were in along the X axis:
        //
        //      0     1     2
        //   A-----B-----C-----D
        //
        // We want to know if we're in span 0, 1, or 2. We can just check
        // by doing `if x > B` for span 1 or 2, and `if x > C` for span 2.
        // Instead of doing if statements, we can just cast the bool to an
        // int and add them up.
        //
        // Next we do this along the Y axis.
        //
        // After our results, we can merge the bits such that the higher
        // two bits are the Y value, and the lower 2 bits are the X value.
        // This gives us: 0bYYXX.
        //
        // Since each coordinate in the following image has its own unique
        // bitcode, we can switch on the bitcode to get the corners.
        //
        //       XY values           Binary codes
        //
        //      02 | 12 | 22       1000|1001|1010
        //         |    |           8  | 9  | A
        //     ----o----o----      ----o----o----
        //      01 | 11 | 21       0100|0101|0110
        //         |    |           4  | 5  | 6
        //     ----o----o----      ----o----o----
        //      00 | 10 | 20       0000|0001|0010
        //         |    |           0  | 1  | 2
        //
        // Note this is my optimization to the Cohen-Sutherland algorithm
        // bitcode detector.
        uint horizontalBits = Convert.ToUInt32(position.X > Left) + Convert.ToUInt32(position.X > Right);
        uint verticalBits = Convert.ToUInt32(position.Y > Bottom) + Convert.ToUInt32(position.Y > Top);

        switch (horizontalBits | (verticalBits << 2))
        {{
        case 0x0: // Bottom left
            return (TopLeft, BottomRight);
        case 0x1: // Bottom middle
            return (BottomLeft, BottomRight);
        case 0x2: // Bottom right
            return (BottomLeft, TopRight);
        case 0x4: // Middle left
            return (TopLeft, BottomLeft);
        case 0x5: // Center (this shouldn't be a case via precondition).
            return (TopLeft, BottomRight);
        case 0x6: // Middle right
            return (BottomRight, TopRight);
        case 0x8: // Top left
            return (TopRight, BottomLeft);
        case 0x9: // Top middle
            return (TopRight, TopLeft);
        case 0xA: // Top right
            return (BottomRight, TopLeft);
        default:
            Fail(""Unexpected spanning edge bit code"");
            return (TopLeft, BottomRight);
        }}
    }}");
                    w.WriteLine();
                }
            }


            foreach (string className in new[] { StructType, InstanceType })
            {
                w.WithCBlock($"public {StructType} Combine(params {className}[] boxes)", () =>
                {
                    w.WriteLine($"{VecStruct} min = Min;");
                    w.WriteLine($"{VecStruct} max = Max;");
                    w.WithCBlock("for (int i = 0; i < boxes.Length; i++)", () =>
                    {
                        w.WriteLine($"{className} box = boxes[i];");
                        w.WriteLine($"min.X = min.X.Min(box.Min.X);");
                        w.WriteLine($"min.Y = min.Y.Min(box.Min.Y);");
                        if (m_dimension == 3)
                            w.WriteLine($"min.Z = min.Z.Min(box.Min.Z);");
                        w.WriteLine($"max.X = max.X.Max(box.Max.X);");
                        w.WriteLine($"max.Y = max.Y.Max(box.Max.Y);");
                        if (m_dimension == 3)
                            w.WriteLine($"max.Z = max.Z.Max(box.Max.Z);");
                    });
                    w.WriteLine("return new(min, max);");
                });
            }

            if (m_isStruct && m_type.IsFloatingPointPrimitive())
            {
                foreach (var itemType in new[] { StructType, InstanceType, SegStruct, SegClassStruct, SegClassT })
                {
                    bool isSeg = itemType.StartsWith("Seg");
                    bool isBounding = itemType.StartsWith("Bounding");

                    string genericPrefix = itemType == SegClassT ? "<T>" : "";
                    string whereConstraint = itemType == SegClassT ? WhereVec : "";
                    string methodName = isSeg ? "Bound" : "Combine";
                    w.WithCBlock($"public static {StructType}? {methodName}{genericPrefix}(IEnumerable<{itemType}> items) {whereConstraint}", () =>
                    {
                        w.WriteLine("if (items.Empty())");
                        w.WriteLine("    return null;");

                        string converter = isSeg ? ".Box" : (isBounding ? ".Struct" : "");
                        w.WriteLine($"{StructType} initial = items.First(){converter};");
                        
                        string selector = isBounding ? ".Select(s => s.Struct)" : (isSeg ? ".Select(s => s.Box)" : "");
                        w.WithCBlock($"return items.Skip(1){selector}.Aggregate(initial, (acc, box) =>", () =>
                        {
                            w.WriteLine($"{VecStruct} min = acc.Min;");
                            w.WriteLine($"{VecStruct} max = acc.Max;");
                            w.WriteLine("min.X = min.X.Min(box.Min.X);");
                            w.WriteLine("min.Y = min.Y.Min(box.Min.Y);");
                            if (m_dimension == 3)
                                w.WriteLine("min.Z = min.Z.Min(box.Min.Z);");
                            w.WriteLine("max.X = max.X.Max(box.Max.X);");
                            w.WriteLine("max.Y = max.Y.Max(box.Max.Y);");
                            if (m_dimension == 3)
                                w.WriteLine("max.Z = max.Z.Max(box.Max.Z);");
                            w.WriteLine($"return new {StructType}(min, max);");
                        });
                        w.WriteLine(");");
                    });
                }
            }

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
