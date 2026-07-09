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

            if (descriptor != null)
            {
                string sourceFamily = string.IsNullOrWhiteSpace(descriptor.SourceFamilyName)
                    ? font.FamilyName
                    : descriptor.SourceFamilyName;
                SKTypeface exact = TryCreateFromStyleSetIndex(sourceFamily, descriptor.StyleSetIndex);
                if (exact != null)
                {
                    return exact;
                }

                return CreateTypeface(sourceFamily, descriptor.Weight, descriptor.Width, descriptor.Slant);
            }

            if (font.HasExactStyleSetFace)
            {
                SKTypeface exact = TryCreateFromStyleSetIndex(font.FamilyName, font.StyleSetIndex);
                if (exact != null)
                {
                    return exact;
                }
            }

            return CreateTypeface(font.FamilyName, font.Weight, font.Width, font.Slant);
        }

        public static SKTypeface CreateTypeface(string familyName, int weight, int width, SKFontStyleSlant slant)
        {
            if (string.IsNullOrWhiteSpace(familyName))
            {
                return null;
            }

            SKFontStyle style = new SKFontStyle(weight, width, slant);
            SKTypeface typeface = TryCreateExactFromStyleSet(familyName, style);
            if (typeface != null)
            {
                return typeface;
            }

            try
            {
                return SKTypeface.FromFamilyName(familyName, style);
            }
            catch
            {
                return null;
            }
        }

        private static SKTypeface TryCreateFromStyleSetIndex(string familyName, int styleSetIndex)
        {
            if (string.IsNullOrWhiteSpace(familyName) || styleSetIndex < 0)
            {
                return null;
            }

            try
            {
                using (SKFontStyleSet styleSet = SKFontManager.Default.GetFontStyles(familyName))
                {
                    if (styleSet == null || styleSetIndex >= styleSet.Count)
                    {
                        return null;
                    }

                    return styleSet.CreateTypeface(styleSetIndex);
                }
            }
            catch
            {
                return null;
            }
        }

        private static SKTypeface TryCreateExactFromStyleSet(string familyName, SKFontStyle style)
        {
            try
            {
                using (SKFontStyleSet styleSet = SKFontManager.Default.GetFontStyles(familyName))
                {
                    if (styleSet == null || styleSet.Count == 0)
                    {
                        return null;
                    }

                    for (int i = 0; i < styleSet.Count; i++)
                    {
                        SKFontStyle candidate = styleSet[i];
                        if (candidate.Weight == style.Weight
                            && candidate.Width == style.Width
                            && candidate.Slant == style.Slant)
                        {
                            return styleSet.CreateTypeface(i);
                        }
                    }
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
