using System;
using System.Collections.Generic;
using System.Linq;
using CodegenCS;

namespace Generators
{
    public class VectorGenerator
    {
        private static readonly string[] DimensionFields = { "X", "Y", "Z", "W" };
        
        private readonly Types m_type;
        private readonly int m_dimension;
        private readonly bool m_isStruct;
        private readonly string[] m_fields;

        public string StructType => GetStructDim(m_dimension);
        public string InstanceType => GetInstanceDim(m_dimension);
        private string ClassName => m_isStruct ? StructType : InstanceType;

        private VectorGenerator(Types type, int dimension, bool isStruct)
        {
            m_type = type;
            m_dimension = dimension;
            m_isStruct = isStruct;
            m_fields = DimensionFields[..dimension];
        }

        private string GetStructDim(int dim) => GetStructDimType(dim, m_type);
        private string GetInstanceDim(int dim) => GetInstanceDimType(dim, m_type);
        private static string GetStructDimType(int dim, Types type) => "Vec" + dim + type.GetShorthand();
        private static string GetInstanceDimType(int dim, Types type) => "Vector" + dim + type.GetShorthand();
        
        private static string CommaSeparate(params object[] objects) => string.Join(", ", objects);

        private static string CommaSeparatePrefix(string prefix, params object[] objects)
        {
            return CommaSeparateWrap(prefix, "", objects);
        }
        
        private static string CommaSeparateSuffix(string suffix, params object[] objects)
        {
            return CommaSeparateWrap("", suffix, objects);
        }

        private static string CommaSeparateWrap(string prefix, string suffix, params object[] objects)
        {
            return CommaSeparate(objects.Select(obj => prefix + obj + suffix).ToArray());
        }
        
        private static string CommaSeparateRepeat(object obj, int times)
        {
            List<object> objs = new();
            for (int i = 0; i < times; i++)
                objs.Add(obj);
            return CommaSeparate(objs.ToArray());
        }
        
        private void PerformGeneration()
        {
            var w = new CodegenTextWriter();

            w.WriteCommentHeader();
            w.WriteLine("using System;");
            w.WriteLine("using System.Collections.Generic;");
            w.WriteLine("using Helion.Util.Extensions;");
            w.WriteLine("using Helion.Util.Geometry;");
            w.WriteLine();
            w.WriteNamespaceBlock("Geometry.Vectors", () =>
            {
                string classOrStruct = m_isStruct ? "struct" : "class";
                w.WithCBlock($"public {classOrStruct} {ClassName}", () =>
                {
                    WriteStaticReadonly(w);
                    WriteFields(w);
                    WriteProperties(w);
                    WriteConstructors(w);
                    WriteDeconstructions(w);
                    WriteIndexerMethod(w);
                    WriteOperators(w);
                    WriteMethods(w);
                });
            });

            string path = $"Geometry/Vectors/{ClassName}.cs";
            Console.WriteLine($"Generating {path}");
            w.WriteToCoreProject(path);
        }

        private void WriteStaticReadonly(CodegenTextWriter w)
        {
            w.WriteLine($"public static readonly {ClassName} Zero = ({CommaSeparateRepeat(0, m_dimension)});");
            w.WriteLine($"public static readonly {ClassName} One = ({CommaSeparateRepeat(1, m_dimension)});");
            w.WriteLine();
        }

        private void WriteFields(CodegenTextWriter w)
        {
            foreach (string field in m_fields)
                w.WriteLine($"public {m_type.PrimitiveType()} {field};");
            w.WriteLine();
        }

        private void WriteProperties(CodegenTextWriter w)
        {
            w.WriteLine($"public {m_type.PrimitiveType()} U => X;");
            w.WriteLine($"public {m_type.PrimitiveType()} V => Y;");
            if (m_dimension >= 3)
            {
                w.WriteLine($"public {GetStructDim(2)} XY => new(X, Y);");
                w.WriteLine($"public {GetStructDim(2)} XZ => new(X, Z);");
            }
            if (m_dimension >= 4)
                w.WriteLine($"public {GetStructDim(3)} XYZ => new(X, Y, Z);");
            
            if (m_type != Types.Int)
                w.WriteLine($"public {GetStructDimType(m_dimension, Types.Int)} Int => new({CommaSeparatePrefix("(int)", m_fields)});");
            if (m_type != Types.Float)
                w.WriteLine($"public {GetStructDimType(m_dimension, Types.Float)} Float => new({CommaSeparatePrefix("(float)", m_fields)});");
            if (m_type != Types.Double)
                w.WriteLine($"public {GetStructDimType(m_dimension, Types.Double)} Double => new({CommaSeparatePrefix("(double)", m_fields)});");
            if (m_type != Types.Fixed)
                w.WriteLine($"public {GetStructDimType(m_dimension, Types.Fixed)} FixedPoint => new({CommaSeparateWrap("Fixed.From(", ")", m_fields)});");
            
            if (!m_isStruct)
                w.WriteLine($"public {StructType} Struct => new({CommaSeparate(m_fields)});");
            
            w.WriteLine("public IEnumerable<double> Values => GetEnumerableValues();");
            
            w.WriteLine();
        }

