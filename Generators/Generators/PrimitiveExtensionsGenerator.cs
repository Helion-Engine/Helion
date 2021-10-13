using System;
using CodegenCS;

namespace Generators.Generators;

public class PrimitiveExtensionsGenerator
{
    private static void WriteMethods(Types type, CodegenTextWriter w)
    {
        string primitive = type.PrimitiveType().ToLower();

        if (type.IsSigned())
        {
            w.WriteLine($"public static bool DifferentSign(this {primitive} first, {primitive} second) => (first ^ second) < 0;");
            w.WriteLine();
        }

        if (type.IsFloatingPointPrimitive())
        {
            string epsilon = type == Types.Double ? "0.00001" : "0.0001f";

            w.WithCBlock($"public static bool ApproxEquals(this {primitive} value, {primitive} target, {primitive} epsilon = {epsilon})", () =>
            {
                w.WriteLine($"return value >= target - epsilon && value <= target + epsilon;");
            });
            w.WriteLine();

            w.WithCBlock($"public static bool ApproxZero(this {primitive} value, {primitive} epsilon = {epsilon})", () =>
            {
                w.WriteLine($"return value.ApproxEquals(0, epsilon);");
            });
            w.WriteLine();

            w.WithCBlock($"public static {primitive} Interpolate(this {primitive} start, {primitive} end, {primitive} t)", () =>
            {
                w.WriteLine($"return start + (t * (end - start));");
            });
            w.WriteLine();

            w.WithCBlock($"public static {primitive} Floor(this {primitive} self)", () =>
            {
                if (type == Types.Float)
                    w.WriteLine($"return MathF.Floor(self);");
                else
                    w.WriteLine($"return Math.Floor(self);");
            });
            w.WriteLine();

            w.WithCBlock($"public static {primitive} Ceiling(this {primitive} self)", () =>
            {
                if (type == Types.Float)
                    w.WriteLine($"return MathF.Ceiling(self);");
                else
                    w.WriteLine($"return Math.Ceiling(self);");
            });
            w.WriteLine();

            w.WriteLine($"public static bool DifferentSign(this {primitive} first, {primitive} second) => (first * second) < 0;");
            w.WriteLine();

            w.WriteLine($"public static bool InNormalRange(this {primitive} value) => value >= 0 && value <= 1;");
            w.WriteLine();
        }

        if (type != Types.Fixed)
        {
            w.WriteLine($"public static {primitive} Clamp(this {primitive} value, {primitive} low, {primitive} high) => value < low ? low : (value > high ? high : value);");
            w.WriteLine();

            w.WriteLine($"public static ({primitive} min, {primitive} max) MinMax({primitive} first, {primitive} second) => (first.Min(second), first.Max(second));");
            w.WriteLine();
        }

        w.WithCBlock($"public static {primitive} Min(this {primitive} self, {primitive} other)", () =>
        {
            w.WriteLine($"return Math.Min(self, other);");
        });
        w.WriteLine();

        w.WithCBlock($"public static {primitive} Max(this {primitive} self, {primitive} other)", () =>
        {
            w.WriteLine($"return Math.Max(self, other);");
        });
        w.WriteLine();

        if (!type.IsUnsigned())
        {
            w.WithCBlock($"public static {primitive} Abs(this {primitive} self)", () =>
            {
                w.WriteLine($"return Math.Abs(self);");
            });
            w.WriteLine();
        }
    }

    public static void Generate()
    {
        var w = new CodegenTextWriter();

        w.WriteCommentHeader();
        w.WriteLine("using System;");
        w.WriteLine();
        w.WriteNamespaceBlock("Util.Extensions", () =>
        {
            w.WithCBlock("public static class PrimitiveExtensions", () =>
            {
                foreach (Types type in Enum.GetValues<Types>())
                    if (type != Types.Fixed)
                        WriteMethods(type, w);
            });
        });

        string path = "Util/Extensions/PrimitiveExtensions.cs";
        Console.WriteLine($"Generating {path}");
        w.WriteToCoreProject(path);
    }
}
