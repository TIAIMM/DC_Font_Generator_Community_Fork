using System;
using System.Collections.Generic;
using System.Drawing;
using SkiaSharp;

namespace DC_Font_Generator
{
    class DrawFont : IDisposable
    {
        private const int NoEffectVerticalPadding = 1;

        public FontDescriptor Font; //目前字型
        public FontStyleDescriptor StyleDescriptor;
        public float ascentPixel = 0; //目前字型上升值
        public float descentPixel = 0; //目前字型下降值
        public float lineSpacingPixel = 0;//目前字型行距
        

        public Color BackColor = Color.FromArgb(0, Color.Black);
        private Color fontColor = Color.FromArgb(0xFF, Color.White);
        public Color OutlineColor = Color.FromArgb(0xFF, Color.FromArgb(80, 80, 80));
        public int OutlineWidth = 0;
        public float CDZ_BottomAlign = 0; //CDZ的底部對齊位置
        private int glow = 4;
        private Color glowcolor = Color.FromArgb(0x80, 0x80, 0x80, 0x80);
        public float SpaceWidth = 0; //空白字型的寬度

        public int DrawMode = 1; //0=無特效 1=反鋸齒

        private SKTypeface skTypeface;
        private bool ownsSkTypeface;
        private GlyphRenderContext glyphRenderContext;
        private FontRenderBackend renderBackend = FontRenderBackendSelector.ReadRequestedBackend();

        public FontRenderBackend RenderBackend
        {
            get { return renderBackend; }
            set
            {
                if (renderBackend != value)
                {
                    renderBackend = value;
                    ResetGlyphRenderContext();
                }
            }
        }

        public DrawFont()
        {
            CreateGlow();
        }

        private void CreateGlow()
        {
        }

        private void CreateOutline()
        {
        }

        public int Glow
        {
            set
            {
                if (glow != value)
                {
                    glow = value;
                    CreateGlow();
                    CreateDrawingZone();
                }
            }
            get { return glow; }
        }

        public Color GlowColor
        {
            set
            {
                if (glowcolor != value)
                {
                    glowcolor = value;
                    CreateGlow();
                }
            }
            get { return glowcolor; }
        }

        public int Outline
        {
            set
            {
                if (OutlineWidth != value)
                {
                    OutlineWidth = value;
                    CreateDrawingZone();
                }
            }
        }

        public Color FontColor
        {
            set
            {
                if (fontColor != value)
                {
                    fontColor = value;
                }
            }
            get { return fontColor; }
        }

        /// <summary>
        /// 設定現在使用的字型
        /// </summary>
        public FontDescriptor FontData
        {
            set
            {
                if (!ReferenceEquals(Font, value))
                {
                    Font = value;
                    using (SKTypeface typeface = value?.CreateTypeface())
                    {
                        if (typeface != null)
                        {
                            using (SKFont skFont = new SKFont(typeface, value.SizePixels))
                            {
                                skFont.GetFontMetrics(out SKFontMetrics metrics);
                                ascentPixel = -metrics.Ascent;
                                descentPixel = metrics.Descent;
                                lineSpacingPixel = -metrics.Ascent + metrics.Descent + metrics.Leading;
                            }
                        }
                        else
                        {
                            ascentPixel = value.SizePixels;
                            descentPixel = 0;
                            lineSpacingPixel = value.SizePixels * 1.2f;
                        }
                    }

                    CreateSkiaTypeface();
                    CreateDrawingZone();
                    CreateSpaceWidth();
                }
            }
            get { return Font; }
        }

        private void CreateDrawingZone()
        {
            int shift = (OutlineWidth * 2) + (glow * 2);
            CDZ_BottomAlign = (shift / 2) + ascentPixel + 0.5f;
            ResetGlyphRenderContext();
        }

        /// <summary>
        /// 建立Space的寬度
        /// </summary>
        private void CreateSpaceWidth()
        {
            float measureWidth = 0;
            try
            {
                using (SKFont font = CreateTextFont())
                {
                    measureWidth = font.MeasureText(" ");
                }
            }
            catch
            {
                measureWidth = Font != null ? Font.SizePixels / 4f : 1f;
            }

            SpaceWidth = measureWidth;

            if (Font != null && SpaceWidth < Font.SizePixels / 4)
            {
                SpaceWidth = Font.SizePixels / 4;
            }

            float maxSpace = lineSpacingPixel / 3;
            if (SpaceWidth > maxSpace)
            {
                SpaceWidth = maxSpace;
            }

            SpaceWidth = Math.Max(1f, RoundMetric(SpaceWidth));
        }

