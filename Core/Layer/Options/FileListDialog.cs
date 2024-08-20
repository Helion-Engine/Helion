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
    private string m_file;
    private string m_path;
    private List<object> m_directoryContents = new List<object>();
    private bool m_listsNeedUpdate = true;

    public FileInfo? SelectedFile => new FileInfo(Path.Combine(m_path, m_file));

    public FileListDialog(ConfigHud config, IConfigValue configValue, OptionMenuAttribute attr)
        : base(config, configValue, attr)
    {
        try
        {
            FileInfo file = (FileInfo)ConfigValue.ObjectValue;
            m_path = file != null ? Path.GetDirectoryName(file.ToString()) ?? string.Empty : string.Empty;
            m_file = file != null ? Path.GetFileName(file.ToString()) ?? string.Empty : string.Empty;
        }
        catch
        {
            m_path = string.Empty;
            m_file = string.Empty;
        }
    }

    public override void HandleInput(IConsumableInput input)
    {
        // Pressing enter will go into directory, IFF the currently selected item is a directory.
        if (Directory.Exists(Path.Combine(m_path, m_file)) && input.ConsumeKeyPressed(Window.Input.Key.Enter))
        {
            m_path = Path.Combine(m_path, m_file);

            // Simplify path to prevent accumulation of ".." tokens
            if (Path.IsPathFullyQualified(m_path))
            {
                m_path = new DirectoryInfo(m_path).FullName;
            }
            else
            {
                m_path = Path.GetRelativePath(AppContext.BaseDirectory, m_path);
            }

            m_file = string.Empty;
            m_listsNeedUpdate = true;
        }

        // Backspace and typed characters will change the current path directly
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

        base.HandleInput(input);
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

    protected override void RenderDialogHeader(IHudRenderContext hud)
    {
        RenderDialogText(hud, $"Directory: {m_path}", color: Graphics.Color.Yellow, wrapLines: false);
    }

    protected override void SelectedRowChanged(string selectedRowLabel, int selectedRowId)
    {
        try
        {
            m_file = selectedRowLabel;
        }
        catch
        {
            m_file = string.Empty;
        }
    }
}
