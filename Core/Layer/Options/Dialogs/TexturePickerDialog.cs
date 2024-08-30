namespace Helion.Layer.Options.Dialogs
{
    using Helion.Render.Common.Renderers;
    using Helion.Util.Configs.Components;
    using Helion.Util.Configs.Options;
    using Helion.Util.Configs.Values;
    using System;
    using System.Collections.Generic;

    internal class TexturePickerDialog : ListDialog
    {
        public string SelectedTexture { get; private set; }

        public TexturePickerDialog(ConfigHud config, IConfigValue configValue, OptionMenuAttribute attr) : base(config, configValue, attr)
        {
        }

        protected override void ModifyListElements(List<string> valuesList, IHudRenderContext hud, bool sizeChanged)
        {
            throw new NotImplementedException();
        }

        protected override void RenderDialogHeader(IHudRenderContext hud)
        {
            throw new NotImplementedException();
        }

        protected override void SelectedRowChanged(string selectedRowLabel, int selectedRowIndex, bool mouseClick)
        {
            throw new NotImplementedException();
        }
    }
}