        /// <summary>
        /// 繪製文字
        /// </summary>
        public Bitmap DrawingFont(char c, out float BottomAlign)
        {
            GlyphRenderResult glyph = RenderGlyph(c);
            BottomAlign = glyph.fTopEdge;
            return glyph.GlyphImage != null ? glyph.GlyphImage.ToBitmap() : new Bitmap(1, 1);
        }

        public GlyphRenderResult RenderGlyph(char c)
        {
            return RenderGlyphSkia(c);
        }

        private GlyphRenderResult RenderGlyphSkia(char c)
        {
            GlyphRenderResult result = new GlyphRenderResult();
            if (c < 32)
            {
                return CreateSpaceResult(result);
            }

            string text = c.ToString();
            int effectShift = glow + OutlineWidth;
            float originX = effectShift + 0.5f;
            float baseline = CDZ_BottomAlign;

            SKTypeface primaryTypeface = ResolveTypefaceForCharacter(c, out bool ownsPrimary);
            try
            {
                GlyphRenderResult primaryResult = TryRenderGlyph(c, text, effectShift, originX, baseline, primaryTypeface);
                if (!primaryResult.IsSpace)
                {
                    return primaryResult;
                }
            }
            finally
            {
                if (ownsPrimary && primaryTypeface != null)
                {
                    primaryTypeface.Dispose();
                }
            }

            if (!ownsPrimary)
            {
                SKTypeface fallbackTypeface = SKFontManager.Default.MatchCharacter(c);
                if (fallbackTypeface != null)
                {
                    try
                    {
                        return TryRenderGlyph(c, text, effectShift, originX, baseline, fallbackTypeface);
                    }
                    finally
                    {
                        fallbackTypeface.Dispose();
                    }
                }
            }

            return result;
        }

        private GlyphRenderResult TryRenderGlyph(char c, string text, int effectShift, float originX, float baseline, SKTypeface typeface)
        {
            GlyphRenderResult result = new GlyphRenderResult();
            int surfaceSize = Math.Max(1, (int)Math.Ceiling(lineSpacingPixel * 2f + (effectShift * 4f) + 4f));

            using (SKFont font = CreateTextFont(typeface))
            using (SKPaint fillPaint = CreateTextPaint(SKPaintStyle.Fill, FontColor, 0f))
            {
                SKPath glyphPath = null;
                bool directWritePath = DirectWriteGlyphPathService.TryGetGlyphPath(
                    Font,
                    StyleDescriptor,
                    c,
                    originX,
                    baseline,
                    out glyphPath);

                try
                {
                    if (!IsUsableGlyphPath(glyphPath))
                    {
                        glyphPath?.Dispose();
                        glyphPath = GetTextPath(font, text, originX, baseline);
                        directWritePath = false;
                    }

                    if (!IsUsableGlyphPath(glyphPath))
                    {
                        return CreateSpaceResult(result);
                    }

                    SKRect originBounds = glyphPath.Bounds;
                    result.OriginSize = new Size((int)Math.Ceiling(originBounds.Width), (int)Math.Ceiling(originBounds.Height));
                    result.BodyTopEdge = baseline - originBounds.Top;
                    result.BodyDrop = originBounds.Bottom - baseline;
                    float measuredAdvance = MeasureLayoutAdvance(font, text, originBounds.Width);
                    result.LayoutAdvance = Math.Max(1f, Math.Max(measuredAdvance, originBounds.Width));
                    result.BakedLeftPad = (int)Math.Floor(originX);
                    result.BakedAdvance = Math.Max(1, GameFontMetricQuantizer.ToGameInt(result.LayoutAdvance));
                    result.RealSpace = directWritePath
                        ? 0f
                        : GetSkiaPathRealSpace(font, text, originBounds.Width, originX, baseline);
                    surfaceSize = Math.Max(
                        surfaceSize,
                        (int)Math.Ceiling(result.LayoutAdvance + (effectShift * 4f) + 4f));

                    SKCanvas canvas = GetGlyphRenderContext().PrepareCanvas(surfaceSize, surfaceSize, BackColor);

                    DrawSkiaEffects(canvas, glyphPath);
                    canvas.DrawPath(glyphPath, fillPaint);

                    byte[] pixels = GetGlyphRenderContext().ReadPixels();
                    Rectangle contentBounds = SkiaBitmapInterop.FindContentBounds(pixels, surfaceSize, surfaceSize, BackColor);
                    if (contentBounds.Width <= 0 || contentBounds.Height <= 0)
                    {
                        return CreateSpaceResult(result);
                    }

                    result.LeftBearing = CalculateLeftBearing(contentBounds, originX);
                    result.RightOverhang = CalculateRightOverhang(contentBounds, result.BakedLeftPad + result.BakedAdvance);

                    Rectangle bakedBounds = BakeHorizontalBounds(contentBounds, GetRightSidePadding(), surfaceSize);
                    int verticalPadding = GetNoEffectVerticalPadding();
                    int topPadding = verticalPadding;
                    int bottomPadding = verticalPadding;
                    float virtualTop = bakedBounds.Top - topPadding;
                    float virtualBottom = bakedBounds.Bottom + bottomPadding;

                    result.EffectTopPad = originBounds.Top - virtualTop;
                    result.EffectBottomPad = virtualBottom - originBounds.Bottom;
                    result.GlyphImage = CreateVerticallyPaddedGlyphImage(
                        pixels,
                        surfaceSize,
                        bakedBounds,
                        topPadding,
                        bottomPadding,
                        BackColor);
                    result.fTopEdge = FloorMetric(CDZ_BottomAlign - virtualTop);
                    return result;
                }
                finally
                {
                    glyphPath?.Dispose();
                }
            }
        }

