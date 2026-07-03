using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using SkiaSharp;

namespace DC_Font_Generator
{
    internal sealed class FontPickerFontEntry
    {
        private readonly bool regular;
        private readonly bool bold;
        private readonly bool italic;
        private readonly bool boldItalic;

        private FontPickerFontEntry(string name, bool regular, bool bold, bool italic, bool boldItalic)
        {
            Name = name;
            this.regular = regular;
            this.bold = bold;
            this.italic = italic;
            this.boldItalic = boldItalic;
        }

        public string Name { get; }
        public bool HasAnyStyle => regular || bold || italic || boldItalic;

        public static FontPickerFontEntry FromFontFamily(string fontName)
        {
            try
            {
                using (FontFamily family = new FontFamily(fontName))
                {
                    return FromFamily(family);
                }
            }
            catch
            {
                return new FontPickerFontEntry(fontName, true, false, false, false);
            }
        }

        public static FontPickerFontEntry FromFamily(FontFamily family)
        {
            return new FontPickerFontEntry(
                family.Name,
                family.IsStyleAvailable(FontStyle.Regular),
                family.IsStyleAvailable(FontStyle.Bold),
                family.IsStyleAvailable(FontStyle.Italic),
                family.IsStyleAvailable(FontStyle.Bold | FontStyle.Italic));
        }

        public bool IsStyleAvailable(FontStyle style)
        {
            switch (style)
            {
                case FontStyle.Regular:
                    return regular;
                case FontStyle.Bold:
                    return bold;
                case FontStyle.Italic:
                    return italic;
                case FontStyle.Bold | FontStyle.Italic:
                    return boldItalic;
                default:
                    return false;
            }
        }
    }

    internal sealed class FontPickerStyleItem
    {
        public FontPickerStyleItem(string name, FontStyle style)
        {
            Name = name;
            Style = style;
        }

        public string Name { get; }
        public FontStyle Style { get; }

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

        public static FontPickerStyleResult GetStyles(FontPickerFontEntry entry, FontStyle preferredStyle)
        {
            FontPickerStyleResult result = new FontPickerStyleResult();
            AddStyleIfAvailable(entry, result.Styles, FontStyle.Regular, "Regular");
            AddStyleIfAvailable(entry, result.Styles, FontStyle.Bold, "Bold");
            AddStyleIfAvailable(entry, result.Styles, FontStyle.Italic, "Italic");
            AddStyleIfAvailable(entry, result.Styles, FontStyle.Bold | FontStyle.Italic, "Bold Italic");

            if (result.Styles.Count == 0)
            {
                result.Styles.Add(new FontPickerStyleItem("Regular", FontStyle.Regular));
            }

            for (int i = 0; i < result.Styles.Count; i++)
            {
                if (result.Styles[i].Style == preferredStyle)
                {
                    result.SelectedIndex = i;
                    break;
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

        public static Font CreateSelectedFont(string fontName, FontStyle style, float size)
        {
            try
            {
                Font font = new Font(fontName, size, style, GraphicsUnit.Pixel);
                if (!IsUsableFont(font))
                {
                    font.Dispose();
                    return null;
                }

                return font;
            }
            catch
            {
                return null;
            }
        }

        public static Font CreateDisplayFont(Font selectedFont, float maximumSize)
        {
            if (selectedFont == null)
            {
                return null;
            }

            if (selectedFont.Size <= maximumSize)
            {
                return selectedFont;
            }

            return new Font(selectedFont.FontFamily, maximumSize, selectedFont.Style, GraphicsUnit.Pixel);
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
                SKFontStyleWeight weight = font.Bold ? SKFontStyleWeight.Bold : SKFontStyleWeight.Normal;
                SKFontStyleSlant slant = font.Italic ? SKFontStyleSlant.Italic : SKFontStyleSlant.Upright;
                using (SKTypeface typeface = SKTypeface.FromFamilyName(font.FontFamily.Name, weight, SKFontStyleWidth.Normal, slant)
                    ?? SKTypeface.FromFamilyName(font.Name, weight, SKFontStyleWidth.Normal, slant))
                {
                    return typeface != null && typeface.GlyphCount > 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private static void AddStyleIfAvailable(
            FontPickerFontEntry entry,
            IList<FontPickerStyleItem> styles,
            FontStyle style,
            string name)
        {
            if (entry.IsStyleAvailable(style))
            {
                styles.Add(new FontPickerStyleItem(name, style));
            }
        }
    }
}
