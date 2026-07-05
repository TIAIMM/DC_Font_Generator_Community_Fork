using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace DC_Font_Generator
{
    class DrawFont : IDisposable
    {
        public FontDescriptor Font; //目前字型
        public FontStyleDescriptor StyleDescriptor;
        public float ascentPixel = 0; //目前字型上升值
        public float descentPixel = 0; //目前字型下降值
        public float lineSpacingPixel = 0;//目前字型行距
        

        public Color BackColor = Color.FromArgb(0, Color.Black);
        private Color fontColor = Color.FromArgb(0xFF, Color.White);
        public Color OutlineColor = Color.FromArgb(0xFF, Color.FromArgb(80, 80, 80));
        public int OutlineWidth = 0;
        public float CDZ_BottomAlign = 0; //CDZ的底部對齊位置
        private int glow = 4;
        private Color glowcolor = Color.FromArgb(0x80, 0x80, 0x80, 0x80);
        public float SpaceWidth = 0; //空白字型的寬度

        
        public int DrawMode = 1; //0=無特效 1=反鋸齒

        private SKTypeface skTypeface;
        private bool ownsSkTypeface;
        private GlyphRenderContext glyphRenderContext;
        private FontRenderBackend renderBackend = FontRenderBackendSelector.ReadRequestedBackend();

        public FontRenderBackend RenderBackend
        {
            get { return renderBackend; }
            set
            {
                if (renderBackend != value)
                {
                    renderBackend = value;
                    ResetGlyphRenderContext();
                }
            }
        }

		public DrawFont()
        {
            CreateGlow();
        }
        /// <summary>
        /// 製作glow用筆刷
        /// </summary>
        private void CreateGlow()
        {
        }
        private void CreateOutline()
        {

        }
        public int Glow
        {
            set
            {
                if (glow != value)
                {
                    glow = value;
                    CreateGlow();
                    CreateDrawingZone();
                }
            }
            get { return glow; }
        }
        public Color GlowColor
        {
            set
            {
                if (glowcolor != value)
                {
                    glowcolor=value;
                    CreateGlow();
                    
                }
            }
            get { return glowcolor; }
        }
        public int Outline
        {
            set
            {
                if (OutlineWidth != value)
                {
                    OutlineWidth = value;
                    CreateDrawingZone();
                }
            }
        }
        public Color FontColor
        {
            set
            {
                if (fontColor != value)
                {
                    fontColor = value;
                }
            }
            get { return fontColor; }
        }
        /// <summary>
        /// 設定現在使用的字型
        /// </summary>
        public FontDescriptor FontData
        {
            set
            {
                if (!ReferenceEquals(Font, value))
                {
                    Font = value;
                    using (SKTypeface typeface = value?.CreateTypeface())
                    {
                        if (typeface != null)
                        {
                            using (SKFont skFont = new SKFont(typeface, value.SizePixels))
                            {
                                skFont.GetFontMetrics(out SKFontMetrics metrics);
                                ascentPixel = -metrics.Ascent;
                                descentPixel = metrics.Descent;
                                lineSpacingPixel = -metrics.Ascent + metrics.Descent + metrics.Leading;
                            }
                        }
                        else { ascentPixel = value.SizePixels; descentPixel = 0; lineSpacingPixel = value.SizePixels * 1.2f; }
                    }
                    CreateSkiaTypeface(); CreateDrawingZone(); CreateSpaceWidth();
                }
            }
            get { return Font; }
        }
        private void CreateDrawingZone()
        {
			int shift = (OutlineWidth * 2) + (glow * 2);

			//建立底部對齊點
			CDZ_BottomAlign = (shift / 2) + ascentPixel + 0.5f; // 增加0.5像素偏移补偿
            ResetGlyphRenderContext();
		}

		/// <summary>
		/// 建立Space的寬度
		/// </summary>
		private void CreateSpaceWidth()
		{
            float measureWidth = 0;
            try
            {
                using (SKFont font = CreateTextFont())
                {
                    measureWidth = font.MeasureText(" ");
                }
            }
            catch
            {
                measureWidth = Font != null ? Font.SizePixels / 4f : 1f;
            }

			SpaceWidth = measureWidth;

			// 确保最小值（通常空格至少为字号的1/4）
			if (Font != null && SpaceWidth < Font.SizePixels / 4)
			{
				SpaceWidth = Font.SizePixels / 4;
			}

			// 添加上限约束（不超过行间距的1/3）
			float maxSpace = lineSpacingPixel / 3;
			if (SpaceWidth > maxSpace)
			{
				SpaceWidth = maxSpace;
			}

            SpaceWidth = Math.Max(1f, RoundMetric(SpaceWidth));
		}

		/// <summary>
		/// 繪製文字
		/// </summary>
		/// <param name="c">字元</param>
		/// <param name="BottomAlign">底部對齊傳出值</param>
		/// <returns></returns>
		public Bitmap DrawingFont(char c, out float BottomAlign)
		{
			GlyphRenderResult glyph = RenderGlyph(c);
			BottomAlign = glyph.fTopEdge;
			return glyph.Image ?? new Bitmap(1, 1);
		}

		public GlyphRenderResult RenderGlyph(char c)
		{
            return RenderGlyphSkia(c);
		}

		private GlyphRenderResult RenderGlyphSkia(char c)
		{
			GlyphRenderResult result = new GlyphRenderResult();
			if (c < 32)
			{
				return CreateSpaceResult(result);
			}

            string text = c.ToString();
            int effectShift = glow + OutlineWidth;
            float originX = effectShift + 0.5f;
            float baseline = CDZ_BottomAlign;
            int surfaceSize = Math.Max(1, (int)Math.Ceiling(lineSpacingPixel * 2f + (effectShift * 4f) + 4f));

            SKTypeface glyphTypeface = ResolveTypefaceForCharacter(c, out bool ownsGlyphTypeface);
            try
            {
                using (SKFont font = CreateTextFont(glyphTypeface))
                using (SKPaint fillPaint = CreateTextPaint(SKPaintStyle.Fill, FontColor, 0f))
                using (SKPath glyphPath = GetTextPath(font, text, originX, baseline))
                {
                    if (glyphPath == null)
                    {
                        return CreateSpaceResult(result);
                    }

                    SKRect originBounds = glyphPath.Bounds;
                    if (originBounds.Width <= 0 || originBounds.Height <= 0)
                    {
                        return CreateSpaceResult(result);
                    }

                    result.OriginSize = new Size((int)Math.Ceiling(originBounds.Width), (int)Math.Ceiling(originBounds.Height));
                    result.LayoutAdvance = Math.Max(1f, RoundMetric(MeasureLayoutAdvance(font, text, originBounds.Width)));
                    result.RealSpace = GetSkiaPathRealSpace(font, text, originBounds.Width, originX, baseline);
                    surfaceSize = Math.Max(
                        surfaceSize,
                        (int)Math.Ceiling(result.LayoutAdvance + (effectShift * 4f) + 4f));

                    SKCanvas canvas = GetGlyphRenderContext().PrepareCanvas(surfaceSize, surfaceSize, BackColor);

                    DrawSkiaEffects(canvas, glyphPath);
                    canvas.DrawPath(glyphPath, fillPaint);

                    byte[] pixels = GetGlyphRenderContext().ReadPixels();
                    Rectangle bounds = SkiaBitmapInterop.FindContentBounds(pixels, surfaceSize, surfaceSize, BackColor);
                    if (bounds.Width <= 0 || bounds.Height <= 0)
                    {
                        return CreateSpaceResult(result);
                    }

                    result.RightOverhang = CalculateRightOverhang(bounds, originX, result.LayoutAdvance);
                    bounds = NormalizeEffectHorizontalBounds(bounds, originX, result.LayoutAdvance, surfaceSize);
                    result.Image = SkiaBitmapInterop.CreateBitmapFromBgra(pixels, surfaceSize, bounds);
                    result.fTopEdge = FloorMetric(CDZ_BottomAlign - bounds.Y);
                    return result;
                }
            }
            finally
            {
                if (ownsGlyphTypeface && glyphTypeface != null)
                {
                    glyphTypeface.Dispose();
                }
            }
		}

        private Rectangle NormalizeEffectHorizontalBounds(Rectangle contentBounds, float originX, float layoutAdvance, int surfaceSize)
        {
            if (glow <= 0 && OutlineWidth <= 0)
            {
                return contentBounds;
            }

            int effectPad = (int)Math.Ceiling((Math.Max(0, glow) + Math.Max(0, OutlineWidth)) / 2f);
            if (effectPad < 1)
            {
                effectPad = 1;
            }

            int left = (int)Math.Floor(originX - effectPad);
            int right = (int)Math.Ceiling(originX + layoutAdvance + effectPad);

            if (left > contentBounds.Left)
            {
                left = contentBounds.Left;
            }

            if (right < contentBounds.Right)
            {
                right = contentBounds.Right;
            }

            if (left < 0) left = 0;
            if (right > surfaceSize) right = surfaceSize;
            if (right <= left) right = Math.Min(surfaceSize, left + 1);

            return Rectangle.FromLTRB(left, contentBounds.Top, right, contentBounds.Bottom);
        }

        private float CalculateRightOverhang(Rectangle contentBounds, float originX, float layoutAdvance)
        {
            if (glow <= 0 && OutlineWidth <= 0)
            {
                return 0f;
            }

            float logicalRight = originX + layoutAdvance;
            float overhang = contentBounds.Right - logicalRight;
            return overhang > 0f ? overhang : 0f;
        }

        private GlyphRenderResult CreateSpaceResult(GlyphRenderResult result)
        {
            result.IsSpace = true;
            result.OriginSize = new Size((int)SpaceWidth, 0);
            result.LayoutAdvance = SpaceWidth;
            result.RealSpace = SpaceWidth;
            result.RightOverhang = 0f;
            return result;
        }

        private static float MeasureLayoutAdvance(SKFont font, string text, float fallbackWidth)
        {
            float advance = font.MeasureText(text);
            if (float.IsNaN(advance) || float.IsInfinity(advance) || advance <= 0f)
            {
                advance = fallbackWidth;
            }

            return advance;
        }

        private static float RoundMetric(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return (float)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        private static float CeilingMetric(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return (float)Math.Ceiling(value);
        }

        private static float FloorMetric(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0f;
            }

            return (float)Math.Floor(value);
        }

        private void DrawSkiaEffects(SKCanvas canvas, SKPath glyphPath)
        {
            if (glow > 0)
            {
                int size = OutlineWidth + glow;
                int glowStep = 0x80 / (glow + 1);
                int alpha = glowStep;
                for (int i = 0; i < glow; i++)
                {
                    using (SKPaint glowPaint = CreateTextPaint(
                        SKPaintStyle.Stroke,
                        Color.FromArgb(alpha, glowcolor.R, glowcolor.G, glowcolor.B),
                        Math.Max(1, size - i)))
                    {
                        canvas.DrawPath(glyphPath, glowPaint);
                    }

                    if (i >= OutlineWidth)
                    {
                        alpha += glowStep;
                        if (alpha > 0x80)
                        {
                            alpha = 0x80;
                        }
                    }
                }
            }

            if (OutlineWidth > 0)
            {
                using (SKPaint outlinePaint = CreateTextPaint(SKPaintStyle.Stroke, OutlineColor, OutlineWidth))
                {
                    canvas.DrawPath(glyphPath, outlinePaint);
                }
            }
        }

        private float GetSkiaPathRealSpace(SKFont font, string text, float originWidth, float originX, float baseline)
        {
            using (SKPath doublePath = GetTextPath(font, text + text, originX, baseline))
            {
                if (doublePath == null)
                {
                    return 0f;
                }

                SKRect doubleBounds = doublePath.Bounds;
                return (doubleBounds.Width - (originWidth * 2f)) / 4f;
            }
        }

        private SKPaint CreateTextPaint(SKPaintStyle style, Color color, float strokeWidth)
        {
            SKPaint paint = new SKPaint();
            paint.IsAntialias = DrawMode == 1;
            paint.Color = SkiaBitmapInterop.ToSKColor(color);
            paint.Style = style;
            paint.StrokeWidth = strokeWidth;
            paint.StrokeJoin = SKStrokeJoin.Round;
            return paint;
        }

        private SKFont CreateTextFont()
        {
            return CreateTextFont(skTypeface ?? SKTypeface.Default);
        }

        private SKFont CreateTextFont(SKTypeface typeface)
        {
            return new SKFont(typeface ?? SKTypeface.Default, Font != null ? Font.SizePixels : 12f);
        }

        private static SKPath GetTextPath(SKFont font, string text, float x, float y)
        {
            return font.GetTextPath(text.AsSpan(), new SKPoint(x, y));
        }

        private SKTypeface ResolveTypefaceForCharacter(char c, out bool ownsResolvedTypeface)
        {
            ownsResolvedTypeface = false;
            int codepoint = c;
            SKTypeface current = skTypeface ?? SKTypeface.Default;
            if (current != null && current.ContainsGlyph(codepoint))
            {
                return current;
            }

            SKTypeface fallback = null;
            if (Font != null)
            {
                int weight, width;
                SKFontStyleSlant slant;
                GetStyleValues(out weight, out width, out slant);
                fallback = SKFontManager.Default.MatchCharacter(
                    Font.FamilyName,
                    weight,
                    width,
                    slant,
                    new[] { "zh-Hans", "zh-CN", "zh" },
                    codepoint);
            }

            if (fallback == null)
            {
                fallback = SKFontManager.Default.MatchCharacter(codepoint);
            }

            if (fallback != null)
            {
                ownsResolvedTypeface = true;
                return fallback;
            }

            return current;
        }

        private void CreateSkiaTypeface()
        {
            SKTypeface next = null;
            bool ownsNext = false;
            if (Font != null)
            {
                int weight, width;
                SKFontStyleSlant slant;
                GetStyleValues(out weight, out width, out slant);
                next = SKTypeface.FromFamilyName(Font.FamilyName, weight, width, slant);
                ownsNext = next != null;
                if (next == null)
                {
                    next = SKTypeface.FromFamilyName(Font.FamilyName, weight, width, slant);
                    ownsNext = next != null;
                }
            }

            if (next == null)
            {
                next = SKTypeface.Default;
                ownsNext = false;
            }

            if (ownsSkTypeface && skTypeface != null)
            {
                skTypeface.Dispose();
            }

            skTypeface = next;
            ownsSkTypeface = ownsNext;
            ResetGlyphRenderContext();
        }

        private void GetStyleValues(out int weight, out int width, out SKFontStyleSlant slant)
        {
            if (StyleDescriptor != null)
            {
                weight = StyleDescriptor.Weight;
                width = StyleDescriptor.Width;
                slant = StyleDescriptor.Slant;
            }
            else
            {
                weight = Font != null ? Font.Weight : 400;
                width = Font != null ? Font.Width : (int)SKFontStyleWidth.Normal;
                slant = Font != null ? Font.Slant : SKFontStyleSlant.Upright;
            }
        }

        private GlyphRenderContext GetGlyphRenderContext()
        {
            if (glyphRenderContext != null)
            {
                return glyphRenderContext;
            }

            FontRenderBackend backend = renderBackend == FontRenderBackend.Auto
                ? FontRenderBackend.Cpu
                : renderBackend;
            try
            {
                glyphRenderContext = new GlyphRenderContext(FontRenderBackendSelector.CreateFactory(backend));
            }
            catch
            {
                if (backend == FontRenderBackend.Cpu)
                {
                    throw;
                }

                glyphRenderContext = new GlyphRenderContext(FontRenderBackendSelector.CreateFactory(FontRenderBackend.Cpu));
            }

            return glyphRenderContext;
        }

        private void ResetGlyphRenderContext()
        {
            glyphRenderContext?.Dispose();
            glyphRenderContext = null;
        }

		public class GlyphRenderResult
		{
			public Bitmap Image;
            public Size OriginSize;
            public float LayoutAdvance;
			public float RealSpace;
            public float RightOverhang;
			public float fTopEdge;
			public bool IsSpace;

            public float BottomAlign
            {
                get { return fTopEdge; }
                set { fTopEdge = value; }
            }
		}

		/// <summary>
		/// 取得原字型真實高度
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public Size GetOriginFontHeight(char c, out SizeF DisplaySize, out float RealSpace)
		{
            GlyphRenderResult glyph = RenderGlyph(c);
            DisplaySize = glyph.OriginSize;
            RealSpace = glyph.RealSpace;
            return glyph.OriginSize;
		}

		// 统一的边界检测方法
		private Rectangle GetFontBounds(BmpPixelData bmpData)
		{
			int backArgb = BackColor.ToArgb();
			int top = int.MaxValue;
			int left = int.MaxValue;
			int bottom = int.MinValue;
			int right = int.MinValue;
			bool found = false;

			// 单次遍历同时检测所有边界
			for (int y = 0; y < bmpData.Height; y++)
			{
				int row = y * bmpData.Stride;
				for (int x = 0; x < bmpData.Width; x++)
				{
					if (bmpData.GetArgb(row, x) != backArgb)
					{
						found = true;
						if (y < top) top = y;
						if (y > bottom) bottom = y;
						if (x < left) left = x;
						if (x > right) right = x;
					}
				}
			}

			if (!found) return Rectangle.Empty;

			return new Rectangle(left, top, right - left + 1, bottom - top + 1);
		}

		public Bitmap GetOriginFont(char c, out bool IsEmpty)
		{
            GlyphRenderResult glyph = RenderGlyph(c);
            IsEmpty = glyph.IsSpace || glyph.Image == null;
            if (glyph.Image == null)
            {
                return new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            }

            return new Bitmap(glyph.Image);
		}

		/// <summary>
		/// 裁切bitmap
		/// </summary>
		/// <param name="img">原始bitmap</param>
		/// <param name="cropArea">正方形</param>
		/// <returns>裁好的bitmap</returns>
		public Bitmap cropImage(Bitmap img, Rectangle cropArea)
		{
			if (cropArea.Width <= 0 || cropArea.Height <= 0)
				return new Bitmap(1, 1);

			// 使用LockBits直接复制内存块
			var cropped = new Bitmap(cropArea.Width, cropArea.Height, PixelFormat.Format32bppArgb);

			// 锁定源位图
			var srcData = img.LockBits(
				new Rectangle(0, 0, img.Width, img.Height),
				ImageLockMode.ReadOnly,
				PixelFormat.Format32bppArgb);

			// 锁定目标位图
			var destData = cropped.LockBits(
				new Rectangle(0, 0, cropped.Width, cropped.Height),
				ImageLockMode.WriteOnly,
				PixelFormat.Format32bppArgb);

			try
			{
				int srcStride = srcData.Stride;
				int destStride = destData.Stride;
				int bytesPerPixel = 4; // 32bppArgb

				// 计算要复制的字节数
				int copyWidth = Math.Min(cropArea.Width * bytesPerPixel, srcStride);

				// 计算源图像起始位置
				IntPtr srcPtr = srcData.Scan0 + (cropArea.Y * srcStride) + (cropArea.X * bytesPerPixel);
				IntPtr destPtr = destData.Scan0;

				// 逐行复制
				for (int y = 0; y < cropArea.Height; y++)
				{
					CopyMemory(destPtr, srcPtr, (uint)copyWidth);
					srcPtr = IntPtr.Add(srcPtr, srcStride);
					destPtr = IntPtr.Add(destPtr, destStride);
				}
			}
			finally
			{
				img.UnlockBits(srcData);
				cropped.UnlockBits(destData);
			}

			return cropped;
		}

		// 导入内存复制函数
		[DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
		private static extern void CopyMemory(IntPtr dest, IntPtr src, uint length);

		/// <summary>
		/// 取得字型真實大小
		/// </summary>
		/// <param name="image"></param>
		/// <returns></returns>
		public Rectangle GetFontGSize(Bitmap image)
		{
			using (var bmpData = new BmpPixelData(image))
			{
				return GetFontBounds(bmpData);
			}
		}

		/// <summary>
		/// 複製圖
		/// </summary>
		/// <param name="Source"></param>
		/// <param name="Target"></param>
		/// <param name="point"></param>
		public void CopyImage(Bitmap Source, ref Bitmap Target, Point point)
		{
			// 使用LockBits进行内存复制
			using (var sourceData = new BmpPixelData(Source))
			using (var targetData = new BmpPixelData(Target))
			{
				int sourceWidth = Math.Min(Source.Width, Target.Width - point.X);
				int sourceHeight = Math.Min(Source.Height, Target.Height - point.Y);
				int backArgb = BackColor.ToArgb();

				for (int y = 0; y < sourceHeight; y++)
				{
					int sourceRow = y * sourceData.Stride;
					int targetRow = (y + point.Y) * targetData.Stride + point.X * 4;

					for (int x = 0; x < sourceWidth; x++)
					{
						int argb = sourceData.GetArgb(sourceRow, x);
						if (argb != backArgb)
							targetData.SetArgb(targetRow + x * 4, argb);
					}
				}
			}
		}

        public void Dispose()
        {
            ResetGlyphRenderContext();
            if (ownsSkTypeface && skTypeface != null)
            {
                skTypeface.Dispose();
            }
            skTypeface = null;
            ownsSkTypeface = false;
        }

		private class BmpPixelData : IDisposable
		{
			private Bitmap _bitmap;
			private BitmapData _data;
			public byte[] Bytes { get; }
			public int Width { get; }
			public int Height { get; }
			public int Stride { get; }

			public BmpPixelData(Bitmap bmp)
			{
				_bitmap = bmp;
				Width = bmp.Width;
				Height = bmp.Height;
				Rectangle rect = new Rectangle(0, 0, Width, Height);
				_data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
				Stride = _data.Stride;
				Bytes = new byte[Stride * Height];
				Marshal.Copy(_data.Scan0, Bytes, 0, Bytes.Length);
			}

			public int GetArgb(int row, int x)
			{
				int idx = row + x * 4;
				return Bytes[idx] | (Bytes[idx + 1] << 8) |
					   (Bytes[idx + 2] << 16) | (Bytes[idx + 3] << 24);
			}

			public void SetArgb(int offset, int argb)
			{
				Bytes[offset] = (byte)(argb);
				Bytes[offset + 1] = (byte)(argb >> 8);
				Bytes[offset + 2] = (byte)(argb >> 16);
				Bytes[offset + 3] = (byte)(argb >> 24);
			}

			public void Dispose()
			{
				Marshal.Copy(Bytes, 0, _data.Scan0, Bytes.Length);
				_bitmap.UnlockBits(_data);
			}
		}
	}
}