        private void WriteConstructors(CodegenTextWriter w)
        {
            string[] lowerFields = m_fields.Select(f => f.ToLower()).ToArray();
            w.WithCBlock($"public {ClassName}({CommaSeparatePrefix(m_type.PrimitiveType() + " ", lowerFields)})", () =>
            {
                foreach (var field in m_fields)
                    w.WriteLine($"{field} = {field.ToLower()};");
            });
            w.WriteLine();

            string generics = CommaSeparateRepeat(m_type.PrimitiveType(), m_dimension);
            w.WithCBlock($"public static implicit operator {ClassName}(ValueTuple<{generics}> tuple)", () =>
            {
                string[] args = m_fields.Select((f, i) => $"tuple.Item{i + 1}").ToArray();
                w.WriteLine($"return new({CommaSeparate(args)});");
            });
            w.WriteLine();
        }

        private void WriteDeconstructions(CodegenTextWriter w)
        {
            string[] lowerFields = m_fields.Select(f => f.ToLower()).ToArray();
            w.WithCBlock($"public void Deconstruct({CommaSeparatePrefix($"out {m_type.PrimitiveType()} ", lowerFields)})", () =>
            {
                foreach (var field in m_fields)
                    w.WriteLine($"{field.ToLower()} = {field};");
            });
            w.WriteLine();
        }

        private void WriteIndexerMethod(CodegenTextWriter w)
        {
            w.WithCBlock($"public {m_type.PrimitiveType()} this[int index]", () =>
            {
                w.WithCBlock("get", () =>
                {
                    w.WithCBlock("return index switch", () =>
                    {
                        for (int i = 0; i < m_fields.Length; i++)
                            w.WriteLine($"{i} => {m_fields[i]},");
                        w.WriteLine("_ => throw new IndexOutOfRangeException()");
                    });
                    w.WriteLine(";");
                });
                w.WithCBlock("set", () =>
                {
                    w.WithCBlock("switch (index)", () =>
                    {
                        for (int i = 0; i < m_fields.Length; i++)
                        {
                            w.WriteLine($"case {i}:");
                            w.WriteLine($"    {m_fields[i]} = value;");
                            w.WriteLine($"    break;");
                        }
                        w.WriteLine("default:");
                        w.WriteLine("    throw new IndexOutOfRangeException();");
                    });
                });
            });
            w.WriteLine();
        }
        
        private void WriteOperators(CodegenTextWriter w)
        {
            string[] self = m_fields.Select(f => $"self.{f}").ToArray();
            (string, string)[] selfOther = m_fields.Select(f => ($"self.{f}", $"other.{f}")).ToArray();
            (string, string)[] selfValue = m_fields.Select(f => ($"self.{f}", "value")).ToArray();

            w.WriteLine($"public static {StructType} operator -({ClassName} self) => new({CommaSeparatePrefix("-", self)});");
            WriteOperator("+", selfOther);
            WriteOperator("-", selfOther);
            WriteOperator("*", selfOther);
            WriteOperatorValue("*", m_type.PrimitiveType(), selfValue);
            WriteOperator("/", selfOther);
            WriteOperatorValue("/", m_type.PrimitiveType(), selfValue, true);
            
            string equality = string.Join(" && ", selfOther.Select(pair => $"{pair.Item1} == {pair.Item2}"));
            w.WriteLine($"public static bool operator ==({ClassName} self, {StructType} other) => {equality};");
            w.WriteLine($"public static bool operator ==({ClassName} self, {InstanceType} other) => {equality};");
            w.WriteLine($"public static bool operator !=({ClassName} self, {StructType} other) => !(self == other);");
            w.WriteLine($"public static bool operator !=({ClassName} self, {InstanceType} other) => !(self == other);");
            w.WriteLine();

            string Fuse(string join, (string, string)[] pairs)
            {
                string[] fusedPairs = pairs.Select(pair => $"{pair.Item1} {join} {pair.Item2}").ToArray();
                return CommaSeparate(fusedPairs);
            }

            void WriteOperator(string op, (string, string)[] pairs)
            {
                w.WriteLine($"public static {StructType} operator {op}({ClassName} self, {StructType} other) => new({Fuse(op, pairs)});");
                w.WriteLine($"public static {StructType} operator {op}({ClassName} self, {InstanceType} other) => new({Fuse(op, pairs)});");
            }
            
            void WriteOperatorValue(string op, string valueType, (string, string)[] pairs, bool noPrefixOp = false)
            {
                w.WriteLine($"public static {StructType} operator {op}({ClassName} self, {valueType} value) => new({Fuse(op, pairs)});");
                if (!noPrefixOp)
                    w.WriteLine($"public static {StructType} operator {op}({valueType} value, {ClassName} self) => new({Fuse(op, pairs)});");
            }
        }

