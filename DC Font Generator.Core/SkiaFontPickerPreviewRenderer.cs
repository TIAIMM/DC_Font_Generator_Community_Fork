using System;
using System.Drawing;
using System.Drawing.Imaging;
using SkiaSharp;

namespace DC_Font_Generator
{
    internal static class SkiaFontPickerPreviewRenderer
    {
        public static Bitmap Render(Size size, FontPickerPreviewRequest request)
        {
            Bitmap bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppArgb);
            SKImageInfo imageInfo = new SKImageInfo(
                size.Width,
                size.Height,
                SKColorType.Bgra8888,
                SKAlphaType.Premul);

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
            int codePage = NormalizePreviewCodePage(request.EncodingCodePage);
            PreviewText text = PreviewText.ForEncoding(codePage, request.AsciiOnly);

            FontDescriptor selectedFont = request.PreviewFont;
            FontStyleDescriptor selectedStyle = request.PreviewFontStyleDescriptor;
            FontDescriptor singleFont = request.EditingDoubleByteFont
                ? request.SingleByteFont ?? selectedFont
                : selectedFont;
            FontDescriptor doubleFont = request.EditingDoubleByteFont
                ? selectedFont
                : request.DoubleByteFont ?? selectedFont;

            FontStyleDescriptor singleStyle = ReferenceEquals(singleFont, selectedFont)
                ? selectedStyle
                : FontStyleDescriptor.FromFontDescriptor(singleFont);
            FontStyleDescriptor doubleStyle = ReferenceEquals(doubleFont, selectedFont)
                ? selectedStyle
                : FontStyleDescriptor.FromFontDescriptor(doubleFont);

            float mixedLineHeight = Math.Max(
                GetLineHeight(singleFont),
                text.HasDoubleByteText ? GetLineHeight(doubleFont) : 0f);
            float selectedLineHeight = GetLineHeight(selectedFont);
            float y = 10f;

            DrawLine(
                canvas,
                request,
                y,
                new PreviewRun(text.SingleByteText, singleFont, singleStyle),
                text.HasDoubleByteText
                    ? new PreviewRun(text.DoubleByteText, doubleFont, doubleStyle)
                    : null);
            y += Math.Max(1f, mixedLineHeight);

            // The normal mixed preview intentionally uses the configured SBCS font for Latin
            // while editing the DBCS font. This dedicated line renders both Latin and CJK with
            // the selected face, so italic, slab and fixed variants can be inspected directly.
            DrawLine(
                canvas,
                request,
                y,
                new PreviewRun(
                    "STYLE: AaBb 0123 HMW / " + text.DoubleByteOnlyText,
                    selectedFont,
                    selectedStyle));
            y += Math.Max(1f, selectedLineHeight);

            DrawLine(
                canvas,
                request,
                y,
                new PreviewRun("SBCS: " + text.SingleByteOnlyText, singleFont, singleStyle));
            y += Math.Max(1f, mixedLineHeight);

            if (text.HasDoubleByteText)
            {
                DrawLine(
                    canvas,
                    request,
                    y,
                    new PreviewRun("DBCS: " + text.DoubleByteOnlyText, doubleFont, doubleStyle));
            }
        }

        private static void DrawLine(
            SKCanvas canvas,
            FontPickerPreviewRequest request,
            float y,
            params PreviewRun[] runs)
        {
            float x = 10f;
            foreach (PreviewRun run in runs)
            {
                if (run == null || run.Font == null || string.IsNullOrEmpty(run.Text))
                {
                    continue;
                }

                x += DrawRun(canvas, request, run, x, y) + 8f;
            }
        }

        private static float DrawRun(
            SKCanvas canvas,
            FontPickerPreviewRequest request,
            PreviewRun run,
            float x,
            float y)
        {
            SKTypeface typeface = CreateTypeface(run.Font, run.Style);
            bool ownsTypeface = typeface != null && !ReferenceEquals(typeface, SKTypeface.Default);
            typeface ??= SKTypeface.Default;

            try
            {
                int effectShift = Math.Max(0, request.Glow) + Math.Max(0, request.Outline);
                float baseline = y + effectShift + GetAscent(run.Font) + 0.5f;
                float currentX = x + effectShift + 0.5f;
                float startX = currentX;

                using (SKFont font = new SKFont(typeface, Math.Max(1f, run.Font.SizePixels)))
                {
                    foreach (char c in run.Text)
                    {
                        float advance = DrawCharacter(
                            canvas,
                            request,
                            font,
                            c,
                            currentX,
                            baseline);

                        if (advance <= 0f && !IsSpacingCharacter(c))
                        {
                            using (SKTypeface fallback = SKFontManager.Default.MatchCharacter(c))
                            {
                                if (fallback != null)
                                {
                                    using (SKFont fallbackFont = new SKFont(
                                        fallback,
                                        Math.Max(1f, run.Font.SizePixels)))
                                    {
                                        advance = DrawCharacter(
                                            canvas,
                                            request,
                                            fallbackFont,
                                            c,
                                            currentX,
                                            baseline);
                                    }
                                }
                            }
                        }

                        if (advance <= 0f && IsSpacingCharacter(c))
                        {
                            advance = Math.Max(1f, run.Font.SizePixels / 4f);
                        }

                        currentX += Math.Max(0f, advance);
                    }
                }

                return Math.Max(0f, currentX - startX);
            }
            finally
            {
                if (ownsTypeface)
                {
                    typeface.Dispose();
                }
            }
        }

