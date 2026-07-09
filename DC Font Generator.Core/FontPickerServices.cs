using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using SkiaSharp;

namespace DC_Font_Generator
{
    public sealed class FontStyleDescriptor
    {
        public FontStyleDescriptor(
            string name,
            int weight,
            int width,
            SKFontStyleSlant slant,
            int styleSetIndex = -1,
            string sourceFamilyName = null)
        {
            Name = string.IsNullOrWhiteSpace(name)
                ? StyleNameFromValues(weight, width, slant)
                : name.Trim();
            Weight = weight;
            Width = width;
            Slant = slant;
            StyleSetIndex = styleSetIndex;
            SourceFamilyName = string.IsNullOrWhiteSpace(sourceFamilyName) ? null : sourceFamilyName;
        }

        public string Name { get; }
        public int Weight { get; }
        public int Width { get; }
        public SKFontStyleSlant Slant { get; }
        public int StyleSetIndex { get; }
        public string SourceFamilyName { get; }
        public bool HasExactStyleSetFace => StyleSetIndex >= 0 && !string.IsNullOrWhiteSpace(SourceFamilyName);

        public SKFontStyle ToSKFontStyle() => new SKFontStyle(Weight, Width, Slant);

        public bool Matches(int weight, SKFontStyleSlant slant)
        {
            return Weight == weight && Slant == slant;
        }

        public bool Matches(FontStyleDescriptor other)
        {
            if (other == null)
            {
                return false;
            }

            if (HasExactStyleSetFace && other.HasExactStyleSetFace)
            {
                return StyleSetIndex == other.StyleSetIndex
                    && string.Equals(SourceFamilyName, other.SourceFamilyName, StringComparison.OrdinalIgnoreCase);
            }

            return Weight == other.Weight
                && Width == other.Width
                && Slant == other.Slant;
        }

        public string Serialize()
        {
            return $"w{Weight}-wid{Width}-{Slant}";
        }

        public static FontStyleDescriptor Deserialize(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            try
            {
                var parts = key.Split('-');
                int weight = 400, width = 5;
                SKFontStyleSlant slant = SKFontStyleSlant.Upright;
                foreach (var p in parts)
                {
                    if (p.StartsWith("w") && int.TryParse(p.Substring(1), out int w)) weight = w;
                    else if (p.StartsWith("wid") && int.TryParse(p.Substring(3), out int wid)) width = wid;
                    else if (Enum.TryParse(p, out SKFontStyleSlant s)) slant = s;
                }
                return new FontStyleDescriptor(StyleNameFromValues(weight, width, slant), weight, width, slant);
            }
            catch { return null; }
        }

        public static FontStyleDescriptor FromLegacyFontStyle(FontStyle fs)
        {
            int weight = 400;
            SKFontStyleSlant slant = SKFontStyleSlant.Upright;
            if ((fs & FontStyle.Bold) != 0) weight = 700;
            if ((fs & FontStyle.Italic) != 0) slant = SKFontStyleSlant.Italic;
            return new FontStyleDescriptor(
                FontStyleToString(fs),
                weight,
                (int)SKFontStyleWidth.Normal,
                slant);
        }

        public static string FontStyleToString(FontStyle fs)
        {
            return fs switch
            {
                FontStyle.Regular => "Regular",
                FontStyle.Bold => "Bold",
                FontStyle.Italic => "Italic",
                FontStyle.Bold | FontStyle.Italic => "Bold Italic",
                _ => fs.ToString()
            };
        }

        public static string StyleNameFromSkia(string skiaStyleName, int weight, int width, SKFontStyleSlant slant)
        {
            if (!string.IsNullOrWhiteSpace(skiaStyleName))
            {
                return skiaStyleName.Trim();
            }

            return StyleNameFromValues(weight, width, slant);
        }

