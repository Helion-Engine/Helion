namespace Helion.Layer.Options.Sections
{
    using Helion.Audio.Sounds;
    using Helion.Graphics;
    using Helion.Util.Configs;
    using Helion.Util.Configs.Options;

    public class CompatibilitySection : ListedConfigSection
    {
        private const string HeaderText = "Items displayed in orange have been set automatically";

        public CompatibilitySection(IConfig config, OptionSectionType optionType, SoundManager soundManager)
            : base(config, optionType, soundManager)
        {
        }

        protected override string GetExtendedHeaderText(out Color desiredColor)
        {
            desiredColor = Color.Orange;
            return HeaderText;
        }
    }
}
