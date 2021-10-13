using System;
using System.Collections.Generic;
using System.Linq;
using CodegenCS;

namespace Generators.GeneratorsNew;

public abstract class Generator
{
    public static readonly Types[] CommonGeometryTypes = { Types.Int, Types.Fixed, Types.Float, Types.Double };

    public readonly Types Type;
    public readonly int Dimension;
    public readonly string StructPrefix;
    public readonly string ClassPrefix;
    public readonly bool IsStruct;

    public bool IsClass => !IsStruct;
    public bool IsIntegral => !IsFloatDoubleFixed;
    public bool IsFixed => Type == Types.Fixed;
    public bool IsFloatOrDouble => Type.IsFloatingPointPrimitive();
    public bool IsFloatDoubleFixed => IsFloatOrDouble || IsFixed;
    public bool IsIntFloatDouble => Type == Types.Int || IsFloatOrDouble;
    public string Primitive => Type.PrimitiveType();
    public string MathClass => Type == Types.Float ? "MathF" : "Math";
    public string StructLayoutPack => $"[StructLayout(LayoutKind.Sequential, Pack = {Type.SizeOf()})]";
    public string Shorthand => Type.GetShorthand();
    public string ClassOrStructDeclaration => IsStruct ? "struct" : "class";
    public string ClassName => InstanceDim(Dimension);
    public string StructName => StructDim(Dimension);
    public string ClassOrInstanceName => IsStruct ? StructName : ClassName;

    protected Generator(Types type, int dimension, string structPrefix, string classPrefix, bool isStruct)
    {
        Type = type;
        Dimension = dimension;
        StructPrefix = structPrefix;
        ClassPrefix = classPrefix;
        IsStruct = isStruct;
    }

    public static string Commas(params object[] objects)
    {
        return string.Join(", ", objects);
    }

    public static string CommaRepeat(object obj, int times)
    {
        List<object> objs = new();
        for (int i = 0; i < times; i++)
            objs.Add(obj);
        return Commas(objs.ToArray());
    }

    public static string CommaWrap(string prefix, string suffix, params object[] objects)
    {
        return string.Join(", ", objects.Select(o => $"{prefix}{o}{suffix}"));
    }

    public static string CommaPrefix(string prefix, params object[] objects)
    {
        return string.Join(", ", objects.Select(o => $"{prefix}{o}"));
    }

    public static string CommaSuffix(string suffix, params object[] objects)
    {
        return string.Join(", ", objects.Select(o => $"{o}{suffix}"));
    }

    public string StructDim(int dim) => StructDimType(dim, Type);
    public string InstanceDim(int dim) => InstanceDimType(dim, Type);
    public string StructDimType(int dim, Types type) => StructPrefix + dim + type.GetShorthand();
    public string InstanceDimType(int dim, Types type) => ClassPrefix + dim + type.GetShorthand();

    protected static void WriteCommentHeader(CodegenTextWriter w)
    {
        w.WriteCommentHeader();
    }

    protected abstract void Generate(CodegenTextWriter w);

    public void PerformGeneration(CodegenTextWriter w, string folderName, string className)
    {
        Generate(w);

        string path = $"Geometry/{folderName}/{className}.cs";
        Console.WriteLine($"Generating {path}");
        w.WriteToCoreProject(path);
    }
}
