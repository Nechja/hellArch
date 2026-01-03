using MudBlazor;

namespace Admin.Theme;

public static class HellArchTheme
{
    public static MudTheme Create() => new()
    {
        PaletteDark = CreateDarkPalette(),
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "2px"
        }
    };

    private static PaletteDark CreateDarkPalette() => new()
    {
        // Backgrounds
        Black = "#0a0a0a",
        Background = "#0a0a0a",
        BackgroundGray = "#111111",
        Surface = "#161616",
        DrawerBackground = "#111111",
        AppbarBackground = "#111111",

        // Text
        TextPrimary = "#ffffff",
        TextSecondary = "#9a9a9a",
        TextDisabled = "#737373",

        // Primary - Neon Pink
        Primary = "#D162A4",
        PrimaryContrastText = "#0a0a0a",
        PrimaryDarken = "#A30262",
        PrimaryLighten = "#B55690",

        // Secondary - Dusty Pink
        Secondary = "#B55690",
        SecondaryContrastText = "#ffffff",

        // Tertiary - Peach
        Tertiary = "#FF9A56",
        TertiaryContrastText = "#0a0a0a",

        // Feedback states - Neon colors
        Success = "#39ff14",
        SuccessContrastText = "#0a0a0a",
        Warning = "#ffff00",
        WarningContrastText = "#0a0a0a",
        Error = "#ff0033",
        ErrorContrastText = "#ffffff",
        Info = "#D162A4",
        InfoContrastText = "#0a0a0a",

        // Interactive
        ActionDefault = "#9a9a9a",
        ActionDisabled = "#737373",
        ActionDisabledBackground = "#2a2a2a",

        // Borders & Dividers
        Divider = "#2a2a2a",
        DividerLight = "#3a3a3a",
        LinesDefault = "#2a2a2a",
        LinesInputs = "#3a3a3a",
        TableLines = "#2a2a2a",
        TableStriped = "#111111",
        TableHover = "#1c1c1c",

        // Overlays
        OverlayDark = "rgba(0, 0, 0, 0.8)",
        OverlayLight = "rgba(255, 255, 255, 0.1)"
    };
}
