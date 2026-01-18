namespace Execor.Models
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
            Name = "Cotton Candy",
            PrimaryColor = "#68C6FD",
            SecondaryColor = "#F497C6",
            BackgroundColor = "#18181B",
            SurfaceColor = "#27272A",
            BorderColor = "#3F3F46",
            TextColor = "#FFFFFF",
            SubTextColor = "#A1A1AA"
        };

        public static Theme Purple => new Theme
        {
            Name = "Sizzurp",
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
            Name = "Alien Za",
            PrimaryColor = "#06FD00",
            SecondaryColor = "#CBFF2F",
            BackgroundColor = "#18181B",
            SurfaceColor = "#27272A",
            BorderColor = "#3F3F46",
            TextColor = "#FFFFFF",
            SubTextColor = "#A1A1AA"
        };

        public static Theme Red => new Theme
        {
            Name = "Red Hot",
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
            Name = "Cyan",
            PrimaryColor = "#1DAECD",
            SecondaryColor = "#00CEE6",
            BackgroundColor = "#18181B",
            SurfaceColor = "#27272A",
            BorderColor = "#3F3F46",
            TextColor = "#FFFFFF",
            SubTextColor = "#A1A1AA"
        };

        public static Theme Orange => new Theme
        {
            Name = "Cheese Pizza",
            PrimaryColor = "#D35805",
            SecondaryColor = "#FF922F",
            BackgroundColor = "#18181B",
            SurfaceColor = "#27272A",
            BorderColor = "#3F3F46",
            TextColor = "#FFFFFF",
            SubTextColor = "#A1A1AA"
        };

        public static Theme Vanilla => new Theme {
            Name = "Vanilla",
            PrimaryColor = "#FFD69D",
            SecondaryColor = "#FFF8DD",
            BackgroundColor = "#18181B",
            SurfaceColor = "#27272A",
            BorderColor = "#3F3F46",
            TextColor = "#FFFFFF",
            SubTextColor = "#A1A1AA"
        };

        public static Theme HKCS => new Theme
        {
            Name = "HKCS",
            PrimaryColor = "#4261DD",
            SecondaryColor = "#FFE041",
            BackgroundColor = "#18181B",
            SurfaceColor = "#27272A",
            BorderColor = "#3F3F46",
            TextColor = "#FFFFFF",
            SubTextColor = "#A1A1AA"
        };

        public static Theme CandyCane => new Theme
        {
            Name = "Candy Cane",
            PrimaryColor = "#FF2B2B",
            SecondaryColor = "#FFFFFF",
            BackgroundColor = "#18181B",
            SurfaceColor = "#27272A",
            BorderColor = "#3F3F46",
            TextColor = "#FFFFFF",
            SubTextColor = "#A1A1AA"
        };
    }
}