using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Helion.Maps.Shared;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Values;
using MoreLinq;
using Xunit;

namespace Helion.Tests.Unit.Util.Configs.Impl
{
    public class ConfigTest
    {
        private readonly Config m_config;
        
        public ConfigTest()
        {
            m_config = new Config();
        }
        
        [Fact(DisplayName = "Can iterate over all config elements")]
        public void CanIterateOverAllElements()
        {
            // This is also a safeguard against any accidental config value changes.
            // If this breaks because someone changed something willingly, then this
            // should be updated with the new value.
            Dictionary<string, IConfigValue> expected = new()
            {
                ["audio.device"] = m_config.Audio.Device,
                ["audio.musicvolume"] = m_config.Audio.MusicVolume,
                ["audio.soundvolume"] = m_config.Audio.SoundVolume,
                ["audio.volume"] = m_config.Audio.Volume,
                ["compatibility.preferdehacked"] = m_config.Compatibility.PreferDehacked,
                ["compatibility.vanillasectorphysics"] = m_config.Compatibility.VanillaSectorPhysics,
                ["compatibility.vanillashortesttexture"] = m_config.Compatibility.VanillaShortestTexture,
                ["console.maxmessages"] = m_config.Console.MaxMessages,
                ["developer.render.debug"] = m_config.Developer.Render.Debug,
                ["files.directories"] = m_config.Files.Directories,
                ["game.alwaysrun"] = m_config.Game.AlwaysRun,
                ["game.autoaim"] = m_config.Game.AutoAim,
                ["game.fastmonsters"] = m_config.Game.FastMonsters,
                ["game.levelstat"] = m_config.Game.LevelStat,
                ["game.nomonsters"] = m_config.Game.NoMonsters,
                ["game.skill"] = m_config.Game.Skill,
                ["hud.automap.scale"] = m_config.Hud.AutoMap.Scale,
                ["hud.movebob"] = m_config.Hud.MoveBob,
                ["hud.scale"] = m_config.Hud.Scale,
                ["hud.statusbarsize"] = m_config.Hud.StatusBarSize,
                ["mouse.focus"] = m_config.Mouse.Focus,
                ["mouse.look"] = m_config.Mouse.Look,
                ["mouse.pitch"] = m_config.Mouse.Pitch,
                ["mouse.pixeldivisor"] = m_config.Mouse.PixelDivisor,
                ["mouse.rawinput"] = m_config.Mouse.RawInput,
                ["mouse.sensitivity"] = m_config.Mouse.Sensitivity,
                ["mouse.yaw"] = m_config.Mouse.Yaw,
                ["player.name"] = m_config.Player.Name,
                ["player.gender"] = m_config.Player.Gender,
                ["render.anisotropy"] = m_config.Render.Anisotropy,
                ["render.fakecontrast"] = m_config.Render.FakeContrast,
                ["render.filter.font"] = m_config.Render.Filter.Font,
                ["render.filter.texture"] = m_config.Render.Filter.Texture,
                ["render.lightdropoff"] = m_config.Render.LightDropoff,
                ["render.maxfps"] = m_config.Render.MaxFPS,
                ["render.multisample"] = m_config.Render.Multisample,
                ["render.showfps"] = m_config.Render.ShowFPS,
                ["render.spriteclip"] = m_config.Render.SpriteClip,
                ["render.spriteclipcorpse"] = m_config.Render.SpriteClipCorpse,
                ["render.spriteclipcorpsefactormax"] = m_config.Render.SpriteClipCorpseFactorMax,
                ["render.spriteclipfactormax"] = m_config.Render.SpriteClipFactorMax,
                ["render.spriteclipmin"] = m_config.Render.SpriteClipMin,
                ["render.virtualdimension.enable"] = m_config.Render.VirtualDimension.Enable,
                ["render.virtualdimension.dimension"] = m_config.Render.VirtualDimension.Dimension,
                ["render.vsync"] = m_config.Render.VSync,
                ["window.border"] = m_config.Window.Border,
                ["window.dimension"] = m_config.Window.Dimension,
                ["window.state"] = m_config.Window.State,
            };

            Dictionary<string, ConfigComponent> pathToValue = m_config.ToDictionary();
            expected.Count.Should().Be(pathToValue.Count);
            
            foreach ((string path, ConfigComponent component) in m_config)
            {
                expected.Should().ContainKey(path);
                component.Should().BeEquivalentTo(pathToValue[path]);
            }
        }
        
        [Fact(DisplayName = "Can get existing component")]
        public void CanGetExistingComponent()
        {
            (string path, ConfigComponent expected) = m_config.First();
            m_config.TryGetComponent(path, out ConfigComponent? actual).Should().BeTrue();
            actual.Should().BeEquivalentTo(expected);
        }
        
        [Fact(DisplayName = "Cannot get a missing component")]
        public void CannotGetMissingComponent()
        {
            m_config.TryGetComponent("no such component", out _).Should().BeFalse();
        }
        
        [Fact(DisplayName = "Apply queued changes affects only those with proper bit flags")]
        public void TestAppliedQueuedChanges()
        {
            Config config = new();
            // Sanity check that the flag wasn't removed.
            config.Game.Skill.SetFlags.Should().Be(ConfigSetFlags.OnNewWorld);
            
            SkillLevel original = config.Game.Skill;
            // In case someone messes with the default value, we always want a
            // valid one. This will pick VeryEasy, or Nightmare if it's already
            // set to VeryEasy.
            SkillLevel newSkillLevel = original == SkillLevel.VeryEasy ? SkillLevel.Nightmare : SkillLevel.VeryEasy;
            
            config.Game.Skill.Set(newSkillLevel);
            config.Game.Skill.Value.Should().Be(original);
            
            config.ApplyQueuedChanges(ConfigSetFlags.OnNewWorld);
            config.Game.Skill.Value.Should().Be(newSkillLevel);
        }
    }
}