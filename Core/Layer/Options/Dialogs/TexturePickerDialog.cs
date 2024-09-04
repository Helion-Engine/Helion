namespace Helion.Layer.Options.Dialogs
{
    using Helion.Render.Common.Renderers;
    using Helion.Util.Configs.Components;
    using Helion.Util.Configs.Options;
    using Helion.Util.Configs.Values;
    using Helion.Window;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    internal class TexturePickerDialog : ListDialog
    {
        private const int BlinkIntervalMilliseconds = 500;

        private bool m_isInitialized;
        private string m_typedName;
        private string m_header;
        private string m_headerWithUnderscore;
        public string SelectedTexture { get; private set; }

        public TexturePickerDialog(ConfigWindow config, IConfigValue configValue, OptionMenuAttribute attr)
            : base(config, configValue, attr)
        {
            SelectedTexture = (string)configValue.ObjectValue;
            m_typedName = SelectedTexture;

            m_header = $"Texture: {m_typedName}";
            m_headerWithUnderscore = $"Texture: {m_typedName}_";
        }

        public override void HandleInput(IConsumableInput input)
        {
            bool textChanged = false;

            // Backspace and typed characters will change the current path directly
            if (input.ConsumePressOrContinuousHold(Window.Input.Key.Backspace))
            {
                m_typedName = m_typedName.Length > 0 ? m_typedName.Remove(m_typedName.Length - 1) : m_typedName;
                UpdateHeader();
                textChanged = true;
            }

            ReadOnlySpan<char> typedChars = input.ConsumeTypedCharacters();
            if (typedChars.Length > 0)
            {
                m_typedName = $"{m_typedName}{typedChars}";
                UpdateHeader();
                textChanged = true;

            }

            if (textChanged)
            {
                (_, string selectedTexture) = SelectRowByBeginsWith(m_typedName, StringComparison.OrdinalIgnoreCase, false);
                if (!string.IsNullOrEmpty(selectedTexture))
                {
                    SelectedTexture = selectedTexture;
                    EnsureSelectedVisible = true;
                }
            }

            base.HandleInput(input);
        }

        protected override void ModifyListElements(List<string> valuesList, IHudRenderContext hud, bool sizeChanged, out bool didChange)
        {
            didChange = false;

            if (!m_isInitialized)
            {
                valuesList.Clear();

                // Note:  We could also include the sprites here, but that makes this list even more ridiculously long.
                List<string> names =
                [
                    .. hud.Textures.GetNames(Resources.ResourceNamespace.Textures),
                    .. hud.Textures.GetNames(Resources.ResourceNamespace.Flats),
                    .. hud.Textures.GetNames(Resources.ResourceNamespace.Graphics),
                ];

                valuesList.AddRange(names.Distinct().Order());

                m_isInitialized = true;
                didChange = true;
            }
        }

        protected override void RenderDialogHeader(IHudRenderContext hud)
        {
            if (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / BlinkIntervalMilliseconds % 2 == 0)
            {
                RenderDialogText(hud, m_header, color: Graphics.Color.Yellow);
            }
            else
            {
                RenderDialogText(hud, m_headerWithUnderscore, color: Graphics.Color.Yellow);
            }

            RenderDialogImage(hud, SelectedTexture, (100, 100));
        }

        protected override void SelectedRowChanged(string selectedRowLabel, int selectedRowIndex, bool mouseClick)
        {
            SelectedTexture = selectedRowLabel;
            m_typedName = selectedRowLabel;
            UpdateHeader();
        }

        private void UpdateHeader()
        {
            m_header = $"Texture: {m_typedName}";
            m_headerWithUnderscore = $"Texture: {m_typedName}_";
        }
    }
}