        public static string StyleNameFromValues(int weight, SKFontStyleSlant slant)
        {
            return StyleNameFromValues(weight, (int)SKFontStyleWidth.Normal, slant);
        }

        public static string StyleNameFromValues(int weight, int width, SKFontStyleSlant slant)
        {
            string weightName = weight switch
            {
                100 => "Thin",
                200 => "ExtraLight",
                300 => "Light",
                350 => "SemiLight",
                400 => "Regular",
                500 => "Medium",
                600 => "SemiBold",
                700 => "Bold",
                800 => "ExtraBold",
                900 => "Black",
                1000 => "ExtraBlack",
                _ => $"W{weight}"
            };
            string widthName = width switch
            {
                (int)SKFontStyleWidth.UltraCondensed => "UltraCondensed",
                (int)SKFontStyleWidth.ExtraCondensed => "ExtraCondensed",
                (int)SKFontStyleWidth.Condensed => "Condensed",
                (int)SKFontStyleWidth.SemiCondensed => "SemiCondensed",
                (int)SKFontStyleWidth.Normal => "",
                (int)SKFontStyleWidth.SemiExpanded => "SemiExpanded",
                (int)SKFontStyleWidth.Expanded => "Expanded",
                (int)SKFontStyleWidth.ExtraExpanded => "ExtraExpanded",
                (int)SKFontStyleWidth.UltraExpanded => "UltraExpanded",
                _ => width == (int)SKFontStyleWidth.Normal ? "" : $"Width{width}"
            };
            string slantName = slant switch
            {
                SKFontStyleSlant.Italic => "Italic",
                SKFontStyleSlant.Oblique => "Oblique",
                _ => ""
            };

            List<string> parts = new List<string>();
            if (!string.IsNullOrEmpty(widthName)) parts.Add(widthName);
            if (!(weight == 400 && parts.Count > 0)) parts.Add(weightName);
            if (!string.IsNullOrEmpty(slantName)) parts.Add(slantName);
            return string.Join(" ", parts);
        }
    }

    public sealed class FontVerticalMetrics
    {
        public FontVerticalMetrics(float ascent, float descent, float leading)
        {
            Ascent = ascent;
            Descent = descent;
            Leading = leading;
        }

        public float Ascent { get; }
        public float Descent { get; }
        public float Leading { get; }
        public float LineSpacing => Ascent + Descent + Leading;
        public float TargetCenter => (Descent - Ascent) / 2f;
    }

    public sealed class FontDescriptor
    {
        public FontDescriptor(string familyName, float sizePixels,
            int weight = 400, int width = 5, SKFontStyleSlant slant = SKFontStyleSlant.Upright)
        {
            FamilyName = familyName; SizePixels = sizePixels;
            Weight = weight; Width = width; Slant = slant;
        }
        public string FamilyName { get; }
        public float SizePixels { get; }
        public int Weight { get; }
        public int Width { get; }
        public SKFontStyleSlant Slant { get; }
        public SKFontStyle ToSKFontStyle() => new SKFontStyle(Weight, Width, Slant);
        public SKTypeface CreateTypeface()
        {
            return SkiaTypefaceService.CreateTypeface(this);
        }
        public float GetLineSpacing()
        {
            return GetVerticalMetrics().LineSpacing;
        }
        public float GetAscent()
        {
            return GetVerticalMetrics().Ascent;
        }
        public FontVerticalMetrics GetVerticalMetrics()
        {
            using (SKTypeface tf = CreateTypeface())
            using (SKFont skFont = new SKFont(tf ?? SKTypeface.Default, SizePixels))
            {
                skFont.GetFontMetrics(out SKFontMetrics metrics);
                return new FontVerticalMetrics(-metrics.Ascent, metrics.Descent, metrics.Leading);
            }
        }
        public System.Drawing.Font ToGdiFont()
        {
            FontStyle gdiStyle = FontStyle.Regular;
            if (Weight >= 600) gdiStyle |= FontStyle.Bold;
            if (Slant != SKFontStyleSlant.Upright) gdiStyle |= FontStyle.Italic;
            return new System.Drawing.Font(FamilyName, SizePixels, gdiStyle, GraphicsUnit.Pixel);
        }
        public static FontDescriptor FromGdiFont(System.Drawing.Font f, FontStyleDescriptor d = null)
        {
            if (d != null)
            {
                string familyName = string.IsNullOrWhiteSpace(d.SourceFamilyName)
                    ? f.FontFamily.Name
                    : d.SourceFamilyName;
                return new FontDescriptor(familyName, f.Size, d.Weight, d.Width, d.Slant);
            }

            return new FontDescriptor(f.FontFamily.Name, f.Size,
                f.Bold ? 700 : 400, 5,
                f.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
        }
    }

