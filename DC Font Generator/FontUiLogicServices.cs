using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace DC_Font_Generator
{
    internal sealed class HexKeyInputResult
    {
        public bool Handled { get; set; }
        public char KeyChar { get; set; }
        public bool ClearExistingText { get; set; }
    }

    internal sealed class TextureSizeSelectionResult
    {
        public Size Size { get; set; }
        public int SizeXIndex { get; set; } = -1;
        public int SizeYIndex { get; set; } = -1;
        public bool Success => SizeXIndex >= 0 && SizeYIndex >= 0;
    }

    internal sealed class EncodingSelectionResult
    {
        public int CharactersCount { get; set; }
        public bool HasSelection { get; set; }
        public bool DoubleByteFontEnabled { get; set; }
    }

    internal static class EncodingSelectionService
    {
        public static EncodingSelectionResult Select(FontEncoding encoding, int index)
        {
            EncodingSelectionResult result = new EncodingSelectionResult();
            result.CharactersCount = encoding.SwitchEnc(index);
            result.HasSelection = index >= 0;
            result.DoubleByteFontEnabled = index != 0 && index != 6;
            return result;
        }
    }

    internal static class EncodingInputService
    {
        public static string TextToHex(string text, Encoding encoding)
        {
            if (string.IsNullOrEmpty(text)) return "";

            byte[] bytes = encoding.GetBytes(text[0].ToString());
            if (bytes.Length == 1)
            {
                return string.Format("{0:X2}", bytes[0]);
            }

            if (bytes.Length > 1)
            {
                return string.Format("{0:X2}{1:X2}", bytes[0], bytes[1]);
            }

            return "";
        }

        public static string HexToText(string hex, Encoding encoding)
        {
            int len = hex.Length;
            if (len != 4 && len != 2)
            {
                return "";
            }

            int value;
            if (!int.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value))
            {
                return "";
            }
            byte[] buffer = len == 4
                ? new[] { (byte)(value / 0x100), (byte)(value % 0x100) }
                : new[] { (byte)(value % 0x100) };
            return encoding.GetChars(buffer)[0].ToString();
        }

        public static HexKeyInputResult EvaluateHexKey(char keyChar, string currentText)
        {
            if (keyChar == (char)8)
            {
                return new HexKeyInputResult { Handled = false, KeyChar = keyChar };
            }

            char normalized = char.ToUpperInvariant(keyChar);
            bool isHex = (normalized >= '0' && normalized <= '9') || (normalized >= 'A' && normalized <= 'F');
            return new HexKeyInputResult
            {
                Handled = !isHex,
                KeyChar = normalized,
                ClearExistingText = currentText.Length > 3
            };
        }
    }

    internal static class TextureSizeSelectionService
    {
        public static Size GetSelectedSize(TexSize width, TexSize height)
        {
            return new Size(width.size, height.size);
        }

        public static TextureSizeSelectionResult FindSize(IList<TexSize> widths, IList<TexSize> heights, Size imageSize)
        {
            TextureSizeSelectionResult result = new TextureSizeSelectionResult { Size = imageSize };
            for (int i = 0; i < widths.Count; i++)
            {
                if (widths[i].size == imageSize.Width)
                {
                    result.SizeXIndex = i;
                    break;
                }
            }

            for (int i = 0; i < heights.Count; i++)
            {
                if (heights[i].size == imageSize.Height)
                {
                    result.SizeYIndex = i;
                    break;
                }
            }

            return result;
        }

        public static FontAtlasRequest CreateAtlasRequest(
            IList<Main> fontSections,
            FontEncoding encoding,
            IList<TexSize> widths,
            IList<TexSize> heights,
            int currentWidthIndex,
            int currentHeightIndex,
            int gap,
            int arrangeMethod,
            Color backgroundColor)
        {
            return new FontAtlasRequest
            {
                FontSections = fontSections,
                Encoding = encoding,
                CandidateWidths = ToIntSizes(widths),
                CandidateHeights = ToIntSizes(heights),
                CurrentWidthIndex = currentWidthIndex,
                CurrentHeightIndex = currentHeightIndex,
                Gap = gap,
                ArrangeMode = ToFontArrangeMode(arrangeMethod),
                BackgroundColor = backgroundColor
            };
        }

        public static FontArrangeMode ToFontArrangeMode(int arrangeMethod)
        {
            if (arrangeMethod == 1) return FontArrangeMode.Width;
            if (arrangeMethod == 2) return FontArrangeMode.Code;
            return FontArrangeMode.Height;
        }

        private static List<int> ToIntSizes(IList<TexSize> texSizes)
        {
            List<int> sizes = new List<int>(texSizes.Count);
            foreach (TexSize size in texSizes)
            {
                sizes.Add(size.size);
            }

            return sizes;
        }
    }

    internal static class ProjectRequestFactory
    {
        public static int GetArrangeMethod(bool widthOrdered, bool codeOrdered)
        {
            if (widthOrdered) return 1;
            if (codeOrdered) return 2;
            return 0;
        }

        public static ProjectArrangeSelection GetArrangeSelection(int arrangeMethod)
        {
            return new ProjectArrangeSelection
            {
                HeightOrdered = arrangeMethod == 0,
                WidthOrdered = arrangeMethod == 1,
                CodeOrdered = arrangeMethod == 2
            };
        }

        public static Color GetBackgroundColor(int argb)
        {
            if (argb == Color.FromArgb(0xFF, Color.Black).ToArgb())
            {
                return Color.FromArgb(0, Color.Black);
            }

            return Color.FromArgb(argb);
        }

        public static ProjectSaveRequest CreateSaveRequest(
            int encodingIndex,
            int sizeXIndex,
            int sizeYIndex,
            string texFileName,
            decimal gap,
            int backgroundColorArgb,
            int arrangeMethod,
            IList<Main> fontSections)
        {
            return new ProjectSaveRequest
            {
                EncodingIndex = encodingIndex,
                SizeXIndex = sizeXIndex,
                SizeYIndex = sizeYIndex,
                TexFileName = texFileName,
                Gap = gap,
                BackGroundColorArgb = backgroundColorArgb,
                ArrangeMethod = arrangeMethod,
                FontSections = fontSections
            };
        }
    }

    internal sealed class ProjectArrangeSelection
    {
        public bool HeightOrdered { get; set; }
        public bool WidthOrdered { get; set; }
        public bool CodeOrdered { get; set; }
    }
}
