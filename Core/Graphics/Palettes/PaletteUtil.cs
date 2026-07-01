using Helion.Resources.Archives.Collection;
using Helion.Util.Configs;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using System;

namespace Helion.Graphics.Palettes;

public static class PaletteUtil
{
    // Loads the correct palette for the player and returns the black color from that palette.
    public static Color GetBlackColor(ArchiveCollection archiveCollection, IConfig config, Player player)
    {
        var paletteIndex = (int)GetPalette(config, player);
        var colormap = archiveCollection.Definitions.Colormaps[0].IndexLayer(0);

        if (!archiveCollection.Data.Palette.TryGetLayer(paletteIndex, out var palette) || colormap.Length == 0)
            return Color.Black;

        int index = colormap[0];
        if (index >= palette.Length)
            return Color.Black;

        return palette[index];
    }

    public static PaletteIndex GetPalette(IConfig config, Player player)
    {
        var palette = PaletteIndex.Normal;
        var powerup = player.Inventory.PowerupEffectColor;
        var damageCount = player.DamageCount;
        var damageIntensity = (float)config.Game.PainIntensity.Value;

        if (powerup != null && powerup.PowerupType == PowerupType.Strength)
        {
            var berserkAmount = 12 - (powerup.Ticks >> 6);
            var berserkIntensity = (float)config.Game.BerserkIntensity.Value;

            if (berserkAmount * berserkIntensity > damageCount * damageIntensity)
            {
                damageCount = berserkAmount;
                damageIntensity = berserkIntensity;
            }
        }

        if (damageCount > 0)
        {
            palette = GetDamagePalette(damageCount, damageIntensity);
        }
        else if (player.BonusCount > 0)
        {
            palette = GetBonusPalette(player.BonusCount);
        }

        if (palette == PaletteIndex.Normal && powerup != null &&
            powerup.PowerupType == PowerupType.IronFeet && powerup.DrawPowerupEffect)
        {
            palette = PaletteIndex.Green;
        }

        return palette;
    }

    private static PaletteIndex GetBonusPalette(int bonusCount)
    {
        const int BonusPals = 4;
        const int StartBonusPals = 9;
        int palette = (bonusCount + 7) >> 3;
        if (palette >= BonusPals)
            palette = BonusPals - 1;
        palette += StartBonusPals;
        return (PaletteIndex)palette;
    }

    private static PaletteIndex GetDamagePalette(int damageCount, float damageIntensity)
    {
        if (damageIntensity <= 0)
            return PaletteIndex.Normal;

        const int RedPals = 8;
        const int StartRedPals = 1;
        int palette = (damageCount + 7) >> 3;
        palette = (int)(palette * damageIntensity);
        if (palette >= RedPals)
            palette = RedPals - 1;
        palette += StartRedPals;
        return (PaletteIndex)palette;
    }
}
