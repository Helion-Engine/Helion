using System;
using System.Collections.Generic;
using System.Globalization;
using Helion.Util;

namespace Helion.Maps.Shared;

/// <summary>The mapped value of a UDMF user property, which can unconditionally be accessed as an integer, decimal or text.</summary>
readonly struct MapUserValue {
    public readonly int Integer;
    public readonly double Decimal;
    public readonly string Text;

    public MapUserValue(string val) {
        Text = val;
        if (val.Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            Integer = 1;
            Decimal = 1.0;
            return;
        }
        if (val.Equals("false", StringComparison.OrdinalIgnoreCase))
        {
            Integer = 0;
            Decimal = 0.0;
            return;
        }

        if (!Parsing.TryParseDouble(val, out var doubleValue)) doubleValue = 0.0;
        if (!int.TryParse(val, out var intValue)) intValue = (int)doubleValue;
        Integer = intValue;
        Decimal = doubleValue;
    }
}

/// <summary>Dictionary-like struct for UDMF properties which only accepts keys starting with `user_`, mapping to an integer, decimal and text.</summary>
public struct MapUserProperties {
    private Dictionary<string, MapUserValue>? m_keyValues;
    private Dictionary<string, MapUserValue>.AlternateLookup<ReadOnlySpan<char>>? m_keyValuesLookup;

    const string UserPrefix = "user_";

    void AddInternal(string key, MapUserValue value) {
        if (!key.StartsWith(UserPrefix, StringComparison.OrdinalIgnoreCase)) return;
        var actualKey = key.ToLower(CultureInfo.InvariantCulture);
        if (m_keyValues == null) {
            m_keyValues = new(StringComparer.OrdinalIgnoreCase);
            m_keyValuesLookup = m_keyValues.GetAlternateLookup<ReadOnlySpan<char>>();
        }
        m_keyValues[actualKey] = value;
    }
    public void Add(ReadOnlySpan<char> key, ReadOnlySpan<char> value) => AddInternal(key.ToString(), new MapUserValue(value.ToString()));

    public readonly int? GetInteger(ReadOnlySpan<char> key) {
        if (m_keyValuesLookup == null) return null;
        if (!m_keyValuesLookup.Value.TryGetValue(key, out var value)) return null;
        return value.Integer;
    }
    public readonly double? GetDecimal(ReadOnlySpan<char> key) {
        if (m_keyValuesLookup == null) return null;
        if (!m_keyValuesLookup.Value.TryGetValue(key, out var value)) return null;
        return value.Decimal;
    }
    public readonly string? GetText(ReadOnlySpan<char> key) {
        if (m_keyValuesLookup == null) return null;
        if (!m_keyValuesLookup.Value.TryGetValue(key, out var value)) return null;
        return value.Text;
    }
}