        private void WriteMethods(CodegenTextWriter w)
        {
            WriteWithMethods(w);
            WriteApproxMethod(w);
            WriteToDimMethods(w);
            w.WriteLine();
            WriteMathMethods(w);
            WriteObjectMethods(w);
        }

        private void WriteWithMethods(CodegenTextWriter w)
        {
            for (int i = 0; i < m_fields.Length; i++)
            {
                string field = m_fields[i];
                string lowerField = field.ToLower();
                
                // We want to have the field we're on lowercased (ex: if y, then [X, y, Z]).
                string[] fieldsCopy = m_fields.ToArray();
                fieldsCopy[i] = lowerField;

                w.WriteLine($"public {StructType} With{field}(double {lowerField}) => new({CommaSeparate(fieldsCopy)});");
            }
        }

        private void WriteApproxMethod(CodegenTextWriter w)
        {
            var approxEqualStrs = m_fields.Select(f => $"{f}.ApproxEquals(other.{f})");
            w.WriteLine($"public bool IsApprox({StructType} other) => {string.Join(" && ", approxEqualStrs)};");
            w.WriteLine($"public bool IsApprox({InstanceType} other) => {string.Join(" && ", approxEqualStrs)};");
        }

        private void WriteToDimMethods(CodegenTextWriter w)
        {
            if (m_dimension == 2)
                w.WriteLine($"public {GetStructDim(3)} To3D({m_type.PrimitiveType()} z) => new(X, Y, z);");
            else if (m_dimension == 3)
                w.WriteLine($"public {GetStructDim(4)} To4D({m_type.PrimitiveType()} w) => new(X, Y, Z, w);");
        }

