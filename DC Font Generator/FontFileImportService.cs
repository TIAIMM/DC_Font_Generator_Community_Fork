using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace DC_Font_Generator
{
    public sealed class FontFileImportResult
    {
        public bool Success { get; set; }
        public Bitmap Texture { get; set; }
        public bool FixedFont { get; set; }
        public float FontMaxWidth { get; set; }
        public FontPerformanceStats PerformanceStats { get; set; } = new FontPerformanceStats();
    }

    public static class FontFileImportService
    {
        public static FontFileImportResult LoadFntAndTex(
            string path,
            bool loadTex,
            FL_FONT fontFile,
            int id,
            FontEncoding fontEncoding,
            Array2D.List2D<Fnt_char> charIndex,
            IProgress<FontProgress> progress = null)
        {
            FontFileImportResult result = new FontFileImportResult
            {
                Texture = new Bitmap(1, 1)
            };

            if (fontEncoding.Temp.Count < 256)
            {
                return result;
            }

            Stopwatch loadFntWatch = Stopwatch.StartNew();
            fontFile.load(path, fontEncoding.enc, fontEncoding.Temp, id);
            loadFntWatch.Stop();
            result.PerformanceStats.Add("LoadFnt", loadFntWatch.Elapsed);
            if (!loadTex)
            {
                result.Success = true;
                return result;
            }

            if (string.IsNullOrEmpty(fontFile.Header.TexFileName))
            {
                return result;
            }

            string directory = Path.GetDirectoryName(path);
            string texPath = Path.Combine(directory, fontFile.Header.TexFileName + ".Tex");
            if (!File.Exists(texPath))
            {
                return result;
            }

            try
            {
                Stopwatch loadTexWatch = Stopwatch.StartNew();
                result.Texture = TextureFileService.LoadTex(texPath);
                loadTexWatch.Stop();
                result.PerformanceStats.Add("LoadTex", loadTexWatch.Elapsed);
            }
            catch
            {
                return result;
            }

            Stopwatch buildIndexWatch = Stopwatch.StartNew();
            BuildCharIndex(fontFile, result.Texture, charIndex, progress);
            buildIndexWatch.Stop();
            result.PerformanceStats.Add("BuildCharIndex", buildIndexWatch.Elapsed);

            if (fontFile.FixedWidth > 0)
            {
                result.FixedFont = true;
                result.FontMaxWidth = fontFile.FixedWidth;
            }

            result.Success = true;
            return result;
        }

        private static void BuildCharIndex(
            FL_FONT fontFile,
            Bitmap texture,
            Array2D.List2D<Fnt_char> charIndex,
            IProgress<FontProgress> progress)
        {
            charIndex.Clear();
            ReportProgress(progress, 0, fontFile.CharList.Count);

            BitmapData bmpData = texture.LockBits(
                new Rectangle(0, 0, texture.Width, texture.Height),
                ImageLockMode.ReadOnly,
                PixelFormat.Format32bppArgb);
            try
            {
                int index = 0;
                foreach (Fnt_char fnt in fontFile.CharList)
                {
                    if (!fnt.Enable)
                    {
                        index++;
                        continue;
                    }

                    if (index % 10 == 0)
                    {
                        ReportProgress(progress, index, fontFile.CharList.Count);
                    }

                    int px = (int)(fnt.pMapping[0].fU * texture.Width);
                    int py = (int)(fnt.pMapping[0].fV * texture.Height);
                    int rx = (int)(fnt.pMapping[3].fU * texture.Width);
                    int ry = (int)(fnt.pMapping[3].fV * texture.Height);

                    if (px < 0) px = 0;
                    if (py < 0) py = 0;
                    if (rx > texture.Width) rx = texture.Width;
                    if (ry > texture.Height) ry = texture.Height;

                    int pw = rx - px;
                    int ph = ry - py;

                    if (pw <= 0 || ph <= 0)
                    {
                        fnt.Empty = true;
                        fnt.IsSpace = true;
                        UpdateEmptyIndex(fontFile, fnt, index);
                        index++;
                        continue;
                    }

                    Rectangle rect = new Rectangle(px, py, pw, ph);
                    fnt.SetLazyFontImage(texture, rect);

                    bool notBlack = TexturePixelCodec.HasNonZeroPixel(bmpData, rect);
                    for (int y = py; y < ry; y++)
                    {
                        charIndex.SetRange(px, y, pw, fnt);
                    }

                    if (notBlack)
                    {
                        fnt.Empty = false;
                        fnt.IsSpace = (pw == 1 && ph == 1);
                    }
                    else
                    {
                        fnt.Empty = true;
                        fnt.IsSpace = true;
                        UpdateEmptyIndex(fontFile, fnt, index);
                    }

                    index++;
                    ReportProgress(progress, index, fontFile.CharList.Count);
                }
            }
            finally
            {
                texture.UnlockBits(bmpData);
            }
        }

        private static void UpdateEmptyIndex(FL_FONT fontFile, Fnt_char fnt, int index)
        {
            if (fnt.IsDC)
            {
                if (fontFile.EmptyDC == -1)
                    fontFile.EmptyDC = index;
            }
            else
            {
                if (fontFile.EmptySC == -1)
                    fontFile.EmptySC = index;
            }
        }

        private static void ReportProgress(IProgress<FontProgress> progress, int value, int maximum)
        {
            progress?.Report(new FontProgress("LoadingFnt", value, maximum));
        }
    }
}
