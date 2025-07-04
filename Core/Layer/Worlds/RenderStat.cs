using Helion.Render.OpenGL.Texture.Fonts;
using Helion.Strings;
using Helion.World;

namespace Helion.Layer.Worlds;

public enum StatType
{
    Kills,
    Items,
    Secrets,
    Deaths
}

public class RenderStat(StatType type, RenderableString renderLabel, RenderableString renderValue)
{
    public StatType StatType = type;
    public string Label = $"{type}: ";
    public SpanString String = new();
    public RenderableString RenderLabel = renderLabel;
    public RenderableString RenderValue = renderValue;

    public (int,int) GetValues(IWorld world)
    {
        return StatType switch
        {
            StatType.Kills => (world.LevelStats.KillCount, world.LevelStats.TotalMonsters),
            StatType.Items => (world.LevelStats.ItemCount, world.LevelStats.TotalItems),
            StatType.Secrets => (world.LevelStats.SecretCount, world.LevelStats.TotalSecrets),
            StatType.Deaths => (world.Player.PlayerStats.DeathCount, int.MinValue),
            _ => (0, 0),
        };
    }

    public bool ShouldRender(IWorld world)
    {
        if (world.WorldType == WorldType.SinglePlayer && StatType == StatType.Deaths)
            return false;

        return true;
    }
}
 