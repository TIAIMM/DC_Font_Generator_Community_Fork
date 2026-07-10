using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;

namespace DC_Font_Generator
{
    internal static class SkiaTypefaceService
    {
        // The cached value is an index in the current SKFontStyleSet, not a native
        // typeface handle. A fresh SKTypeface is created for each caller so normal
        // IDisposable ownership rules are preserved.
        private static readonly ConcurrentDictionary<string, int> ResolvedFaceIndexCache =
            new ConcurrentDictionary<string, int>(StringComparer.Ordinal);

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
                return CreateTypefaceCore(
                    sourceFamily,
                    descriptor.Weight,
                    descriptor.Width,
                    descriptor.Slant,
                    descriptor.StyleSetIndex,
                    descriptor.Name,
                    descriptor.HasExactStyleSetFace);
            }

            return CreateTypefaceCore(
                font.FamilyName,
                font.Weight,
                font.Width,
                font.Slant,
                font.StyleSetIndex,
                font.StyleName,
                font.HasExactStyleSetFace);
        }

        public static SKTypeface CreateTypeface(
            string familyName,
            int weight,
            int width,
            SKFontStyleSlant slant)
        {
            return CreateTypefaceCore(familyName, weight, width, slant, -1, null, false);
        }

        private static SKTypeface CreateTypefaceCore(
            string familyName,
            int weight,
            int width,
            SKFontStyleSlant slant,
            int preferredIndex,
            string styleName,
            bool requireExactFace)
        {
            if (string.IsNullOrWhiteSpace(familyName))
            {
                return null;
            }

            SKFontStyle target = new SKFontStyle(weight, width, slant);
            try
            {
                using (SKFontStyleSet styleSet = SKFontManager.Default.GetFontStyles(familyName))
                {
                    if (styleSet != null && styleSet.Count > 0)
                    {
                        string cacheKey = CreateCacheKey(familyName, styleSet.Count, target, styleName);

                        // Normal hot path after the first successful resolution.
                        if (ResolvedFaceIndexCache.TryGetValue(cacheKey, out int cachedIndex))
                        {
                            SKTypeface cached = TryCreateValidatedFace(
                                styleSet,
                                cachedIndex,
                                target,
                                requireExactFace);
                            if (cached != null)
                            {
                                return cached;
                            }

                            ResolvedFaceIndexCache.TryRemove(cacheKey, out _);
                        }

                        // Most fonts, including non-Super-TTC Sarasa families, resolve here.
                        SKTypeface preferred = TryCreateValidatedFace(
                            styleSet,
                            preferredIndex,
                            target,
                            requireExactFace);
                        if (preferred != null)
                        {
                            ResolvedFaceIndexCache[cacheKey] = preferredIndex;
                            return preferred;
                        }

                        // Some Super TTC families expose two entries with the same advertised
                        // style. Only inspect matching metadata first instead of opening every
                        // face in the collection.
                        List<int> likelyIndices = CollectLikelyIndices(
                            styleSet,
                            target,
                            styleName,
                            preferredIndex);
                        SKTypeface likely = FindBestValidatedFace(
                            styleSet,
                            likelyIndices,
                            target,
                            styleName,
                            preferredIndex,
                            requireExactFace,
                            out int likelyIndex);
                        if (likely != null)
                        {
                            ResolvedFaceIndexCache[cacheKey] = likelyIndex;
                            return likely;
                        }

                        // Rare recovery path for malformed or misleading style-set metadata.
                        // This scans each remaining face once, without OpenStream(), TTC
                        // reconstruction, or glyph-outline probing.
                        List<int> remainingIndices = new List<int>(styleSet.Count);
                        HashSet<int> alreadyTried = new HashSet<int>(likelyIndices);
                        if (preferredIndex >= 0)
                        {
                            alreadyTried.Add(preferredIndex);
                        }

                        for (int i = 0; i < styleSet.Count; i++)
                        {
                            if (!alreadyTried.Contains(i))
                            {
                                remainingIndices.Add(i);
                            }
                        }

                        SKTypeface recovered = FindBestValidatedFace(
                            styleSet,
                            remainingIndices,
                            target,
                            styleName,
                            preferredIndex,
                            requireExactFace,
                            out int recoveredIndex);
                        if (recovered != null)
                        {
                            ResolvedFaceIndexCache[cacheKey] = recoveredIndex;
                            return recovered;
                        }

                        if (requireExactFace)
                        {
                            return null;
                        }
                    }
                }
            }
            catch
            {
                if (requireExactFace)
                {
                    return null;
                }
            }

            try
            {
                SKTypeface fallback = SKFontManager.Default.MatchFamily(familyName, target);
                if (IsUsable(fallback)
                    && (!requireExactFace || ActualStyleEquals(fallback, target)))
                {
                    return fallback;
                }

                DisposeTypeface(fallback);
            }
            catch
            {
            }

            return null;
        }

        private static SKTypeface TryCreateValidatedFace(
            SKFontStyleSet styleSet,
            int index,
            SKFontStyle target,
            bool requireExactFace)
        {
            if (styleSet == null || index < 0 || index >= styleSet.Count)
            {
                return null;
            }

            SKTypeface typeface = null;
            try
            {
                typeface = styleSet.CreateTypeface(index);
                if (!IsUsable(typeface))
                {
                    DisposeTypeface(typeface);
                    return null;
                }

                if (requireExactFace && !ActualStyleEquals(typeface, target))
                {
                    DisposeTypeface(typeface);
                    return null;
                }

                return typeface;
            }
            catch
            {
                DisposeTypeface(typeface);
                return null;
            }
        }

        private static List<int> CollectLikelyIndices(
            SKFontStyleSet styleSet,
            SKFontStyle target,
            string requestedStyleName,
            int preferredIndex)
        {
            List<int> exactNameAndValues = new List<int>();
            List<int> matchingValues = new List<int>();
            List<int> matchingName = new List<int>();

            for (int i = 0; i < styleSet.Count; i++)
            {
                if (i == preferredIndex)
                {
                    continue;
                }

                SKFontStyle advertised = styleSet[i];
                string advertisedName = SafeGetStyleName(styleSet, i);
                bool valuesMatch = StyleValuesEqual(advertised, target);
                bool nameMatches = StyleNamesEqual(advertisedName, requestedStyleName);

                if (valuesMatch && nameMatches)
                {
                    exactNameAndValues.Add(i);
                }
                else if (valuesMatch)
                {
                    matchingValues.Add(i);
                }
                else if (nameMatches)
                {
                    matchingName.Add(i);
                }
            }

            List<int> result = new List<int>(
                exactNameAndValues.Count + matchingValues.Count + matchingName.Count);
            result.AddRange(exactNameAndValues);
            result.AddRange(matchingValues);
            result.AddRange(matchingName);
            return result;
        }

        private static SKTypeface FindBestValidatedFace(
            SKFontStyleSet styleSet,
            IList<int> indices,
            SKFontStyle target,
            string requestedStyleName,
            int preferredIndex,
            bool requireExactFace,
            out int resolvedIndex)
        {
            resolvedIndex = -1;
            SKTypeface best = null;
            int bestScore = int.MinValue;

            for (int order = 0; order < indices.Count; order++)
            {
                int index = indices[order];
                SKTypeface candidate = TryCreateValidatedFace(
                    styleSet,
                    index,
                    target,
                    requireExactFace);
                if (candidate == null)
                {
                    continue;
                }

                int score = ScoreCandidate(
                    styleSet,
                    index,
                    candidate,
                    target,
                    requestedStyleName,
                    preferredIndex,
                    order);
                if (score > bestScore)
                {
                    DisposeTypeface(best);
                    best = candidate;
                    bestScore = score;
                    resolvedIndex = index;
                }
                else
                {
                    DisposeTypeface(candidate);
                }

                // This is the strongest possible match and avoids scanning additional
                // duplicate sources once the logical style is unambiguous.
                if (bestScore >= 20000)
                {
                    break;
                }
            }

            return best;
        }

        private static int ScoreCandidate(
            SKFontStyleSet styleSet,
            int index,
            SKTypeface typeface,
            SKFontStyle target,
            string requestedStyleName,
            int preferredIndex,
            int searchOrder)
        {
            SKFontStyle advertised = styleSet[index];
            string advertisedName = SafeGetStyleName(styleSet, index);
            string postScriptName = SafeGetPostScriptName(typeface);

            int score = 0;
            if (ActualStyleEquals(typeface, target)) score += 10000;
            else score -= ActualStyleDistance(typeface, target);

            if (StyleValuesEqual(advertised, target)) score += 5000;
            if (StyleNamesEqual(advertisedName, requestedStyleName)) score += 3000;
            if (PostScriptStyleMatches(postScriptName, requestedStyleName)) score += 2000;
            if (index == preferredIndex) score += 500;

            // Preserve deterministic ordering when duplicate installed sources describe
            // the same logical face.
            score -= searchOrder;
            return score;
        }

        private static string CreateCacheKey(
            string familyName,
            int styleCount,
            SKFontStyle target,
            string styleName)
        {
            return NormalizeIdentifier(familyName)
                + "|count=" + styleCount
                + "|w=" + target.Weight
                + "|wid=" + target.Width
                + "|sl=" + (int)target.Slant
                + "|name=" + NormalizeIdentifier(styleName);
        }

        private static bool ActualStyleEquals(SKTypeface typeface, SKFontStyle target)
        {
            return typeface != null
                && typeface.FontWeight == target.Weight
                && typeface.FontWidth == target.Width
                && typeface.FontSlant == target.Slant;
        }

        private static int ActualStyleDistance(SKTypeface typeface, SKFontStyle target)
        {
            if (typeface == null)
            {
                return 100000;
            }

            return Math.Abs(typeface.FontWeight - target.Weight)
                + Math.Abs(typeface.FontWidth - target.Width) * 100
                + (typeface.FontSlant == target.Slant ? 0 : 5000);
        }

        private static string SafeGetStyleName(SKFontStyleSet styleSet, int index)
        {
            try
            {
                return styleSet.GetStyleName(index)?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private static string SafeGetPostScriptName(SKTypeface typeface)
        {
            try
            {
                return typeface?.PostScriptName?.Trim();
            }
            catch
            {
                return null;
            }
        }

        private static bool StyleValuesEqual(SKFontStyle left, SKFontStyle right)
        {
            return left.Weight == right.Weight
                && left.Width == right.Width
                && left.Slant == right.Slant;
        }

        private static bool StyleNamesEqual(string left, string right)
        {
            return !string.IsNullOrWhiteSpace(left)
                && !string.IsNullOrWhiteSpace(right)
                && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool PostScriptStyleMatches(string postScriptName, string styleName)
        {
            string postScript = NormalizeIdentifier(postScriptName);
            string style = NormalizeIdentifier(styleName);
            if (postScript.Length == 0 || style.Length == 0)
            {
                return false;
            }

            if (style == "regular" || style == "normal")
            {
                return postScript.EndsWith("regular", StringComparison.Ordinal)
                    || postScript.EndsWith("normal", StringComparison.Ordinal);
            }

            return postScript.Contains(style, StringComparison.Ordinal);
        }

        private static string NormalizeIdentifier(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            StringBuilder result = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c))
                {
                    result.Append(char.ToLowerInvariant(c));
                }
            }

            return result.ToString();
        }

        private static bool IsUsable(SKTypeface typeface)
        {
            return typeface != null && typeface.GlyphCount > 0;
        }

        private static void DisposeTypeface(SKTypeface typeface)
        {
            if (typeface != null && !ReferenceEquals(typeface, SKTypeface.Default))
            {
                typeface.Dispose();
            }
        }
    }
}
