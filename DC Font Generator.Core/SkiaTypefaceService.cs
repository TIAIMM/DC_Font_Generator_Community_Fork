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
                FontRenderDebugLog.Add("[font-debug] SkiaTypeface: font=<null>");
                return null;
            }

            if (descriptor != null)
            {
                string sourceFamily = string.IsNullOrWhiteSpace(descriptor.SourceFamilyName)
                    ? font.FamilyName
                    : descriptor.SourceFamilyName;
                FontRenderDebugLog.Add($"[font-debug] SkiaTypeface request descriptor: family={sourceFamily}, style={descriptor.Name}, idx={descriptor.StyleSetIndex}, w={descriptor.Weight}, wd={descriptor.Width}, slant={descriptor.Slant}");
                SKTypeface exact = TryCreateFromStyleSetIndex(sourceFamily, descriptor.StyleSetIndex, "descriptor");
                if (exact != null)
                {
                    LogResolved("SkiaTypeface descriptor-index", exact);
                    return exact;
                }

                return CreateTypeface(sourceFamily, descriptor.Weight, descriptor.Width, descriptor.Slant);
            }

            FontRenderDebugLog.Add($"[font-debug] SkiaTypeface request font: family={font.FamilyName}, style={font.StyleName ?? ""}, idx={font.StyleSetIndex}, w={font.Weight}, wd={font.Width}, slant={font.Slant}");
            if (font.HasExactStyleSetFace)
            {
                SKTypeface exact = TryCreateFromStyleSetIndex(font.FamilyName, font.StyleSetIndex, "font");
                if (exact != null)
                {
                    LogResolved("SkiaTypeface font-index", exact);
                    return exact;
                }
            }

            return CreateTypeface(font.FamilyName, font.Weight, font.Width, font.Slant);
        }

        public static SKTypeface CreateTypeface(string familyName, int weight, int width, SKFontStyleSlant slant)
        {
            if (string.IsNullOrWhiteSpace(familyName))
            {
                FontRenderDebugLog.Add("[font-debug] SkiaTypeface request family=<empty>");
                return null;
            }

            FontRenderDebugLog.Add($"[font-debug] SkiaTypeface request values: family={familyName}, w={weight}, wd={width}, slant={slant}");
            SKFontStyle style = new SKFontStyle(weight, width, slant);
            SKTypeface typeface = TryCreateExactFromStyleSet(familyName, style);
            if (typeface != null)
            {
                LogResolved("SkiaTypeface exact-values", typeface);
                return typeface;
            }

            try
            {
                typeface = SKTypeface.FromFamilyName(familyName, style);
                LogResolved("SkiaTypeface FromFamilyName", typeface);
                return typeface;
            }
            catch (Exception ex)
            {
                FontRenderDebugLog.AddException("SkiaTypeface FromFamilyName", ex);
                return null;
            }
        }

        private static SKTypeface TryCreateFromStyleSetIndex(string familyName, int styleSetIndex, string source)
        {
            if (string.IsNullOrWhiteSpace(familyName) || styleSetIndex < 0)
            {
                FontRenderDebugLog.Add($"[font-debug] SkiaTypeface {source}-index skipped: family={familyName ?? "<null>"}, idx={styleSetIndex}");
                return null;
            }

            try
            {
                using (SKFontStyleSet styleSet = SKFontManager.Default.GetFontStyles(familyName))
                {
                    if (styleSet == null || styleSetIndex >= styleSet.Count)
                    {
                        FontRenderDebugLog.Add($"[font-debug] SkiaTypeface {source}-index miss: family={familyName}, idx={styleSetIndex}, styleSetCount={styleSet?.Count ?? 0}");
                        return null;
                    }

                    SKFontStyle style = styleSet[styleSetIndex];
                    string styleName = styleSet.GetStyleName(styleSetIndex);
                    FontRenderDebugLog.Add($"[font-debug] SkiaTypeface {source}-index hit: family={familyName}, idx={styleSetIndex}, styleName={styleName}, w={style.Weight}, wd={style.Width}, slant={style.Slant}");
                    return styleSet.CreateTypeface(styleSetIndex);
                }
            }
            catch (Exception ex)
            {
                FontRenderDebugLog.AddException($"SkiaTypeface {source}-index", ex);
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
                        FontRenderDebugLog.Add($"[font-debug] SkiaTypeface exact-values miss: family={familyName}, styleSetCount=0");
                        return null;
                    }

                    for (int i = 0; i < styleSet.Count; i++)
                    {
                        SKFontStyle candidate = styleSet[i];
                        if (candidate.Weight == style.Weight
                            && candidate.Width == style.Width
                            && candidate.Slant == style.Slant)
                        {
                            FontRenderDebugLog.Add($"[font-debug] SkiaTypeface exact-values hit: family={familyName}, idx={i}, styleName={styleSet.GetStyleName(i)}, w={candidate.Weight}, wd={candidate.Width}, slant={candidate.Slant}");
                            return styleSet.CreateTypeface(i);
                        }
                    }

                    FontRenderDebugLog.Add($"[font-debug] SkiaTypeface exact-values no exact match: family={familyName}, requested w={style.Weight}, wd={style.Width}, slant={style.Slant}, styleSetCount={styleSet.Count}");
                }
            }
            catch (Exception ex)
            {
                FontRenderDebugLog.AddException("SkiaTypeface exact-values", ex);
            }

            return null;
        }

        private static void LogResolved(string stage, SKTypeface typeface)
        {
            if (typeface == null)
            {
                FontRenderDebugLog.Add($"[font-debug] {stage} resolved <null>");
                return;
            }

            FontRenderDebugLog.Add($"[font-debug] {stage} resolved: family={typeface.FamilyName}, w={typeface.FontWeight}, wd={typeface.FontWidth}, slant={typeface.FontSlant}, glyphs={typeface.GlyphCount}");
        }
    }
}
