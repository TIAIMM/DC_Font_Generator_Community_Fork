using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Globalization;

namespace DC_Font_Generator
{
    internal enum GlyphAdjustmentTarget
    {
        None,
        LeftSpacing,
        RightSpacing,
        LineSpacing,
        BottomAlign,
        Scale
    }

    internal sealed class GlyphHitResult
    {
        public bool HasGlyph { get; set; }
        public char Character { get; set; }
        public string Hex { get; set; }
        public Fnt_char AtlasGlyph { get; set; }
        public Fnt_char EditableGlyph { get; set; }
        public RectangleF Bounds { get; set; }
    }

    internal sealed class GlyphSelectionState
    {
        public List<Fnt_char> Selected { get; } = new List<Fnt_char>();
        public List<Fnt_char> RangeSelected { get; } = new List<Fnt_char>();
        public List<Fnt_char> Removed { get; } = new List<Fnt_char>();

        public bool HasAdjustableSelection => Selected.Count > 0 || RangeSelected.Count > 0;

        public void Clear()
        {
            Selected.Clear();
            RangeSelected.Clear();
            Removed.Clear();
        }

        public void ClearSelection()
        {
            Selected.Clear();
            RangeSelected.Clear();
        }

        public bool ApplyRemoved(FontEncoding encoding)
        {
            bool changedBandList = false;
            foreach (Fnt_char fnt in Removed)
            {
                if (!encoding.IsBand(fnt.HEX))
                {
                    encoding.AddBand = fnt.HEX;
                    changedBandList = true;
                }
                fnt.Enable = false;
            }

            Removed.Clear();
            Selected.Clear();
            RangeSelected.Clear();
            return changedBandList;
        }

        public void Toggle(Fnt_char fnt, bool remove)
        {
            List<Fnt_char> list = remove ? Removed : Selected;
            if (list.Contains(fnt))
            {
                list.Remove(fnt);
            }
            else
            {
                list.Add(fnt);
            }
        }

        public void SelectRange(FL_FONT fontFile, string startHex, string endHex, bool includeSingleByte, bool includeDoubleByte)
        {
            RangeSelected.Clear();
            if (startHex == "") startHex = endHex;
            if (endHex == "") endHex = startHex;
            if (startHex == "" && endHex == "" && !includeSingleByte && !includeDoubleByte) return;

            int start = 0;
            int end = 0;
            bool hasRange = startHex != "" || endHex != "";
            if (hasRange)
            {
                start = int.Parse(startHex, NumberStyles.AllowHexSpecifier);
                end = int.Parse(endHex, NumberStyles.AllowHexSpecifier);
                if (start > end)
                {
                    int tmp = start;
                    start = end;
                    end = tmp;
                }
            }

            foreach (Fnt_char fnt in fontFile.CharList)
            {
                if (!fnt.Enable) continue;
                int hex = int.Parse(fnt.HEX, NumberStyles.AllowHexSpecifier);
                bool select = hasRange && hex >= start && hex <= end;
                if (!select && includeSingleByte && hex >= 0 && hex <= 255)
                {
                    select = true;
                }
                if (!select && includeDoubleByte && hex >= 256 && hex <= 65535)
                {
                    select = true;
                }

                if (select && !Selected.Contains(fnt) && !RangeSelected.Contains(fnt))
                {
                    RangeSelected.Add(fnt);
                }
            }
        }

        public void SetSingleByteSelection(FL_FONT fontFile, bool selected)
        {
            int max = Math.Min(256, fontFile.CharList.Count);
            for (int i = 0; i < max; i++)
            {
                Fnt_char fnt = fontFile.CharList[i];
                SetRangeItem(fnt, selected);
            }
        }

        public void SetDoubleByteSelection(FL_FONT fontFile, bool selected)
        {
            for (int i = 256; i < fontFile.CharList.Count; i++)
            {
                Fnt_char fnt = fontFile.CharList[i];
                SetRangeItem(fnt, selected);
            }
        }

        public List<Fnt_char> GetAdjustableFonts()
        {
            List<Fnt_char> fonts = new List<Fnt_char>();
            AddUnique(fonts, Selected);
            AddUnique(fonts, RangeSelected);
            return fonts;
        }

        private void SetRangeItem(Fnt_char fnt, bool selected)
        {
            if (!fnt.Enable) return;
            if (selected)
            {
                if (!RangeSelected.Contains(fnt))
                {
                    RangeSelected.Add(fnt);
                }
            }
            else
            {
                RangeSelected.Remove(fnt);
            }
        }

        private static void AddUnique(List<Fnt_char> target, IEnumerable<Fnt_char> source)
        {
            foreach (Fnt_char fnt in source)
            {
                if (!target.Contains(fnt))
                {
                    target.Add(fnt);
                }
            }
        }
    }

