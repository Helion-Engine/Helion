using System;
using CodegenCS;

namespace Generators.Generators
{
    public class SegmentGenerator
    {
        private readonly Types m_type;
        private readonly int m_dimension;
        private readonly bool m_isStruct;
        private readonly bool m_vecAreStructs;
        
        private string ClassName => m_isStruct ? StructType : (m_vecAreStructs ? ClassStructName : ClassClassName);
        private string PrimitiveType => m_type.PrimitiveType();
        private string StructType => $"Seg{m_dimension}{m_type.GetShorthand()}";
        private string ClassStructName => $"Segment{m_dimension}{m_type.GetShorthand()}";
        private string ClassClassName => $"SegmentT{m_dimension}{m_type.GetShorthand()}";
        private string VecStruct => $"Vec{m_dimension}{m_type.GetShorthand()}";
        private string VecClass => $"Vector{m_dimension}{m_type.GetShorthand()}";
        private string VecFieldType => m_vecAreStructs ? VecStruct : "T";
        private string BoxStruct => $"Box{m_dimension}{m_type.GetShorthand()}";
        
        public SegmentGenerator(Types type, int dimension, bool isStruct, bool vecAreStructs)
        {
            m_type = type;
            m_dimension = dimension;
            m_isStruct = isStruct;
            m_vecAreStructs = isStruct || vecAreStructs;
        }

        private void PerformGeneration()
        {
            var w = new CodegenTextWriter();
            
            w.WriteCommentHeader();
            w.WriteLine("using System;");
            w.WriteLine("using System.Collections.Generic;");
            w.WriteLine("using Helion.Geometry.Boxes;");
            w.WriteLine("using Helion.Geometry.Vectors;");
            w.WriteLine("using Helion.Util.Extensions;");
            w.WriteLine();
            w.WriteNamespaceBlock("Geometry.Segments", () =>
            {
                string classOrStruct = m_isStruct ? "struct" : "class";
                w.WithCBlock($"public {classOrStruct} {ClassName}", () =>
                {
                    WriteFields(w);
                    WriteProperties(w);
                    WriteConstructors(w);
                    WriteDeconstructions(w);
                    WriteIndexerMethods(w);
                    WriteOperators(w);
                    WriteMethods(w);
                });
            });

            string path = $"Geometry/Segments/{ClassName}.cs";
            Console.WriteLine($"Generating {path}");
            w.WriteToCoreProject(path);
        }

        private void WriteFields(CodegenTextWriter w)
        {
            w.WriteLine($"public {VecFieldType} Start;");
            w.WriteLine($"public {VecFieldType} End;");
            w.WriteLine();
        }

        private void WriteProperties(CodegenTextWriter w)
        {
            w.WriteLine($"public {VecStruct} Delta => End - Start;");
            w.WriteLine($"public {PrimitiveType} Length => Start.Distance(End);");
            if (m_dimension == 2)
            {
                w.WriteLine($"public bool IsAxisAligned => Start.X.ApproxEquals(End.X) || Start.Y.ApproxEquals(End.Y);");
                w.WriteLine($"public {BoxStruct} Box => new((Start.X.Min(End.X), Start.Y.Min(End.Y)), (Start.X.Max(End.X), Start.Y.Max(End.Y)));");
            }
            else if (m_dimension == 3)
            {
                w.WriteLine($"public {BoxStruct} Box => new((Start.X.Min(End.X), Start.Y.Min(End.Y), Start.Z.Min(End.Z)), (Start.X.Max(End.X), Start.Y.Max(End.Y), Start.Z.Max(End.Z)));");
            }
            w.WriteLine($"public IEnumerable<{VecFieldType}> Vertices => GetVertices();");
            
            w.WriteLine();
        }

        private void WriteConstructors(CodegenTextWriter w)
        {
            w.WithCBlock($"public {ClassName}({VecFieldType} start, {VecFieldType} end)", () =>
            {
                w.WriteLine($"Start = start;");
                w.WriteLine($"End = end;");
            });
            w.WriteLine();
            
            w.WithCBlock($"public static implicit operator {ClassName}(ValueTuple<{VecFieldType}, {VecFieldType}> tuple)", () =>
            {
                w.WriteLine($"return new(tuple.Item1, tuple.Item2);");
            });
            w.WriteLine();
        }

