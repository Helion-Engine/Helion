using System.Drawing;
using Helion.Input;
using Helion.Menus.Impl;
using Helion.Render.Commands;
using Helion.Resources.Archives.Collection;

namespace Helion.Layer
{
    public class TitlepicLayer : GameLayer
    {
        private readonly ArchiveCollection m_archiveCollection;
        
        protected override double Priority => 0.1;

        public TitlepicLayer(GameLayer parent, ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;
            Parent = parent;
        }
        
        public override void HandleInput(InputEvent input)
        {
            base.HandleInput(input);

            if (input.HasAnyKeyPressed() && Parent?.Count == 1)
            {
                MainMenu mainMenu = new();
                MenuLayer menuLayer = new(this, mainMenu, m_archiveCollection);
                Parent.Add(menuLayer);
            }
        }

        public override void Render(RenderCommands commands)
        {
            var (width, height) = commands.ResolutionInfo.VirtualDimensions;
            commands.DrawImage("TITLEPIC", 0, 0, width, height, Color.White);

            if (ShouldDarken())
                commands.FillRect(new(0, 0, width, height), Color.Black, 0.5f);
            
            base.Render(commands);
        }

        private bool ShouldDarken() => Parent != null && Parent.Contains<MenuLayer>();
    }
}