        private static bool IsUsableGlyphPath(SKPath path)
        {
            if (path == null || path.IsEmpty)
            {
                return false;
            }

            SKRect bounds = path.Bounds;
            return bounds.Width > 0 && bounds.Height > 0;
        }

        private int GetRightSidePadding()
        {
            int effectPadding = (int)Math.Ceiling((glow + OutlineWidth) * 0.25f);
            return Math.Max(1, effectPadding);
        }

        private int GetNoEffectVerticalPadding()
        {
            return glow <= 0 && OutlineWidth <= 0 ? NoEffectVerticalPadding : 0;
        }

        private static Rectangle BakeHorizontalBounds(Rectangle contentBounds, int rightPadding, int surfaceSize)
        {
            int left = 0;
            int right = contentBounds.Right + Math.Max(0, rightPadding);

            if (left < 0) left = 0;
            if (right > surfaceSize) right = surfaceSize;
            if (right <= left) right = Math.Min(surfaceSize, left + 1);

            return Rectangle.FromLTRB(left, contentBounds.Top, right, contentBounds.Bottom);
        }

        private static Bgra32Image CreateVerticallyPaddedGlyphImage(
            byte[] pixels,
            int sourceWidth,
            Rectangle sourceBounds,
            int topPadding,
            int bottomPadding,
            Color background)
        {
            Bgra32Image glyph = SkiaBitmapInterop.CreateImageFromBgra(pixels, sourceWidth, sourceBounds);
            topPadding = Math.Max(0, topPadding);
            bottomPadding = Math.Max(0, bottomPadding);
            if (topPadding == 0 && bottomPadding == 0)
            {
                return glyph;
            }

            Bgra32Image padded = new Bgra32Image(glyph.Width, glyph.Height + topPadding + bottomPadding);
            padded.Clear(background);
            glyph.CopyTo(padded, 0, topPadding);
            return padded;
        }

        private static float CalculateLeftBearing(Rectangle contentBounds, float originX)
        {
            float leftBearing = contentBounds.Left - originX;
            return float.IsNaN(leftBearing) || float.IsInfinity(leftBearing) ? 0f : leftBearing;
        }

