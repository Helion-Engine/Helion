namespace Helion.Tests.Unit.Util.Configs.Impl
{
    using FluentAssertions;
    using Helion.Resources.IWad;
    using Helion.Tests.Unit.GameAction;
    using Helion.Util.Configs.Impl;
    using System;
    using System.Linq;
    using System.Reflection;
    using Xunit;

    [Collection("Config events")]
    public class ConfigEventLeakageTest
    {
        [Fact(DisplayName = "Creating and destroying a game world cannot leak event handlers attached to the default Config")]
        public void CannotLeakConfigEventHandlers()
        {
            Config config = new Config();

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
            config.Audio.Device.OnChanged += Device_OnChanged;
            var handlerCounts = GetEventHandlerCounts(eventDefinitionsByPropAndField, config);
            int audioDeviceChangedHandlerCount = handlerCounts.First(hc => hc.PropertyName == "Audio" && hc.FieldName == "Device" && hc.EventName == "OnChanged").EventCount;
            audioDeviceChangedHandlerCount.Should().BeGreaterThan(0, "Found registered event on Audio Device Changed");

            // Sanity check #3:  Make sure we can detect when we've unregistered this handler.
            config.Audio.Device.OnChanged -= Device_OnChanged;
            handlerCounts = GetEventHandlerCounts(eventDefinitionsByPropAndField, config);
            int audioDeviceChangedHandlerCountAfter = handlerCounts.First(hc => hc.PropertyName == "Audio" && hc.FieldName == "Device" && hc.EventName == "OnChanged").EventCount;
            (audioDeviceChangedHandlerCount - audioDeviceChangedHandlerCountAfter).Should().Be(1);

            // Now for the main event.
            // Create a game world and simulate a second of gameplay, then tear down the game world.
            var world = WorldAllocator.LoadMap("Resources/boomactions.zip", "boomactions.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, config: config);
            GameActions.TickWorld(world, 35);
            world.Dispose();

            // Check whether the game world added any event handlers then forgot to remove them.
            var handlerCountsAfterWorldTeardown = GetEventHandlerCounts(eventDefinitionsByPropAndField, config);
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
                            .GetInvocationList().Length ?? 0))
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
}
