using System;
using System.Collections.Generic;
using System.Drawing;

namespace DC_Font_Generator
{
    internal sealed class IniLinkMenuState
    {
        public bool Checked { get; set; }
        public bool Enabled { get; set; }
    }

    internal sealed class FontSectionViewState
    {
        public string FontLabel { get; set; }
        public float FontMaxWidth { get; set; }
        public bool FixedFont { get; set; }
        public int Glow { get; set; }
        public int Outline { get; set; }
        public Color GlowColor { get; set; }
        public Color OutlineColor { get; set; }
        public Color FontColor { get; set; }
        public Font SingleByteFont { get; set; }
        public Font DoubleByteFont { get; set; }
        public string SingleByteFontText { get; set; }
        public string DoubleByteFontText { get; set; }
        public string FntName { get; set; }
        public bool CanMoveUp { get; set; }
        public bool CanMoveDown { get; set; }
        public bool CanRemove { get; set; }
        public bool CanAdd { get; set; }
        public List<IniLinkMenuState> IniLinks { get; } = new List<IniLinkMenuState>(8);
        public bool LinkButtonEnabled { get; set; }
        public string LinkLabelText { get; set; }
        public bool LeftSpacingEnabled { get; set; }
        public bool RightSpacingEnabled { get; set; }
        public bool LineSpacingEnabled { get; set; }
    }

    internal sealed class FontSectionNavigationResult
    {
        public int SelectedIndex { get; set; }
        public bool Changed { get; set; }
    }

    internal sealed class FontSectionPickerState
    {
        public Font CurrentFont { get; set; }
        public Font SingleByteFont { get; set; }
        public Font DoubleByteFont { get; set; }
        public bool EditingDoubleByteFont { get; set; }
        public bool AsciiOnly { get; set; }
        public int EncodingCodePage { get; set; }
        public int Glow { get; set; }
        public Color GlowColor { get; set; }
        public int Outline { get; set; }
        public Color OutlineColor { get; set; }
        public Color FontColor { get; set; }
    }

    internal static class FontSectionStateService
    {
        public static FontSectionViewState CreateViewState(IList<Main> sections, int selectedIndex)
        {
            Main selected = GetSection(sections, selectedIndex);
            FontSectionViewState state = new FontSectionViewState
            {
                FontLabel = "Fnt " + (selectedIndex + 1) + "/" + sections.Count,
                FontMaxWidth = selected.FontMaxWidth,
                FixedFont = selected.fixedFont,
                Glow = selected.Glow,
                Outline = selected.Outline,
                GlowColor = selected.GlowColor,
                OutlineColor = selected.OutlineColor,
                FontColor = selected.FontColor,
                SingleByteFont = selected.font1,
                DoubleByteFont = selected.font2,
                SingleByteFontText = GetFontText(selected.ImportFont1name, selected.font1),
                DoubleByteFontText = GetFontText(selected.ImportFont2name, selected.font2),
                FntName = selected.name,
                CanMoveUp = selectedIndex > 0 && sections.Count > 1,
                CanMoveDown = selectedIndex < sections.Count - 1 && sections.Count > 1,
                CanRemove = sections.Count > 1,
                CanAdd = sections.Count < 8,
                LeftSpacingEnabled = !selected.fixedFont,
                RightSpacingEnabled = !selected.fixedFont,
                LineSpacingEnabled = true
            };

            ApplyIniState(sections, selectedIndex, state);
            ApplyLinkState(sections, selectedIndex, state);
            return state;
        }

        public static FontSectionPickerState CreatePickerState(
            IList<Main> sections,
            int selectedIndex,
            bool editingDoubleByteFont,
            FontEncoding encoding)
        {
            Main selected = GetSection(sections, selectedIndex);
            return new FontSectionPickerState
            {
                CurrentFont = editingDoubleByteFont ? selected.font2 : selected.font1,
                SingleByteFont = selected.font1,
                DoubleByteFont = selected.font2,
                EditingDoubleByteFont = editingDoubleByteFont,
                AsciiOnly = encoding.ASCII_Only,
                EncodingCodePage = encoding.enc.CodePage,
                Glow = selected.Glow,
                GlowColor = selected.GlowColor,
                Outline = selected.Outline,
                OutlineColor = selected.OutlineColor,
                FontColor = selected.FontColor
            };
        }

        public static Font GetCurrentFont(Main section, bool doubleByteFont)
        {
            return doubleByteFont ? section.font2 : section.font1;
        }

        public static void ApplySelectedFont(IList<Main> sections, int selectedIndex, bool doubleByteFont, Font font)
        {
            ApplySelectedFont(GetSection(sections, selectedIndex), doubleByteFont, font);
        }

        public static void ApplySelectedFont(Main section, bool doubleByteFont, Font font)
        {
            if (doubleByteFont)
            {
                section.font2 = font;
                section.ImportFont2name = "";
                section.DCfontLink = -1;
            }
            else
            {
                section.font1 = font;
                section.ImportFont1name = "";
            }

            section.Clear();
        }

        public static void ApplyNumericChange(IList<Main> sections, int selectedIndex, string tag, float value, bool clear)
        {
            ApplyNumericChange(GetSection(sections, selectedIndex), tag, value, clear);
        }