    internal static class GlyphSelectionService
    {
        public static GlyphHitResult HitTest(
            Array2D.List2D<Fnt_char> charIndex,
            int x,
            int y,
            Size textImageSize,
            IList<Main> mainList,
            int mainSelect)
        {
            if (x < 0 || y < 0)
            {
                return new GlyphHitResult();
            }

            Fnt_char atlasGlyph = charIndex[x, y];
            if (atlasGlyph == null)
            {
                return new GlyphHitResult();
            }

            string hex = atlasGlyph.HEX;
            Fnt_char editableGlyph = atlasGlyph;
            if (atlasGlyph.ID != mainSelect)
            {
                Fnt_char linkedGlyph = mainList[mainSelect].FntFile.GetFntFromHEX(hex);
                if (linkedGlyph.Enable)
                {
                    editableGlyph = linkedGlyph;
                }
            }

            float rx = atlasGlyph.x1 * textImageSize.Width;
            float ry = atlasGlyph.y1 * textImageSize.Height;
            float bx = atlasGlyph.x4 * textImageSize.Width;
            float by = atlasGlyph.y4 * textImageSize.Height;

            return new GlyphHitResult
            {
                HasGlyph = true,
                Character = atlasGlyph.c,
                Hex = hex,
                AtlasGlyph = atlasGlyph,
                EditableGlyph = editableGlyph,
                Bounds = new RectangleF(rx, ry, bx - rx, by - ry)
            };
        }
    }

    internal static class GlyphAdjustmentService
    {
        public static bool Apply(Main main, IList<Fnt_char> fonts, GlyphAdjustmentTarget target, float delta)
        {
            if (target == GlyphAdjustmentTarget.None) return false;

            if (target == GlyphAdjustmentTarget.LineSpacing)
            {
                main.FntFile.Header.LineHeight += delta;
                main.FntFile.Header.LineHeightFixed += delta;
                return true;
            }

            foreach (Fnt_char fnt in fonts)
            {
                ApplyToGlyph(fnt, target, delta);
            }

            if (main.fixedFont)
            {
                main.FixedFont(main.fixedFont, main.FontMaxWidth);
            }

            return true;
        }

        public static void Restore(Main main, IList<Fnt_char> fonts)
        {
            main.FntFile.Header.LineHeight -= main.FntFile.Header.LineHeightFixed;
            main.FntFile.Header.LineHeightFixed = 0;

            foreach (Fnt_char fnt in fonts)
            {
                if (!fnt.Enable) continue;
                fnt.LeftSpace -= fnt.LeftSpaceFixed;
                fnt.LeftSpaceFixed = 0;
                fnt.RightSpace -= fnt.RightSpaceFixed;
                fnt.RightSpaceFixed = 0;
                fnt.BottomAlign -= fnt.BottomAlignFixed;
                fnt.BottomAlignFixed = 0;
                fnt.charViewHeight -= fnt.charViewHeightFixed;
                fnt.charViewHeightFixed = 0;
                fnt.charViewWidth -= fnt.charViewWidthFixed;
                fnt.charViewWidthFixed = 0;
            }
        }

        private static void ApplyToGlyph(Fnt_char fnt, GlyphAdjustmentTarget target, float delta)
        {
            switch (target)
            {
                case GlyphAdjustmentTarget.LeftSpacing:
                    fnt.LeftSpace += delta;
                    fnt.LeftSpaceFixed += delta;
                    break;
                case GlyphAdjustmentTarget.RightSpacing:
                    fnt.RightSpace += delta;
                    fnt.RightSpaceFixed += delta;
                    break;
                case GlyphAdjustmentTarget.BottomAlign:
                    fnt.BottomAlign += delta;
                    fnt.BottomAlignFixed += delta;
                    break;
                case GlyphAdjustmentTarget.Scale:
                    if (fnt.charViewHeight + delta > 0 && fnt.charViewWidth + delta > 0)
                    {
                        fnt.charViewHeight += delta;
                        fnt.charViewWidth += delta;
                        fnt.charViewHeightFixed += delta;
                        fnt.charViewWidthFixed += delta;
                        fnt.BottomAlign += delta;
                        fnt.BottomAlignFixed += delta;
                    }
                    break;
            }
        }
    }

    internal static class GlyphOverlayRenderer
    {
        public static Bitmap CreateMask(
            Bitmap textImage,
            Size textImageSize,
            IEnumerable<Fnt_char> selected,
            IEnumerable<Fnt_char> rangeSelected,
            IEnumerable<Fnt_char> removed)
        {
            Bitmap mask = (Bitmap)textImage.Clone();
            using (Graphics graphics = Graphics.FromImage(mask))
            {
                ConfigureOverlayGraphics(graphics);
                foreach (Fnt_char fnt in selected)
                {
                    DrawGlyphRectangle(graphics, fnt, textImageSize, false);
                }
                foreach (Fnt_char fnt in rangeSelected)
                {
                    DrawGlyphRectangle(graphics, fnt, textImageSize, false);
                }
                foreach (Fnt_char fnt in removed)
                {
                    DrawGlyphRectangle(graphics, fnt, textImageSize, true);
                }
            }

            return mask;
        }

