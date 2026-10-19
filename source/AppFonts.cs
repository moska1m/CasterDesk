namespace CasterDesk.Core;

internal static class AppFonts
{
    internal static Font Body(float size, FontStyle style = FontStyle.Regular) =>
        new("Segoe UI", size, style);
    internal static Font Heading(float size, FontStyle style = FontStyle.Bold) =>
        new("Bahnschrift SemiCondensed", size, style);
}
