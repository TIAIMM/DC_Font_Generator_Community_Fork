using System;
using System.Collections.Generic;
using System.Drawing;
using SkiaSharp;
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
        public int Version { get; private set; }

        public bool HasAdjustableSelection => Selected.Count > 0 || RangeSelected.Count > 0;

        public void Clear()
        {
            bool changed = Selected.Count > 0 || RangeSelected.Count > 0 || Removed.Count > 0;
            Selected.Clear();
            RangeSelected.Clear();
            Removed.Clear();
            if (changed) Touch();
        }

        public void ClearSelection()
        {
            bool changed = Selected.Count > 0 || RangeSelected.Count > 0;
            Selected.Clear();
            RangeSelected.Clear();
            if (changed) Touch();
        }

        public bool ApplyRemoved(FontEncoding encoding)
        {
            bool changedBandList = false;
            bool changedSelection = Removed.Count > 0 || Selected.Count > 0 || RangeSelected.Count > 0;
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
            if (changedSelection) Touch();
            return changedBandList;
        }

        public void Toggle(Fnt_char fnt, bool remove)
        {
            if (fnt == null)
            {
                return;
            }

            bool changed = remove ? ToggleRemoved(fnt) : ToggleSelected(fnt);
            if (changed) Touch();
        }

        private bool ToggleSelected(Fnt_char fnt)
        {
            bool removedSelected = Selected.Remove(fnt);
            bool removedRangeSelected = RangeSelected.Remove(fnt);
            if (removedSelected || removedRangeSelected)
            {
                return true;
            }

            Removed.Remove(fnt);
            Selected.Add(fnt);
            return true;
        }

        private bool ToggleRemoved(Fnt_char fnt)
        {
            if (Removed.Remove(fnt))
            {
                return true;
            }

            Selected.Remove(fnt);
            RangeSelected.Remove(fnt);
            Removed.Add(fnt);
            return true;
        }

        public void SelectRange(FL_FONT fontFile, string startHex, string endHex, bool includeSingleByte, bool includeDoubleByte)
        {
            bool changed = RangeSelected.Count > 0;
            RangeSelected.Clear();
            if (startHex == "") startHex = endHex;
            if (endHex == "") endHex = startHex;
            if (startHex == "" && endHex == "" && !includeSingleByte && !includeDoubleByte)
            {
                if (changed) Touch();
                return;
            }

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
                    changed = true;
                }
            }
            if (changed) Touch();
        }

        public void SetSingleByteSelection(FL_FONT fontFile, bool selected)
        {
            bool changed = false;
            int max = Math.Min(256, fontFile.CharList.Count);
            for (int i = 0; i < max; i++)
            {
                Fnt_char fnt = fontFile.CharList[i];
                changed |= SetRangeItem(fnt, selected);
            }
            if (changed) Touch();
        }

        public void SetDoubleByteSelection(FL_FONT fontFile, bool selected)
        {
            bool changed = false;
            for (int i = 256; i < fontFile.CharList.Count; i++)
            {
                Fnt_char fnt = fontFile.CharList[i];
                changed |= SetRangeItem(fnt, selected);
            }
            if (changed) Touch();
        }

        public List<Fnt_char> GetAdjustableFonts()
        {
            List<Fnt_char> fonts = new List<Fnt_char>();
            AddUnique(fonts, Selected);
            AddUnique(fonts, RangeSelected);
            return fonts;
        }

        private bool SetRangeItem(Fnt_char fnt, bool selected)
        {
            if (!fnt.Enable) return false;
            if (selected)
            {
                if (!RangeSelected.Contains(fnt))
                {
                    RangeSelected.Add(fnt);
                    return true;
                }
            }
            else
            {
                return RangeSelected.Remove(fnt);
            }
            return false;
        }

        private void Touch()
        {
            unchecked
            {
                Version++;
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
            int mainSelect,
            int tolerance = 0)
        {
            if (charIndex == null ||
                mainList == null ||
                mainSelect < 0 ||
                mainSelect >= mainList.Count ||
                x < 0 ||
                y < 0 ||
                x >= textImageSize.Width ||
                y >= textImageSize.Height)
            {
                return new GlyphHitResult();
            }

            Fnt_char atlasGlyph = FindAtlasGlyph(charIndex, x, y, textImageSize, tolerance);
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

        private static Fnt_char FindAtlasGlyph(
            Array2D.List2D<Fnt_char> charIndex,
            int x,
            int y,
            Size textImageSize,
            int tolerance)
        {
            Fnt_char glyph = charIndex[x, y];
            if (glyph != null || tolerance <= 0)
            {
                return glyph;
            }

            Fnt_char bestGlyph = null;
            int bestDistance = int.MaxValue;
            int radiusLimit = tolerance * tolerance;
            for (int dy = -tolerance; dy <= tolerance; dy++)
            {
                int py = y + dy;
                if (py < 0 || py >= textImageSize.Height) continue;
                for (int dx = -tolerance; dx <= tolerance; dx++)
                {
                    int px = x + dx;
                    if (px < 0 || px >= textImageSize.Width) continue;

                    int distance = (dx * dx) + (dy * dy);
                    if (distance == 0 || distance > radiusLimit || distance >= bestDistance)
                    {
                        continue;
                    }

                    glyph = charIndex[px, py];
                    if (glyph != null)
                    {
                        bestGlyph = glyph;
                        bestDistance = distance;
                    }
                }
            }

            return bestGlyph;
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
            Bitmap mask = new Bitmap(textImage.Width, textImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            SKImageInfo imageInfo = new SKImageInfo(mask.Width, mask.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (SKSurface surface = SKSurface.Create(imageInfo))
            using (SKBitmap baseBitmap = SkiaBitmapInterop.CreateSKBitmap(textImage))
            using (SKPaint paint = CreateStrokePaint(SKColors.Red))
            {
                SKCanvas canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);
                canvas.DrawBitmap(baseBitmap, 0, 0);
                foreach (Fnt_char fnt in selected)
                {
                    DrawGlyphRectangle(canvas, paint, fnt, textImageSize, false);
                }
                foreach (Fnt_char fnt in rangeSelected)
                {
                    DrawGlyphRectangle(canvas, paint, fnt, textImageSize, false);
                }
                foreach (Fnt_char fnt in removed)
                {
                    DrawGlyphRectangle(canvas, paint, fnt, textImageSize, true);
                }

                canvas.Flush();
                SkiaBitmapInterop.CopySurfaceToBitmap(surface, mask);
            }

            return mask;
        }

        public static void DrawFocus(Bitmap mask, GlyphHitResult hit)
        {
            if (!hit.HasGlyph) return;
            using (SKPaint red = CreateStrokePaint(SKColors.Red))
            using (SKPaint yellow = CreateStrokePaint(SKColors.Yellow))
            {
                SkiaBitmapInterop.DrawToBitmap(mask, canvas =>
                {
                    canvas.DrawRect(
                        new SKRect(hit.Bounds.X, hit.Bounds.Y, hit.Bounds.Right, hit.Bounds.Bottom),
                        red);

                    float baselineY = hit.Bounds.Y + hit.EditableGlyph.BottomAlign;
                    canvas.DrawLine(hit.Bounds.X + 1, baselineY, hit.Bounds.Right - 1, baselineY, yellow);
                });
            }
        }

        public static Rectangle GetFocusDirtyBounds(GlyphHitResult hit, Size textImageSize)
        {
            if (!hit.HasGlyph) return Rectangle.Empty;
            float baselineY = hit.Bounds.Y + hit.EditableGlyph.BottomAlign;
            int left = (int)Math.Floor(hit.Bounds.Left - 2);
            int top = (int)Math.Floor(Math.Min(hit.Bounds.Top, baselineY) - 2);
            int right = (int)Math.Ceiling(hit.Bounds.Right + 2);
            int bottom = (int)Math.Ceiling(Math.Max(hit.Bounds.Bottom, baselineY) + 2);

            Rectangle dirty = Rectangle.FromLTRB(left, top, right, bottom);
            Rectangle bounds = new Rectangle(0, 0, textImageSize.Width, textImageSize.Height);
            return Rectangle.Intersect(dirty, bounds);
        }

        public static void RestoreRegion(Bitmap target, Bitmap source, Rectangle dirtyBounds)
        {
            if (target == null || source == null || dirtyBounds.IsEmpty)
            {
                return;
            }

            SkiaBitmapInterop.CopyBitmapRegion(source, target, dirtyBounds);
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

        private static void DrawGlyphRectangle(SKCanvas canvas, SKPaint paint, Fnt_char fnt, Size textImageSize, bool removed)
        {
            float rx = fnt.x1 * textImageSize.Width;
            float ry = fnt.y1 * textImageSize.Height;
            float bx = fnt.x4 * textImageSize.Width;
            float by = fnt.y4 * textImageSize.Height;

            if (removed)
            {
                canvas.DrawLine(rx, ry, bx, by, paint);
                canvas.DrawLine(bx, ry, rx, by, paint);
            }
            else
            {
                canvas.DrawRect(new SKRect(rx, ry, bx, by), paint);
            }
        }

        private static SKPaint CreateStrokePaint(SKColor color)
        {
            return new SKPaint
            {
                IsAntialias = false,
                Color = color,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1f
            };
        }
    }

    internal static class GlyphPreviewRenderer
    {
        public static void Render(Bitmap target, string text, Main main, IList<Main> fontSections)
        {
            SKImageInfo imageInfo = new SKImageInfo(target.Width, target.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
            using (SKSurface surface = SKSurface.Create(imageInfo))
            {
                SKCanvas canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);
                if (string.IsNullOrEmpty(text))
                {
                    canvas.Flush();
                    SkiaBitmapInterop.CopySurfaceToBitmap(surface, target);
                    return;
                }

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
                    using (SKBitmap glyphBitmap = SkiaBitmapInterop.CreateSKBitmap(fontImage))
                    {
                        SKRect destination = new SKRect(
                            point.X,
                            point.Y,
                            point.X + fnt.charViewWidth,
                            point.Y + fnt.charViewHeight);
                        canvas.DrawBitmap(glyphBitmap, destination);
                    }
                    lastFnt = fnt;
                }

                canvas.Flush();
                SkiaBitmapInterop.CopySurfaceToBitmap(surface, target);
            }
        }
    }
}
