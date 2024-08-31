using FluentAssertions;
using Helion.Maps.Shared;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Values;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

namespace Helion.Tests.Unit.Util.Configs.Impl;

public class ConfigTest
{
    private readonly Config m_config;

    public ConfigTest()
    {
        m_config = new Config();
    }

    /*
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
            ["render.forcepipelineflush"] = m_config.Render.ForcePipelineFlush,
            ["render.filter.font"] = m_config.Render.Filter.Font,
            ["render.filter.texture"] = m_config.Render.Filter.Texture,
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

        Dictionary<string, ConfigComponent> pathToValue = m_config.GetComponents();

        // If we fail this, it means we removed or added a field. If so,
        // then add it to the dictionary above. This makes it so we can
        // test every config field an make sure it works, so if someone
        // adds a new field and screws up, we will know when the test
        // starts trying to do stuff with it.
        //
        // What this line below does however is act as a quick alert to
        // the developer that they forgot to track it. It also is a cheap
        // way of making sure that we didn't accidentally nuke a config
        // field, so it does two things.
        expected.Count.Should().Be(pathToValue.Count);

        foreach ((string path, ConfigComponent component) in m_config)
        {
            expected.Should().ContainKey(path);
            component.Should().BeEquivalentTo(pathToValue[path]);
        }
    }
    */

    [Fact(DisplayName = "Can get existing component")]
    public void CanGetExistingComponent()
    {
        (string path, ConfigComponent expected) = m_config.GetComponents().First();
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

    [Fact(DisplayName = "All config values should be resettable from their own default values")]
    public void TestConfigReset()
    {
        Config config = new();
        List<(IConfigValue, Helion.Util.Configs.Options.OptionMenuAttribute, ConfigInfoAttribute)> allConfigFields = config.GetAllConfigFields();
        foreach ((IConfigValue cfgValue, _, _) in allConfigFields)
        {
            ConfigSetResult result = cfgValue.Set(cfgValue.ObjectDefaultValue);
            result.Should().NotBe(ConfigSetResult.NotSetByBadConversion);
        }
    }

    [Fact(DisplayName = "Creating and destroying a game world cannot leak event handlers attached to the default Config")]
    public void CannotLeakConfigEventHandlers()
    {
        // Gather up all of the (prop, field, event) tuples defined in the Config type, 
        // e.g. Audio Volume OnChanged
        var eventDefinitionsByPropAndField = typeof(Config)
            .GetProperties()
            .SelectMany(prop => prop.PropertyType
                .GetFields()
                .SelectMany(field => field.FieldType
                    .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(evt => evt.FieldType.Name.StartsWith("EventHandler"))
                    .Select(evt => (prop, field, evt))))
            .ToArray();

        // First do some sanity checks to ensure that we have correctly enumerated our event handlers and can actually
        // detect changes.

        // Sanity check #1:  We should have discovered at least _some_ event handlers (230 at the time this test was written)
        eventDefinitionsByPropAndField.Length.Should().BeGreaterThan(0, "Config classes should have events defined upon them");

        // Sanity check #2:  Register a trivial hanlder on Audio device OnChanged, make sure we can _find_ it.
        m_config.Audio.Device.OnChanged += Device_OnChanged;
        var handlerCounts = GetEventHandlerCounts(eventDefinitionsByPropAndField, m_config);
        int audioDeviceChangedHandlerCount = handlerCounts.First(hc => hc.PropertyName == "Audio" && hc.FieldName == "Device" && hc.EventName == "OnChanged").EventCount;
        audioDeviceChangedHandlerCount.Should().BeGreaterThan(0, "Found registered event on Audio Device Changed");

        // Sanity check #3:  Make sure we can detect when we've unregistered this handler
        m_config.Audio.Device.OnChanged -= Device_OnChanged;
        handlerCounts = GetEventHandlerCounts(eventDefinitionsByPropAndField, m_config);
        int audioDeviceChangedHandlerCountAfter = handlerCounts.First(hc => hc.PropertyName == "Audio" && hc.FieldName == "Device" && hc.EventName == "OnChanged").EventCount;
        (audioDeviceChangedHandlerCount - audioDeviceChangedHandlerCountAfter).Should().Be(1);

        // Now for the main event
        // Create a game world and simulate a second of gameplay, then tear down the game world.

        var world = WorldAllocator.LoadMap("Resources/boomactions.zip", "boomactions.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        GameActions.TickWorld(world, 35);
        world.Dispose();

        var handlerCountsAfterWorldTeardown = GetEventHandlerCounts(eventDefinitionsByPropAndField, m_config);
        for (int i = 0; i < handlerCountsAfterWorldTeardown.Length; i++)
        {
            var (propertyName, fieldName, eventName, beforeEventCount) = handlerCounts[i];
            var (_, _, _, afterEventCount) = handlerCountsAfterWorldTeardown[i];

            afterEventCount.Should().Be(beforeEventCount, $"{propertyName}.{fieldName}.{eventName} should have the same number of event handlers");
        }
    }

    private static (string PropertyName, string FieldName, string EventName, int EventCount)[] GetEventHandlerCounts((PropertyInfo prop, FieldInfo field, FieldInfo evt)[] eventDefinitionsByPropAndField, Config config)
    {
        var registeredEventHandlerCounts = eventDefinitionsByPropAndField
            .Select(evtDef => (
                PropertyName: evtDef.prop.Name,
                FieldName: evtDef.field.Name,
                EventName: evtDef.evt.Name,
                EventCount:
                    (evtDef.evt.GetValue(evtDef.field.GetValue(evtDef.prop.GetValue(config))) as MulticastDelegate)?
                        .GetInvocationList().Length ?? 0)
                )
            .OrderBy(evt => evt.PropertyName)
            .ThenBy(evt => evt.FieldName)
            .ThenBy(evt => evt.EventName)
            .ToArray();

        return registeredEventHandlerCounts;
    }

    private static void Device_OnChanged(object? sender, string e)
    {
        throw new NotImplementedException();
    }
}
