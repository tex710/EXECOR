namespace Execor.Models
{
    public class CustomTheme
    {
        public string Id { get; set; }
        public int SlotNumber { get; set; } // 1-5
        public string Name { get; set; }
        public string PrimaryColor { get; set; }
        public string SecondaryColor { get; set; }
        public string BackgroundColor { get; set; }
        public string SurfaceColor { get; set; }
        public string BorderColor { get; set; }
        public string TextColor { get; set; }
        public string SubTextColor { get; set; }
    }
}