using FluentAssertions;
using Helion.Util.CommandLine;
using Xunit;

namespace Helion.Tests.Unit.Util.CommandLine
{
    public class CommandLineArgsTest
    {
        [Fact(DisplayName = "Can set many command line arguments")]
        public void CanSetMany()
        {
            CommandLineArgs args = CommandLineArgs.Parse(new[] { "-iwad", "DOOM.WAD", "-file", "yes.pk3", "no.wad", "+map", "E1M3" });

            args.Iwad.Should().Be("DOOM.WAD");
            args.Files.Should().Equal("yes.pk3", "no.wad");
            args.Map.Should().Be("E1M3");
        }
        
        [Fact(DisplayName = "Can set command line iwad arg")]
        public void CanSetIwad()
        {
            CommandLineArgs args = CommandLineArgs.Parse(new[] { "-iwad", "dumb2.wad" });

            args.Iwad.Should().Be("dumb2.wad");
        }
        
        [Fact(DisplayName = "Can set command line files")]
        public void CanAddFiles()
        {
            CommandLineArgs args = CommandLineArgs.Parse(new[] { "-file", "a.wad", "b.pk3" });

            args.Files.Should().Equal("a.wad", "b.pk3");
        }
        
        [Fact(DisplayName = "Can set command line log path")]
        public void CanAddLog()
        {
            CommandLineArgs args = CommandLineArgs.Parse(new[] { "-log", "yes.log" });

            args.LogPath.Should().Be("yes.log");
        }

        [Fact(DisplayName = "Can set command line skill")]
        public void CanAddSkill()
        {
            CommandLineArgs args = CommandLineArgs.Parse(new[] { "-skill", "3" });

            args.Skill.Should().Be(3);
        }
        
        [Fact(DisplayName = "Can set command line warp")]
        public void CanAddWarp()
        {
            CommandLineArgs args = CommandLineArgs.Parse(new[] { "-warp", "4" });

            args.Warp.Should().Be("4");
        }
        
        [Fact(DisplayName = "Can set command line map")]
        public void CanAddMap()
        {
            CommandLineArgs args = CommandLineArgs.Parse(new[] { "+map", "map02" });

            args.Map.Should().Be("map02");
        }
        
        [Fact(DisplayName = "Can set command line no monsters")]
        public void CanAddNoMonsters()
        {
            CommandLineArgs args = CommandLineArgs.Parse(new[] { "-nomonsters" });

            args.NoMonsters.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Can set command line fast monsters")]
        public void CanAddFastMonsters()
        {
            CommandLineArgs args = CommandLineArgs.Parse(new[] { "+sv_fastmonsters", "1" });

            args.SV_FastMonsters.Should().BeTrue();
        }
    }
}
