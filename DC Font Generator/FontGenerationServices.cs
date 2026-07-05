using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

namespace DC_Font_Generator
{
    public enum FontArrangeMode
    {
        Height,
        Width,
        Code
    }

    public sealed class FontProgress
    {
        public FontProgress(string stage, int value, int maximum)
        {
            Stage = stage;
            Value = value;
            Maximum = maximum;
        }

        public string Stage { get; }
        public int Value { get; }
        public int Maximum { get; }
    }

    public sealed class FontAtlasRequest
    {
        public IList<Main> FontSections { get; set; } = Array.Empty<Main>();
        public FontEncoding Encoding { get; set; }
        public IList<int> CandidateWidths { get; set; } = Array.Empty<int>();
        public IList<int> CandidateHeights { get; set; } = Array.Empty<int>();
        public int CurrentWidthIndex { get; set; }
        public int CurrentHeightIndex { get; set; }
        public int Gap { get; set; }
        public FontArrangeMode ArrangeMode { get; set; }
        public Color BackgroundColor { get; set; }
        public FontPerformanceStats PerformanceStats { get; set; }
    }

    public sealed class FontAtlasResult
    {
        public bool Success { get; set; }
        public Size TextImageSize { get; set; }
        public int SizeXIndex { get; set; } = -1;
        public int SizeYIndex { get; set; } = -1;
        public Bgra32Image TextPixels { get; set; }
        public Bitmap TextImage { get; set; }
        public Array2D.List2D<Fnt_char> CharIndex { get; set; }
    }

    public static class FontGenerationServices
    {
        private const string DrawingStage = "Drawing";

        public static FontAtlasResult BuildAtlas(FontAtlasRequest request, IProgress<FontProgress> progress)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            Stopwatch layoutWatch = Stopwatch.StartNew();
            List<Fnt_char> fonts = CollectFonts(request);
            bool vertical = request.ArrangeMode == FontArrangeMode.Width;
            SortFonts(fonts, request.ArrangeMode);

            List<Rectangle> placements = new List<Rectangle>(fonts.Count);
            Size bestSize;
            int sizeXIndex;
            int sizeYIndex;
            if (!FindBestTexSize(
                fonts,
                request.CandidateWidths,
                request.CandidateHeights,
                request.CurrentWidthIndex,
                request.CurrentHeightIndex,
                request.Gap,
                vertical,
                placements,
                out bestSize,
                out sizeXIndex,
                out sizeYIndex))
            {
                layoutWatch.Stop();
                request.PerformanceStats?.Add("AtlasLayout", layoutWatch.Elapsed);
                return new FontAtlasResult { Success = false };
            }
            layoutWatch.Stop();
            request.PerformanceStats?.Add("AtlasLayout", layoutWatch.Elapsed);

            Array2D.List2D<Fnt_char> charIndex = new Array2D.List2D<Fnt_char>();
            Bgra32Image textPixels = new Bgra32Image(bestSize.Width, bestSize.Height);
            Stopwatch drawWatch = Stopwatch.StartNew();
            try
            {
                TexturePixelCodec.Clear(textPixels, GetAtlasBackground(request.BackgroundColor));

                Report(progress, 0, fonts.Count);
                for (int i = 0; i < fonts.Count; i++)
                {
                    DrawFontPlacement(fonts[i], placements[i], charIndex, bestSize, textPixels);
                    Report(progress, i + 1, fonts.Count);
                }
            }
            finally
            {
                drawWatch.Stop();
                request.PerformanceStats?.Add("AtlasDraw", drawWatch.Elapsed);
            }
            Bitmap textImage = textPixels.ToBitmap();

            return new FontAtlasResult
            {
                Success = true,
                TextImageSize = bestSize,
                SizeXIndex = sizeXIndex,
                SizeYIndex = sizeYIndex,
                TextPixels = textPixels,
                TextImage = textImage,
                CharIndex = charIndex
            };
        }

        public static bool TryLayoutFonts(List<Fnt_char> fonts, Size size, int gap, bool vertical, List<Rectangle> placements)
        {
            placements.Clear();

            Rectangle p = new Rectangle(0, 0, 0, 0);
            int lineShift = 0;

            foreach (Fnt_char fnt in fonts)
            {
                if (!fnt.Enable)
                {
                    placements.Add(Rectangle.Empty);
                    continue;
                }

                int currentGap = gap;
                bool addSpace = false;
                if (currentGap == 0 && fnt.IsSpace)
                {
                    currentGap = 1;
                    if (vertical) p.X += 1;
                    else p.Y += 1;
                    addSpace = true;
                }

                int width = Math.Max(0, (int)fnt.fWidth);
                int height = Math.Max(0, (int)fnt.fHeight);

                if (vertical)
                {
                    if ((p.Y + height + currentGap) >= size.Height)
                    {
                        p.Y = currentGap;
                        p.X += lineShift + currentGap;
                        lineShift = width;
                    }
                    if (p.X + width >= size.Width)
                    {
                        return true;
                    }
                }
                else
                {
                    if ((p.X + width + currentGap) >= size.Width)
                    {
                        p.X = currentGap;
                        p.Y += lineShift + currentGap;
                        lineShift = height;
                    }
                    if (p.Y + height >= size.Height)
                    {
                        return true;
                    }
                }

                placements.Add(new Rectangle(p.X, p.Y, width, height));

                if (vertical)
                {
                    lineShift = Math.Max(lineShift, width + currentGap);
                    p.Y += height + currentGap;
                }
                else
                {
                    lineShift = Math.Max(lineShift, height + currentGap);
                    p.X += width + currentGap;
                }

                if (addSpace)
                {
                    if (vertical) p.Y += 1;
                    else p.X += 1;
                }
            }

            return false;
        }