        private void WriteMathMethods(CodegenTextWriter w)
        {
            foreach (string operation in new[] { "Floor", "Ceiling", "Abs" })
                w.WriteLine($"public {StructType} {operation}() => new({CommaSeparateSuffix($".{operation}()", m_fields)});");

            if (m_type.IsFloatingPointPrimitive())
            {
                w.WriteLine($"public {StructType} Unit() => this / Length();");
                
                if (m_isStruct)
                    w.WriteLine("public void Normalize() => this /= Length();");
                else
                {
                    w.WithCBlock("public void Normalize()", () =>
                    {
                        w.WriteLine($"{m_type.PrimitiveType()} len = Length();");
                        foreach (string field in m_fields)
                            w.WriteLine($"{field} /= len;");
                    });
                }

                string len = string.Join(" + ", m_fields.Select(f => $"({f} * {f})"));
                w.WriteLine($"public {m_type.PrimitiveType()} LengthSquared() => {len};");
                
                w.WriteLine($"public {m_type.PrimitiveType()} Length() => Math.Sqrt(LengthSquared());");
                w.WriteLine($"public {m_type.PrimitiveType()} DistanceSquared({StructType} other) => (this - other).LengthSquared();");
                w.WriteLine($"public {m_type.PrimitiveType()} DistanceSquared({InstanceType} other) => (this - other).LengthSquared();");
                w.WriteLine($"public {m_type.PrimitiveType()} Distance({StructType} other) => (this - other).Length();");
                w.WriteLine($"public {m_type.PrimitiveType()} Distance({InstanceType} other) => (this - other).Length();");
                w.WriteLine($"public {StructType} Interpolate({StructType} end, double t) => this + (t * (end - this));");
                w.WriteLine($"public {StructType} Interpolate({InstanceType} end, double t) => this + (t * (end - this));");
            }
            
            string dot = string.Join(" + ", m_fields.Select(f => $"({f} * other.{f})"));
            w.WriteLine($"public {m_type.PrimitiveType()} Dot({StructType} other) => {dot};");
            w.WriteLine($"public {m_type.PrimitiveType()} Dot({InstanceType} other) => {dot};");

            if (m_type.IsFloatingPointPrimitive())
            {
                if (m_dimension == 2)
                {
                    w.WriteLine($"public {m_type.PrimitiveType()} Component({StructType} onto) => Dot(onto) / onto.Length();");
                    w.WriteLine($"public {m_type.PrimitiveType()} Component({InstanceType} onto) => Dot(onto) / onto.Length();");
                    w.WriteLine($"public {StructType} Projection({StructType} onto) => Dot(onto) / onto.LengthSquared() * onto;");
                    w.WriteLine($"public {StructType} Projection({InstanceType} onto) => Dot(onto) / onto.LengthSquared() * onto;");
                    w.WriteLine($"public {StructType} RotateRight90() => new(Y, -X);");
                    w.WriteLine($"public {StructType} RotateLeft90() => new(-Y, X);");
                    w.WriteLine($"public static {StructType} UnitCircle({m_type.PrimitiveType()} radians) => new(Math.Cos(radians), Math.Sin(radians));");
                }

                if (m_dimension == 3)
                {
                    w.WithCBlock($"public static {StructType} UnitSphere({m_type.PrimitiveType()} angle, {m_type.PrimitiveType()} pitch)", () =>
                    {
                        w.WriteLine($"{m_type.PrimitiveType()} sinAngle = Math.Sin(angle);");
                        w.WriteLine($"{m_type.PrimitiveType()} cosAngle = Math.Cos(angle);");
                        w.WriteLine($"{m_type.PrimitiveType()} sinPitch = Math.Sin(pitch);");
                        w.WriteLine($"{m_type.PrimitiveType()} cosPitch = Math.Cos(pitch);");
                        w.WriteLine("return new(cosAngle * cosPitch, sinAngle * cosPitch, sinPitch);");
                    });
                    
                    w.WriteLine($"public {m_type.PrimitiveType()} Pitch(in {StructType} other, {m_type.PrimitiveType()} length) => Math.Atan2(other.Z - Z, length);");
                    w.WriteLine($"public {m_type.PrimitiveType()} Pitch({InstanceType} other, {m_type.PrimitiveType()} length) => Math.Atan2(other.Z - Z, length);");
                    w.WriteLine($"public {m_type.PrimitiveType()} Pitch({m_type.PrimitiveType()} z, {m_type.PrimitiveType()} length) => Math.Atan2(z - Z, length);");
                    w.WriteLine($"public {m_type.PrimitiveType()} Angle(in {StructType} other) => Math.Atan2(other.Y - Y, other.X - X);");
                    w.WriteLine($"public {m_type.PrimitiveType()} Angle({InstanceType} other) => Math.Atan2(other.Y - Y, other.X - X);");
                    w.WriteLine($"public {m_type.PrimitiveType()} Angle(in {GetStructDim(2)} other) => Math.Atan2(other.Y - Y, other.X - X);");
                    w.WriteLine($"public {m_type.PrimitiveType()} Angle({GetInstanceDim(2)} other) => Math.Atan2(other.Y - Y, other.X - X);");
                    WriteApproxDistance($"in {StructType}");
                    WriteApproxDistance(InstanceType);
                    
                    void WriteApproxDistance(string param)
                    {
                        w.WithCBlock($"public {m_type.PrimitiveType()} ApproximateDistance2D({param} other)", () =>
                        {
                            w.WriteLine($"{m_type.PrimitiveType()} dx = Math.Abs(X - other.X);");
                            w.WriteLine($"{m_type.PrimitiveType()} dy = Math.Abs(Y - other.Y);");
                            w.WriteLine($"if (dx < dy)");
                            w.WriteLine($"    return dx + dy - (dx / 2);");
                            w.WriteLine($"return dx + dy - (dy / 2);");
                        });
                    }
                }
            }

            w.WriteLine();
            w.WithCBlock($"private IEnumerable<{m_type.PrimitiveType()}> GetEnumerableValues()", () =>
            {
                w.WriteLine("yield return X;");
                w.WriteLine("yield return Y;");
                if (m_dimension >= 3)
                    w.WriteLine("yield return Z;");
                if (m_dimension >= 4)
                    w.WriteLine("yield return W;");
            });
            w.WriteLine();
        }
        
        private void WriteObjectMethods(CodegenTextWriter w)
        {
            string interpolated = CommaSeparateWrap("{", "}", m_fields);
            w.WriteLine("public override string ToString() => $\"" + interpolated + "\";");

            string[] compared = m_fields.Select(f => $"{f} == v.{f}").ToArray();
            w.WriteLine($"public override bool Equals(object? obj) => obj is {ClassName} v && {string.Join(" && ", compared)};");
            
            w.WriteLine($"public override int GetHashCode() => HashCode.Combine({CommaSeparate(m_fields)});");
        }

        public static void Generate()
        {
            foreach (Types type in new[] { Types.Int, Types.Float, Types.Double, Types.Fixed })
            {
                foreach (int dimension in new[] { 2, 3, 4 })
                {
                    new VectorGenerator(type, dimension, true).PerformGeneration();
                    new VectorGenerator(type, dimension, false).PerformGeneration();
                }
            }
        }
    }
}
