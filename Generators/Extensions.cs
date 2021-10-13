using System;
using CodegenCS;

namespace Generators;

public static class Extensions
{
    private const string SaveProjectPath = "../../../../Core";
    private const string NamespacePrefix = "";
    public const string CommentHeader = @"// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------
";
    public static void WriteCommentHeader(this CodegenTextWriter writer)
    {
        writer.WriteLine(CommentHeader);
    }

    public static void WriteNamespaceBlock(this CodegenTextWriter writer, string namespacePath, Action action)
    {
        writer.WithCBlock($"namespace Helion.{namespacePath}", action);
    }

    public static void WriteToCoreProject(this CodegenTextWriter writer, string path)
    {
        writer.SaveToFile($"{SaveProjectPath}/{path}");
    }
}
