using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Helion.Util.Configs.Impl;
using Helion.Util.Configs.Values;
using MoreLinq;
using Xunit;

namespace Helion.Tests.Unit.Util.Configs.Impl;

public class ConfigAliasMappingTest
{
    private readonly Config m_config;
    private readonly Dictionary<string, IConfigValue> m_expectedMapping;

    public ConfigAliasMappingTest()
    {
        m_config = new Config();

        // This has the added benefit of making sure we didn't accidentally
        // remove something. If we did, then someone's binds might break. It
        // is up to the test breaker to decide whether the line should be
        // removed or not. This is only a safety guard.
        m_expectedMapping = new Dictionary<string, IConfigValue>
        {
            ["m_pitch"] = m_config.Mouse.Pitch,
            ["m_sensitivity"] = m_config.Mouse.Sensitivity,
            ["m_yaw"] = m_config.Mouse.Yaw,
            ["mouse_sensitivity"] = m_config.Mouse.Sensitivity,
            ["sv_nomonsters"] = m_config.Game.NoMonsters
        };
    }

    [Fact(DisplayName = "Can iterate over aliases")]
    public void CanIterateOverMapping()
    {
        ConfigAliasMapping mapping = new(m_config);

        Dictionary<string, IConfigValue> actual = mapping.GetDictionary();
        m_expectedMapping.Count.Should().Be(actual.Count);

        foreach ((string key, IConfigValue value) in m_expectedMapping)
            actual[key].Should().BeEquivalentTo(value);
    }

    [Fact(DisplayName = "Can find existing mappings")]
    public void CanFindExisting()
    {
        ConfigAliasMapping mapping = new(m_config);

        (string randomKey, IConfigValue randomItem) = m_expectedMapping.First();
        mapping.TryGet(randomKey, out IConfigValue? item).Should().BeTrue();
        item.Should().BeEquivalentTo(randomItem);
    }

    [Fact(DisplayName = "Cannot find missing mappings")]
    public void CannotFindMissing()
    {
        ConfigAliasMapping mapping = new(m_config);

        mapping.TryGet("no such mapping", out _).Should().BeFalse();
    }
}
