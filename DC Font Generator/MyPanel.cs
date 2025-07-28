namespace Fallout3_Font_Generator
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Windows.Forms;
    using System.IO;
    using System.Collections;

    internal class MyPanel : Panel
    {
        private FL_FONT iFntFile;
        private Font ifont1;
        private float ifont1_left;
        private float ifont1_right;
        private Font ifont2;
        private bool iisTextOverFlow;
        private Bitmap iTextImage;
        public Size iTextImageSize;
        private PointF sc_i右下角;
        private PointF sc_i左上角;
        private PointF dc_i右下角;
        private PointF dc_i左上角;
        private bool onSave;
        private float dc_Interval_X; //間距X
        private float dc_Interval_Y; //間距Y
        private float sc_Interval_X; //間距X
        private float sc_Interval_Y; //間距Y
        public List<string> Temp;
        public Encoding enc = Encoding.Default;
        public Hashtable TempWith = new Hashtable();
        public bool ASCII_Only = false;
        private PictureBox pb = new PictureBox();
        public float font_shadow = 3f;
        public event EventHandler TextOverFlow;

        private Font _Font;
        private float ascentPixel=0;      // ascent converted to pixels
        private float descentPixel=0;     // descent converted to pixels
        private float higthPixel=0;
        private float lineSpacingPixel=0; // line spacing converted to pixels
        public MyPanel()
        {
            
            this.BackColor = Color.Black;
            this.ifont1 = Control.DefaultFont;
            this.ifont2 = this.ifont1;
            NowFont = this.ifont1;
            this.sc_i左上角 = new PointF();
            this.dc_i左上角 = new PointF();
            this.sc_i右下角 = new PointF();
            this.dc_i右下角 = new PointF();
            this.iFntFile = new FL_FONT();
            this.iTextImageSize = new Size(0x400, 0x400);
            this.iisTextOverFlow = false;
            this.ifont1_left = 0f;
            this.ifont1_right = 0f;
            this.dc_Interval_X = 6f;
            this.dc_Interval_Y = 6f;
            this.sc_Interval_X = 6f;
            this.sc_Interval_Y = 6f;
            Temp = new List<string>(1);
            Temp.Add("0x0000");
        }

        private void FillWithBlack(int times)
        {
            while (times > 0)
            {
                Fnt_char item = new Fnt_char();
                item.BottomAlign = 0f;
                item.charViewHeight = 0f;
                item.charViewWidth = 0f;
                item.LeftSpace = 0f;
                item.RightSpace = 0f;
                item.x1 = 0f;
                item.y1 = 0f;
                item.x2 = 0f;
                item.y2 = 0f;
                item.x3 = 0f;
                item.y3 = 0f;
                item.x4 = 0f;
                item.y4 = 0f;
                this.iFntFile.CharList.Add(item);
                times--;
            }
        }

        private bool isDoubledChar(char c)
        {
            if (enc.GetBytes(c.ToString()).Length == 1)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 計算文字的大小
        /// </summary>
        /// <param name="c"></param>
        /// <param name="font"></param>
        /// <param name="OriginalRectangleF"></param>
        /// <returns></returns>
        private RectangleF MeasureChar(char c, ref RectangleF p, bool dc, Graphics graphics, Brush brush2, ref float LineShiftY)
        {

            Graphics gp = pb.CreateGraphics();
            //SizeF ef = gp.MeasureString(c.ToString(), _Font, OriginalRectangleF.Location, StringFormat.GenericTypographic);

            SizeF ef = new SizeF(lineSpacingPixel + font_shadow, lineSpacingPixel + font_shadow);
            Bitmap image;
            //if (ef.Width>0)
                image = new Bitmap((int) ef.Width, (int) ef.Height);
            //else
            //    image = new Bitmap(1, (int)ef.Height);
            
            Graphics g = Graphics.FromImage(image);
            //g.CompositingQuality=0;
            //g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
            //g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
            //g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

            g.Clear(Color.Black);
            RectangleF p2 = new RectangleF(this.font_shadow, this.font_shadow, (float)image.Width, (float)image.Height);
                

            for (float k = p2.Location.X - font_shadow; k <= (p2.Location.X + font_shadow); k++)
            {
                for (float m = p2.Location.Y - font_shadow; m < (p2.Location.Y + font_shadow); m++)
                {
                    if ((k != p2.Location.X) || (m != p2.Location.Y))
                    {
                        g.DrawString(c.ToString(), _Font, brush, k, m);
                    }
                }
            }

            g.DrawString(c.ToString(), _Font, brush2, p2.Location);
            RectangleF ef2 = new RectangleF();

            //取得左上XY
            bool flag=false;
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    if (!image.GetPixel(x, y).ToArgb().Equals(Color.Black.ToArgb()))
                    {
                        ef2.Y = y;
                        ef2.X = x;
                        flag=true;
                        break;
                    }
                    if(flag) break;
                }
            }
            if (flag)
            {
                flag = false;
                //取得右下XY
                for (int y = image.Height-1; y >= 0; y--)
                {
                    for (int x = image.Width-1; x >= 0; x--)
                    {
                        if (!image.GetPixel(x, y).ToArgb().Equals(Color.Black.ToArgb()))
                        {
                            ef2.Height = y - ef2.Y + 1;
                            ef2.Width = x - ef2.X + 1;
                            flag=true;
                            break;
                        }
                    }
                    if (flag) break;
                }
            }
            float Ix;
            float Iy;
            if (dc)
            {
                Ix = this.dc_Interval_X; Iy = this.dc_Interval_Y;
            }
            else
            {
                Ix = this.sc_Interval_X; Iy = this.sc_Interval_Y;
            }
            if ((p.X + ef2.Width + Ix) >= this.iTextImageSize.Width) //超過X邊界
            {
                p.X = Ix;
                p.Y += LineShiftY + Iy;
                if (p.Y >= this.iTextImageSize.Height)
                {
                    this.iisTextOverFlow = true;
                    this.TextOverFlow(this, new EventArgs());
                    flag = false;
                }
                this.iisTextOverFlow = false;
                this.TextOverFlow(this, new EventArgs());
            }
            //搬移image到this.iTextImage
            if (flag)
            {
                int sx = (int)p.X;
                int sy = (int)p.Y;

                
                for (float y = ef2.Y; y < ef2.Height + ef2.Y; y++)
                {
                    sx = (int)p.X;
                    for (float x = ef2.X; x < ef2.Width+ef2.X; x++)
                    {
                        Color pixel = image.GetPixel((int)x, (int)y);

                        this.iTextImage.SetPixel(sx, sy, pixel);
                        sx++;
                    }
                    sy++;
                }
            }


            image.Dispose();
            g.Dispose();
            return ef2;
        }
        private Font NowFont
        {
            set
            {
                if (_Font != value)
                {
                    _Font = value;
                    FontFamily fontFamily = _Font.FontFamily;

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
                }
            }
        }

        /// <summary>
        /// 計算單字大小
        /// </summary>
        /// <param name="c"></param>
        /// <param name="font"></param>
        /// <returns></returns>
        private SizeF GetFontSize(char c)
        {
            Graphics g = pb.CreateGraphics();
            g.CompositingQuality = 0;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighSpeed;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SystemDefault;
            g.Clear(Color.Black);

            g.DrawString(c.ToString(), _Font, Brushes.White, new PointF(0f, 0f), StringFormat.GenericTypographic);
            SizeF stringSize = g.MeasureString(c.ToString(), _Font, 1000, StringFormat.GenericTypographic);


            return stringSize;
        }
        public Color color = Color.FromArgb(0, Color.Black);
        public Color color2 = Color.FromArgb(0xff, Color.White);
        public Brush brush = new Pen(Color.FromArgb(200, Color.FromArgb(80, 80, 80)), 2f).Brush;
        private Fnt_char SC_SpaceItem = new Fnt_char();
        private Fnt_char DC_SpaceItem = new Fnt_char();
        bool SC_SpaceIsEmpty = true;
        bool DC_SpaceIsEmpty = true;

        public void draw()
        {
            SC_SpaceIsEmpty = true;
            DC_SpaceIsEmpty = true;
            if (this.iTextImage != null)
                this.iTextImage.Dispose();

            Brush brush2 = new Pen(color2).Brush;
            this.iTextImage = new Bitmap(this.iTextImageSize.Width, this.iTextImageSize.Height);
            //int num = 3; //文字陰影範圍
            Graphics graphics = Graphics.FromImage(this.iTextImage);
            graphics.PageUnit = GraphicsUnit.Pixel;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            RectangleF p = new RectangleF(this.sc_Interval_X / 2f, this.sc_Interval_Y / 2f, 0f, 0f);
            graphics.Clear(color);
            this.iFntFile.reset();
            float Ix = this.sc_Interval_X;
            float Iy = this.sc_Interval_Y;
            float LineShiftY = 0f;
            NowFont = this.font1;
            foreach (string str in Temp)
            {
                if (str.Length == 6)
                {
                    this.FillWithBlack(1);
                    continue;
                }
                int num2 = int.Parse(str.Substring(2, 4), NumberStyles.AllowHexSpecifier);
                byte[] buffer;
                char c = '\0';

                bool dc = false;
                if ((num2 / 0x100) > 0)
                {
                    if (ASCII_Only) continue;
                    buffer = new byte[] { (byte)(num2 / 0x100), (byte)(num2 % 0x100) };
                    c = enc.GetChars(buffer)[0];
                    NowFont = this.font2;
                    dc = true;
                }
                else
                {
                    buffer = new byte[] { (byte)num2 };
                    c = Encoding.GetEncoding("ASCII").GetChars(buffer)[0];
                    NowFont = this.font1;
                }

                p.Size = GetFontSize(c); //取得文字大小

                if (p.Size.IsEmpty)
                {
                    this.FillWithBlack(1);
                    continue;
                }
                bool IsSpace = false;
                if (p.Size.Width == 0)
                {
                    p.Size = GetFontSize('Z');
                    IsSpace = true;
                }
                else
                    IsSpace = false;

                bool drawed = false;

                drawed = CreateFont(ref p, c, graphics, ref LineShiftY, dc, IsSpace, brush2);
                /*
                if (dc)
                {

                    DrawFont(p, graphics, c, brush2);
                    
                }
                else
                {

                    DrawFont(p, graphics, c, brush2);
                    drawed = CreateFont(ref p, c, graphics, ref LineShiftY, dc, IsSpace, brush2);
                }
                */

                if (drawed)
                {
                    if ((p.X + p.Width + Ix) >= this.iTextImageSize.Width) //超過X邊界
                    {
                        p.X = Ix;
                        p.Y += LineShiftY + Iy;
                        if (p.Y >= this.iTextImageSize.Height)
                        {
                            this.iisTextOverFlow = true;
                            this.TextOverFlow(this, new EventArgs());
                            break;
                        }
                        this.iisTextOverFlow = false;
                        this.TextOverFlow(this, new EventArgs());
                    }
                }
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            if (this.iTextImage!=null)
            e.Graphics.DrawImage(this.iTextImage, new Point(0, 0));
            
            //foreach (Fnt_char _char3 in this.iFntFile.CharList)
            //{
            //    _char3.BottomAlign -= _char3.BottomAlign / 4f;
            //}
        }

        private void DrawFont(RectangleF p, Graphics graphics, char c, Brush brush2)
        {
            for (float k = p.Location.X - font_shadow; k <= (p.Location.X + font_shadow); k++)
            {
                for (float m = p.Location.Y - font_shadow; m < (p.Location.Y + font_shadow); m++)
                {
                    if ((k != p.Location.X) || (m != p.Location.Y))
                    {
                        graphics.DrawString(c.ToString(), _Font, brush, k, m);
                    }
                }
            }
            graphics.DrawString(c.ToString(), _Font, brush2, p.Location);

        }
        private bool CreateFont(ref RectangleF p, char c, Graphics graphics, ref float LineShiftY, bool dc, bool IsSpace, Brush brush2)
        {
            //p.Size=new SizeF(this.font1.Size, this.font1.Size);

            
            
            //RectangleF ef = new RectangleF(p.Location, p.Size);
            RectangleF ef = MeasureChar(c, ref p, dc, graphics, brush2,ref LineShiftY);

            //ef3.X += this.ifont1_left;
            //ef3.Width += this.ifont1_right + this.ifont1_left;
            ef.X += this.sc_i左上角.X;// -(float)font_shadow;
            ef.Y += this.sc_i左上角.Y;// -(float)font_shadow;
            ef.Width += this.sc_i右下角.X;// +(float)(font_shadow * 2);
            ef.Height += this.sc_i右下角.Y;// +(float)(font_shadow * 2);
            Fnt_char fnt = new Fnt_char();
            //fnt.BottomAlign = ef.Height;    //底部對齊位置
            fnt.BottomAlign = ascentPixel;
            fnt.charViewHeight = ef.Height; //顯示高度
            fnt.charViewWidth = ef.Width;   //顯示寬度
            fnt.LeftSpace = 0f;
            fnt.RightSpace = 0f;
            fnt.x1 = ef.X / ((float)this.iTextImageSize.Width);                 //圖片左上X
            fnt.y1 = ef.Y / ((float)this.iTextImageSize.Height);                //圖片左上Y
            fnt.x2 = (ef.X + ef.Width) / ((float)this.iTextImageSize.Width);   //圖片右下X
            fnt.y2 = ef.Y / ((float)this.iTextImageSize.Height);                //圖片右下Y
            fnt.x3 = fnt.x1;                 //左上X
            fnt.y3 = fnt.y1; //左上Y
            fnt.x4 = fnt.x2;   //右下X
            fnt.y4 = fnt.y2; //右下Y



            if (IsSpace)
            {
                if (!dc)
                {
                    if (SC_SpaceIsEmpty) //保存Space的資料
                    {
                        SC_SpaceItem = fnt;
                        SC_SpaceIsEmpty = false;
                    }
                    else  //使用之前保存Space的資料
                    {
                        fnt = SC_SpaceItem;
                        this.iFntFile.CharList.Add(fnt);
                        return false; //不繼續繪圖
                    }
                }
                else
                {
                    if (IsSpace)
                    {
                        if (DC_SpaceIsEmpty) //保存Space的資料
                        {
                            DC_SpaceItem = fnt;
                            DC_SpaceIsEmpty = false;
                        }
                        else //使用之前保存Space的資料
                        {
                            fnt = SC_SpaceItem;
                            this.iFntFile.CharList.Add(fnt);
                            return false; //不繼續繪圖
                        }
                    }
                }
            }
            float LineShiftX = fnt.charViewWidth + this.sc_Interval_X;


            if (LineShiftY < fnt.charViewHeight)
            {
                LineShiftY = fnt.charViewHeight;
            }

            this.iFntFile.CharList.Add(fnt);
            this.iFntFile.Header.LineHeight = (this.iFntFile.Header.LineHeight < ef.Height) ? ef.Height : this.iFntFile.Header.LineHeight;

            if (!this.onSave)
            {
                graphics.DrawRectangle(Pens.Red, ef.X, ef.Y, ef.Width, ef.Height);
            }
            //this.PointFOffset(ref p, p.Width + this.sc_Interval_X, 0f);
            this.PointFOffset(ref p, LineShiftX, 0f);
            return true;

        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            base.Invalidate();
        }

        private void PointFOffset(ref PointF p, float x, float y)
        {
            p.X = p.X + x;
            p.Y = p.Y + y;
        }

        private void PointFOffset(ref RectangleF p, float x, float y)
        {
            p.X += x;
            p.Y += y;
        }

        public void PreSaveRefresh(bool onSave)
        {
            this.onSave = onSave;
            base.Invalidate();
        }
        /*
        public List<string> chars
        {
            get
            {
                return this.ichars;
            }
            set
            {
                this.ichars = value;
                base.Invalidate();
            }
        }
        */
        public FL_FONT FntFile
        {
            get
            {
                return this.iFntFile;
            }
        }

        public Font font1
        {
            get
            {
                return this.ifont1;
            }
            set
            {
                this.ifont1 = value;
                base.Invalidate();
            }
        }

        public SizeF font1_frameoffset
        {
            set
            {
                this.ifont1_left = value.Width;
                this.ifont1_right = value.Height;
                base.Invalidate();
            }
        }

        public Font font2
        {
            get
            {
                return this.ifont2;
            }
            set
            {
                this.ifont2 = value;
                base.Invalidate();
            }
        }

        public bool isTextOverFlow
        {
            get
            {
                return this.iisTextOverFlow;
            }
        }

        public Bitmap TextImage
        {
            get
            {
                return this.iTextImage;
            }
        }

        public Size TextImageSize
        {
            get
            {
                return this.iTextImageSize;
            }
            set
            {
                this.iTextImageSize = value;
                base.Invalidate();
            }
        }

        public PointF SC右下角
        {
            set
            {
                this.sc_i右下角 = value;
                base.Invalidate();
            }
        }

        public PointF SC左上角
        {
            set
            {
                this.sc_i左上角 = value;
                base.Invalidate();
            }
        }
        public PointF DC右下角
        {
            set
            {
                this.dc_i右下角 = value;
                base.Invalidate();
            }
        }

        public PointF DC左上角
        {
            set
            {
                this.dc_i左上角 = value;
                base.Invalidate();
            }
        }
        public float SC_Interval_X
        {
            set
            {
                this.sc_Interval_X = value;
                base.Invalidate();
            }
        }
        public float SC_Interval_Y
        {
            set
            {
                this.sc_Interval_Y = value;
                base.Invalidate();
            }
        }
        public float DC_Interval_X
        {
            set
            {
                this.dc_Interval_X = value;
                base.Invalidate();
            }
        }
        public float DC_Interval_Y
        {
            set
            {
                this.dc_Interval_Y = value;
                base.Invalidate();
            }
        }


        public int MakeTemp()
        {
            //清空樣版
            //Temp = new string[24322];
            Temp = new List<string>();
            TempWith = new Hashtable();

            for (int i = 0; i < 256; i++)
            {
                Temp.Add("0x" + i.ToString("X4"));
                TempWith.Add(i.ToString(), i.ToString());
            }
            int TmpCount = 256;
            for (uint hh = 0x81; hh <= 0xFE; hh++) //81 FE //A1 F7
            {
                for (uint ll = 0x40; ll <= 0xFE; ll++) //40 FE //A1 FE
                {
                    uint hex = (hh * 256) + ll;
                    Temp.Add("0x" + hex.ToString("X4"));
                    TempWith.Add(hex.ToString(), TmpCount.ToString());
                    TmpCount++;
                }

            }

            int count = 0;

            //輸出ASCII
            for (byte b = 0x20; b <= 0x7F; b++)
            {
                char c = Convert.ToChar(b);
                Temp[b] = "0x" + b.ToString("X4") + " " + c;
                count++;
            }


            return count;

        }
        public int output(UInt16 start, UInt16 end)
        {
            int count = 0;
            for (UInt16 i = start; i <= end; i++)
            {
                string si = i.ToString();
                string sindex = (string)TempWith[si];
                int index = int.Parse(sindex);
                byte[] buffer = new byte[] { (byte)(i / 0x100), (byte)(i % 0x100) };
                char c = enc.GetChars(buffer)[0];

                Temp[index] = "0x" + i.ToString("X4") + " " + c.ToString();
                count = count + 1;

            }
            return count;
        }

        public int Big5()
        {
            enc = Encoding.GetEncoding(950);
            int count = MakeTemp();
            count += output(0xA140, 0xA17E); count += output(0xA1A1, 0xA1FE);
            count += output(0xA240, 0xA27E); count += output(0xA2A1, 0xA2FE);
            count += output(0xA340, 0xA37E); count += output(0xA3A1, 0xA3BF); count += output(0xA3E1, 0xA3E1);
            count += output(0xA440, 0xA47E); count += output(0xA4A1, 0xA4FE);
            count += output(0xA540, 0xA57E); count += output(0xA5A1, 0xA5FE);
            count += output(0xA640, 0xA67E); count += output(0xA6A1, 0xA6FE);
            count += output(0xA740, 0xA77E); count += output(0xA7A1, 0xA7FE);
            count += output(0xA840, 0xA87E); count += output(0xA8A1, 0xA8FE);
            count += output(0xA940, 0xA97E); count += output(0xA9A1, 0xA9FE);
            count += output(0xAA40, 0xAA7E); count += output(0xAAA1, 0xAAFE);
            count += output(0xAB40, 0xAB7E); count += output(0xABA1, 0xABFE);
            count += output(0xAC40, 0xAC7E); count += output(0xACA1, 0xACFE);
            count += output(0xAD40, 0xAD7E); count += output(0xADA1, 0xADFE);
            count += output(0xAE40, 0xAE7E); count += output(0xAEA1, 0xAEFE);
            count += output(0xAF40, 0xAF7E); count += output(0xAFA1, 0xAFFE);

            count += output(0xB040, 0xB07E); count += output(0xB0A1, 0xB0FE);
            count += output(0xB140, 0xB17E); count += output(0xB1A1, 0xB1FE);
            count += output(0xB240, 0xB27E); count += output(0xB2A1, 0xB2FE);
            count += output(0xB340, 0xB37E); count += output(0xB3A1, 0xB3FE);
            count += output(0xB440, 0xB47E); count += output(0xB4A1, 0xB4FE);
            count += output(0xB540, 0xB57E); count += output(0xB5A1, 0xB5FE);
            count += output(0xB640, 0xB67E); count += output(0xB6A1, 0xB6FE);
            count += output(0xB740, 0xB77E); count += output(0xB7A1, 0xB7FE);
            count += output(0xB840, 0xB87E); count += output(0xB8A1, 0xB8FE);
            count += output(0xB940, 0xB97E); count += output(0xB9A1, 0xB9FE);
            count += output(0xBA40, 0xBA7E); count += output(0xBAA1, 0xBAFE);
            count += output(0xBB40, 0xBB7E); count += output(0xBBA1, 0xBBFE);
            count += output(0xBC40, 0xBC7E); count += output(0xBCA1, 0xBCFE);
            count += output(0xBD40, 0xBD7E); count += output(0xBDA1, 0xBDFE);
            count += output(0xBE40, 0xBE7E); count += output(0xBEA1, 0xBEFE);
            count += output(0xBF40, 0xBF7E); count += output(0xBFA1, 0xBFFE);

            count += output(0xC040, 0xC07E); count += output(0xC0A1, 0xC0FE);
            count += output(0xC140, 0xC17E); count += output(0xC1A1, 0xC1FE);
            count += output(0xC240, 0xC27E); count += output(0xC2A1, 0xC2FE);
            count += output(0xC340, 0xC37E); count += output(0xC3A1, 0xC3FE);
            count += output(0xC440, 0xC47E); count += output(0xC4A1, 0xC4FE);
            count += output(0xC540, 0xC57E); count += output(0xC5A1, 0xC5FE);
            count += output(0xC640, 0xC67E); //count += output(0xC6A1, 0xC6FE);
            //count += output(0xC740, 0xC77E); count += output(0xC7A1, 0xC7FE);
            //count += output(0xC840, 0xC87E); count += output(0xC8A1, 0xC8FE);
            count += output(0xC940, 0xC97E); count += output(0xC9A1, 0xC9FE);
            count += output(0xCA40, 0xCA7E); count += output(0xCAA1, 0xCAFE);
            count += output(0xCB40, 0xCB7E); count += output(0xCBA1, 0xCBFE);
            count += output(0xCC40, 0xCC7E); count += output(0xCCA1, 0xCCFE);
            count += output(0xCD40, 0xCD7E); count += output(0xCDA1, 0xCDFE);
            count += output(0xCE40, 0xCE7E); count += output(0xCEA1, 0xCEFE);
            count += output(0xCF40, 0xCF7E); count += output(0xCFA1, 0xCFFE);

            count += output(0xD040, 0xD07E); count += output(0xD0A1, 0xD0FE);
            count += output(0xD140, 0xD17E); count += output(0xD1A1, 0xD1FE);
            count += output(0xD240, 0xD27E); count += output(0xD2A1, 0xD2FE);
            count += output(0xD340, 0xD37E); count += output(0xD3A1, 0xD3FE);
            count += output(0xD440, 0xD47E); count += output(0xD4A1, 0xD4FE);
            count += output(0xD540, 0xD57E); count += output(0xD5A1, 0xD5FE);
            count += output(0xD640, 0xD67E); count += output(0xD6A1, 0xD6FE);
            count += output(0xD740, 0xD77E); count += output(0xD7A1, 0xD7FE);
            count += output(0xD840, 0xD87E); count += output(0xD8A1, 0xD8FE);
            count += output(0xD940, 0xD97E); count += output(0xD9A1, 0xD9FE);
            count += output(0xDA40, 0xDA7E); count += output(0xDAA1, 0xDAFE);
            count += output(0xDB40, 0xDB7E); count += output(0xDBA1, 0xDBFE);
            count += output(0xDC40, 0xDC7E); count += output(0xDCA1, 0xDCFE);
            count += output(0xDD40, 0xDD7E); count += output(0xDDA1, 0xDDFE);
            count += output(0xDE40, 0xDE7E); count += output(0xDEA1, 0xDEFE);
            count += output(0xDF40, 0xDF7E); count += output(0xDFA1, 0xDFFE);

            count += output(0xE040, 0xE07E); count += output(0xE0A1, 0xE0FE);
            count += output(0xE140, 0xE17E); count += output(0xE1A1, 0xE1FE);
            count += output(0xE240, 0xE27E); count += output(0xE2A1, 0xE2FE);
            count += output(0xE340, 0xE37E); count += output(0xE3A1, 0xE3FE);
            count += output(0xE440, 0xE47E); count += output(0xE4A1, 0xE4FE);
            count += output(0xE540, 0xE57E); count += output(0xE5A1, 0xE5FE);
            count += output(0xE640, 0xE67E); count += output(0xE6A1, 0xE6FE);
            count += output(0xE740, 0xE77E); count += output(0xE7A1, 0xE7FE);
            count += output(0xE840, 0xE87E); count += output(0xE8A1, 0xE8FE);
            count += output(0xE940, 0xE97E); count += output(0xE9A1, 0xE9FE);
            count += output(0xEA40, 0xEA7E); count += output(0xEAA1, 0xEAFE);
            count += output(0xEB40, 0xEB7E); count += output(0xEBA1, 0xEBFE);
            count += output(0xEC40, 0xEC7E); count += output(0xECA1, 0xECFE);
            count += output(0xED40, 0xED7E); count += output(0xEDA1, 0xEDFE);
            count += output(0xEE40, 0xEE7E); count += output(0xEEA1, 0xEEFE);
            count += output(0xEF40, 0xEF7E); count += output(0xEFA1, 0xEFFE);

            count += output(0xF040, 0xF07E); count += output(0xF0A1, 0xF0FE);
            count += output(0xF140, 0xF17E); count += output(0xF1A1, 0xF1FE);
            count += output(0xF240, 0xF27E); count += output(0xF2A1, 0xF2FE);
            count += output(0xF340, 0xF37E); count += output(0xF3A1, 0xF3FE);
            count += output(0xF440, 0xF47E); count += output(0xF4A1, 0xF4FE);
            count += output(0xF540, 0xF57E); count += output(0xF5A1, 0xF5FE);
            count += output(0xF640, 0xF67E); count += output(0xF6A1, 0xF6FE);
            count += output(0xF740, 0xF77E); count += output(0xF7A1, 0xF7FE);
            count += output(0xF840, 0xF87E); count += output(0xF8A1, 0xF8FE);
            count += output(0xF940, 0xF97E); count += output(0xF9A1, 0xF9FE);
            //count += output(0xFA40, 0xFA7E); count += output(0xFAA1, 0xFAFE);
            //count += output(0xFB40, 0xFB7E); count += output(0xFBA1, 0xFBFE);
            //count += output(0xFC40, 0xFC7E); count += output(0xFCA1, 0xFCFE);
            //count += output(0xFD40, 0xFD7E); count += output(0xFDA1, 0xFDFE);
            //count += output(0xFE40, 0xFE7E); count += output(0xFEA1, 0xFEFE);
            return count;
        }


        public int GB2312()
        {
            enc = Encoding.GetEncoding(936);
            int count = MakeTemp();
            count += output( 0xA1A1, 0xA1FE);
            //count += output(0xA2A1, 0xA2AA); count += output(0xA2B1, 0xA2E2); count += output(0xA2E5, 0xA2EE); count += output(0xA2F1, 0xA2FC);

            count += output( 0xA3A1, 0xA3FE);
            count += output( 0xA4A1, 0xA4F3);
            count += output( 0xA5A1, 0xA5F6);
            //count += output(0xA6A1, 0xA6B8); count += output(0xA6C1, 0xA6D8); count += output(0xA6E0, 0xA6EB); count += output(0xA6EE, 0xA6F2); count += output(0xA6F4, 0xA6F5);

            //count += output(0xA7A1, 0xA7C1); count += output(0xA7D1, 0xA7F1);
            //count += output(0xA8A1, 0xA8C0); count += output(0xA8C5, 0xA8E9);
            //count += output( 0xA9A4, 0xA9F4);

            count += output( 0xB0A1, 0xB0FE);
            count += output( 0xB1A1, 0xB1FE);
            count += output( 0xB2A1, 0xB2FE);
            count += output( 0xB3A1, 0xB3FE);
            count += output( 0xB4A1, 0xB4FE);
            count += output( 0xB5A1, 0xB5FE);
            count += output( 0xB6A1, 0xB6FE);
            count += output( 0xB7A1, 0xB7FE);
            count += output( 0xB8A1, 0xB8FE);
            count += output( 0xB9A1, 0xB9FE);
            count += output( 0xBAA1, 0xBAFE);
            count += output( 0xBBA1, 0xBBFE);
            count += output( 0xBCA1, 0xBCFE);
            count += output( 0xBDA1, 0xBDFE);
            count += output( 0xBEA1, 0xBEFE);
            count += output( 0xBFA1, 0xBFFE);

            count += output( 0xC0A1, 0xC0FE);
            count += output( 0xC1A1, 0xC1FE);
            count += output( 0xC2A1, 0xC2FE);
            count += output( 0xC3A1, 0xC3FE);
            count += output( 0xC4A1, 0xC4FE);
            count += output( 0xC5A1, 0xC5FE);
            count += output( 0xC6A1, 0xC6FE);
            count += output( 0xC7A1, 0xC7FE);
            count += output( 0xC8A1, 0xC8FE);
            count += output( 0xC9A1, 0xC9FE);
            count += output( 0xCAA1, 0xCAFE);
            count += output( 0xCBA1, 0xCBFE);
            count += output( 0xCCA1, 0xCCFE);
            count += output( 0xCDA1, 0xCDFE);
            count += output( 0xCEA1, 0xCEFE);
            count += output( 0xCFA1, 0xCFFE);

            count += output( 0xD0A1, 0xD0FE);
            count += output( 0xD1A1, 0xD1FE);
            count += output( 0xD2A1, 0xD2FE);
            count += output( 0xD3A1, 0xD3FE);
            count += output( 0xD4A1, 0xD4FE);
            count += output( 0xD5A1, 0xD5FE);
            count += output( 0xD6A1, 0xD6FE);
            count += output( 0xD7A1, 0xD7FE);
            count += output( 0xD8A1, 0xD8FE);
            count += output( 0xD9A1, 0xD9FE);
            count += output( 0xDAA1, 0xDAFE);
            count += output( 0xDBA1, 0xDBFE);
            count += output( 0xDCA1, 0xDCFE);
            count += output( 0xDDA1, 0xDDFE);
            count += output( 0xDEA1, 0xDEFE);
            count += output( 0xDFA1, 0xDFFE);

            count += output( 0xE0A1, 0xE0FE);
            count += output( 0xE1A1, 0xE1FE);
            count += output( 0xE2A1, 0xE2FE);
            count += output( 0xE3A1, 0xE3FE);
            count += output( 0xE4A1, 0xE4FE);
            count += output( 0xE5A1, 0xE5FE);
            count += output( 0xE6A1, 0xE6FE);
            count += output( 0xE7A1, 0xE7FE);
            count += output( 0xE8A1, 0xE8FE);
            count += output( 0xE9A1, 0xE9FE);
            count += output( 0xEAA1, 0xEAFE);
            count += output( 0xEBA1, 0xEBFE);
            count += output( 0xECA1, 0xECFE);
            count += output( 0xEDA1, 0xEDFE);
            count += output( 0xEEA1, 0xEEFE);
            count += output( 0xEFA1, 0xEFFE);

            count += output( 0xF0A1, 0xF0FE);
            count += output( 0xF1A1, 0xF1FE);
            count += output( 0xF2A1, 0xF2FE);
            count += output( 0xF3A1, 0xF3FE);
            count += output( 0xF4A1, 0xF4FE);
            count += output( 0xF5A1, 0xF5FE);
            count += output( 0xF6A1, 0xF6FE);
            count += output( 0xF7A1, 0xF7FE);
            return count;
        }

        public int SJIS()
        {
            enc = Encoding.GetEncoding(932);
            int count = MakeTemp();
            count += output(0x8140, 0x817E); count += output(0x8180, 0x81AC); count += output(0x81B8, 0x81BF); count += output(0x81C8, 0x81CE); count += output(0x81DA, 0x81DF); count += output(0x81E0, 0x81E8); count += output(0x81F0, 0x81F7); count += output(0x81FC, 0x81FC);
            count += output(0x824F, 0x8258); count += output(0x8260, 0x8279); count += output(0x8281, 0x829A); count += output(0x829F, 0x82F1);
            count += output(0x8340, 0x837E); count += output(0x8380, 0x8396); count += output(0x839F, 0x83B6); count += output(0x83BF, 0x83D6);
            count += output(0x8440, 0x8460); count += output(0x8470, 0x847E); count += output(0x8480, 0x8491); count += output(0x849F, 0x84BE);
            count += output(0x8740, 0x875D); count += output(0x875F, 0x8775); count += output(0x877E, 0x877E); count += output(0x8780, 0x879C);

            count += output( 0x889F, 0x88FC);

            for (uint i = 0x89; i <= 0x9F; i++)
            {
                uint a = (i * 256) + 0x40;
                uint b = (i * 256) + 0x7E;
                count += output((ushort)a, (ushort)b);

                a = (i * 256) + 0x80;
                b = (i * 256) + 0xFC;
                count += output((ushort)a, (ushort)b);
            }

            for (uint i = 0xE0; i <= 0xE9; i++)
            {
                uint a = (i * 256) + 0x40;
                uint b = (i * 256) + 0x7E;
                count += output((ushort)a, (ushort)b);

                a = (i * 256) + 0x80;
                b = (i * 256) + 0xFC;
                count += output((ushort)a, (ushort)b);
            }

            count += output( 0xEA40, 0xEA7E); count += output( 0xEA80, 0xEAA4);

            count += output(0xED40, 0xED7E); count += output(0xED80, 0xEDEC); count += output(0xEDEF, 0xEDFC);
            count += output( 0xEE40, 0xEE7E); count += output( 0xEE80, 0xEEFC);

            count += output( 0xFA40, 0xFA7E); count += output( 0xFA80, 0xFAFC);
            count += output( 0xFB40, 0xFB7E); count += output( 0xFB80, 0xFBFC);
            count += output( 0xFC40, 0xFC4B);
            return count;
        }
        public int Korea()
        {
            enc = Encoding.GetEncoding(949);
            int count = MakeTemp();
            for (uint i = 0x81; i <= 0xC5; i++)
            {
                uint a = (i * 256) + 0x41;
                uint b = (i * 256) + 0x5A;
                count += output((ushort)a, (ushort)b);

                a = (i * 256) + 0x60;
                b = (i * 256) + 0x7A;
                count += output((ushort)a, (ushort)b);

                a = (i * 256) + 0x81;
                b = (i * 256) + 0xFE;
                count += output((ushort)a, (ushort)b);
            }

            count += output(0xC641, 0xC652); count += output(0xC6A1, 0xC6FE);
            count += output(0xC7A1, 0xC7FE);
            count += output(0xC8A1, 0xC8FE);
            for (uint i = 0xCA; i <= 0xFD; i++)
            {
                uint a = (i * 256) + 0xA1;
                uint b = (i * 256) + 0xFE;
                count += output((ushort)a, (ushort)b);
            }
   

            return count;
        }
        public void WriteToFile()
        {
            StreamWriter sw = new StreamWriter("CodepageDebug.txt", false, enc);
            for (int i = 0; i < Temp.Count; i++)
            {
                sw.WriteLine(Temp[i]);
            }
            sw.Close();
        }

    }
}

