namespace Helion.Resource.Definitions.Fonts
{
    public class CharDefinition
    {
        public readonly char Character;
        public readonly string ImageName;
        public readonly bool Default;
        public readonly FontAlignment? Alignment;

        public CharDefinition(char character, string imageName, bool isDefault, FontAlignment? alignment)
        {
            Character = character;
            ImageName = imageName;
            Default = isDefault;
            Alignment = alignment;
        }
    }
}