    internal sealed class FontPickerFontEntry
    {
        private FontPickerFontEntry(string name, string familyName, List<FontStyleDescriptor> styles)
        {
            Name = name;
            FamilyName = string.IsNullOrWhiteSpace(familyName) ? name : familyName;
            Styles = styles;
        }

        public string Name { get; }
        public string FamilyName { get; }
        public List<FontStyleDescriptor> Styles { get; }
        public bool HasAnyStyle => Styles.Count > 0;

        public static FontPickerFontEntry FromFontFamily(string fontName)
        {
            try
            {
                using (SKFontStyleSet styleSet = SKFontManager.Default.GetFontStyles(fontName))
                {
                    if (styleSet == null || styleSet.Count == 0)
                    {
                        // Fallback: try GDI+ for basic styles
                        return FromLegacy(fontName);
                    }

                    List<FontStyleDescriptor> styles = new List<FontStyleDescriptor>(styleSet.Count);
                    HashSet<string> seen = new HashSet<string>();
                    for (int i = 0; i < styleSet.Count; i++)
                    {
                        string styleName = styleSet.GetStyleName(i);
                        SKFontStyle skStyle = styleSet[i];
                        FontStyleDescriptor descriptor = CreateDescriptor(fontName, i, styleName, skStyle);
                        string key = $"{descriptor.Weight}-{descriptor.Width}-{descriptor.Slant}";
                        if (!seen.Contains(key))
                        {
                            seen.Add(key);
                            styles.Add(descriptor);
                        }
                    }

                    // Sort: Upright first, then Italic, then Oblique; within each group, light to heavy
                    styles.Sort((a, b) =>
                    {
                        int slantOrder = GetSlantOrder(a.Slant).CompareTo(GetSlantOrder(b.Slant));
                        if (slantOrder != 0) return slantOrder;
                        int widthOrder = a.Width.CompareTo(b.Width);
                        if (widthOrder != 0) return widthOrder;
                        return a.Weight.CompareTo(b.Weight);
                    });

                    return new FontPickerFontEntry(fontName, fontName, styles);
                }
            }
            catch
            {
                return FromLegacy(fontName);
            }
        }

        public static List<FontPickerFontEntry> FromFontFamilyFlattened(string fontName)
        {
            try
            {
                using (SKFontStyleSet styleSet = SKFontManager.Default.GetFontStyles(fontName))
                {
                    if (styleSet == null || styleSet.Count == 0)
                    {
                        return new List<FontPickerFontEntry> { FromLegacy(fontName) };
                    }

                    List<FontPickerFontEntry> entries = new List<FontPickerFontEntry>(styleSet.Count);
                    HashSet<string> seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int i = 0; i < styleSet.Count; i++)
                    {
                        string styleName = styleSet.GetStyleName(i);
                        SKFontStyle skStyle = styleSet[i];
                        FontStyleDescriptor descriptor = CreateDescriptor(fontName, i, styleName, skStyle);
                        string displayName = CreateFlattenedDisplayName(fontName, descriptor.Name, skStyle);
                        if (!seenNames.Add(displayName))
                        {
                            displayName = $"{displayName} #{i + 1}";
                            seenNames.Add(displayName);
                        }

                        entries.Add(new FontPickerFontEntry(
                            displayName,
                            fontName,
                            new List<FontStyleDescriptor> { descriptor }));
                    }

                    return entries;
                }
            }
            catch
            {
                return new List<FontPickerFontEntry> { FromLegacy(fontName) };
            }
        }