        private static float DrawCharacter(
            SKCanvas canvas,
            FontPickerPreviewRequest request,
            SKFont font,
            char c,
            float x,
            float baseline)
        {
            float advance = font.MeasureText(c.ToString());
            if (IsSpacingCharacter(c))
            {
                return Math.Max(advance, 1f);
            }

            ushort glyphId = font.GetGlyph(c);
            if (glyphId == 0)
            {
                return 0f;
            }

            using (SKPath path = font.GetGlyphPath(glyphId))
            {
                if (!IsUsablePath(path))
                {
                    return 0f;
                }

                SKMatrix translation = SKMatrix.CreateTranslation(x, baseline);
                path.Transform(in translation);

                DrawGlow(canvas, request, path);
                DrawOutline(canvas, request, path);
                using (SKPaint fill = CreateFillPaint(request.FontColor))
                {
                    canvas.DrawPath(path, fill);
                }

                return Math.Max(1f, Math.Max(advance, path.Bounds.Width));
            }
        }

        private static SKTypeface CreateTypeface(
            FontDescriptor font,
            FontStyleDescriptor descriptor)
        {
            if (font == null)
            {
                return null;
            }

            return descriptor != null
                ? SkiaTypefaceService.CreateTypeface(font, descriptor)
                : font.CreateTypeface();
        }

        private static bool IsUsablePath(SKPath path)
        {
            if (path == null || path.IsEmpty)
            {
                return false;
            }

            SKRect bounds = path.Bounds;
            return bounds.Width > 0f && bounds.Height > 0f;
        }

        private static bool IsSpacingCharacter(char c)
        {
            return c == ' ' || c == '\u00A0' || char.IsWhiteSpace(c);
        }

        private static void DrawGlow(
            SKCanvas canvas,
            FontPickerPreviewRequest request,
            SKPath path)
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
                    Color.FromArgb(
                        alpha,
                        request.GlowColor.R,
                        request.GlowColor.G,
                        request.GlowColor.B),
                    Math.Max(1, size - i)))
                {
                    canvas.DrawPath(path, paint);
                }

                if (i >= request.Outline)
                {
                    alpha = Math.Min(0x80, alpha + glowStep);
                }
            }
        }

        private static void DrawOutline(
            SKCanvas canvas,
            FontPickerPreviewRequest request,
            SKPath path)
        {
            if (request.Outline <= 0)
            {
                return;
            }

            using (SKPaint paint = CreateStrokePaint(
                request.OutlineColor,
                request.Outline))
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

        private static float GetLineHeight(FontDescriptor font)
        {
            if (font == null)
            {
                return 0f;
            }

            float height = font.GetLineSpacing();
            return float.IsNaN(height) || float.IsInfinity(height) || height <= 0f
                ? Math.Max(1f, font.SizePixels * 1.2f)
                : height;
        }

        private static float GetAscent(FontDescriptor font)
        {
            if (font == null)
            {
                return 0f;
            }

            float ascent = font.GetAscent();
            return float.IsNaN(ascent) || float.IsInfinity(ascent) || ascent <= 0f
                ? Math.Max(1f, font.SizePixels)
                : ascent;
        }

        private static int NormalizePreviewCodePage(int codePage)
        {
            return codePage == 932 || codePage == 936 || codePage == 949 || codePage == 950
                ? codePage
                : 936;
        }

        private sealed class PreviewRun
        {
            public PreviewRun(
                string text,
                FontDescriptor font,
                FontStyleDescriptor style)
            {
                Text = text;
                Font = font;
                Style = style;
            }

            public string Text { get; }
            public FontDescriptor Font { get; }
            public FontStyleDescriptor Style { get; }
        }

        private sealed class PreviewText
        {
            private PreviewText(
                string singleByteText,
                string singleByteOnlyText,
                string doubleByteText,
                string doubleByteOnlyText)
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
                    return new PreviewText(
                        "Here is example ! HHHHHH",
                        "0123456789 ABC xyz",
                        "",
                        "");
                }

                switch (codePage)
                {
                    case 932:
                        return new PreviewText(
                            "Here is example ! ",
                            "ABC 123 HHHHHH",
                            "日本語カナ",
                            "テスト日本語");
                    case 949:
                        return new PreviewText(
                            "Here is example ! ",
                            "ABC 123 HHHHHH",
                            "한글테스트",
                            "가나다라한글");
                    case 950:
                        return new PreviewText(
                            "Here is example ! ",
                            "ABC 123 HHHHHH",
                            "測試測試繁體",
                            "正體中文測試");
                    case 936:
                    default:
                        return new PreviewText(
                            "Here is example ! ",
                            "ABC 123 HHHHHH",
                            "测试测试简体",
                            "简体中文测试");
                }
            }
        }
    }
}
