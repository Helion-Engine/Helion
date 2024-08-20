using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Layer.Options.Sections;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Extensions;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Window;
using Helion.Window.Input;
using System;
using System.IO;
using System.Collections.Generic;
namespace Helion.Layer.Options
{
    internal abstract class ListDialog: DialogBase
    {
        private readonly IConfigValue m_configValue;
        private readonly OptionMenuAttribute m_attr;
        private int m_row;

        public IConfigValue ConfigValue => m_configValue;
        private readonly List<string> m_values = new List<string>();
        
        public ListDialog(ConfigHud config, IConfigValue configValue, OptionMenuAttribute attr)
        : base(config, "OK", "Cancel")
        {
            m_configValue = configValue;
            m_attr = attr;
        }

        /// <summary>
        /// Return a list of strings the user can select from the dialog
        /// </summary>        
        protected abstract void PopulateListElements(List<string> valuesList);

        /// <summary>
        /// Render any additional messages or controls needed at the top of the dialog, and add text offsets so we know 
        /// where to render the list.
        /// </summary>
        protected abstract void RenderDialogHeader(IHudRenderContext context);

        /// <summary>
        /// Called when the selected string has changed
        /// </summary>
        protected abstract void SelectedStringChanged(string selectedStringOption);

        protected override void RenderDialogContents(IRenderableSurfaceContext ctx, IHudRenderContext hud)
        {
            RenderDialogText(hud, m_attr.Name, windowAlign: Align.TopMiddle, anchorAlign: Align.TopMiddle);

            RenderDialogHeader(hud);
            PopulateListElements(m_values);     
            
            foreach(string str in m_values)
            {
                RenderDialogText(hud, str);
            }
        }

        public override void HandleInput(IConsumableInput input)
        {
            base.HandleInput(input);

            if (input.ConsumePressOrContinuousHold(Key.Down))
                m_row = Math.Min(m_row + 1, m_values.Count - 1);
            if (input.ConsumePressOrContinuousHold(Key.Up))
                m_row = Math.Max(m_row-1, 0);
        }
    }
}
