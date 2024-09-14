namespace Helion.Layer.Options.Sections
{
    using Helion.Audio.Sounds;
    using Helion.Render.Common.Renderers;
    using Helion.Resources.Archives.Collection;
    using Helion.Resources.Definitions;
    using Helion.Resources.Definitions.Compatibility;
    using Helion.Util.Configs;
    using Helion.Util.Configs.Options;

    public class CompatibilitySection : ListedConfigSection
    {
        private readonly ArchiveCollection m_archiveCollection;

        public CompatibilitySection(IConfig config, OptionSectionType optionType, SoundManager soundManager, ArchiveCollection archiveCollection) 
            : base(config, optionType, soundManager)
        {
            m_archiveCollection = archiveCollection;
        }

        public override void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud, int startY, bool didMouseWheelScroll)
        {
            base.Render(ctx, hud, startY, didMouseWheelScroll);
        }
    }
}
