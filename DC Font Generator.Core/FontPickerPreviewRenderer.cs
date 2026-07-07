using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using SkiaSharp;

namespace DC_Font_Generator
{
    internal sealed class FontPickerPreviewRequest
    {
        public FontDescriptor PreviewFont { get; set; }
        public FontStyleDescriptor PreviewFontStyleDescriptor { get; set; }
        public FontDescriptor SingleByteFont { get; set; }
        public FontDescriptor DoubleByteFont { get; set; }
        public bool EditingDoubleByteFont { get; set; }
        public bool AsciiOnly { get; set; }
        public int EncodingCodePage { get; set; }
        public int Glow { get; set; }
        public Color GlowColor { get; set; }
        public int Outline { get; set; }
        public Color OutlineColor { get; set; }
        public Color FontColor { get; set; }
        public Color BackColor { get; set; }
    }

    internal static class FontPickerPreviewRenderer
    {
        public static Bitmap Render(Size size, FontPickerPreviewRequest request)
        {
            Bitmap bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            SKImageInfo imageInfo = new SKImageInfo(size.Width, size.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (SKSurface surface = SKSurface.Create(imageInfo))
            {
                SKCanvas canvas = surface.Canvas;
                canvas.Clear(SkiaBitmapInterop.ToSKColor(request.BackColor));

                if (request.PreviewFont != null)
                {
                    DrawPreview(canvas, request);
                }

                canvas.Flush();
                SkiaBitmapInterop.CopySurfaceToBitmap(surface, bitmap);
            }

            return bitmap;
        }

        private static void DrawPreview(SKCanvas canvas, FontPickerPreviewRequest request)
        {
            int previewCodePage = NormalizePreviewCodePage(request.EncodingCodePage);
            PreviewText previewText = PreviewText.ForEncoding(previewCodePage, false);
            FontDescriptor singleFont = request.EditingDoubleByteFont ? request.SingleByteFont : request.PreviewFont;
            FontDescriptor doubleFont = request.EditingDoubleByteFont ? request.PreviewFont : request.DoubleByteFont;
            singleFont ??= request.PreviewFont;
            doubleFont ??= request.PreviewFont;

            float y = 10f;
            float lineHeight = previewText.HasDoubleByteText
                ? Math.Max(GetLineHeight(singleFont), GetLineHeight(doubleFont))
                : GetLineHeight(singleFont);

            DrawPreviewLine(
                canvas,
                request,
                y,
                new PreviewRun(previewText.SingleByteText, singleFont),
                previewText.HasDoubleByteText ? new PreviewRun(previewText.DoubleByteText, doubleFont) : null);

            y += lineHeight;

            if (previewText.HasDoubleByteText)
            {
                DrawPreviewLine(canvas, request, y, new PreviewRun("SBCS: " + previewText.SingleByteOnlyText, singleFont));
                y += lineHeight;
                DrawPreviewLine(canvas, request, y, new PreviewRun("DBCS: " + previewText.DoubleByteOnlyText, doubleFont));
            }
            else
            {
                DrawPreviewLine(canvas, request, y, new PreviewRun(previewText.SingleByteOnlyText, singleFont));
            }
        }

        private static void DrawPreviewLine(
            SKCanvas canvas,
            FontPickerPreviewRequest request,
            float y,
            params PreviewRun[] runs)
        {
            float x = 10f;
            for (int i = 0; i < runs.Length; i++)
            {
                PreviewRun run = runs[i];
                if (run == null || string.IsNullOrEmpty(run.Text) || run.Font == null)
                {
                    continue;
                }

                DrawTextRun(canvas, request, run.Font, run.Text, x, y);
                FontStyleDescriptor descriptor = ReferenceEquals(run.Font, request.PreviewFont)
                    ? request.PreviewFontStyleDescriptor
                    : null;
                x += MeasureTextWidth(run.Font, descriptor, run.Text) + 8f;
            }
        }

        private static void DrawTextRun(SKCanvas canvas, FontPickerPreviewRequest request, FontDescriptor font, string text, float x, float y)
        {
            int effectShift = request.Glow + request.Outline;
            float currentX = x + effectShift + 0.5f;
            float baseline = y + effectShift + GetAscent(font) + 0.5f;

            foreach (char c in text)
            {
                FontStyleDescriptor fontDescriptor = ReferenceEquals(font, request.PreviewFont)
                    ? request.PreviewFontStyleDescriptor : null;
                SKTypeface typeface = ResolveTypefaceForCharacter(font, fontDescriptor, c, out bool ownsTypeface);
                try
                {
                    float advance = TryDrawChar(canvas, request, font, c, currentX, baseline, typeface);
                    if (advance <= 0 && ownsTypeface && typeface != null)
                    {
                        // The user's font couldn't render this character (e.g. TTC false positive).
                        // Retry with a system-wide search for a font that actually contains the glyph.
                        typeface.Dispose();
                        ownsTypeface = false;
                        SKTypeface fallback = SKFontManager.Default.MatchCharacter(c);
                        if (fallback != null)
                        {
                            advance = TryDrawChar(canvas, request, font, c, currentX, baseline, fallback);
                            fallback.Dispose();
                        }
                    }

                    currentX += advance;
                }
                finally
                {
                    if (ownsTypeface && typeface != null)
                    {
                        typeface.Dispose();
                    }
                }
            }
        }

        private static float TryDrawChar(SKCanvas canvas, FontPickerPreviewRequest request, FontDescriptor font, char c, float currentX, float baseline, SKTypeface typeface)
        {
            using (SKFont skFont = new SKFont(typeface, font.SizePixels))
            using (SKPath path = GetTextPath(skFont, c.ToString(), currentX, baseline))
            {
                bool canRender = path != null && path.Bounds.Width > 0 && path.Bounds.Height > 0;
                if (canRender)
                {
                    DrawGlow(canvas, request, path);
                    DrawOutline(canvas, request, path);
                    using (SKPaint fill = CreateFillPaint(request.FontColor))
                    {
                        canvas.DrawPath(path, fill);
                    }
                }

                float advance = skFont.MeasureText(c.ToString());
                if (advance <= 0)
                {
                    advance = path?.Bounds.Width ?? 0f;
                }

                return canRender ? Math.Max(advance, 1f) : 0f;
            }
        }

        private static float MeasureTextWidth(FontDescriptor font, FontStyleDescriptor descriptor, string text)
        {
            float width = 0f;
            foreach (char c in text)
            {
                SKTypeface typeface = ResolveTypefaceForCharacter(font, descriptor, c, out bool ownsTypeface);
                try
                {
                    // Use the same path check as TryDrawChar to decide whether to retry,
                    // so that measurement and rendering stay consistent.
                    if (!CanRenderChar(typeface, font.SizePixels, c)
                        && ownsTypeface && typeface != null)
                    {
                        typeface.Dispose();
                        ownsTypeface = false;
                        SKTypeface fallback = SKFontManager.Default.MatchCharacter(c);
                        if (fallback != null)
                        {
                            width += MeasureCharAdvance(fallback, font.SizePixels, c);
                            fallback.Dispose();
                            continue;
                        }
                    }

                    width += MeasureCharAdvance(typeface, font.SizePixels, c);
                }
                finally
                {
                    if (ownsTypeface && typeface != null)
                    {
                        typeface.Dispose();
                    }
                }
            }

            return width;
        }

        private static bool CanRenderChar(SKTypeface typeface, float sizePixels, char c)
        {
            using (SKFont skFont = new SKFont(typeface, sizePixels))
            {
                byte[] textBytes = Encoding.Unicode.GetBytes(c.ToString());
                using (SKPath path = skFont.GetTextPath(textBytes, SKTextEncoding.Utf16, new SKPoint(0, 0)))
                {
                    return path != null && path.Bounds.Width > 0 && path.Bounds.Height > 0;
                }
            }
        }

        private static float MeasureCharAdvance(SKTypeface typeface, float sizePixels, char c)
        {
            using (SKFont skFont = new SKFont(typeface, sizePixels))
            {
                return skFont.MeasureText(c.ToString());
            }
        }

        private static void DrawGlow(SKCanvas canvas, FontPickerPreviewRequest request, SKPath path)
        {
            if (request.Glow <= 0)
            {
                return;
            }

            int size = request.Outline + request.Glow;
            int glowStep = 0x80 / (request.Glow + 1);
            int alpha = glowStep;
            for (int i = 0; i < request.Glow; i++)
            {
                using (SKPaint paint = CreateStrokePaint(
                    Color.FromArgb(alpha, request.GlowColor.R, request.GlowColor.G, request.GlowColor.B),
                    Math.Max(1, size - i)))
                {
                    canvas.DrawPath(path, paint);
                }

                if (i >= request.Outline)
                {
                    alpha += glowStep;
                    if (alpha > 0x80)
                    {
                        alpha = 0x80;
                    }
                }
            }
        }

        private static void DrawOutline(SKCanvas canvas, FontPickerPreviewRequest request, SKPath path)
        {
            if (request.Outline <= 0)
            {
                return;
            }

            using (SKPaint paint = CreateStrokePaint(request.OutlineColor, request.Outline))
            {
                canvas.DrawPath(path, paint);
            }
        }

        private static SKPaint CreateFillPaint(Color color)
        {
            return new SKPaint
            {
                IsAntialias = true,
                Color = SkiaBitmapInterop.ToSKColor(color),
                Style = SKPaintStyle.Fill
            };
        }

        private static SKPaint CreateStrokePaint(Color color, float width)
        {
            return new SKPaint
            {
                IsAntialias = true,
                Color = SkiaBitmapInterop.ToSKColor(color),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = width,
                StrokeJoin = SKStrokeJoin.Round
            };
        }

        private static SKPath GetTextPath(SKFont font, string text, float x, float y)
        {
            byte[] textBytes = Encoding.Unicode.GetBytes(text);
            return font.GetTextPath(textBytes, SKTextEncoding.Utf16, new SKPoint(x, y));
        }

        private static SKTypeface ResolveTypefaceForCharacter(FontDescriptor font, FontStyleDescriptor descriptor, char c, out bool ownsTypeface)
        {
            ownsTypeface = false;
            int weight, width;
            SKFontStyleSlant slant;
            GetStyleValues(font, descriptor, out weight, out width, out slant);

            // Try the user's font first — skip ContainsGlyph which can give false results for TTC fonts.
            // If the font can't actually render the character, the caller will retry with MatchCharacter.
            SKTypeface typeface = SkiaTypefaceService.CreateTypeface(font.FamilyName, weight, width, slant);
            if (typeface != null)
            {
                ownsTypeface = true;
                return typeface;
            }

            return SKTypeface.Default;
        }

        private static void GetStyleValues(FontDescriptor font, FontStyleDescriptor descriptor, out int weight, out int width, out SKFontStyleSlant slant)
        {
            if (descriptor != null)
            {
                weight = descriptor.Weight;
                width = descriptor.Width;
                slant = descriptor.Slant;
            }
            else
            {
                weight = font != null ? font.Weight : 400;
                width = font != null ? font.Width : (int)SKFontStyleWidth.Normal;
                slant = font != null ? font.Slant : SKFontStyleSlant.Upright;
            }
        }

        private static int NormalizePreviewCodePage(int codePage)
        {
            if (IsDoubleByteCodePage(codePage))
            {
                return codePage;
            }

            return 936;
        }

        private static bool IsDoubleByteCodePage(int codePage)
        {
            return codePage == 932
                || codePage == 936
                || codePage == 949
                || codePage == 950;
        }

        private static float GetLineHeight(FontDescriptor font)
        {
            if (font == null)
            {
                return 0f;
            }

            return font.GetLineSpacing();
        }

        private static float GetAscent(FontDescriptor font)
        {
            if (font == null)
            {
                return 0f;
            }

            return font.GetAscent();
        }

        private sealed class PreviewRun
        {
            public PreviewRun(string text, FontDescriptor font)
            {
                Text = text;
                Font = font;
            }

            public string Text { get; }
            public FontDescriptor Font { get; }
        }

        private sealed class PreviewText
        {
            private PreviewText(string singleByteText, string singleByteOnlyText, string doubleByteText, string doubleByteOnlyText)
            {
                SingleByteText = singleByteText;
                SingleByteOnlyText = singleByteOnlyText;
                DoubleByteText = doubleByteText;
                DoubleByteOnlyText = doubleByteOnlyText;
            }

            public string SingleByteText { get; }
            public string SingleByteOnlyText { get; }
            public string DoubleByteText { get; }
            public string DoubleByteOnlyText { get; }
            public bool HasDoubleByteText => !string.IsNullOrEmpty(DoubleByteText);

            public static PreviewText ForEncoding(int codePage, bool asciiOnly)
            {
                if (asciiOnly)
                {
                    return new PreviewText("Here is example ! HHHHHH", "0123456789 ABC xyz", "", "");
                }

                switch (codePage)
                {
                    case 932:
                        return new PreviewText("Here is example ! ", "ABC 123 HHHHHH", "\u65E5\u672C\u8A9E\u30AB\u30CA", "\u30C6\u30B9\u30C8\u65E5\u672C\u8A9E");
                    case 949:
                        return new PreviewText("Here is example ! ", "ABC 123 HHHHHH", "\uD55C\uAE00\uD14C\uC2A4\uD2B8", "\uAC00\uB098\uB2E4\uB77C\uD55C\uAE00");
                    case 950:
                        return new PreviewText("Here is example ! ", "ABC 123 HHHHHH", "\u6E2C\u8A66\u6E2C\u8A66\u7E41\u9AD4", "\u6B63\u9AD4\u4E2D\u6587\u6E2C\u8A66");
                    case 936:
                    default:
                        return new PreviewText("Here is example ! ", "ABC 123 HHHHHH", "\u6D4B\u8BD5\u6D4B\u8BD5\u7B80\u4F53", "\u7B80\u4F53\u4E2D\u6587\u6D4B\u8BD5");
                }
            }
        }
    }
}
