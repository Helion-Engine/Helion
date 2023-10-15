namespace Helion.Layer.EndGame
{
    public class CastData
    {
        public CastData(string definitionName, string displayName)
        {
            DefitionName = definitionName;
            DisplayName = displayName;
        }

        public string DefitionName { get; set; }
        public string DisplayName { get; set; }
    }
}