        private void WriteDeconstructions(CodegenTextWriter w)
        {
            w.WithCBlock($"public void Deconstruct(out {VecFieldType} start, out {VecFieldType} end)", () =>
            {
                w.WriteLine($"start = Start;");
                w.WriteLine($"end = End;");
            });
            
            w.WriteLine();
        }

        private void WriteIndexerMethods(CodegenTextWriter w)
        {
            w.WriteLine($"public {VecFieldType} this[int index] => index == 0 ? Start : End;");
            w.WriteLine($"public {VecFieldType} this[Endpoint endpoint] => endpoint == Endpoint.Start ? Start : End;");
            
            w.WriteLine();
        }

        private void WriteOperators(CodegenTextWriter w)
        {
            string vecClass = m_vecAreStructs ? VecClass : "T";
            
            w.WriteLine($"public static {StructType} operator +({ClassName} self, {VecStruct} other) => new(self.Start + other, self.End + other);");
            w.WriteLine($"public static {StructType} operator +({ClassName} self, {vecClass} other) => new(self.Start + other, self.End + other);");
            w.WriteLine($"public static {StructType} operator -({ClassName} self, {VecStruct} other) => new(self.Start - other, self.End - other);");
            w.WriteLine($"public static {StructType} operator -({ClassName} self, {vecClass} other) => new(self.Start - other, self.End - other);");
            w.WriteLine($"public static bool operator ==({ClassName} self, {ClassName} other) => self.Start == other.Start && self.End == other.End;");
            w.WriteLine($"public static bool operator !=({ClassName} self, {ClassName} other) => !(self == other);");
            
            w.WriteLine();
        }

