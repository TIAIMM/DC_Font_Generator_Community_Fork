using System;
using System.Collections.Generic;
using System.Drawing;

namespace DC_Font_Generator
{
    internal sealed class GlyphInteractionRequest
    {
        public Bitmap TextImage { get; set; }
        public Size TextImageSize { get; set; }
        public Array2D.List2D<Fnt_char> CharIndex { get; set; }
        public IList<Main> FontSections { get; set; } = Array.Empty<Main>();
        public int SelectedFontIndex { get; set; }
        public GlyphSelectionState Selection { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool ToggleSelection { get; set; }
        public bool Remove { get; set; }
        public bool CreateMask { get; set; } = true;
        public int HitTolerance { get; set; }
        public string ToolTipFormat { get; set; }
    }

    internal sealed class GlyphInteractionResult
    {
        public bool HasGlyph { get; set; }
        public GlyphHitResult Hit { get; set; }
        public string StatusText { get; set; } = "";
        public string ToolTip { get; set; } = "";
        public Bitmap MaskImage { get; set; }
    }

    internal static class GlyphInteractionService
    {
        public static Bitmap CreateMask(Bitmap textImage, Size textImageSize, GlyphSelectionState selection)
        {
            if (textImage == null || selection == null)
            {
                return null;
            }

            return GlyphOverlayRenderer.CreateMask(
                textImage,
                textImageSize,
                selection.Selected,
                selection.RangeSelected,
                selection.Removed);
        }

        public static GlyphInteractionResult Handle(GlyphInteractionRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.TextImage == null || request.Selection == null)
            {
                return new GlyphInteractionResult();
            }

            GlyphHitResult hit = GlyphSelectionService.HitTest(
                request.CharIndex,
                request.X,
                request.Y,
                request.TextImageSize,
                request.FontSections,
                request.SelectedFontIndex,
                request.HitTolerance);

            if (hit.HasGlyph && request.ToggleSelection)
            {
                request.Selection.Toggle(hit.EditableGlyph, request.Remove);
            }

            Bitmap mask = request.CreateMask
                ? CreateMask(request.TextImage, request.TextImageSize, request.Selection)
                : null;

            if (!hit.HasGlyph)
            {
                return new GlyphInteractionResult { Hit = hit, MaskImage = mask };
            }

            if (mask != null)
            {
                GlyphOverlayRenderer.DrawFocus(mask, hit);
            }

            Main selected = request.FontSections[request.SelectedFontIndex];
            return new GlyphInteractionResult
            {
                HasGlyph = true,
                Hit = hit,
                StatusText = string.Format("[{0}] Hex:[{1}]", hit.Character, hit.Hex),
                ToolTip = GlyphOverlayRenderer.FormatTooltip(
                    request.ToolTipFormat,
                    hit,
                    selected.FntFile.Header.LineHeight,
                    selected.FntFile.Header.LineHeightFixed),
                MaskImage = mask
            };
        }
    }
}
