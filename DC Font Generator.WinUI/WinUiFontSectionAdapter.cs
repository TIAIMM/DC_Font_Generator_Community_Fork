using System;
using System.Collections.Generic;
using DC_Font_Generator;
using WinUIColor = Windows.UI.Color;

namespace DC_Font_Generator.WinUI;

public sealed class WinUiFontSectionViewState
{
    public string FontLabel { get; set; }
    public string FntName { get; set; }
    public string SingleByteFontText { get; set; }
    public string DoubleByteFontText { get; set; }
    public bool FixedFont { get; set; }
    public float FontMaxWidth { get; set; }
    public int Glow { get; set; }
    public int Outline { get; set; }
    public WinUIColor GlowColor { get; set; }
    public WinUIColor OutlineColor { get; set; }
    public WinUIColor FontColor { get; set; }
    public bool UseManualBaseLine { get; set; }
    public float ManualBaseLine { get; set; }
    public bool CanAdd { get; set; }
    public bool CanRemove { get; set; }
    public bool CanMoveUp { get; set; }
    public bool CanMoveDown { get; set; }
}

public static class WinUiFontSectionAdapter
{
    public static WinUiFontSectionViewState CreateViewState(IList<Main> sections, int selectedIndex)
    {
        if (sections == null) throw new ArgumentNullException(nameof(sections));

        FontSectionViewState state = FontSectionStateService.CreateViewState(sections, selectedIndex);
        return new WinUiFontSectionViewState
        {
            FontLabel = state.FontLabel,
            FntName = state.FntName,
            SingleByteFontText = state.SingleByteFontText,
            DoubleByteFontText = state.DoubleByteFontText,
            FixedFont = state.FixedFont,
            FontMaxWidth = state.FontMaxWidth,
            Glow = state.Glow,
            Outline = state.Outline,
            GlowColor = WinUiColorAdapter.ToWinUiColor(state.GlowColor),
            OutlineColor = WinUiColorAdapter.ToWinUiColor(state.OutlineColor),
            FontColor = WinUiColorAdapter.ToWinUiColor(state.FontColor),
            UseManualBaseLine = state.UseManualBaseLine,
            ManualBaseLine = state.ManualBaseLine,
            CanAdd = state.CanAdd,
            CanRemove = state.CanRemove,
            CanMoveUp = state.CanMoveUp,
            CanMoveDown = state.CanMoveDown
        };
    }

    public static void ApplyGlow(IList<Main> sections, int selectedIndex, int glow)
    {
        FontSectionStateService.ApplyNumericChange(sections, selectedIndex, "Glow", glow, true);
    }

    public static void ApplyOutline(IList<Main> sections, int selectedIndex, int outline)
    {
        FontSectionStateService.ApplyNumericChange(sections, selectedIndex, "Outline", outline, true);
    }

    public static void ApplyFixedFont(IList<Main> sections, int selectedIndex, bool enabled, float fixedWidth)
    {
        FontSectionStateService.ApplyFixedFont(sections, selectedIndex, enabled, fixedWidth);
    }

    public static void ApplyFixedFontWidth(IList<Main> sections, int selectedIndex, bool enabled, float fixedWidth)
    {
        FontSectionStateService.ApplyFixedFontWidth(sections, selectedIndex, enabled, fixedWidth);
    }

    public static void ApplyManualBaseLine(IList<Main> sections, int selectedIndex, bool enabled, float value)
    {
        FontSectionStateService.ApplyManualBaseLine(sections, selectedIndex, enabled, value, true);
    }
}