        private static FontStyleDescriptor CreateDescriptor(string familyName, int styleIndex, string styleName, SKFontStyle skStyle)
        {
            int weight = skStyle.Weight;
            int width = skStyle.Width;
            SKFontStyleSlant slant = skStyle.Slant;
            string displayName = FontStyleDescriptor.StyleNameFromSkia(styleName, weight, width, slant);
            return new FontStyleDescriptor(displayName, weight, width, slant, styleIndex, familyName);
        }

        private static string CreateFlattenedDisplayName(string familyName, string styleName, SKFontStyle skStyle)
        {
            if (IsRegularStyle(styleName, skStyle))
            {
                return familyName;
            }

            if (familyName.EndsWith(" " + styleName, StringComparison.OrdinalIgnoreCase)
                || familyName.EndsWith("-" + styleName, StringComparison.OrdinalIgnoreCase))
            {
                return familyName;
            }

            return familyName + " " + styleName;
        }

        private static bool IsRegularStyle(string styleName, SKFontStyle skStyle)
        {
            bool regularValues = skStyle.Weight == 400
                && skStyle.Width == (int)SKFontStyleWidth.Normal
                && skStyle.Slant == SKFontStyleSlant.Upright;
            if (!regularValues)
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(styleName)
                || string.Equals(styleName, "Regular", StringComparison.OrdinalIgnoreCase)
                || string.Equals(styleName, "Normal", StringComparison.OrdinalIgnoreCase);
        }

        private static FontPickerFontEntry FromLegacy(string fontName)
        {
            try
            {
                using (FontFamily family = new FontFamily(fontName))
                {
                    List<FontStyleDescriptor> styles = new List<FontStyleDescriptor>();
                    if (family.IsStyleAvailable(FontStyle.Regular))
                        styles.Add(FontStyleDescriptor.FromLegacyFontStyle(FontStyle.Regular));
                    if (family.IsStyleAvailable(FontStyle.Bold))
                        styles.Add(FontStyleDescriptor.FromLegacyFontStyle(FontStyle.Bold));
                    if (family.IsStyleAvailable(FontStyle.Italic))
                        styles.Add(FontStyleDescriptor.FromLegacyFontStyle(FontStyle.Italic));
                    if (family.IsStyleAvailable(FontStyle.Bold | FontStyle.Italic))
                        styles.Add(FontStyleDescriptor.FromLegacyFontStyle(FontStyle.Bold | FontStyle.Italic));
                    return new FontPickerFontEntry(fontName, fontName, styles);
                }
            }
            catch
            {
                return new FontPickerFontEntry(fontName, fontName,
                    new List<FontStyleDescriptor> { FontStyleDescriptor.FromLegacyFontStyle(FontStyle.Regular) });
            }
        }

        public bool HasStyleMatching(int weight, SKFontStyleSlant slant)
        {
            return Styles.Any(s => s.Matches(weight, slant));
        }

        private static int GetSlantOrder(SKFontStyleSlant slant)
        {
            return slant switch
            {
                SKFontStyleSlant.Upright => 0,
                SKFontStyleSlant.Italic => 1,
                SKFontStyleSlant.Oblique => 2,
                _ => 3
            };
        }
    }

    internal sealed class FontPickerStyleItem
    {
        public FontPickerStyleItem(string name, FontStyleDescriptor descriptor)
        {
            Name = name;
            Descriptor = descriptor;
        }

        public string Name { get; }
        public FontStyleDescriptor Descriptor { get; }

