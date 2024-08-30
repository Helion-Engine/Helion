using Helion.Render.Common.Renderers;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Window;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Helion.Layer.Options.Dialogs;

internal class FileListDialog : ListDialog
{
    private const int BlinkIntervalMilliseconds = 500;
    private readonly HashSet<string> m_supportedFileExtensions;

    private string m_file;
    private string m_path;
    private string? m_header;
    private string? m_headerWithUnderscore;
    private List<string> m_valuesListRaw = new List<string>();
    private bool m_listsNeedUpdate = true;

    public FileInfo? SelectedFile => new FileInfo(Path.Combine(m_path, m_file));

    public FileListDialog(ConfigHud config, IConfigValue configValue, OptionMenuAttribute attr, string supportedFileExtensions)
        : base(config, configValue, attr)
    {
        m_supportedFileExtensions = new HashSet<string>(
            supportedFileExtensions.Split(",", StringSplitOptions.RemoveEmptyEntries),
            StringComparer.OrdinalIgnoreCase);

        try
        {
            string path = ConfigValue.ObjectValue.ToString() ?? string.Empty;
            m_path = Path.GetDirectoryName(path) ?? string.Empty;
            m_file = Path.GetFileName(path) ?? string.Empty;
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
        // Else be careful to fall through because OK is also Enter.
        if (Directory.Exists(Path.Combine(m_path, m_file)) && input.ConsumeKeyPressed(Window.Input.Key.Enter))
        {
            ChangeDirectory();
        }

        // Backspace and typed characters will change the current path directly
        if (input.ConsumePressOrContinuousHold(Window.Input.Key.Backspace))
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

    private void ChangeDirectory()
    {
        m_path = Path.GetRelativePath(AppContext.BaseDirectory, Path.Combine(m_path, m_file));
        m_file = string.Empty;
        m_listsNeedUpdate = true;
    }

    protected override void ModifyListElements(List<string> valuesList, IHudRenderContext hud, bool sizeChanged, out bool didChange)
    {
        didChange = false;

        if (m_listsNeedUpdate || sizeChanged)
        {
            m_header = TruncateTextToDialogWidth($"Directory: {m_path}", hud);
            m_headerWithUnderscore = TruncateTextToDialogWidth($"Directory: {m_path}_", hud);

            m_valuesListRaw.Clear();
            valuesList.Clear();
            didChange = true;

            DirectoryInfo directory = new DirectoryInfo(m_path.Length > 0 ? m_path : AppContext.BaseDirectory);

            if (directory?.Exists == true)
            {
                DirectoryInfo? parent = directory.Parent;
                if (parent != null)
                {
                    m_valuesListRaw.Add("..");
                    valuesList.Add("[..]");
                }

                try
                {
                    DirectoryInfo[] subDirectories = directory.GetDirectories();
                    FileInfo[] filteredFiles = directory.GetFiles()
                        .Where(f => m_supportedFileExtensions.Contains(f.Extension))
                        .ToArray();

                    m_valuesListRaw.AddRange(subDirectories.Select(sd => sd.Name));
                    m_valuesListRaw.AddRange(filteredFiles.Select(f => f.Name));

                    valuesList.AddRange(subDirectories.Select(sd => TruncateTextToDialogWidth($"[{sd.Name}]", hud)));
                    valuesList.AddRange(filteredFiles.Select(f => TruncateTextToDialogWidth(f.Name, hud)));
                }
                catch
                {
                    // IO exceptions, permissions, etc.
                }
            }

            m_listsNeedUpdate = false;
        }
    }

    protected override void RenderDialogHeader(IHudRenderContext hud)
    {
        if (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / BlinkIntervalMilliseconds % 2 == 0)
        {
            RenderDialogText(hud, m_header ?? string.Empty, color: Graphics.Color.Yellow);
        }
        else
        {
            RenderDialogText(hud, m_headerWithUnderscore ?? string.Empty, color: Graphics.Color.Yellow);
        }
    }

    protected override void SelectedRowChanged(string selectedRowLabel, int selectedRowId, bool mouseClick)
    {
        try
        {
            m_file = m_valuesListRaw[selectedRowId];
            if (mouseClick && Directory.Exists(Path.Combine(m_path, m_file)))
            {
                // If the user clicked on a directory name using the mouse, assume that they want to open that directory.
                ChangeDirectory();
            }
        }
        catch
        {
            m_file = string.Empty;
        }
    }
}
