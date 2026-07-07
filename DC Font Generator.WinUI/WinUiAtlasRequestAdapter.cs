using System;
using System.Collections.Generic;
using DC_Font_Generator;

namespace DC_Font_Generator.WinUI;

public sealed class WinUiAtlasRequestOptions
{
    public IList<int> CandidateWidths { get; set; } = Array.Empty<int>();
    public IList<int> CandidateHeights { get; set; } = Array.Empty<int>();
    public int CurrentWidthIndex { get; set; }
    public int CurrentHeightIndex { get; set; }
    public int Gap { get; set; }
    public FontArrangeMode ArrangeMode { get; set; } = FontArrangeMode.Width;
    public Windows.UI.Color BackgroundColor { get; set; } = Windows.UI.Color.FromArgb(0, 0, 0, 0);
}

public static class WinUiAtlasRequestAdapter
{
    public static FontAtlasRequest Create(
        IList<Main> fontSections,
        FontEncoding encoding,
        WinUiAtlasRequestOptions options)
    {
        if (fontSections == null) throw new ArgumentNullException(nameof(fontSections));
        if (encoding == null) throw new ArgumentNullException(nameof(encoding));
        if (options == null) throw new ArgumentNullException(nameof(options));

        return new FontAtlasRequest
        {
            FontSections = fontSections,
            Encoding = encoding,
            CandidateWidths = options.CandidateWidths,
            CandidateHeights = options.CandidateHeights,
            CurrentWidthIndex = options.CurrentWidthIndex,
            CurrentHeightIndex = options.CurrentHeightIndex,
            Gap = options.Gap,
            ArrangeMode = options.ArrangeMode,
            BackgroundColor = WinUiColorAdapter.ToDrawingColor(options.BackgroundColor)
        };
    }
}