        public static void DrawFocus(Graphics graphics, GlyphHitResult hit)
        {
            if (!hit.HasGlyph) return;
            using (Pen red = new Pen(Color.Red, 1f))
            {
                graphics.DrawRectangle(red, hit.Bounds.X, hit.Bounds.Y, hit.Bounds.Width, hit.Bounds.Height);
            }

            float baselineY = hit.Bounds.Y + hit.EditableGlyph.BottomAlign;
            graphics.DrawLine(Pens.Yellow, hit.Bounds.X + 1, baselineY, hit.Bounds.Right - 1, baselineY);
        }

        public static string FormatTooltip(string format, GlyphHitResult hit, float lineHeight, float lineHeightFixed)
        {
            return string.Format(
                format,
                hit.Character,
                hit.Hex,
                hit.EditableGlyph.charViewWidth,
                hit.EditableGlyph.charViewHeight,
                lineHeight,
                lineHeightFixed,
                hit.EditableGlyph.BottomAlign,
                hit.EditableGlyph.LeftSpace,
                hit.EditableGlyph.RightSpace,
                hit.Bounds.Width,
                hit.Bounds.Height,
                hit.EditableGlyph.ID);
        }

        private static void DrawGlyphRectangle(Graphics graphics, Fnt_char fnt, Size textImageSize, bool removed)
        {
            float rx = fnt.x1 * textImageSize.Width;
            float ry = fnt.y1 * textImageSize.Height;
            float bx = fnt.x4 * textImageSize.Width;
            float by = fnt.y4 * textImageSize.Height;

            using (Pen red = new Pen(Color.Red, 1f))
            {
                if (removed)
                {
                    graphics.DrawLine(red, new PointF(rx, ry), new PointF(bx, by));
                    graphics.DrawLine(red, new PointF(bx, ry), new PointF(rx, by));
                }
                else
                {
                    graphics.DrawRectangle(red, rx, ry, bx - rx, by - ry);
                }
            }
        }

        private static void ConfigureOverlayGraphics(Graphics graphics)
        {
            graphics.PageUnit = GraphicsUnit.Pixel;
            graphics.CompositingQuality = CompositingQuality.HighSpeed;
            graphics.InterpolationMode = InterpolationMode.Bicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.None;
            graphics.SmoothingMode = SmoothingMode.None;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        }
    }

    internal static class GlyphPreviewRenderer
    {
        public static void Render(Bitmap target, string text, Main main, IList<Main> fontSections)
        {
            using (Graphics graphics = Graphics.FromImage(target))
            {
                ConfigurePreviewGraphics(graphics);
                graphics.Clear(Color.FromArgb(0, Color.Black));
                if (string.IsNullOrEmpty(text)) return;

                float lineHeight = main.FntFile.Header.LineHeight;
                PointF point = new PointF(0, 0);
                char[] chars = text.ToCharArray();
                Fnt_char lastFnt = main.FntFile.GetFntFromChar(' ');
                int linePoint = (int)lineHeight;

                for (int i = 0; i < chars.Length; i++)
                {
                    point.X += lastFnt.charViewWidth + lastFnt.RightSpace;
                    Fnt_char fnt = main.FntFile.GetFntFromChar(chars[i]);
                    point.X += fnt.LeftSpace;
                    if (point.X > target.Width)
                    {
                        point.X = 0;
                        linePoint += (int)lineHeight;
                    }

                    point.Y = linePoint - fnt.BottomAlign;
                    if (point.Y > target.Height) break;

                    Bitmap fontImage;
                    if (fnt.IsDC && main.DCfontLink > -1)
                    {
                        int link = main.DCfontLink;
                        Fnt_char linked = fontSections[link].FntFile.GetFntFromChar(chars[i]);
                        fontImage = linked.FontImage;
                    }
                    else
                    {
                        fontImage = fnt.FontImage;
                    }

                    if (point.X < 0) point.X = 0;
                    if (point.Y < lineHeight - fnt.BottomAlign) point.Y = lineHeight - fnt.BottomAlign;
                    if (point.Y < 0) point.Y = 0;
                    graphics.DrawImage(fontImage, point.X, point.Y, fnt.charViewWidth, fnt.charViewHeight);
                    lastFnt = fnt;
                }
            }
        }

        private static void ConfigurePreviewGraphics(Graphics graphics)
        {
            graphics.PageUnit = GraphicsUnit.Pixel;
            graphics.CompositingQuality = CompositingQuality.HighSpeed;
            graphics.InterpolationMode = InterpolationMode.Bicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.None;
            graphics.SmoothingMode = SmoothingMode.None;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        }
    }
}
