using System;
using CodegenCS;

namespace Generators.GeneratorsNew
{
    public class VectorGenerator : Generator
    {
        private static readonly string[] DimensionFields = { "X", "Y", "Z", "W" };

        private readonly string[] m_fields;
        
        public VectorGenerator(Types type, int dimension, string structPrefix, string classPrefix, bool isStruct) : 
            base(type, dimension, structPrefix, classPrefix, isStruct)
        {
            if (dimension is < 2 or > 4)
                throw new Exception("Invalid dimension");

            m_fields = DimensionFields[..Dimension];
        }

        private void WriteImports(CodegenTextWriter w)
        {
            w.WriteLine("using System;");
            w.WriteLine("using System.Collections.Generic;");
            w.WriteLine("using System.Runtime.InteropServices;");
            w.WriteLine("using GlmSharp;");
            w.WriteLine("using Helion.Geometry.Boxes;");
            w.WriteLine("using Helion.Geometry.Segments;");
            w.WriteLine("using Helion.Util.Extensions;");
        }

        private void WriteClass(CodegenTextWriter w)
        {
            w.WriteNamespaceBlock("Geometry.Vectors", () =>
            {
                if (IsStruct)
                    w.WriteLine(StructLayoutPack);

                w.WithCBlock($"public {ClassOrStructDeclaration} {ClassOrInstanceName}", () =>
                {
                    WriteStaticReadonly(w);
                    WriteFields(w);
                    WriteProperties(w);
                    // WriteConstructors(w);
                    // WriteDeconstructions(w);
                    // WriteIndexerMethod(w);
                    // WriteOperators(w);
                    // WriteMethods(w);
                });
            });
        }

        private void WriteFields(CodegenTextWriter w)
        {
            foreach (string field in m_fields)
                w.WriteLine($"public {Primitive} {field};");
            
            w.WriteLine();
        }

        private void WriteProperties(CodegenTextWriter w)
        {
            w.WriteLine($"public {Primitive} U => X;");
            w.WriteLine($"public {Primitive} V => Y;");

            if (Dimension >= 3)
            {
                w.WriteLine($"public {StructDim(2)} XY => new(X, Y);");
                w.WriteLine($"public {StructDim(2)} XZ => new(X, Z);");
                w.WriteLine($"public {StructDim(2)} YZ => new(Y, Z);");
            }

            if (Dimension >= 4)
            {
                w.WriteLine($"public {StructDim(2)} XW => new(X, W);");
                w.WriteLine($"public {StructDim(2)} YW => new(Y, W);");
                w.WriteLine($"public {StructDim(2)} ZW => new(Z, W);");
                w.WriteLine($"public {StructDim(3)} XYZ => new(X, Y, Z);");
                w.WriteLine($"public {StructDim(3)} XYW => new(X, Y, W);");
                w.WriteLine($"public {StructDim(3)} XZW => new(X, Z, W);");
                w.WriteLine($"public {StructDim(3)} YZW => new(Y, Z, W);");
            }
            
            // Things like `public Vec2D Double => new(X.ToDouble(), Y.ToDouble());`
            foreach (Types type in CommonGeometryTypes)
            {
                if (Type == type)
                    continue;

                string common = Type.CommonName();
                string args = CommaSuffix($".To{common}()", m_fields);
                w.WriteLine($"public {StructDimType(Dimension, type)} {common} => new({args});");
            }

            if (IsIntFloatDouble && Dimension is 2 or 3)
            {
                string args = $"({CommaRepeat(0, Dimension)}), ({Commas(m_fields)})";
                w.WriteLine($"public Box{DimensionFields}{Shorthand} Box => new({args});");
            }

            if (Type == Types.Float)
                w.WriteLine($"public vec{Dimension} Glm => new({m_fields});");
         
            if (IsClass)
                w.WriteLine($"public {StructName} Struct => new({Commas(m_fields)});");
        }

        private void WriteStaticReadonly(CodegenTextWriter w)
        {
            if (IsFixed)
            {
                w.WriteLine($"public static readonly {ClassName} Zero = new({CommaRepeat("Fixed.Zero()", Dimension)});");
                w.WriteLine($"public static readonly {ClassName} One = new({CommaRepeat("Fixed.One()", Dimension)});");
            }
            else
            {
                w.WriteLine($"public static readonly {ClassName} Zero = new({CommaRepeat(0, Dimension)});");
                w.WriteLine($"public static readonly {ClassName} One = new({CommaRepeat(1, Dimension)});");   
            }
            
            w.WriteLine();
        }

        protected override void Generate(CodegenTextWriter w)
        {
            WriteCommentHeader(w);
            WriteImports(w);
            WriteClass(w);
        }
    }
}
