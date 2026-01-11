namespace HackHelper.Models
{
    public class Theme
    {
        public string Name { get; set; }
        public string PrimaryColor { get; set; }      // Main accent color
        public string SecondaryColor { get; set; }    // Secondary accent
        public string BackgroundColor { get; set; }   // Main background
        public string SurfaceColor { get; set; }      // Cards/panels
        public string BorderColor { get; set; }       // Borders
        public string TextColor { get; set; }         // Primary text
        public string SubTextColor { get; set; }      // Secondary text

        // Predefined themes
        public static Theme BluePink => new Theme
        {
            Name = "Blue & Pink (Default)",
            PrimaryColor = "#3B82F6",
            SecondaryColor = "#EC4899",
            BackgroundColor = "#18181B",
            SurfaceColor = "#27272A",
            BorderColor = "#3F3F46",
            TextColor = "#FFFFFF",
            SubTextColor = "#A1A1AA"
        };

        public static Theme Purple => new Theme
        {
            Name = "Purple Dream",
            PrimaryColor = "#A855F7",
            SecondaryColor = "#EC4899",
            BackgroundColor = "#18181B",
            SurfaceColor = "#27272A",
            BorderColor = "#3F3F46",
            TextColor = "#FFFFFF",
            SubTextColor = "#A1A1AA"
        };

        public static Theme Green => new Theme
        {
            Name = "Matrix Green",
            PrimaryColor = "#10B981",
            SecondaryColor = "#34D399",
            BackgroundColor = "#18181B",
            SurfaceColor = "#27272A",
            BorderColor = "#3F3F46",
            TextColor = "#FFFFFF",
            SubTextColor = "#A1A1AA"
        };

        public static Theme Red => new Theme
        {
            Name = "Red Alert",
            PrimaryColor = "#FF0000",
            SecondaryColor = "#FF004C",
            BackgroundColor = "#18181B",
            SurfaceColor = "#27272A",
            BorderColor = "#3F3F46",
            TextColor = "#FFFFFF",
            SubTextColor = "#A1A1AA"
        };

        public static Theme Cyan => new Theme
        {
            Name = "Cyan Wave",
            PrimaryColor = "#06B6D4",
            SecondaryColor = "#22D3EE",
            BackgroundColor = "#18181B",
            SurfaceColor = "#27272A",
            BorderColor = "#3F3F46",
            TextColor = "#FFFFFF",
            SubTextColor = "#A1A1AA"
        };

        public static Theme Orange => new Theme
        {
            Name = "Sunset Orange",
            PrimaryColor = "#F97316",
            SecondaryColor = "#FB923C",
            BackgroundColor = "#18181B",
            SurfaceColor = "#27272A",
            BorderColor = "#3F3F46",
            TextColor = "#FFFFFF",
            SubTextColor = "#A1A1AA"
        };
    }
}