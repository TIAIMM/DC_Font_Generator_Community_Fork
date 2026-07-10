using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkiaSharp;

namespace DC_Font_Generator
{
    public sealed class FontStyleDescriptor
    {
        private const string SerializationVersion = "v2";

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
            SourceFamilyName = string.IsNullOrWhiteSpace(sourceFamilyName)
                ? null
                : sourceFamilyName.Trim();
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

            if (Weight != other.Weight || Width != other.Width || Slant != other.Slant)
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(Name)
                || string.IsNullOrWhiteSpace(other.Name)
                || string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
        }

        public string Serialize()
        {
            return string.Join("|",
                SerializationVersion,
                "w=" + Weight,
                "wid=" + Width,
                "sl=" + (int)Slant,
                "idx=" + StyleSetIndex,
                "family=" + Encode(SourceFamilyName),
                "name=" + Encode(Name));
        }

        public static FontStyleDescriptor Deserialize(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            try
            {
                if (key.StartsWith(SerializationVersion + "|", StringComparison.Ordinal))
                {
                    return DeserializeVersion2(key);
                }

                return DeserializeLegacy(key);
            }
            catch
            {
                return null;
            }
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

        public static FontStyleDescriptor FromFontDescriptor(FontDescriptor font)
        {
            if (font == null)
            {
                return null;
            }

            return new FontStyleDescriptor(
                font.StyleName,
                font.Weight,
                font.Width,
                font.Slant,
                font.StyleSetIndex,
                font.FamilyName);
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
            return !string.IsNullOrWhiteSpace(skiaStyleName)
                ? skiaStyleName.Trim()
                : StyleNameFromValues(weight, width, slant);
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
                950 => "ExtraBlack",
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

        private static FontStyleDescriptor DeserializeVersion2(string key)
        {
            int weight = 400;
            int width = (int)SKFontStyleWidth.Normal;
            int styleIndex = -1;
            SKFontStyleSlant slant = SKFontStyleSlant.Upright;
            string familyName = null;
            string styleName = null;

            string[] parts = key.Split('|');
            for (int i = 1; i < parts.Length; i++)
            {
                int separator = parts[i].IndexOf('=');
                if (separator <= 0)
                {
                    continue;
                }

                string name = parts[i].Substring(0, separator);
                string value = parts[i].Substring(separator + 1);
                switch (name)
                {
                    case "w":
                        if (int.TryParse(value, out int parsedWeight)) weight = parsedWeight;
                        break;
                    case "wid":
                        if (int.TryParse(value, out int parsedWidth)) width = parsedWidth;
                        break;
                    case "sl":
                        if (int.TryParse(value, out int slantValue))
                        {
                            slant = (SKFontStyleSlant)slantValue;
                        }
                        break;
                    case "idx":
                        if (int.TryParse(value, out int parsedIndex)) styleIndex = parsedIndex;
                        break;
                    case "family":
                        familyName = Decode(value);
                        break;
                    case "name":
                        styleName = Decode(value);
                        break;
                }
            }

            return new FontStyleDescriptor(
                styleName,
                weight,
                width,
                slant,
                styleIndex,
                familyName);
        }

        private static FontStyleDescriptor DeserializeLegacy(string key)
        {
            int weight = 400;
            int width = (int)SKFontStyleWidth.Normal;
            SKFontStyleSlant slant = SKFontStyleSlant.Upright;
            foreach (string part in key.Split('-'))
            {
                if (part.StartsWith("wid", StringComparison.Ordinal)
                    && int.TryParse(part.Substring(3), out int parsedWidth))
                {
                    width = parsedWidth;
                }
                else if (part.StartsWith("w", StringComparison.Ordinal)
                    && int.TryParse(part.Substring(1), out int parsedWeight))
                {
                    weight = parsedWeight;
                }
                else if (Enum.TryParse(part, true, out SKFontStyleSlant parsedSlant))
                {
                    slant = parsedSlant;
                }
            }

            return new FontStyleDescriptor(StyleNameFromValues(weight, width, slant), weight, width, slant);
        }

        private static string Encode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }

            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        private static string Decode(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
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
        public FontDescriptor(
            string familyName,
            float sizePixels,
            int weight = 400,
            int width = 5,
            SKFontStyleSlant slant = SKFontStyleSlant.Upright,
            int styleSetIndex = -1,
            string styleName = null)
        {
            FamilyName = familyName;
            SizePixels = sizePixels;
            Weight = weight;
            Width = width;
            Slant = slant;
            StyleSetIndex = styleSetIndex;
            StyleName = styleName;
        }

        public string FamilyName { get; }
        public float SizePixels { get; }
        public int Weight { get; }
        public int Width { get; }
        public SKFontStyleSlant Slant { get; }
        public int StyleSetIndex { get; }
        public string StyleName { get; }
        public bool HasExactStyleSetFace => StyleSetIndex >= 0 && !string.IsNullOrWhiteSpace(FamilyName);
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
                return new FontDescriptor(familyName, f.Size, d.Weight, d.Width, d.Slant, d.StyleSetIndex, d.Name);
            }

            return new FontDescriptor(
                f.FontFamily.Name,
                f.Size,
                f.Bold ? 700 : 400,
                (int)SKFontStyleWidth.Normal,
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
                        return FromLegacy(fontName);
                    }

                    List<FontStyleDescriptor> styles = new List<FontStyleDescriptor>(styleSet.Count);
                    for (int i = 0; i < styleSet.Count; i++)
                    {
                        string styleName = styleSet.GetStyleName(i);
                        SKFontStyle skStyle = styleSet[i];
                        styles.Add(CreateDescriptor(fontName, i, styleName, skStyle));
                    }

                    styles.Sort(CompareStyles);
                    return new FontPickerFontEntry(fontName, fontName, styles);
                }
            }
            catch
            {
                return FromLegacy(fontName);
            }
        }

        public FontStyleDescriptor ResolveStyle(FontStyleDescriptor preferred)
        {
            if (preferred == null || Styles.Count == 0)
            {
                return Styles.Count > 0 ? Styles[0] : null;
            }

            if (preferred.HasExactStyleSetFace)
            {
                FontStyleDescriptor indexed = Styles.FirstOrDefault(style =>
                    style.StyleSetIndex == preferred.StyleSetIndex
                    && string.Equals(style.SourceFamilyName, preferred.SourceFamilyName, StringComparison.OrdinalIgnoreCase));
                if (indexed != null
                    && indexed.Weight == preferred.Weight
                    && indexed.Width == preferred.Width
                    && indexed.Slant == preferred.Slant
                    && NamesCompatible(indexed.Name, preferred.Name))
                {
                    return indexed;
                }
            }

            FontStyleDescriptor nameAndValues = Styles.FirstOrDefault(style =>
                style.Weight == preferred.Weight
                && style.Width == preferred.Width
                && style.Slant == preferred.Slant
                && NamesEqual(style.Name, preferred.Name));
            if (nameAndValues != null)
            {
                return nameAndValues;
            }

            List<FontStyleDescriptor> valueMatches = Styles.Where(style =>
                style.Weight == preferred.Weight
                && style.Width == preferred.Width
                && style.Slant == preferred.Slant).ToList();
            if (valueMatches.Count == 1)
            {
                return valueMatches[0];
            }

            if (!string.IsNullOrWhiteSpace(preferred.Name))
            {
                FontStyleDescriptor nameMatch = Styles.FirstOrDefault(style => NamesEqual(style.Name, preferred.Name));
                if (nameMatch != null)
                {
                    return nameMatch;
                }
            }

            return valueMatches.Count > 0 ? valueMatches[0] : null;
        }

        public bool HasStyleMatching(int weight, SKFontStyleSlant slant)
        {
            return Styles.Any(style => style.Matches(weight, slant));
        }

        private static FontStyleDescriptor CreateDescriptor(string familyName, int styleIndex, string styleName, SKFontStyle skStyle)
        {
            int weight = skStyle.Weight;
            int width = skStyle.Width;
            SKFontStyleSlant slant = skStyle.Slant;
            string displayName = FontStyleDescriptor.StyleNameFromSkia(styleName, weight, width, slant);
            return new FontStyleDescriptor(displayName, weight, width, slant, styleIndex, familyName);
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
                return new FontPickerFontEntry(
                    fontName,
                    fontName,
                    new List<FontStyleDescriptor> { FontStyleDescriptor.FromLegacyFontStyle(FontStyle.Regular) });
            }
        }

        private static int CompareStyles(FontStyleDescriptor left, FontStyleDescriptor right)
        {
            int slantOrder = GetSlantOrder(left.Slant).CompareTo(GetSlantOrder(right.Slant));
            if (slantOrder != 0) return slantOrder;
            int widthOrder = left.Width.CompareTo(right.Width);
            if (widthOrder != 0) return widthOrder;
            int weightOrder = left.Weight.CompareTo(right.Weight);
            if (weightOrder != 0) return weightOrder;
            int nameOrder = string.Compare(left.Name, right.Name, StringComparison.CurrentCultureIgnoreCase);
            if (nameOrder != 0) return nameOrder;
            return left.StyleSetIndex.CompareTo(right.StyleSetIndex);
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

        private static bool NamesEqual(string left, string right)
        {
            return !string.IsNullOrWhiteSpace(left)
                && !string.IsNullOrWhiteSpace(right)
                && string.Equals(left.Trim(), right.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static bool NamesCompatible(string left, string right)
        {
            return string.IsNullOrWhiteSpace(left)
                || string.IsNullOrWhiteSpace(right)
                || NamesEqual(left, right);
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
            Dictionary<string, int> nameCounts = entry.Styles
                .GroupBy(style => style.Name ?? "", StringComparer.CurrentCultureIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Count(), StringComparer.CurrentCultureIgnoreCase);

            foreach (FontStyleDescriptor style in entry.Styles)
            {
                string displayName = style.Name;
                if (nameCounts.TryGetValue(style.Name ?? "", out int count) && count > 1)
                {
                    displayName = $"{style.Name} (face {style.StyleSetIndex})";
                }
                result.Styles.Add(new FontPickerStyleItem(displayName, style));
            }

            if (result.Styles.Count == 0)
            {
                FontStyleDescriptor fallback = FontStyleDescriptor.FromLegacyFontStyle(FontStyle.Regular);
                result.Styles.Add(new FontPickerStyleItem(fallback.Name, fallback));
            }

            FontStyleDescriptor resolved = entry.ResolveStyle(preferredDescriptor);
            if (resolved != null)
            {
                for (int i = 0; i < result.Styles.Count; i++)
                {
                    if (ReferenceEquals(result.Styles[i].Descriptor, resolved)
                        || result.Styles[i].Descriptor.Matches(resolved))
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
            if (!entries.TryGetValue(fontName, out FontPickerFontEntry entry))
            {
                entry = FontPickerFontEntry.FromFontFamily(fontName);
                entries[fontName] = entry;
            }

            return entry;
        }

        public static FontStyleDescriptor ResolveDescriptor(string fontName, FontStyleDescriptor preferredDescriptor)
        {
            string sourceFamilyName = !string.IsNullOrWhiteSpace(preferredDescriptor?.SourceFamilyName)
                ? preferredDescriptor.SourceFamilyName
                : fontName;
            FontPickerFontEntry entry = FontPickerFontEntry.FromFontFamily(sourceFamilyName);
            return entry.ResolveStyle(preferredDescriptor);
        }

        public static decimal ClampFontSize(float size, decimal minimum, decimal maximum)
        {
            decimal value = (decimal)Math.Round(size);
            if (value < minimum) return minimum;
            if (value > maximum) return maximum;
            return value;
        }

        public static FontDescriptor CreateSelectedFont(string fontName, FontStyleDescriptor descriptor, float size)
        {
            string familyName = !string.IsNullOrWhiteSpace(descriptor?.SourceFamilyName)
                ? descriptor.SourceFamilyName
                : fontName;

            return new FontDescriptor(
                familyName,
                size,
                descriptor?.Weight ?? 400,
                descriptor?.Width ?? (int)SKFontStyleWidth.Normal,
                descriptor?.Slant ?? SKFontStyleSlant.Upright,
                descriptor?.StyleSetIndex ?? -1,
                descriptor?.Name);
        }

        internal static SKTypeface CreateTypefaceFromDescriptor(string familyName, FontStyleDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return SkiaTypefaceService.CreateTypeface(
                    familyName,
                    400,
                    (int)SKFontStyleWidth.Normal,
                    SKFontStyleSlant.Upright);
            }

            string sourceFamilyName = !string.IsNullOrWhiteSpace(descriptor.SourceFamilyName)
                ? descriptor.SourceFamilyName
                : familyName;
            FontDescriptor font = new FontDescriptor(
                sourceFamilyName,
                12f,
                descriptor.Weight,
                descriptor.Width,
                descriptor.Slant,
                descriptor.StyleSetIndex,
                descriptor.Name);
            return SkiaTypefaceService.CreateTypeface(font, descriptor);
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
                selectedFont.Slant,
                selectedFont.StyleSetIndex,
                selectedFont.StyleName);
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
                if (string.IsNullOrEmpty(familyName) || !seenNames.Add(familyName))
                {
                    continue;
                }

                FontPickerFontEntry entry = FontPickerFontEntry.FromFontFamily(familyName);
                if (entry.HasAnyStyle)
                {
                    entries.Add(entry);
                }
            }

            entries.Sort((left, right) =>
                string.Compare(left.Name, right.Name, StringComparison.CurrentCultureIgnoreCase));
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
    }
}