        public override string ToString()
        {
            return Name;
        }
    }

    internal sealed class FontPickerFilterResult
    {
        public List<FontPickerFontEntry> Entries { get; } = new List<FontPickerFontEntry>();
        public int SelectedIndex { get; set; }
    }

    internal sealed class FontPickerStyleResult
    {
        public List<FontPickerStyleItem> Styles { get; } = new List<FontPickerStyleItem>();
        public int SelectedIndex { get; set; }
    }

    internal static class FontPickerCatalogService
    {
        private static readonly object FontCacheLock = new object();
        private static Task<List<FontPickerFontEntry>> fontLoadTask;

        public static Task<List<FontPickerFontEntry>> EnsureFontLoadTask()
        {
            lock (FontCacheLock)
            {
                if (fontLoadTask == null)
                {
                    fontLoadTask = Task.Run(LoadInstalledFontEntries);
                }

                return fontLoadTask;
            }
        }

        public static List<FontPickerFontEntry> EnsureSelectedEntry(IEnumerable<FontPickerFontEntry> entries, string selectedFontName)
        {
            List<FontPickerFontEntry> result = new List<FontPickerFontEntry>(entries);
            if (!ContainsFontEntry(result, selectedFontName))
            {
                result.Insert(0, FontPickerFontEntry.FromFontFamily(selectedFontName));
            }

            return result;
        }

        public static FontPickerFilterResult Filter(IList<FontPickerFontEntry> entries, string filter, string selectedFontName)
        {
            FontPickerFilterResult result = new FontPickerFilterResult();
            string value = (filter ?? "").Trim();
            foreach (FontPickerFontEntry entry in entries)
            {
                if (value.Length > 0
                    && !entry.Name.StartsWith(value, StringComparison.CurrentCultureIgnoreCase))
                {
                    continue;
                }

                result.Entries.Add(entry);
            }

            if (value.Length > 0)
            {
                foreach (FontPickerFontEntry entry in entries)
                {
                    if (entry.Name.StartsWith(value, StringComparison.CurrentCultureIgnoreCase)
                        || entry.Name.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) < 0)
                    {
                        continue;
                    }

                    result.Entries.Add(entry);
                }
            }

            result.SelectedIndex = FindEntryIndex(result.Entries, selectedFontName);
            if (result.SelectedIndex < 0 && result.Entries.Count > 0)
            {
                result.SelectedIndex = 0;
            }

            return result;
        }

        public static FontPickerStyleResult GetStyles(FontPickerFontEntry entry, FontStyleDescriptor preferredDescriptor)
        {
            FontPickerStyleResult result = new FontPickerStyleResult();

            foreach (var style in entry.Styles)
            {
                result.Styles.Add(new FontPickerStyleItem(style.Name, style));
            }

            if (result.Styles.Count == 0)
            {
                var def = FontStyleDescriptor.FromLegacyFontStyle(FontStyle.Regular);
                result.Styles.Add(new FontPickerStyleItem(def.Name, def));
            }

            if (preferredDescriptor != null)
            {
                for (int i = 0; i < result.Styles.Count; i++)
                {
                    if (result.Styles[i].Descriptor.Matches(preferredDescriptor))
                    {
                        result.SelectedIndex = i;
                        break;
                    }
                }
            }

            return result;
        }

        public static FontPickerFontEntry GetEntryOrFallback(IDictionary<string, FontPickerFontEntry> entries, string fontName)
        {
            FontPickerFontEntry entry;
            if (!entries.TryGetValue(fontName, out entry))
            {
                entry = FontPickerFontEntry.FromFontFamily(fontName);
                entries[fontName] = entry;
            }

            return entry;
        }

        public static decimal ClampFontSize(float size, decimal minimum, decimal maximum)
        {
            decimal value = (decimal)Math.Round(size);
            if (value < minimum)
            {
                return minimum;
            }

            if (value > maximum)
            {
                return maximum;
            }

            return value;
        }

