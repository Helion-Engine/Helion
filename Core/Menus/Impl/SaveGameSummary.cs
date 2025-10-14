using Helion.Graphics;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Util.Extensions;
using Helion.World.Save;
using System;

namespace Helion.Menus.Impl;

public class SaveGameSummary(SaveGame saveGame)
{
    public const string TEXTURENAME = "SAVEGAMETHUMBNAIL";

    public readonly IRenderableTextureHandle? SaveGameImage;
    public readonly string MapName = saveGame.Model?.MapName ?? string.Empty;
    public readonly string Date = $"{saveGame.Model?.Date}";
    public readonly string[] Stats = saveGame.Model?.SaveGameStats == null
            ? []
            : [
                $"Kills: {saveGame.Model.SaveGameStats.KillCount} / {saveGame.Model.SaveGameStats.TotalMonsters}",
                $"Secrets: {saveGame.Model.SaveGameStats.SecretCount} / {saveGame.Model.SaveGameStats.TotalSecrets}",
                $"Elapsed: {TimeSpan.FromSeconds(saveGame.Model.SaveGameStats.LevelTime / 35)}"
            ];
    private readonly Image? m_saveGameImage = saveGame.GetSaveGameImage();

    public IRenderableTextureHandle? UpdateSaveGameTexture(IHudRenderContext hud)
    {
        if (m_saveGameImage != null)
        {
            return hud.CreateOrReplaceImage(m_saveGameImage, TEXTURENAME, Resources.ResourceNamespace.Textures, false);
        }

        return null;
    }
}
