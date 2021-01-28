namespace Helion.Resources.Definitions.MapInfo
{
    public class EndGameDef
    {
        public string Pic { get; set; } = string.Empty;
        public string Music { get; set; } = string.Empty;
        public HorizontalScroll HorizontalScroll { get; set; } = HorizontalScroll.None;
        public VerticalScroll VerticalScroll { get; set; } = VerticalScroll.None;
        public bool Cast { get; set; }
    }
}