        private void WriteMethods(CodegenTextWriter w)
        {
            string epsilon = m_type == Types.Double ? "0.000001" : (m_type == Types.Float ? "0.0001f" : "FIXED_EPSILON");

            w.WriteLine($"public {VecStruct} Opposite(Endpoint endpoint) => endpoint == Endpoint.Start ? End : Start;");
            w.WriteLine($"public {StructType} WithStart({VecStruct} start) => (start, End);");
            w.WriteLine($"public {StructType} WithStart({VecClass} start) => (start.Struct, End);");
            w.WriteLine($"public {StructType} WithEnd({VecStruct} end) => (Start, end);");
            w.WriteLine($"public {StructType} WithEnd({VecClass} end) => (Start, end.Struct);");
            w.WriteLine($"public {VecStruct} FromTime({PrimitiveType} t) => Start + (Delta * t);");

            if (m_dimension == 2)
            {
                string VecStruct3D() => "Vec3" + m_type.GetShorthand();
                string VecClass3D() => "Vector3" + m_type.GetShorthand();
                
                w.WriteLine($"public bool SameDirection({StructType} seg) => SameDirection(seg.Delta);");
                w.WriteLine($"public bool SameDirection({ClassStructName} seg) => SameDirection(seg.Delta);");

                w.WithCBlock($"public bool SameDirection({VecStruct} delta)", () =>
                {
                    w.WriteLine($"{VecStruct} thisDelta = Delta;");
                    w.WriteLine($"return !thisDelta.X.DifferentSign(delta.X) && !thisDelta.Y.DifferentSign(delta.Y);");
                });
                w.WithCBlock($"public bool SameDirection({VecClass} delta)", () =>
                {
                    w.WriteLine($"{VecStruct} thisDelta = Delta;");
                    w.WriteLine($"return !thisDelta.X.DifferentSign(delta.X) && !thisDelta.Y.DifferentSign(delta.Y);");
                });

                w.WithCBlock($"public double PerpDot({VecStruct} point)", () =>
                {
                    w.WriteLine($"return (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));");
                });
                w.WithCBlock($"public double PerpDot({VecClass} point)", () =>
                {
                    w.WriteLine($"return (Delta.X * (point.Y - Start.Y)) - (Delta.Y * (point.X - Start.X));");
                });
                
                w.WriteLine($"public double PerpDot({VecStruct3D()} point) => PerpDot(point.XY);");
                w.WriteLine($"public double PerpDot({VecClass3D()} point) => PerpDot(point.XY);");
                w.WriteLine($"public bool OnRight({VecStruct} point) => PerpDot(point) <= 0;");
                w.WriteLine($"public bool OnRight({VecClass} point) => PerpDot(point) <= 0;");
                w.WriteLine($"public bool OnRight({VecStruct3D()} point) => PerpDot(point.XY) <= 0;");
                w.WriteLine($"public bool OnRight({VecClass3D()} point) => PerpDot(point.XY) <= 0;");
                w.WriteLine($"public bool DifferentSides({VecStruct} first, {VecStruct} second) => OnRight(first) != OnRight(second);");
                w.WriteLine($"public bool DifferentSides({VecClass} first, {VecClass} second) => OnRight(first) != OnRight(second);");
                w.WriteLine($"public bool DifferentSides({StructType} seg) => OnRight(seg.Start) != OnRight(seg.End);");
                w.WriteLine($"public bool DifferentSides({ClassStructName} seg) => OnRight(seg.Start) != OnRight(seg.End);");
                
                if (m_type != Types.Fixed)
                {
                    w.WithCBlock($"public Rotation ToSide({VecStruct} point, {PrimitiveType} epsilon = {epsilon})", () =>
                    {
                        w.WriteLine($"{PrimitiveType} value = PerpDot(point);");
                        w.WriteLine($"bool approxZero = value.ApproxZero(epsilon);");
                        w.WriteLine($"return approxZero ? Rotation.On : (value < 0 ? Rotation.Right : Rotation.Left);");
                    });    
                    
                    w.WithCBlock($"public bool Parallel({StructType} seg, {PrimitiveType} epsilon = {epsilon})", () =>
                    {
                        w.WriteLine($"return (Delta.Y * seg.Delta.X).ApproxEquals(Delta.X * seg.Delta.Y, epsilon);");
                    }); 
                    w.WithCBlock($"public bool Parallel({ClassStructName} seg, {PrimitiveType} epsilon = {epsilon})", () =>
                    {
                        w.WriteLine($"return (Delta.Y * seg.Delta.X).ApproxEquals(Delta.X * seg.Delta.Y, epsilon);");
                    }); 
                    
                    w.WithCBlock($"public bool Collinear({StructType} seg)", () =>
                    {
                        w.WriteLine($"return CollinearHelper(seg.Start, Start, End) && CollinearHelper(seg.End, Start, End);");
                    }); 
                    w.WithCBlock($"public bool Collinear({ClassStructName} seg)", () =>
                    {
                        w.WriteLine($"return CollinearHelper(seg.Start, Start, End) && CollinearHelper(seg.End, Start, End);");
                    });

                    w.WriteLine($"public bool Intersects({StructType} other) => Intersection(other, out {PrimitiveType} t) && (t >= 0 && t <= 1);");
                    w.WriteLine($"public bool Intersects({ClassStructName} other) => Intersection(other, out {PrimitiveType} t) && (t >= 0 && t <= 1);");

                    string intersectionInternal = $@"
                        {PrimitiveType} areaStart = DoubleTriArea(Start, End, seg.End);
                        {PrimitiveType} areaEnd = DoubleTriArea(Start, End, seg.Start);

                        if (areaStart.DifferentSign(areaEnd))
                        {{
                            {PrimitiveType} areaThisStart = DoubleTriArea(seg.Start, seg.End, Start);
                            {PrimitiveType} areaThisEnd = DoubleTriArea(seg.Start, seg.End, End);
                            
                            if (areaStart.DifferentSign(areaEnd))
                            {{
                                t = areaThisStart / (areaThisStart - areaThisEnd);
                                return t >= 0 && t <= 1;
                            }}
                        }}

                        t = default;
                        return false;";
                    w.WithCBlock($"public bool Intersection({StructType} seg, out {PrimitiveType} t)", () =>
                    {
                        w.WriteLine(intersectionInternal);
                    });
                    w.WithCBlock($"public bool Intersection({ClassStructName} seg, out {PrimitiveType} t)", () =>
                    {
                        w.WriteLine(intersectionInternal);
                    });
                    
                    string intersectionAsLineInternal = $@"
                        {PrimitiveType} determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
                        if (determinant.ApproxZero())
                        {{
                            tThis = default;
                            return false;
                        }}

                        {VecStruct} startDelta = Start - seg.Start;
                        tThis = ((seg.Delta.X * startDelta.Y) - (seg.Delta.Y * startDelta.X)) / determinant;
                        return true;";
                    w.WithCBlock($"public bool IntersectionAsLine({StructType} seg, out {PrimitiveType} tThis)", () =>
                    {
                        w.WriteLine(intersectionAsLineInternal);
                    });
                    w.WithCBlock($"public bool IntersectionAsLine({ClassStructName} seg, out {PrimitiveType} tThis)", () =>
                    {
                        w.WriteLine(intersectionAsLineInternal);
                    });
                    
                    string intersectionAsLineTwoInternal = $@"
                        {PrimitiveType} determinant = (-seg.Delta.X * Delta.Y) + (Delta.X * seg.Delta.Y);
                        if (determinant.ApproxZero())
                        {{
                                tThis = default;
                                tOther = default;
                                return false;
                        }}

                        {VecStruct} startDelta = Start - seg.Start;
                        {PrimitiveType} inverseDeterminant = 1.0f / determinant;
                        tThis = ((seg.Delta.X * startDelta.Y) - (seg.Delta.Y * startDelta.X)) * inverseDeterminant;
                        tOther = ((-Delta.Y * startDelta.X) + (Delta.X * startDelta.Y)) * inverseDeterminant;
                        return true;";
                    w.WithCBlock($"public bool IntersectionAsLine({StructType} seg, out {PrimitiveType} tThis, out {PrimitiveType} tOther)", () =>
                    {
                        w.WriteLine(intersectionAsLineTwoInternal);
                    });
                    w.WithCBlock($"public bool IntersectionAsLine({ClassStructName} seg, out {PrimitiveType} tThis, out {PrimitiveType} tOther)", () =>
                    {
                        w.WriteLine(intersectionAsLineTwoInternal);
                    });
                    
                    string closestPointInternal = $@"
                        {VecStruct} pointToStartDelta = Start - point;
                        {PrimitiveType} t = -Delta.Dot(pointToStartDelta) / Delta.Dot(Delta);

                        if (t <= 0)
                            return Start;
                        if (t >= 1)
                            return End;
                        return FromTime(t);";
                    w.WithCBlock($"public {VecStruct} ClosestPoint({VecStruct} point)", () =>
                    {
                        w.WriteLine(closestPointInternal);
                    });
                    w.WithCBlock($"public {VecStruct} ClosestPoint({VecClass} point)", () =>
                    {
                        w.WriteLine(closestPointInternal);
                    });
                    
                    string intersectsBoxInternal = @"
                        if (Start.X.ApproxEquals(End.X))
                            return box.Min.X < Start.X && Start.X < box.Max.X;
                        if (Start.Y.ApproxEquals(End.Y))
                            return box.Min.Y < Start.Y && Start.Y < box.Max.Y;
                        return (box.Min.X < box.Max.X) ^ (box.Min.Y < box.Max.Y) ?
                            DifferentSides(box.BottomLeft, box.TopRight) :
                            DifferentSides(box.TopLeft, box.BottomRight);";
                    w.WithCBlock($"public bool Intersects({BoxStruct} box)", () =>
                    {
                        w.WriteLine(intersectsBoxInternal);
                    });

                    w.WriteLine();
                }
                
                w.WriteLine($@"public override string ToString() => $""({{Start}}), ({{End}})"";");
                w.WriteLine($"public override bool Equals(object? obj) => obj is {ClassName} seg && Start == seg.Start && End == seg.End;");
                w.WriteLine($"public override int GetHashCode() => HashCode.Combine(Start.GetHashCode(), End.GetHashCode());");
                w.WriteLine();
                
                w.WithCBlock($"private IEnumerable<{VecFieldType}> GetVertices()", () =>
                {
                    w.WriteLine($"yield return Start;");
                    w.WriteLine($"yield return End;");
                });
                
                w.WithCBlock($"private static bool CollinearHelper({VecStruct} first, {VecStruct} second, {VecStruct} third)", () =>
                {
                    w.WriteLine($"return ((first.X * (second.Y - third.Y)) + (second.X * (third.Y - first.Y)) + (third.X * (first.Y - second.Y))).ApproxZero();");
                });
                    
                w.WithCBlock($"private static double DoubleTriArea({VecStruct} first, {VecStruct} second, {VecStruct} third)", () =>
                {
                    w.WriteLine($"return ((first.X - third.X) * (second.Y - third.Y)) - ((first.Y - third.Y) * (second.X - third.X));");
                });
            }
        }

        public static void Generate()
        {
            foreach (Types type in new[] { Types.Float, Types.Double, Types.Fixed })
            {
                foreach (int dimension in new[] { 2, 3 })
                {
                    new SegmentGenerator(type, dimension, true, false).PerformGeneration();
                    new SegmentGenerator(type, dimension, false, true).PerformGeneration();
                    new SegmentGenerator(type, dimension, false, false).PerformGeneration();
                }
            }
        }
    }
}
