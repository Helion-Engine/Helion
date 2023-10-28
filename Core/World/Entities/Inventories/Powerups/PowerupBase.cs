using Helion.Graphics;
using Helion.Models;
using Helion.Util;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Properties.Components;
using Helion.World.Entities.Players;
using System;
using System.Globalization;

namespace Helion.World.Entities.Inventories.Powerups;

public class PowerupBase : IPowerup
{
    public EntityDefinition EntityDefinition { get; private set; }
    public PowerupType PowerupType { get; private set; }
    public Color? DrawColor { get; private set; }
    public float DrawAlpha { get; private set; }
    public bool DrawPowerupEffect { get; private set; } = true;
    public bool DrawEffectActive { get; private set; } = true;
    public PowerupEffectType EffectType { get; private set; } = PowerupEffectType.None;
    public int Ticks => m_tics;
    public int EffectTicks => m_effectTics;
    public readonly Color? PowerupColor;
    public readonly float PowerupAlpha;

    private const int DefaultEffectTicks = 60 * (int)Constants.TicksPerSecond;

    private readonly Player m_player;
    private int m_tics;
    private int m_effectTics;
    private float m_subAlpha;

    public PowerupBase(Player player, EntityDefinition definition, PowerupType type)
    {
        m_player = player;
        EntityDefinition = definition;
        PowerupType = type;

        if (EntityDefinition.Properties.Powerup.Color != null)
        {
            PowerupColor = GetColor(EntityDefinition.Properties.Powerup.Color);
            DrawColor = PowerupColor;
            PowerupAlpha = (float)EntityDefinition.Properties.Powerup.Color.Alpha;
            DrawAlpha = PowerupAlpha;
        }

        SetTics();
        InitType();

        if (EntityDefinition.Properties.Powerup.Colormap != null)
            EffectType = PowerupEffectType.ColorMap;
        else if (DrawColor.HasValue)
            EffectType = PowerupEffectType.Color;
    }

    public PowerupBase(Player player, EntityDefinition definition, PowerupModel model)
    {
        m_player = player;
        EntityDefinition = definition;
        PowerupType = (PowerupType)model.PowerupType;
        PowerupColor = ColorModel.ToColor(model.PowerupColor);
        DrawColor = ColorModel.ToColor(model.DrawColor);
        DrawAlpha = model.DrawAlpha;
        DrawPowerupEffect = model.DrawPowerupEffect;
        DrawEffectActive = model.DrawEffectActive;
        EffectType = (PowerupEffectType)model.EffectType;
        m_tics = model.Tics;
        m_effectTics = model.EffectTics;
        m_subAlpha = model.SubAlpha;
        PowerupAlpha = model.PowerupAlpha;
    }

    public PowerupModel ToPowerupModel()
    {
        return new PowerupModel()
        {
            Name = EntityDefinition.Name.ToString(),
            PowerupType = (int)PowerupType,
            PowerupColor = ColorModel.ToColorModel(PowerupColor),
            DrawColor = ColorModel.ToColorModel(DrawColor),
            DrawAlpha = DrawAlpha,
            DrawPowerupEffect = DrawPowerupEffect,
            DrawEffectActive = DrawEffectActive,
            EffectType = (int)EffectType,
            Tics = m_tics,
            EffectTics = m_effectTics,
            SubAlpha = m_subAlpha,
            PowerupAlpha = PowerupAlpha
        };
    }

    private void SetTics()
    {
        if (EntityDefinition.Properties.Powerup.Duration < 0)
            m_tics = -EntityDefinition.Properties.Powerup.Duration * (int)Constants.TicksPerSecond;
        else
            m_tics = EntityDefinition.Properties.Powerup.Duration;

        if (PowerupType == PowerupType.Strength)
        {
            m_effectTics = DefaultEffectTicks;
            m_subAlpha = DrawAlpha / DefaultEffectTicks;
        }
        else
        {
            m_effectTics = m_tics;
        }
    }

    private static Color? GetColor(PowerupColor color)
    {
        if (color.Color.Length < 8)
            return null;

        if (!int.TryParse(color.Color.AsSpan(0, 2), NumberStyles.HexNumber, null, out int r))
            return null;
        if (!int.TryParse(color.Color.AsSpan(3, 2), NumberStyles.HexNumber, null, out int g))
            return null;
        if (!int.TryParse(color.Color.AsSpan(6, 2), NumberStyles.HexNumber, null, out int b))
            return null;

        return Color.FromInts(0, r, g, b);
    }

    public virtual InventoryTickStatus Tick(Player player)
    {
        if (PowerupType == PowerupType.Strength)
            m_tics++;
        else
            m_tics--;

        m_effectTics--;

        if (m_effectTics > 0)
        {
            CheckDrawPowerupEffect();
        }
        else
        {
            DrawEffectActive = false;
            DrawColor = null;
        }

        if (m_tics <= 0)
        {
            HandleDestroy();
            return InventoryTickStatus.Destroy;
        }

        return InventoryTickStatus.Continue;
    }

    private void CheckDrawPowerupEffect()
    {
        if (PowerupType == PowerupType.Strength && PowerupColor.HasValue)
        {
            DrawAlpha -= m_subAlpha;
            return;
        }

        DrawPowerupEffect = m_effectTics > 128 || (m_effectTics & 8) > 0;
    }

    private void InitType()
    {
        if (PowerupType == PowerupType.Invisibility)
        {
            m_player.Flags.Shadow = true;
        }
        else if (PowerupType == PowerupType.Invulnerable)
        {
            EntityDefinition.Properties.Powerup.Color = new PowerupColor("00 00 00");
            EntityDefinition.Properties.Powerup.Colormap = new PowerupColorMap((0, 0, 0, 0));
        }
    }

    private void HandleDestroy()
    {
        if (PowerupType == PowerupType.Invisibility)
            m_player.Flags.Shadow = false;
    }

    public void Reset()
    {
        SetTics();

        DrawEffectActive = true;
        DrawColor = PowerupColor;
        DrawAlpha = PowerupAlpha;
    }
}
