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
        public FontStyleDescriptor(string name, int weight, int width, SKFontStyleSlant slant)
        {
            Name = name;
            Weight = weight;
            Width = width;
            Slant = slant;
        }

        public string Name { get; }
        public int Weight { get; }
        public int Width { get; }
        public SKFontStyleSlant Slant { get; }

        public SKFontStyle ToSKFontStyle() => new SKFontStyle(Weight, Width, Slant);

        public bool Matches(int weight, SKFontStyleSlant slant)
        {
            return Weight == weight && Slant == slant;
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
                return new FontStyleDescriptor(StyleNameFromValues(weight, slant), weight, width, slant);
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

        public static string StyleNameFromValues(int weight, SKFontStyleSlant slant)
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
            string slantName = slant switch
            {
                SKFontStyleSlant.Italic => " Italic",
                SKFontStyleSlant.Oblique => " Oblique",
                _ => ""
            };
            return weightName + slantName;
        }
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
            return SKTypeface.FromFamilyName(FamilyName, Weight, Width, Slant)
                ?? SKTypeface.FromFamilyName(FamilyName);
        }
        public float GetLineSpacing()
        {
            using (SKTypeface tf = CreateTypeface())
            using (SKFont skFont = new SKFont(tf ?? SKTypeface.Default, SizePixels))
            {
                skFont.GetFontMetrics(out SKFontMetrics metrics);
                return -metrics.Ascent + metrics.Descent + metrics.Leading;
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
            if (d != null) return new FontDescriptor(f.FontFamily.Name, f.Size, d.Weight, d.Width, d.Slant);
            return new FontDescriptor(f.FontFamily.Name, f.Size,
                f.Bold ? 700 : 400, 5,
                f.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright);
        }
    }

    internal sealed class FontPickerFontEntry
    {
        private FontPickerFontEntry(string name, List<FontStyleDescriptor> styles)
        {
            Name = name;
            Styles = styles;
        }

        public string Name { get; }
        public List<FontStyleDescriptor> Styles { get; }
        public bool HasAnyStyle => Styles.Count > 0;

        public static FontPickerFontEntry FromFontFamily(string fontName)
        {
            try
            {
                SKFontStyleSet styleSet = SKFontManager.Default.GetFontStyles(fontName);
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
                    int weight = skStyle.Weight;
                    int width = skStyle.Width;
                    SKFontStyleSlant slant = skStyle.Slant;

                    string displayName = FontStyleDescriptor.StyleNameFromValues(weight, slant);
                    string key = $"{weight}-{width}-{slant}";
                    if (!seen.Contains(key))
                    {
                        seen.Add(key);
                        styles.Add(new FontStyleDescriptor(displayName, weight, width, slant));
                    }
                }

                // Always include a "Regular" (weight=400, slant=Upright) if any upright style exists
                if (!styles.Any(s => s.Matches(400, SKFontStyleSlant.Upright))
                    && styles.Any(s => s.Slant == SKFontStyleSlant.Upright))
                {
                    var lightest = styles.Where(s => s.Slant == SKFontStyleSlant.Upright)
                        .OrderBy(s => s.Weight).First();
                }

                // Sort: Upright first, then Italic, then Oblique; within each group, light to heavy
                styles.Sort((a, b) =>
                {
                    int slantOrder = GetSlantOrder(a.Slant).CompareTo(GetSlantOrder(b.Slant));
                    if (slantOrder != 0) return slantOrder;
                    return a.Weight.CompareTo(b.Weight);
                });

                return new FontPickerFontEntry(fontName, styles);
            }
            catch
            {
                return FromLegacy(fontName);
            }
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
                    return new FontPickerFontEntry(fontName, styles);
                }
            }
            catch
            {
                return new FontPickerFontEntry(fontName,
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
                    if (result.Styles[i].Descriptor.Matches(preferredDescriptor.Weight, preferredDescriptor.Slant))
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
            return new FontDescriptor(fontName, size,
                descriptor?.Weight ?? 400,
                descriptor?.Width ?? 5,
                descriptor?.Slant ?? SKFontStyleSlant.Upright);
        }

        internal static SKTypeface CreateTypefaceFromDescriptor(string familyName, FontStyleDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return SKTypeface.FromFamilyName(familyName);
            }
            return SKTypeface.FromFamilyName(familyName, descriptor.ToSKFontStyle())
                ?? SKTypeface.FromFamilyName(familyName);
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
            SKFontManager fontManager = SKFontManager.Default;
            for (int i = 0; i < fontManager.FontFamilyCount; i++)
            {
                string familyName = fontManager.GetFamilyName(i);
                if (string.IsNullOrEmpty(familyName))
                {
                    continue;
                }

                FontPickerFontEntry entry = FontPickerFontEntry.FromFontFamily(familyName);
                if (entry.HasAnyStyle)
                {
                    entries.Add(entry);
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
                if (string.Equals(entries[i].Name, name, StringComparison.OrdinalIgnoreCase))
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
