using System.IO;
using System.Linq;
using FluentAssertions;
using Helion.Util.Configs.Impl;
using Helion.Window.Input;
using Xunit;

namespace Helion.Tests.Unit.Util.Configs.Impl;

public class FileConfigTest
{
    private static string GetTempFilePath()
    {
        try
        {
            return Path.GetTempFileName();
        }
        catch
        {
            false.Should().BeTrue("OS failed to give us a temporary file path");
            return "";
        }
    }

    [Fact(DisplayName = "Can read config file with no default values set")]
    public void CanReadWithNoDefaults()
    {
        FileConfig fileConfig = new("this path does not exist", false);

        // Defaults are always set
        fileConfig.Keys.GetKeyMapping().FirstOrDefault(x => x.Key == Key.MouseLeft).Should().NotBeNull();
    }

    [Fact(DisplayName = "Can read config file with default values set")]
    public void CanReadWithDefaults()
    {
        FileConfig fileConfig = new("this path does not exist", true);

        // We assume for now there will always be MouseLeft set (like clicking fire),
        // so it not being set means the defaults were set.
        fileConfig.Keys.GetKeyMapping().FirstOrDefault(x => x.Key == Key.MouseLeft).Should().NotBeNull();
    }

    [Fact(DisplayName = "Can write config file")]
    public void CanWrite()
    {
        string tempPath = GetTempFilePath();

        // This is for sanity checking.
        string contentBefore = File.ReadAllText(tempPath);
        contentBefore.Should().NotContain(FileConfig.EngineSectionName);
        contentBefore.Should().NotContain(FileConfig.KeysSectionName);

        // Note: Force it to write since we're not changing anything.
        FileConfig fileConfig = new(tempPath, false);
        fileConfig.Write(tempPath, true).Should().BeTrue();

        // It should be different, and we want to make sure certain parts
        // elements should be there.
        string contentAfter = File.ReadAllText(tempPath);
        contentAfter.Should().Contain(FileConfig.EngineSectionName);
        contentAfter.Should().Contain(FileConfig.KeysSectionName);
    }

    [Fact(DisplayName = "Writing config file with no changes leads to no writing")]
    public void WritingFailsIfNoChanges()
    {
        string tempPath = GetTempFilePath();
        string contentBefore = File.ReadAllText(tempPath);
        contentBefore.Should().NotContain(FileConfig.EngineSectionName);

        FileConfig fileConfig = new(GetTempFilePath(), false);
        fileConfig.Write().Should().BeTrue();

        string contentAfter = File.ReadAllText(tempPath);
        contentAfter.Should().Be(contentBefore);
    }
}
