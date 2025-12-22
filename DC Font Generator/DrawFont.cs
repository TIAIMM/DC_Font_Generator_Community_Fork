using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace DC_Font_Generator
{
    class DrawFont
    {
        public Font _Font; //目前字型
        private FontFamily fontFamily;
        public float ascentPixel = 0; //目前字型上升值
        public float descentPixel = 0; //目前字型下降值
        public float lineSpacingPixel = 0;//目前字型行距
        

        public Color BackColor = Color.FromArgb(0, Color.Black);
        private Color fontColor = Color.FromArgb(0xFF, Color.White);
        public Color OutlineColor = Color.FromArgb(0xFF, Color.FromArgb(80, 80, 80));
        public int OutlineWidth = 0;
        private Brush brush = new Pen(Color.FromArgb(200, Color.FromArgb(80, 80, 80)), 2f).Brush;
        private Brush brush2;
        private int BackGround ;
        private SolidBrush sfbrush = new SolidBrush(Color.FromArgb(255, 255, 255));
        private GraphicsPath path = new GraphicsPath();
        private StringFormat strformat = new StringFormat();
        private Bitmap CDZ_image;
        private Graphics CDZ_g;
        public float CDZ_BottomAlign = 0; //CDZ的底部對齊位置
        private Pen[] GlowPen;
        private int glow = 4;
        private Color glowcolor = Color.FromArgb(0x80, 0x80, 0x80, 0x80);
        public float SpaceWidth = 0; //空白字型的寬度

        
        public int DrawMode = 1; //0=無特效 1=反鋸齒

		private GraphicsPath _reusablePath = new GraphicsPath();
		private PointF _reusablePoint = new PointF(0.5f, 0.5f); // 使用0.5像素偏移

		public DrawFont()
        {
            sfbrush = new SolidBrush(fontColor);
            brush2 = new Pen(fontColor).Brush;
            BackGround = BackColor.ToArgb();
            CreateGlow();
        }
        /// <summary>
        /// 製作glow用筆刷
        /// </summary>
        private void CreateGlow()
        {
            int size = OutlineWidth + glow;
            int glow_step = 0x80 / (glow + 1);
            int gs = glow_step;
            GlowPen = new Pen[glow + OutlineWidth];
            for (int i = 0; i < glow + OutlineWidth; i++)
            {
                GlowPen[i] = new Pen(Color.FromArgb(gs, glowcolor.R, glowcolor.G, glowcolor.B), size - i);
                GlowPen[i].LineJoin = LineJoin.Round;
                if (i >= OutlineWidth)
                    gs += glow_step;

            }
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
                    sfbrush = new SolidBrush(fontColor);
                    brush2 = new Pen(fontColor).Brush;
                }
            }
            get { return fontColor; }
        }
        /// <summary>
        /// 設定現在使用的字型
        /// </summary>
        public Font FontData
        {
            set
            {
                if (_Font != value)
                {
                    _Font = value;
                    fontFamily = _Font.FontFamily;

                    int ascent;             // font family ascent in design units
                    int descent;            // font family descent in design units
                    int lineSpacing;        // font family line spacing in design units

                    int em = fontFamily.GetEmHeight(_Font.Style);

                    ascent = fontFamily.GetCellAscent(_Font.Style); //上升
                    // 14.484375 = 16.0 * 1854 / 2048
                    ascentPixel = _Font.Size * ascent / em; //實際上升值

                    // Display the descent in design units and pixels.
                    descent = fontFamily.GetCellDescent(_Font.Style);
                    // 3.390625 = 16.0 * 434 / 2048
                    descentPixel = _Font.Size * descent / em;


                    // Display the line spacing in design units and pixels.
                    lineSpacing = fontFamily.GetLineSpacing(_Font.Style); //行距
                    // 18.398438 = 16.0 * 2355 / 2048
                    lineSpacingPixel = _Font.Size * lineSpacing / em;

                    CreateDrawingZone();//建立繪字空間
                    CreateSpaceWidth();//建立Space的寬度
                }
            }
            get
            {
                return _Font;
            }
        }
        private void CreateDrawingZone()
        {
			// 释放旧资源
			if (CDZ_g != null) CDZ_g.Dispose();
			if (CDZ_image != null) CDZ_image.Dispose();

			int shift = (OutlineWidth * 2) + (glow * 2);
			CDZ_image = new Bitmap(
				(int)(lineSpacingPixel + shift * 2 + 1),
				(int)(lineSpacingPixel + shift * 2 + 1)
			);

			CDZ_g = Graphics.FromImage(CDZ_image);
			CDZ_g.PixelOffsetMode = PixelOffsetMode.HighQuality;

			if (DrawMode == 1)
            {
                CDZ_g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                CDZ_g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				CDZ_g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
				CDZ_g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                CDZ_g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

			}
            else
            {
				CDZ_g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
				CDZ_g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
				CDZ_g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
				CDZ_g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
				CDZ_g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
			}
            CDZ_g.Clear(BackColor);

			//建立底部對齊點
			CDZ_BottomAlign = (shift / 2) + ascentPixel + 0.5f; // 增加0.5像素偏移补偿
		}

		/// <summary>
		/// 建立Space的寬度
		/// </summary>
		private void CreateSpaceWidth()
		{
			// 方法1：使用常规MeasureString（较快但不够精确）
			SizeF ms = CDZ_g.MeasureString(" ", _Font);
			float measureWidth = ms.Width;

			// 方法2：使用图形路径获取精确宽度（考虑亚像素偏移）
			float pathWidth = 0;
			using (GraphicsPath path = new GraphicsPath())
			{
				try
				{
					// 添加带亚像素偏移的空格测量
					path.AddString(" ",
						fontFamily,
						(int)_Font.Style,
						_Font.Size,
						new PointF(0.5f, 0.5f), // 0.5像素偏移
						strformat);

					RectangleF bounds = path.GetBounds();
					pathWidth = bounds.Width;
				}
				catch
				{
					// 回退到常规测量方法
					pathWidth = measureWidth;
				}
			}

			// 选择最合适的值（优先使用路径测量）
			SpaceWidth = pathWidth > 0 ? pathWidth : measureWidth;

			// 确保最小值（通常空格至少为字号的1/4）
			if (SpaceWidth < _Font.Size / 4)
			{
				SpaceWidth = _Font.Size / 4;
			}

			// 添加上限约束（不超过行间距的1/3）
			float maxSpace = lineSpacingPixel / 3;
			if (SpaceWidth > maxSpace)
			{
				SpaceWidth = maxSpace;
			}
		}

		/// <summary>
		/// 繪製文字
		/// </summary>
		/// <param name="c">字元</param>
		/// <param name="BottomAlign">底部對齊傳出值</param>
		/// <returns></returns>
		public Bitmap DrawingFont(char c, out float BottomAlign)
		{
			// 对于控制字符，返回最小位图
			if (c < 32)
			{
				BottomAlign = 0;
				return new Bitmap(1, 1);
			}

			Bitmap image = null;
			try
			{
				image = CDZ_image;
				CDZ_g.Clear(BackColor);
				int shift = (glow + OutlineWidth);

				// 重用可复用路径
				_reusablePath.Reset();
				_reusablePoint.X = shift + 0.5f; // 添加0.5像素水平偏移
				_reusablePoint.Y = shift + 0.5f; // 添加0.5像素垂直偏移

				_reusablePath.AddString(
					c.ToString(),
					fontFamily,
					(int)_Font.Style,
					_Font.Size,
					_reusablePoint,
					strformat
				);

				if (glow > 0)
				{
					for (int i = 1; i <= glow; ++i)
						CDZ_g.DrawPath(GlowPen[i - 1], _reusablePath);
				}

				if (OutlineWidth > 0)
				{
					using (Pen pen2 = new Pen(OutlineColor, OutlineWidth) { LineJoin = LineJoin.Round })
						CDZ_g.DrawPath(pen2, _reusablePath);
				}

				if (DrawMode == 1)
					CDZ_g.FillPath(sfbrush, _reusablePath);
				else
					CDZ_g.DrawString(c.ToString(), _Font, sfbrush, _reusablePoint);

				Rectangle ef = GetFontGSize(image);
				Bitmap crop = cropImage(image, ef);
				BottomAlign = CDZ_BottomAlign - ef.Y;
				return crop;
			}
			finally
			{
				// 确保路径重置，但保留重用路径
				_reusablePath.Reset();
			}
		}

		/// <summary>
		/// 取得原字型真實高度
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public Size GetOriginFontHeight(char c, out SizeF DisplaySize, out float RealSpace)
		{
			// 使用临时图像避免修改类字段
			using (var tempImage = new Bitmap((int)lineSpacingPixel * 2, (int)lineSpacingPixel))
			{
				// 创建绘图对象
				using (var g = Graphics.FromImage(tempImage))
				{
					// 配置绘图设置
					g.CompositingQuality = CompositingQuality.Default;
					g.InterpolationMode = InterpolationMode.Default;
					g.PixelOffsetMode = PixelOffsetMode.Default;
					g.SmoothingMode = SmoothingMode.None;
					g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

					// 绘制单个字符
					g.Clear(BackColor);
					g.DrawString(c.ToString(), _Font, brush2, Point.Empty);

					// 获取显示尺寸
					DisplaySize = g.MeasureString(c.ToString(), _Font);

					// 使用LockBits获取字符边界
					Rectangle ef1;
					using (var bmpData1 = new BmpPixelData(tempImage))
					{
						ef1 = GetFontBounds(bmpData1);
					}

					if (ef1.Height == 0 && ef1.Width == 0)
					{
						// 空白字符处理
						ef1.Width = (int)SpaceWidth;
						RealSpace = SpaceWidth;
						return new Size(ef1.Width, ef1.Height);
					}

					// 绘制双字符测量间距
					g.Clear(BackColor);
					g.DrawString(c.ToString() + c.ToString(), _Font, brush2, Point.Empty);

					// 使用LockBits获取双字符边界
					Rectangle realdoublespace;
					using (var bmpData2 = new BmpPixelData(tempImage))
					{
						realdoublespace = GetFontBounds(bmpData2);
					}

					// 计算实际间距
					RealSpace = (realdoublespace.Width - (ef1.Width * 2)) / 4;

					return new Size(ef1.Width, ef1.Height);
				}
			}
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
			// 创建临时图像
			using (var tempImage = new Bitmap((int)lineSpacingPixel, (int)lineSpacingPixel))
			using (var g = Graphics.FromImage(tempImage))
			{
				// 配置绘图设置
				g.CompositingQuality = CompositingQuality.Default;
				g.InterpolationMode = InterpolationMode.Default;
				g.PixelOffsetMode = PixelOffsetMode.Default;
				g.SmoothingMode = SmoothingMode.None;
				g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

				// 绘制字符
				g.Clear(BackColor);
				g.DrawString(c.ToString(), _Font, brush2, Point.Empty);

				// 使用LockBits检查非背景像素
				IsEmpty = true;
				var backArgb = BackColor.ToArgb();

				// 使用原始LockBits避免额外封装开销
				var rect = new Rectangle(0, 0, tempImage.Width, tempImage.Height);
				var data = tempImage.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
				try
				{
					// 直接扫描图像数据
					int stride = Math.Abs(data.Stride);
					byte[] pixelData = new byte[stride * tempImage.Height];
					Marshal.Copy(data.Scan0, pixelData, 0, pixelData.Length);

					// 检查每个像素
					for (int y = 0; y < tempImage.Height; y++)
					{
						int row = y * stride;
						for (int x = 0; x < tempImage.Width; x++)
						{
							int idx = row + x * 4;
							int argb = pixelData[idx] |
									   (pixelData[idx + 1] << 8) |
									   (pixelData[idx + 2] << 16) |
									   (pixelData[idx + 3] << 24);

							if (argb != backArgb)
							{
								IsEmpty = false;
								return new Bitmap(tempImage); // 返回克隆图像
							}
						}
					}
				}
				finally
				{
					tempImage.UnlockBits(data);
				}

				return new Bitmap(tempImage); // 返回克隆图像
			}
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