        public static void ApplyNumericChange(Main section, string tag, float value, bool clear)
        {
            switch (tag)
            {
                case "Glow":
                    section.Glow = (int)value;
                    if (clear) section.Clear();
                    break;
                case "Outline":
                    section.Outline = (int)value;
                    if (clear) section.Clear();
                    break;
                case "SC_BA":
                case "DC_BA":
                    if (clear) section.Clear();
                    break;
            }
        }

        public static void ApplyEffectColor(IList<Main> sections, int selectedIndex, string tag, Color color)
        {
            ApplyEffectColor(GetSection(sections, selectedIndex), tag, color);
        }

        public static void ApplyEffectColor(Main section, string tag, Color color)
        {
            switch (tag)
            {
                case "Glow":
                    section.GlowColor = color;
                    section.Clear();
                    break;
                case "Outline":
                    section.OutlineColor = color;
                    section.Clear();
                    break;
                case "FontColor":
                    section.FontColor = color;
                    section.Clear();
                    break;
            }
        }

        public static void ApplyFixedFont(IList<Main> sections, int selectedIndex, bool enabled, float maxWidth)
        {
            ApplyFixedFont(GetSection(sections, selectedIndex), enabled, maxWidth);
        }

        public static void ApplyFixedFont(Main section, bool enabled, float maxWidth)
        {
            section.DrawMode = 1;
            section.FixedFont(enabled, maxWidth);
        }

        public static void ApplyFixedFontWidth(IList<Main> sections, int selectedIndex, bool enabled, float maxWidth)
        {
            ApplyFixedFontWidth(GetSection(sections, selectedIndex), enabled, maxWidth);
        }

        public static void ApplyFixedFontWidth(Main section, bool enabled, float maxWidth)
        {
            section.FixedFont(enabled, maxWidth);
            section.Clear();
        }

        public static void SetName(IList<Main> sections, int selectedIndex, string name)
        {
            SetName(GetSection(sections, selectedIndex), name);
        }

        public static void SetName(Main section, string name)
        {
            section.name = name;
        }

        public static void SetIniLink(IList<Main> sections, int selectedIndex, int zeroBasedSlot, bool value)
        {
            SetIniLink(GetSection(sections, selectedIndex), zeroBasedSlot, value);
        }

        public static void SetIniLink(Main section, int zeroBasedSlot, bool value)
        {
            if (zeroBasedSlot >= 0 && zeroBasedSlot < section.Fallout3INI.Count)
            {
                section.Fallout3INI[zeroBasedSlot] = value;
            }
        }

        public static FontSectionNavigationResult Navigate(IList<Main> sections, int selectedIndex, string direction)
        {
            int newIndex = selectedIndex;
            if (direction == "Up")
            {
                newIndex--;
            }
            else if (direction == "Down")
            {
                newIndex++;
            }

            if (sections.Count == 0)
            {
                newIndex = 0;
            }
            else
            {
                if (newIndex < 0) newIndex = 0;
                if (newIndex >= sections.Count) newIndex = sections.Count - 1;
            }

            return new FontSectionNavigationResult
            {
                SelectedIndex = newIndex,
                Changed = newIndex != selectedIndex
            };
        }

        public static int ClampSelectedIndex(IList<Main> sections, int selectedIndex)
        {
            if (sections == null || sections.Count == 0)
            {
                return 0;
            }

            if (selectedIndex < 0) return 0;
            if (selectedIndex >= sections.Count) return sections.Count - 1;
            return selectedIndex;
        }

        public static bool IsTextOverflowSender(object sender)
        {
            Main section = sender as Main;
            return section != null && section.isTextOverFlow;
        }

        private static Main GetSection(IList<Main> sections, int selectedIndex)
        {
            if (sections == null || selectedIndex < 0 || selectedIndex >= sections.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedIndex));
            }

            return sections[selectedIndex];
        }

        private static string GetFontText(string importName, Font font)
        {
            if (importName != "")
            {
                return importName;
            }

            return font.Name + "," + font.Size;
        }

        private static void ApplyIniState(IList<Main> sections, int selectedIndex, FontSectionViewState state)
        {
            Main selected = sections[selectedIndex];
            int[] owner = new int[8];
            for (int i = 0; i < owner.Length; i++)
            {
                owner[i] = -1;
            }

            for (int sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (sections[sectionIndex].Fallout3INI[i])
                    {
                        owner[i] = sectionIndex;
                    }
                }
            }

            for (int i = 0; i < 8; i++)
            {
                state.IniLinks.Add(new IniLinkMenuState
                {
                    Checked = selected.Fallout3INI[i],
                    Enabled = owner[i] == -1 || owner[i] == selectedIndex
                });
            }
        }

        private static void ApplyLinkState(IList<Main> sections, int selectedIndex, FontSectionViewState state)
        {
            state.LinkButtonEnabled = false;
            for (int i = 0; i < sections.Count; i++)
            {
                if (sections[i].DCfontLink == -1 && i != selectedIndex)
                {
                    state.LinkButtonEnabled = true;
                    break;
                }
            }

            Main selected = sections[selectedIndex];
            if (selected.DCfontLink > -1)
            {
                state.LinkLabelText = "Link to : Fnt" + (selected.DCfontLink + 1);
                state.LinkButtonEnabled = true;
            }
        }
    }
}
