using System;
using SkiaSharp;

namespace DC_Font_Generator
{
    internal static class SkiaGlyphPathService
    {
        public static bool TryGetGlyphPath(
            FontDescriptor font,
            FontStyleDescriptor descriptor,
            char c,
            float originX,
            float baseline,
            out SKPath path)
        {
            path = null;
            if (font == null || c < 32)
            {
                return false;
            }

            SKTypeface typeface = null;
            try
            {
                typeface = descriptor != null
                    ? SkiaTypefaceService.CreateTypeface(font, descriptor)
                    : font.CreateTypeface();
                if (typeface == null || typeface.GlyphCount <= 0)
                {
                    return false;
                }

                using (SKFont skFont = new SKFont(typeface, Math.Max(1f, font.SizePixels)))
                {
                    ushort glyphId = skFont.GetGlyph(c);
                    if (glyphId == 0)
                    {
                        return false;
                    }

                    path = skFont.GetGlyphPath(glyphId);
                    if (!IsUsable(path))
                    {
                        path?.Dispose();
                        path = null;
                        return false;
                    }

                    SKMatrix translation = SKMatrix.CreateTranslation(originX, baseline);
                    path.Transform(in translation);
                    return IsUsable(path);
                }
            }
            catch
            {
                path?.Dispose();
                path = null;
                return false;
            }
            finally
            {
                typeface?.Dispose();
            }
        }

        private static bool IsUsable(SKPath path)
        {
            if (path == null || path.IsEmpty)
            {
                return false;
            }

            SKRect bounds = path.Bounds;
            return bounds.Width > 0f && bounds.Height > 0f;
        }
    }
}