        public static FontDescriptor CreateSelectedFont(string fontName, FontStyleDescriptor descriptor, float size)
        {
            string familyName = !string.IsNullOrWhiteSpace(descriptor?.SourceFamilyName)
                ? descriptor.SourceFamilyName
                : fontName;

            return new FontDescriptor(familyName, size,
                descriptor?.Weight ?? 400,
                descriptor?.Width ?? 5,
                descriptor?.Slant ?? SKFontStyleSlant.Upright);
        }

        internal static SKTypeface CreateTypefaceFromDescriptor(string familyName, FontStyleDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return SkiaTypefaceService.CreateTypeface(familyName, 400, (int)SKFontStyleWidth.Normal, SKFontStyleSlant.Upright);
            }

            string sourceFamilyName = !string.IsNullOrWhiteSpace(descriptor.SourceFamilyName)
                ? descriptor.SourceFamilyName
                : familyName;

            return SkiaTypefaceService.CreateTypeface(sourceFamilyName, descriptor.Weight, descriptor.Width, descriptor.Slant);
        }

        public static System.Drawing.Font CreateDisplayFont(FontDescriptor selectedFont, float maximumSize)
        {
            if (selectedFont == null)
            {
                return null;
            }

            if (selectedFont.SizePixels <= maximumSize)
            {
                return selectedFont.ToGdiFont();
            }

            FontDescriptor scaled = new FontDescriptor(
                selectedFont.FamilyName,
                maximumSize,
                selectedFont.Weight,
                selectedFont.Width,
                selectedFont.Slant);
            return scaled.ToGdiFont();
        }

        private static List<FontPickerFontEntry> LoadInstalledFontEntries()
        {
            List<FontPickerFontEntry> entries = new List<FontPickerFontEntry>();
            HashSet<string> seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            SKFontManager fontManager = SKFontManager.Default;
            for (int i = 0; i < fontManager.FontFamilyCount; i++)
            {
                string familyName = fontManager.GetFamilyName(i);
                if (string.IsNullOrEmpty(familyName))
                {
                    continue;
                }

                foreach (FontPickerFontEntry entry in FontPickerFontEntry.FromFontFamilyFlattened(familyName))
                {
                    if (entry.HasAnyStyle && seenNames.Add(entry.Name))
                    {
                        entries.Add(entry);
                    }
                }
            }

            entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
            return entries;
        }

        private static bool ContainsFontEntry(List<FontPickerFontEntry> entries, string name)
        {
            return FindEntryIndex(entries, name) >= 0;
        }

        private static int FindEntryIndex(IList<FontPickerFontEntry> entries, string name)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (string.Equals(entries[i].Name, name, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(entries[i].FamilyName, name, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private static bool IsUsableFont(Font font)
        {
            try
            {
                int weight = font.Bold ? 700 : 400;
                SKFontStyleSlant slant = font.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
                using (SKTypeface typeface = SKTypeface.FromFamilyName(font.FontFamily.Name, weight, (int)SKFontStyleWidth.Normal, slant)
                    ?? SKTypeface.FromFamilyName(font.Name, weight, (int)SKFontStyleWidth.Normal, slant))
                {
                    return typeface != null && typeface.GlyphCount > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        [Obsolete("Use FontStyleDescriptor-based overload instead.")]
        private static void AddStyleIfAvailable(
            FontPickerFontEntry entry,
            IList<FontPickerStyleItem> styles,
            FontStyle style,
            string name)
        {
            if (entry.HasStyleMatching(
                (style & FontStyle.Bold) != 0 ? 700 : 400,
                (style & FontStyle.Italic) != 0 ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright))
            {
                styles.Add(new FontPickerStyleItem(name,
                    FontStyleDescriptor.FromLegacyFontStyle(style)));
            }
        }
    }
}