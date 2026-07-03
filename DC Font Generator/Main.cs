using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DC_Font_Generator
{
    public class Main
    {
        #region Members

        public int ID = 0;
        public string name = ""; //fnt名稱
        private FL_FONT iFntFile;
        private Font ifont1;
        private Font ifont2;
        private bool iisTextOverFlow;

        public string ImportFont1name = "";
        public string ImportFont2name = "";

        public string PictureFileName = "";
        public event EventHandler TextOverFlow; //圖片空間不足事件

        public bool SkipASCII = false; //忽略ASCII的輸出
        public bool fixedFont = false; //等寬字旗標

        private Font _Font;

        private DrawFont SysDraw = new DrawFont();
        public float FontMaxWidth = 17;
        public float FontMaxHeight = 0;

        public int DCfontLink = -1; //如果大於-1，代表fnt要使用別的區段
        public List<Main> parent;
        public List<bool> Fallout3INI = new List<bool>(8);//所屬ini的編號(由1開始)
        #endregion

        private sealed class FontBuildItem
        {
            public string Hex;
            public char Character;
            public bool IsDC;
            public bool UseFont2;
            public bool IsEmpty;
            public Fnt_char Fnt;
            public float Height;
        }

        private sealed class FontRenderSettings
        {
            public Color BackColor;
            public int DrawMode;
            public int Glow;
            public Color GlowColor;
            public int Outline;
            public Color OutlineColor;
            public Color FontColor;
        }

        private sealed class FontRenderState
        {
            public DrawFont Renderer;
            public Font Font1;
            public Font Font2;
            public Font ActiveFont;
        }


        #region Constructors

        public Main(List<Main> P,int id)
        {
            this.parent = P;
            
            this.ifont1 = SystemFonts.DefaultFont;
            this.ifont2 = this.ifont1;
            this.NowFont = this.ifont1;
            this.iFntFile = new FL_FONT();
            this.iisTextOverFlow = false;

            this.ID = id;
            for (int i = 0; i < 8; i++)
                Fallout3INI.Add(false);
        }

        #endregion

        public void LinkClone()
        {
            if (DCfontLink < 0) return;
            Main Source = parent[DCfontLink];
            int max = Source.FntFile.CharList.Count;
            if (Source.FntFile.CharList.Count < 256) return;
            
            for (int i = 256; i < max; i++)
            {
                Fnt_char sf = Source.FntFile.CharList[i];
                Fnt_char tf = new Fnt_char();

                tf.ID = ID;
                tf.IsDC = true;
                tf.c = sf.c;
                tf.HEX = sf.HEX;
                tf.Enable = sf.Enable;
                if (sf.Enable)
                {
                    tf.BottomAlign = sf.BottomAlign;
                    tf.BottomAlignFixed = sf.BottomAlignFixed;
                    
                    tf.charViewHeight = sf.charViewHeight;
                    tf.charViewHeightFixed = sf.charViewHeightFixed;
                    tf.charViewWidth = sf.charViewWidth;
                    tf.charViewWidthFixed = sf.charViewWidthFixed;
                    tf.Empty = sf.Empty;
                    tf.FixedWidth = sf.FixedWidth;
                    tf.IsSpace = sf.IsSpace;
                    tf.LeftSpace = sf.LeftSpace;
                    tf.LeftSpaceFixed = sf.LeftSpaceFixed;
                    tf.RightSpace = sf.RightSpace;
                    tf.RightSpaceFixed = sf.RightSpaceFixed;
                    tf.x1 = sf.x1;
                    tf.x2 = sf.x2;
                    tf.x3 = sf.x3;
                    tf.x4 = sf.x4;
                    tf.y1 = sf.y1;
                    tf.y2 = sf.y2;
                    tf.y3 = sf.y3;
                    tf.y4 = sf.y4;
                    
                }


                FntFile.Add(tf, tf.HEX, ID);
                    
            }
            //FntFile.EmptyDC
        }

        public void Clear()
        {
            if (this.ImportFont2name != "") return;
            if (this.ImportFont1name != "") 
                this.iFntFile.reset(true); //保留ASCII的部分
            else
                this.iFntFile.reset(false);
        }

        /// <summary>
        /// 重繪製
        /// </summary>
        public bool NewDrawing(FontEncoding enc, IProgress<FontProgress> progress = null)
        {
            this.iisTextOverFlow = false;
            this.TextOverFlow?.Invoke(this, new EventArgs());

            ReportProgress(progress, "Manufacturing", 0, enc.Temp.Count);




            NowFont = this.font1;

            List<FontBuildItem> buildItems = new List<FontBuildItem>();
            HashSet<string> pendingCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int projectedCount = this.iFntFile.CharList.Count;

            int loop_count = -1;
            //製作全文字
            foreach (string str in enc.Temp)
            {
                loop_count++;
                ReportProgress(progress, "Manufacturing", loop_count + 1, enc.Temp.Count);

                string hex = str.Substring(2, 4);
                if (projectedCount >= 24322) continue; //已經都跑過了
                if (this.iFntFile.HasCode(hex) || pendingCodes.Contains(hex))
                {
                    continue; //已經有的字就跳過
                }
                if (DCfontLink > -1 && loop_count>255) continue; //是連結字

                if (str.Length == 6)
                {
                    buildItems.Add(new FontBuildItem { Hex = hex, IsEmpty = true }); //無效的字碼
                    pendingCodes.Add(hex);
                    projectedCount++;
                    continue;
                }

                bool dc;
                bool IsError;
                char c = enc.CheckFontCode(str,out dc,out IsError);
                
                if (IsError)
                {
                    buildItems.Add(new FontBuildItem { Hex = hex, IsEmpty = true });
                    pendingCodes.Add(hex);
                    projectedCount++;
                    continue;
                }

                Font itemFont;
                if (dc)
                {
                    itemFont = this.font2;
                }
                else
                {
                    itemFont = this.font1;

                }

                NowFont = itemFont;
                buildItems.Add(new FontBuildItem
                {
                    Hex = hex,
                    Character = c,
                    IsDC = dc,
                    UseFont2 = dc
                });
                pendingCodes.Add(hex);
                projectedCount++;
            }

            int renderCount = 0;
            foreach (FontBuildItem item in buildItems)
            {
                if (!item.IsEmpty) renderCount++;
            }
            if (renderCount > 0)
            {
                ReportProgress(progress, "Manufacturing", enc.Temp.Count, enc.Temp.Count + renderCount);
            }

            RenderFontBuildItems(buildItems, enc.Temp.Count, progress);
            foreach (FontBuildItem item in buildItems)
            {
                if (item.IsEmpty)
                {
                    this.iFntFile.AddEmpty(item.Hex, ID);
                    continue;
                }

                this.iFntFile.Add(item.Fnt, item.Hex, ID);
                RegisterFontHeight(item.Height);
            }

            //修正同寬字
            if (fixedFont && ImportFont1name == "" && ImportFont2name == "")
            {
                FixedFont(fixedFont, this.FontMaxWidth);
				float lineHeight1 = this.font1.Height;
				float lineHeight2 = this.font2.Height;
				this.iFntFile.Header.LineHeight = Math.Max(lineHeight1, lineHeight2);
			}
            else if (ImportFont1name == "" && ImportFont2name == "")
            {

				//this.iFntFile.Header.LineHeight = SysDraw.lineSpacingPixel;

				//this.iFntFile.Header.LineHeight = (float)FontMaxHeight * 1.3f;
				//登記行高

				float lineHeight1 = this.font1.Height;
				float lineHeight2 = this.font2.Height;
				this.iFntFile.Header.LineHeight = Math.Max(lineHeight1, lineHeight2);

			}
            NormalizeBaselines();
            return true;
        }
        /// <summary>
        /// 固定寬度修正
        /// </summary>
        /// <param name="IsFixed"></param>
        /// <param name="fixedwidth"></param>
        public void FixedFont(bool IsFixed,float fixedwidth)
        {
            this.fixedFont = IsFixed;
            if (IsFixed)
            {
                this.FontMaxWidth = fixedwidth;
                if (ImportFont1name == "") this.iFntFile.FixedWidth = fixedwidth;

                foreach (Fnt_char fnt in this.iFntFile.CharList)
                {
                    if (!fnt.Enable) continue;
                    //if (SkipASCII && !fnt.IsDC) continue;
                    //if (fnt.FixedWidth == FontMaxWidth) continue; //已經處理過

                    if (FontMaxWidth > fnt.charViewWidth)
                    {
                        float shift = ((float)FontMaxWidth - fnt.charViewWidth) / 2f;

                        fnt.LeftSpace = shift; fnt.LeftSpaceFixed = 0;
                        fnt.RightSpace = shift; fnt.RightSpaceFixed = 0;
                    }
                    else if (fnt.charViewWidth > FontMaxWidth)
                    {
                        float shift = (fnt.charViewWidth - (float)FontMaxWidth) / 2f;

                        fnt.LeftSpace = -shift; fnt.LeftSpaceFixed = 0;
                        fnt.RightSpace = -shift; fnt.RightSpaceFixed = 0;
                    }
                    else
                    {
                        fnt.LeftSpace = 0; fnt.LeftSpaceFixed = 0;
                        fnt.RightSpace = 0; fnt.RightSpaceFixed = 0;
                    }
                    fnt.FixedWidth = FontMaxWidth;
                }

            }
            
            
        }
		private void CreateFont(char c, bool dc, string hex)
        {
            float height;
            Fnt_char fnt = BuildFontChar(c, dc, SysDraw, out height);

            this.iFntFile.Add(fnt, hex, ID);
            RegisterFontHeight(height);
        }

        private Fnt_char BuildFontChar(char c, bool dc, DrawFont renderer, out float height)
        {
            DrawFont.GlyphRenderResult glyph = renderer.RenderGlyph(c);
            bool IsSpace = glyph.IsSpace;


            Fnt_char fnt = new Fnt_char();
            fnt.c = c;
            fnt.IsDC = dc;

            height = 0;

            if (glyph.OriginSize.Width > 0)
            {
                SizeF ViewSize;
                if (!IsSpace)
                {
                    //繪製文字

                    fnt.FontImage = glyph.Image;

                    ViewSize = new SizeF(fnt.FontImage.Width, fnt.FontImage.Height);

                }
                else //製造空白
                {
                    //fnt.FontImage = new Bitmap(1, 1);
                    ViewSize = new SizeF(renderer.SpaceWidth, 0);
                    

                }

//                ef.X += this.sc_i左上角.X;
                //ef.Y += this.sc_i左上角.Y;
                //ef.Width += this.sc_i右下角.X;
                //ef.Height += this.sc_i右下角.Y;

                fnt.BottomAlign = glyph.BottomAlign;
                fnt.charViewHeight = (float)ViewSize.Height;  //顯示高度
                fnt.charViewWidth = (float)ViewSize.Width;      //顯示寬度

                fnt.LeftSpace = 0;
                fnt.RightSpace = 0;
                if (!this.fixedFont && !IsSpace)
                {
                    float layoutAdvance = glyph.OriginSize.Width;
                    if (glyph.RealSpace > 0)
                    {
                        layoutAdvance += glyph.RealSpace * 2f;
                    }

                    fnt.RightSpace = layoutAdvance - fnt.charViewWidth;
                }
                else if (glyph.RealSpace > 0)
                {
                    fnt.LeftSpace = glyph.RealSpace;
                    fnt.RightSpace = glyph.RealSpace;
                }
                /*
                if (SysDraw.Glow > 0)
                {
                    float shift = ((float)ef.Width - DisplaySize.Width) / 4;
                    fnt.LeftSpace = shift;
                    fnt.RightSpace = shift;
                }
                */

                if (IsSpace)
                {
                    fnt.LeftSpace = 0;
                    fnt.RightSpace = fnt.charViewWidth;
                    fnt.charViewHeight = 1f;
                    fnt.charViewWidth = 1f;
                    fnt.Empty = true;
                    fnt.IsSpace = true;
                }
                height = fnt.charViewHeight;
            }

            return fnt;
        }

        private void RegisterFontHeight(float height)
        {
            //this.iFntFile.Header.LineHeight = (this.iFntFile.Header.LineHeight < Height) ? Height : this.iFntFile.Header.LineHeight;

            
            //if (ef.Width > FontMaxWidth) FontMaxWidth = ef.Width; //登記最大寬度
            if (height > FontMaxHeight) FontMaxHeight = (int)height; //登記最大高度
            //if (!this.onSave)
            //{
                //graphics.DrawRectangle(Pens.Red, p.X, p.Y, ef.Width, ef.Height);
            //}
            //return true;

        }

        private static void ReportProgress(IProgress<FontProgress> progress, string stage, int value, int maximum)
        {
            progress?.Report(new FontProgress(stage, value, maximum));
        }

        private void RenderFontBuildItems(List<FontBuildItem> buildItems, int progressBase, IProgress<FontProgress> progress)
        {
            int renderTotal = 0;
            foreach (FontBuildItem item in buildItems)
            {
                if (!item.IsEmpty)
                {
                    renderTotal++;
                }
            }
            if (renderTotal == 0) return;

            FontRenderSettings settings = CaptureFontRenderSettings();
            int maxParallelism = Math.Max(1, Math.Min(buildItems.Count, Environment.ProcessorCount - 1));
            int renderedCount = 0;

            Task renderTask = Task.Run(() =>
            {
                Parallel.For<FontRenderState>(
                    0,
                    buildItems.Count,
                    new ParallelOptions { MaxDegreeOfParallelism = maxParallelism },
                    () => CreateFontRenderState(settings),
                    (i, loopState, renderState) =>
                    {
                        FontBuildItem item = buildItems[i];
                        if (!item.IsEmpty)
                        {
                            Font selectedFont = item.UseFont2 ? renderState.Font2 : renderState.Font1;
                            if (renderState.ActiveFont != selectedFont)
                            {
                                renderState.Renderer.FontData = selectedFont;
                                renderState.ActiveFont = selectedFont;
                            }

                            float height;
                            item.Fnt = BuildFontChar(item.Character, item.IsDC, renderState.Renderer, out height);
                            item.Height = height;
                            Interlocked.Increment(ref renderedCount);
                        }
                        return renderState;
                    },
                    DisposeFontRenderState);
            });

            while (!renderTask.IsCompleted)
            {
                ReportProgress(progress, "Manufacturing", progressBase + Volatile.Read(ref renderedCount), progressBase + renderTotal);
                Thread.Sleep(50);
            }

            renderTask.GetAwaiter().GetResult();
            ReportProgress(progress, "Manufacturing", progressBase + Volatile.Read(ref renderedCount), progressBase + renderTotal);
        }

        private FontRenderSettings CaptureFontRenderSettings()
        {
            return new FontRenderSettings
            {
                BackColor = SysDraw.BackColor,
                DrawMode = SysDraw.DrawMode,
                Glow = this.Glow,
                GlowColor = this.GlowColor,
                Outline = this.Outline,
                OutlineColor = this.OutlineColor,
                FontColor = this.FontColor
            };
        }

        private FontRenderState CreateFontRenderState(FontRenderSettings settings)
        {
            return new FontRenderState
            {
                Renderer = CreateDrawFontRenderer(settings),
                Font1 = (Font)this.font1.Clone(),
                Font2 = (Font)this.font2.Clone()
            };
        }

        private DrawFont CreateDrawFontRenderer(FontRenderSettings settings)
        {
            DrawFont renderer = new DrawFont();
            renderer.BackColor = settings.BackColor;
            renderer.DrawMode = settings.DrawMode;
            renderer.OutlineWidth = settings.Outline;
            renderer.GlowColor = settings.GlowColor;
            renderer.OutlineColor = settings.OutlineColor;
            renderer.FontColor = settings.FontColor;
            renderer.Glow = settings.Glow;
            return renderer;
        }

        private void DisposeFontRenderState(FontRenderState renderState)
        {
            if (renderState.Font1 != null)
            {
                renderState.Font1.Dispose();
            }
            if (renderState.Font2 != null)
            {
                renderState.Font2.Dispose();
            }
        }

        /// <summary>
        /// 底部對齊偏移
        /// </summary>
        /// <param name="fnt"></param>
        /// <param name="shift"></param>
        public void BottomAlignShift(float shift,bool sc_only)
        {
            foreach (Fnt_char fnt in this.iFntFile.CharList)
            {
                if (!fnt.Enable) continue;
                if (SkipASCII && !fnt.IsDC) continue;
                if (sc_only && fnt.IsDC) continue;
                fnt.BottomAlign += shift;
                fnt.BottomAlignFixed += shift;
            }

        }

        private void NormalizeBaselines()
        {
            if (ImportFont1name != "" || ImportFont2name != "")
            {
                return;
            }

            float singleByteCenter;
            float doubleByteCenter;
            if (!TryGetSingleByteReferenceCenter(out singleByteCenter)
                || !TryGetDoubleByteReferenceCenter(out doubleByteCenter))
            {
                return;
            }

            float doubleByteShift = doubleByteCenter - singleByteCenter;
            if (Math.Abs(doubleByteShift) < 0.001f)
            {
                return;
            }

            foreach (Fnt_char fnt in this.iFntFile.CharList)
            {
                if (!fnt.Enable || fnt.IsSpace || !fnt.IsDC)
                {
                    continue;
                }

                fnt.BottomAlign += doubleByteShift;
            }
        }

        private bool TryGetSingleByteReferenceCenter(out float center)
        {
            char[] referenceChars = { 'H', 'A', 'M', 'W', '0' };
            List<float> centers = new List<float>();
            for (int i = 0; i < referenceChars.Length; i++)
            {
                Fnt_char fnt = this.iFntFile.GetFntFromChar(referenceChars[i]);
                if (IsBaselineCandidate(fnt, false))
                {
                    centers.Add(GetVisualCenter(fnt));
                }
            }

            if (TryGetMedian(centers, out center))
            {
                return true;
            }

            return TryGetMedianVisualCenter(false, false, false, out center);
        }

        private bool TryGetDoubleByteReferenceCenter(out float center)
        {
            if (TryGetMedianVisualCenter(true, true, true, out center))
            {
                return true;
            }

            if (TryGetMedianVisualCenter(true, true, false, out center))
            {
                return true;
            }

            return TryGetMedianVisualCenter(true, false, false, out center);
        }

        private bool TryGetMedianVisualCenter(bool isDC, bool preferFullHeight, bool cjkOnly, out float center)
        {
            List<float> centers = new List<float>();
            float minHeight = this.iFntFile.Header.LineHeight * 0.5f;

            foreach (Fnt_char fnt in this.iFntFile.CharList)
            {
                if (!IsBaselineCandidate(fnt, isDC))
                {
                    continue;
                }

                if (preferFullHeight && fnt.charViewHeight < minHeight)
                {
                    continue;
                }

                if (cjkOnly && !IsCjkIdeograph(fnt.c))
                {
                    continue;
                }

                centers.Add(GetVisualCenter(fnt));
            }

            return TryGetMedian(centers, out center);
        }

        private float GetVisualCenter(Fnt_char fnt)
        {
            return this.iFntFile.Header.LineHeight - fnt.BottomAlign + (fnt.charViewHeight / 2f);
        }

        private static bool TryGetMedian(List<float> values, out float median)
        {
            if (values.Count == 0)
            {
                median = 0;
                return false;
            }

            values.Sort();
            median = values[values.Count / 2];
            return true;
        }

        private static bool IsBaselineCandidate(Fnt_char fnt, bool isDC)
        {
            return fnt.Enable
                && !fnt.Empty
                && !fnt.IsSpace
                && fnt.IsDC == isDC
                && fnt.charViewWidth > 0
                && fnt.charViewHeight > 0;
        }

        private static bool IsCjkIdeograph(char c)
        {
            return (c >= '\u3400' && c <= '\u4DBF')
                || (c >= '\u4E00' && c <= '\u9FFF')
                || (c >= '\uF900' && c <= '\uFAFF');
        }

        #region Properties
        /// <summary>
        /// 設定字型
        /// </summary>
        private Font NowFont
        {
            set
            {
                if (_Font != value)
                {
                    _Font = value;
                    SysDraw.FontData = value;
                }
            }
            get
            {
                return _Font;
            }
        }

        /// <summary>
        /// 設定Glow
        /// </summary>
        /// <param name="glow"></param>
        public int Glow
        {
            set { this.SysDraw.Glow = value; }
            get { return this.SysDraw.Glow; }

        }
        public Color GlowColor
        {
            set { this.SysDraw.GlowColor = value; }
            get { return this.SysDraw.GlowColor; }
        }
        public Color OutlineColor
        {
            set { this.SysDraw.OutlineColor = value; }
            get { return this.SysDraw.OutlineColor; }
        }
        public Color FontColor
        {
            set { this.SysDraw.FontColor = value; }
            get { return this.SysDraw.FontColor; }
        }
        /// <summary>
        /// 設定Outline
        /// </summary>
        /// <param name="outline"></param>
        public int Outline
        {
            set { this.SysDraw.OutlineWidth = value; }
            get { return this.SysDraw.OutlineWidth; }
        }

        public int DrawMode
        {
            set { SysDraw.DrawMode = value; SysDraw.DrawMode = 1; }
            get { return SysDraw.DrawMode; }
        }
        public string TexName
        {
            set
            { iFntFile.Header.TexFileName = value; }
            get
            { return iFntFile.Header.TexFileName; }
        }

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
            }
        }

        public bool isTextOverFlow
        {
            get
            {
                return this.iisTextOverFlow;
            }
        }

        #endregion

        #region Save

        public void SaveFnt(string path,Encoding enc)
        {
            bool ASCII_Only = false;
            if (DCfontLink > -1) ASCII_Only = true;
            this.FntFile.Header.TexFileName = PictureFileName;
            this.FntFile.save(path, enc, ASCII_Only);
            if (DCfontLink > -1)
            {
                FL_FONT ff = parent[DCfontLink].FntFile;
                ff.save_append(path); //存另一個dc
            }

        }
        public void SaveTex(string path,Bitmap b, IProgress<FontProgress> progress = null)
        {
            TextureFileService.SaveTex(path, b, progress);
        }

        public void SaveBmp(string path,Bitmap b)
        {
            TextureFileService.SaveBmp(path, b);
        }

		#endregion

		#region Load

		/// <summary>
		/// 讀取fnt+tex並建立圖庫
		/// </summary>
		/// <param name="path"></param>
		/// <param name="Tex"></param>
		/// <param name="CharIndex"></param>
		/// <param name="b_tex"></param>
		/// <param name="fenc"></param>
		/// <returns></returns>
		public bool LoadFnt(string path, bool Tex, Array2D.List2D<Fnt_char> CharIndex, out Bitmap b_tex, FontEncoding fenc, IProgress<FontProgress> progress = null)
		{
			FontFileImportResult result = FontFileImportService.LoadFntAndTex(
				path,
				Tex,
				this.FntFile,
				ID,
				fenc,
				CharIndex,
				progress);
			b_tex = result.Texture;
			if (result.FixedFont)
			{
				fixedFont = true;
				this.FontMaxWidth = result.FontMaxWidth;
			}
			return result.Success;
		}

		public Bitmap LoadTex(string path)
		{
			return TextureFileService.LoadTex(path);
		}
		public Bitmap LoadBmp(string path)
        {
            return TextureFileService.LoadBmp(path);
        }

        #endregion

        #region Other

        /// <summary>
        /// 判斷是否為雙字原
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        private bool isDoubledChar(char c, Encoding enc)
        {
            if (enc.GetBytes(c.ToString()).Length == 1)
            {
                return false;
            }
            return true;
        }

        //比较器类 
        public class Fnt_char_Height : IComparer<Fnt_char>
        {
            //按照圖形高度排序
            public int Compare(Fnt_char x, Fnt_char y)
            {
                return y.FontImage.Height - x.FontImage.Height;
            }
        }
        public class Fnt_char_Width : IComparer<Fnt_char>
        {
            //按照圖形寬度排序
            public int Compare(Fnt_char x, Fnt_char y)
            {
                return y.FontImage.Width - x.FontImage.Width;
            }
        }

        #endregion
    }
}