        public static bool FindBestTexSize(
            List<Fnt_char> fonts,
            IList<int> candidateWidths,
            IList<int> candidateHeights,
            int currentWidthIndex,
            int currentHeightIndex,
            int gap,
            bool vertical,
            List<Rectangle> placements,
            out Size bestSize,
            out int sizeXIndex,
            out int sizeYIndex)
        {
            placements.Clear();
            bestSize = Size.Empty;
            sizeXIndex = -1;
            sizeYIndex = -1;

            int sizeXCount = candidateWidths.Count;
            int sizeYCount = candidateHeights.Count;
            if (sizeXCount == 0 || sizeYCount == 0) return false;

            int x = currentWidthIndex;
            int y = currentHeightIndex;
            if (x < 0) x = 0;
            if (y < 0) y = 0;
            if (x >= sizeXCount) x = sizeXCount - 1;
            if (y >= sizeYCount) y = sizeYCount - 1;

            List<Rectangle> candidatePlacements = new List<Rectangle>(fonts.Count);
            while (true)
            {
                Size candidateSize = new Size(candidateWidths[x], candidateHeights[y]);

                if (!TryLayoutFonts(fonts, candidateSize, gap, vertical, candidatePlacements))
                {
                    placements.Clear();
                    placements.AddRange(candidatePlacements);
                    bestSize = candidateSize;
                    sizeXIndex = x;
                    sizeYIndex = y;
                    return true;
                }

                if (x + 1 == sizeXCount && y + 1 == sizeYCount)
                {
                    placements.Clear();
                    return false;
                }

                if (x <= y)
                {
                    if (x + 1 < sizeXCount) x++;
                    else if (y + 1 < sizeYCount) y++;
                }
                else
                {
                    if (y + 1 < sizeYCount) y++;
                    else if (x + 1 < sizeXCount) x++;
                }
            }
        }

        private static List<Fnt_char> CollectFonts(FontAtlasRequest request)
        {
            List<Fnt_char> fonts = new List<Fnt_char>();
            foreach (Main main in request.FontSections)
            {
                foreach (Fnt_char fnt in main.FntFile.CharList)
                {
                    if (fnt.Enable && (request.Encoding == null || !request.Encoding.IsBand(fnt.HEX)))
                    {
                        fonts.Add(fnt);
                    }
                }
            }

            return fonts;
        }

        private static void SortFonts(List<Fnt_char> fonts, FontArrangeMode arrangeMode)
        {
            if (arrangeMode == FontArrangeMode.Height)
            {
                fonts.Sort(new Main.Fnt_char_Height());
            }
            else if (arrangeMode == FontArrangeMode.Width)
            {
                fonts.Sort(new Main.Fnt_char_Width());
            }
        }

        private static Color GetAtlasBackground(Color backgroundColor)
        {
            int opaqueBlack = Color.FromArgb(0xFF, Color.Black).ToArgb();
            int transparentBlack = Color.FromArgb(0, Color.Black).ToArgb();
            int background = backgroundColor.ToArgb();
            if (background == opaqueBlack || background == transparentBlack)
            {
                return Color.FromArgb(transparentBlack);
            }

            return Color.FromArgb(background);
        }

        private static void DrawFontPlacement(
            Fnt_char fnt,
            Rectangle placement,
            Array2D.List2D<Fnt_char> charIndex,
            Size textImageSize,
            Bgra32Image atlas)
        {
            if (!fnt.Enable || placement.Width <= 0 || placement.Height <= 0) return;

            Bgra32Image glyph = fnt.GlyphImage;
            if (glyph != null)
            {
                TexturePixelCodec.CopyImageToImage(glyph, atlas, placement.X, placement.Y);
            }

            int startX = placement.X;
            int startY = placement.Y;
            int width = placement.Width;
            int height = placement.Height;

            for (int y = 0; y < height; y++)
            {
                int currentY = startY + y;
                charIndex.SetRange(startX, currentY, width, fnt);
            }

            float texWidth = textImageSize.Width;
            float texHeight = textImageSize.Height;

            fnt.iTextureIndex = 0;
            fnt.pMapping[0].fU = startX / texWidth;
            fnt.pMapping[0].fV = startY / texHeight;
            fnt.pMapping[1].fU = (startX + width) / texWidth;
            fnt.pMapping[1].fV = startY / texHeight;
            fnt.pMapping[2].fU = startX / texWidth;
            fnt.pMapping[2].fV = (startY + height) / texHeight;
            fnt.pMapping[3].fU = (startX + width) / texWidth;
            fnt.pMapping[3].fV = (startY + height) / texHeight;
        }

        private static void Report(IProgress<FontProgress> progress, int value, int maximum)
        {
            progress?.Report(new FontProgress(DrawingStage, value, maximum));
        }
    }
}
