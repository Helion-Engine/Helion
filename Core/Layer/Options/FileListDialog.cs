using Helion.Render.Common.Renderers;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Window;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Helion.Layer.Options;

internal class FileListDialog : ListDialog
{
    private static readonly HashSet<string> SoundFontFileExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".SF2",
        ".SF3"
    };

    private int m_valueStartX;

    private int m_row;
    private FileInfo? m_file;
    private string m_path = string.Empty;
    private List<object> m_directoryContents = new List<object>();
    private bool m_listsNeedUpdate = true;

    public FileInfo? SelectedFile => m_file;

    public FileListDialog(ConfigHud config, IConfigValue configValue, OptionMenuAttribute attr)
        : base(config, configValue, attr)
    {
        try
        {
            m_file = (FileInfo)ConfigValue.ObjectValue;
            m_path = m_file != null ? Path.GetDirectoryName(m_file.ToString()) ?? string.Empty : string.Empty;
        }
        catch
        {
        }
    }

    public override void HandleInput(IConsumableInput input)
    {
        base.HandleInput(input);

        if (input.ConsumeKeyPressed(Window.Input.Key.Backspace))
        {
            m_path = m_path.Length > 0 ? m_path.Remove(m_path.Length - 1) : m_path;
            m_listsNeedUpdate = true;
        }

        ReadOnlySpan<char> typedChars = input.ConsumeTypedCharacters();
        if (typedChars.Length > 0)
        {
            m_path = $"{m_path}{typedChars}";

            m_listsNeedUpdate = true;
        }
    }

    protected override void PopulateListElements(List<string> valuesList)
    {
        if (m_listsNeedUpdate)
        {
            m_directoryContents.Clear();
            valuesList.Clear();

            DirectoryInfo directory = new DirectoryInfo(m_path.Length > 0 ? m_path : AppContext.BaseDirectory);

            if (directory?.Exists == true)
            {
                DirectoryInfo? parent = directory.Parent;
                DirectoryInfo[] subDirectories = directory.GetDirectories();
                FileInfo[] soundFontFiles = directory.GetFiles()
                    .Where(f => SoundFontFileExtensions.Contains(f.Extension))
                    .ToArray();

                if (parent != null)
                {
                    m_directoryContents.Add(parent);
                    valuesList.Add("..");
                }

                m_directoryContents.AddRange(subDirectories);
                m_directoryContents.AddRange(soundFontFiles);

                valuesList.AddRange(subDirectories.Select(sd => sd.Name));
                valuesList.AddRange(soundFontFiles.Select(f => f.Name));
            }

            m_listsNeedUpdate = false;
        }
    }

    protected override void RenderDialogHeader(IHudRenderContext renderContext)
    {
        renderContext.AddOffset((m_dialogOffset.X + m_padding, 0));
        RenderDialogText(renderContext, $"Directory: {m_path}", color: Graphics.Color.Yellow);
    }

    protected override void SelectedStringChanged(string selectedStringOption)
    {
        try
        {
            m_file = new FileInfo(Path.Combine(m_path, selectedStringOption));
        }
        catch
        {
            m_file = null;
        }
    }
}
