using System;
using SkiaSharp;

namespace DC_Font_Generator
{
    internal static class SkiaTypefaceService
    {
        public static SKTypeface CreateTypeface(FontDescriptor font, FontStyleDescriptor descriptor = null)
        {
            if (font == null)
            {
                return null;
            }

            return CreateTypeface(
                font.FamilyName,
                descriptor?.Weight ?? font.Weight,
                descriptor?.Width ?? font.Width,
                descriptor?.Slant ?? font.Slant);
        }

        public static SKTypeface CreateTypeface(string familyName, int weight, int width, SKFontStyleSlant slant)
        {
            if (string.IsNullOrWhiteSpace(familyName))
            {
                return null;
            }

            SKFontStyle style = new SKFontStyle(weight, width, slant);
            SKTypeface typeface = TryCreateFromStyleSet(familyName, style);
            if (typeface != null)
            {
                return typeface;
            }

            try
            {
                typeface = SKTypeface.FromFamilyName(familyName, style);
                if (typeface != null)
                {
                    return typeface;
                }
            }
            catch
            {
            }

            try
            {
                return SKTypeface.FromFamilyName(familyName, weight, width, slant);
            }
            catch
            {
                return null;
            }
        }

        private static SKTypeface TryCreateFromStyleSet(string familyName, SKFontStyle style)
        {
            try
            {
                using (SKFontStyleSet styleSet = SKFontManager.Default.GetFontStyles(familyName))
                {
                    if (styleSet == null || styleSet.Count == 0)
                    {
                        return null;
                    }

                    SKTypeface exact = styleSet.CreateTypeface(style);
                    if (exact != null)
                    {
                        return exact;
                    }

                    int fallbackIndex = FindClosestStyleIndex(styleSet, style);
                    return fallbackIndex >= 0 ? styleSet.CreateTypeface(fallbackIndex) : null;
                }
            }
            catch
            {
                return null;
            }
        }

        private static int FindClosestStyleIndex(SKFontStyleSet styleSet, SKFontStyle target)
        {
            int bestIndex = -1;
            int bestScore = int.MaxValue;

            for (int i = 0; i < styleSet.Count; i++)
            {
                SKFontStyle candidate = styleSet[i];
                int score =
                    Math.Abs(candidate.Weight - target.Weight)
                    + (Math.Abs(candidate.Width - target.Width) * 100)
                    + (candidate.Slant == target.Slant ? 0 : 1000);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }
    }
}
