namespace Helion.Util.Configs.Options
{
    public enum DialogType
    {
        /// <summary>
        /// Use the default dialog for this data type (usually none, but colors automatically get a color picker)
        /// </summary>
        Default,

        /// <summary>
        /// Use the SoundFont file picker
        /// </summary>
        SoundFontPicker,

        /// <summary>
        /// Use the texture lump picker
        /// </summary>
        TexturePicker,
    }
}
