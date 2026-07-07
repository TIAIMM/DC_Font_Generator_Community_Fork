using System.Drawing;
using WinUIColor = Windows.UI.Color;

namespace DC_Font_Generator.WinUI;

public static class WinUiColorAdapter
{
    public static WinUIColor ToWinUiColor(Color color)
    {
        return WinUIColor.FromArgb(color.A, color.R, color.G, color.B);
    }

    public static Color ToDrawingColor(WinUIColor color)
    {
        return Color.FromArgb(color.A, color.R, color.G, color.B);
    }
}