        private float CalculateRightOverhang(Rectangle contentBounds, float logicalRight)
        {
            if (glow <= 0 && OutlineWidth <= 0)
            {
                return 0f;
            }

            float overhang = contentBounds.Right - logicalRight;
            return overhang > 0f ? overhang : 0f;
        }

        private GlyphRenderResult CreateSpaceResult(GlyphRenderResult result)
        {
            result.IsSpace = true;
            result.OriginSize = new Size((int)SpaceWidth, 0);
            result.LayoutAdvance = SpaceWidth;
            result.BakedLeftPad = 0;
            result.BakedAdvance = Math.Max(1, GameFontMetricQuantizer.ToGameInt(SpaceWidth));
            result.RealSpace = SpaceWidth;
            result.LeftBearing = 0f;
            result.RightOverhang = 0f;
            return result;
        }

        private static float MeasureLayoutAdvance(SKFont font, string text, float fallbackWidth)
        {
            float advance = font.MeasureText(text);
            if (float.IsNaN(advance) || float.IsInfinity(advance) || advance <= 0f)
            {
                advance = fallbackWidth;
            }

            return advance;
        }

        private static float RoundMetric(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return (float)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        private static float CeilingMetric(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return (float)Math.Ceiling(value);
        }

        private static float FloorMetric(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return (float)Math.Floor(value);
        }

        private void DrawSkiaEffects(SKCanvas canvas, SKPath glyphPath)
        {
            if (glow > 0)
            {
                int size = OutlineWidth + glow;
                int glowStep = 0x80 / (glow + 1);
                int alpha = glowStep;
                for (int i = 0; i < glow; i++)
                {
                    using (SKPaint glowPaint = CreateTextPaint(
                        SKPaintStyle.Stroke,
                        Color.FromArgb(alpha, glowcolor.R, glowcolor.G, glowcolor.B),
                        Math.Max(1, size - i)))
                    {
                        canvas.DrawPath(glyphPath, glowPaint);
                    }

                    if (i >= OutlineWidth)
                    {
                        alpha += glowStep;
                        if (alpha > 0x80)
                        {
                            alpha = 0x80;
                        }
                    }
                }
            }

            if (OutlineWidth > 0)
            {
                using (SKPaint outlinePaint = CreateTextPaint(SKPaintStyle.Stroke, OutlineColor, OutlineWidth))
                {
                    canvas.DrawPath(glyphPath, outlinePaint);
                }
            }
        }

        private float GetSkiaPathRealSpace(SKFont font, string text, float originWidth, float originX, float baseline)
        {
            using (SKPath doublePath = GetTextPath(font, text + text, originX, baseline))
            {
                if (doublePath == null)
                {
                    return 0f;
                }

                SKRect doubleBounds = doublePath.Bounds;
                return (doubleBounds.Width - (originWidth * 2f)) / 4f;
            }
        }

        private SKPaint CreateTextPaint(SKPaintStyle style, Color color, float strokeWidth)
        {
            SKPaint paint = new SKPaint();
            paint.IsAntialias = DrawMode == 1;
            paint.Color = SkiaBitmapInterop.ToSKColor(color);
            paint.Style = style;
            paint.StrokeWidth = strokeWidth;
            paint.StrokeJoin = SKStrokeJoin.Round;
            return paint;
        }

        private SKFont CreateTextFont()
        {
            return CreateTextFont(skTypeface ?? SKTypeface.Default);
        }

        private SKFont CreateTextFont(SKTypeface typeface)
        {
            return new SKFont(typeface ?? SKTypeface.Default, Font != null ? Font.SizePixels : 12f);
        }

        private static SKPath GetTextPath(SKFont font, string text, float x, float y)
        {
            byte[] textBytes = System.Text.Encoding.Unicode.GetBytes(text);
            return font.GetTextPath(textBytes, SKTextEncoding.Utf16, new SKPoint(x, y));
        }

        private SKTypeface ResolveTypefaceForCharacter(char c, out bool ownsResolvedTypeface)
        {
            ownsResolvedTypeface = false;
            int codepoint = c;
            SKTypeface current = skTypeface ?? SKTypeface.Default;
            if (current != null && current.ContainsGlyph(codepoint))
            {
                return current;
            }

            SKTypeface fallback = SKFontManager.Default.MatchCharacter(codepoint);
            if (fallback != null)
            {
                ownsResolvedTypeface = true;
                return fallback;
            }

            return current;
        }

        private void CreateSkiaTypeface()
        {
            SKTypeface next = null;
            bool ownsNext = false;
            if (Font != null)
            {
                next = StyleDescriptor != null
                    ? SkiaTypefaceService.CreateTypeface(Font, StyleDescriptor)
                    : Font.CreateTypeface();
                ownsNext = next != null;
            }

            if (next == null)
            {
                next = SKTypeface.Default;
                ownsNext = false;
            }

            if (ownsSkTypeface && skTypeface != null)
            {
                skTypeface.Dispose();
            }

            skTypeface = next;
            ownsSkTypeface = ownsNext;
            ResetGlyphRenderContext();
        }

        private void GetStyleValues(out int weight, out int width, out SKFontStyleSlant slant)
        {
            if (StyleDescriptor != null)
            {
                weight = StyleDescriptor.Weight;
                width = StyleDescriptor.Width;
                slant = StyleDescriptor.Slant;
            }
            else
            {
                weight = Font != null ? Font.Weight : 400;
                width = Font != null ? Font.Width : (int)SKFontStyleWidth.Normal;
                slant = Font != null ? Font.Slant : SKFontStyleSlant.Upright;
            }
        }

        private GlyphRenderContext GetGlyphRenderContext()
        {
            if (glyphRenderContext != null)
            {
                return glyphRenderContext;
            }

            FontRenderBackend backend = renderBackend == FontRenderBackend.Auto
                ? FontRenderBackend.Cpu
                : renderBackend;
            try
            {
                glyphRenderContext = new GlyphRenderContext(FontRenderBackendSelector.CreateFactory(backend));
            }
            catch
            {
                if (backend == FontRenderBackend.Cpu)
                {
                    throw;
                }

                glyphRenderContext = new GlyphRenderContext(FontRenderBackendSelector.CreateFactory(FontRenderBackend.Cpu));
            }

            return glyphRenderContext;
        }

        private void ResetGlyphRenderContext()
        {
            glyphRenderContext?.Dispose();
            glyphRenderContext = null;
        }

        public class GlyphRenderResult
        {
            public Bgra32Image GlyphImage;
            public Size OriginSize;
            public float LayoutAdvance;
            public int BakedLeftPad;
            public int BakedAdvance;
            public float RealSpace;
            public float LeftBearing;
            public float RightOverhang;
            public float BodyTopEdge;
            public float BodyDrop;
            public float EffectTopPad;
            public float EffectBottomPad;
            public float fTopEdge;
            public bool IsSpace;

            public float BottomAlign
            {
                get { return fTopEdge; }
                set { fTopEdge = value; }
            }

            public float GetGeneratedTopEdge(bool useBodyMetrics)
            {
                if (!useBodyMetrics || BodyTopEdge <= 0f)
                {
                    return fTopEdge;
                }

                float topEdge = BodyTopEdge + Math.Max(0f, EffectTopPad);
                if (float.IsNaN(topEdge) || float.IsInfinity(topEdge) || topEdge <= 0f)
                {
                    return fTopEdge;
                }

                return topEdge;
            }
        }

        /// <summary>
        /// 取得原字型真實高度
        /// </summary>
        public Size GetOriginFontHeight(char c, out SizeF DisplaySize, out float RealSpace)
        {
            GlyphRenderResult glyph = RenderGlyph(c);
            DisplaySize = glyph.OriginSize;
            RealSpace = glyph.RealSpace;
            return glyph.OriginSize;
        }

        public Bitmap GetOriginFont(char c, out bool IsEmpty)
        {
            GlyphRenderResult glyph = RenderGlyph(c);
            IsEmpty = glyph.IsSpace || glyph.GlyphImage == null;
            if (glyph.GlyphImage == null)
            {
                return new Bitmap(1, 1);
            }

            return glyph.GlyphImage.ToBitmap();
        }

        public void Dispose()
        {
            ResetGlyphRenderContext();
            if (ownsSkTypeface && skTypeface != null)
            {
                skTypeface.Dispose();
            }
            skTypeface = null;
            ownsSkTypeface = false;
        }
    }
}
