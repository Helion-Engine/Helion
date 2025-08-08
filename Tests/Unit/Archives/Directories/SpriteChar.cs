using FluentAssertions;
using Helion.Resources;
using Helion.Resources.Archives.Directories;
using Helion.Resources.Archives.Entries;
using System.IO.Compression;
using System.Linq;
using Xunit;
using System.IO;

namespace Helion.Tests.Unit.Archives.Directories;

public class SpriteChar
{
    [Fact(DisplayName = "Directory sprite replace ^")]
    public void SpriteReplace()
    {
        const string TestDir = "DirectoryTest";
        using var zipArchive = ZipFile.OpenRead("Resources/spritechar.pk3");
        if (Directory.Exists(TestDir))
            Directory.Delete(TestDir, true);

        zipArchive.ExtractToDirectory(TestDir);

        var archive = new DirectoryArchive(new EntryPath(TestDir));
        archive.Entries.Count.Should().Be(3);
        var globalEntry = archive.Entries.First(x => x.Namespace == ResourceNamespace.Global);
        var spriteEntry = archive.Entries.First(x => x.Namespace == ResourceNamespace.Sprites);
        var textureEntry = archive.Entries.First(x => x.Namespace == ResourceNamespace.Textures);

        var fullTestPath = Path.GetFullPath(TestDir).Replace('\\', '/');

        globalEntry.Path.FullPath.Should().Be(NormalizePathCombine(fullTestPath, "VILE^1"));
        globalEntry.Path.Name.Should().Be("VILE^1");

        spriteEntry.Path.FullPath.Should().Be(NormalizePathCombine(fullTestPath, "Sprites/VILE^1"));
        spriteEntry.Path.Name.Should().Be("VILE\\1");

        textureEntry.Path.FullPath.Should().Be(NormalizePathCombine(fullTestPath, "Textures/VILE^1"));
        textureEntry.Path.Name.Should().Be("VILE^1");
    }

    private static string NormalizePathCombine(string x, string y)
    {
        return Path.Combine(x, y).Replace('\\', '/');
    }
}
