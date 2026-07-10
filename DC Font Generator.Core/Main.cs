using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DC_Font_Generator
{
    public class Main
    {
        #region Members

        private const float DefaultFontSizePixels = 23f;
        private const float SingleByteEffectAdvanceFactor = 0.20f;
        private const float SingleByteEffectAdvanceMax = 1.0f;
        private const float DoubleByteEffectAdvanceFactor = 0.35f;
        private const float DoubleByteEffectAdvanceMax = 2.0f;
        private const float EffectAdvanceOverhangThreshold = 0.5f;
        public int ID = 0;
        public string name = ""; //fnt名稱
        private FL_FONT iFntFile;
        private FontDescriptor _font1Descriptor;
        private FontDescriptor _font2Descriptor;
        private bool iisTextOverFlow;

        public string ImportFont1name = "";
        public string ImportFont2name = "";

        public string PictureFileName = "";
        public Bgra32Image LastLoadedTexturePixels { get; private set; }
        public event EventHandler TextOverFlow; //圖片空間不足事件

        public bool SkipASCII = false; //忽略ASCII的輸出
        public bool fixedFont = false; //等寬字旗標

        private FontDescriptor _nowFontDescriptor;

        private DrawFont SysDraw = new DrawFont();
        public float FontMaxWidth = 17;
        public float FontMaxHeight = 0;
        public bool UseManualBaseLine = true;
        public float ManualBaseLine = 31f;
        private string lastGenerationSignature = "";

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
            public FontStyleDescriptor StyleDescriptor;
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
            public FontRenderBackend RenderBackend;
        }

        private sealed class FontRenderState
        {
            public DrawFont Renderer;
            public FontDescriptor Font1;
            public FontDescriptor Font2;
            public FontStyleDescriptor Font1StyleDescriptor;
            public FontStyleDescriptor Font2StyleDescriptor;
            public FontDescriptor ActiveFont;
            public FontStyleDescriptor ActiveStyleDescriptor;
        }


        #region Constructors

        public Main(List<Main> P,int id)
        {
            this.parent = P;
            
            this._font1Descriptor = new FontDescriptor(SystemFonts.DefaultFont.FontFamily.Name, DefaultFontSizePixels);
            this._font2Descriptor = this._font1Descriptor;
            this.NowFont = this._font1Descriptor;
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
                    tf.fTopEdge = sf.fTopEdge;
                    tf.fTopEdgeFixed = sf.fTopEdgeFixed;
                    
                    tf.fHeight = sf.fHeight;
                    tf.fHeightFixed = sf.fHeightFixed;
                    tf.fWidth = sf.fWidth;
                    tf.fWidthFixed = sf.fWidthFixed;
                    tf.Empty = sf.Empty;
                    tf.FixedWidth = sf.FixedWidth;
                    tf.IsSpace = sf.IsSpace;
                    tf.fLeadingEdge = sf.fLeadingEdge;
                    tf.fLeadingEdgeFixed = sf.fLeadingEdgeFixed;
                    tf.fSpacing = sf.fSpacing;
                    tf.fSpacingFixed = sf.fSpacingFixed;
                    tf.iTextureIndex = sf.iTextureIndex;
                    for (int mappingIndex = 0; mappingIndex < tf.pMapping.Length; mappingIndex++)
                    {
                        tf.pMapping[mappingIndex] = sf.pMapping[mappingIndex];
                    }
                    
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

        internal void ResetGeneratedStateIfRenderSettingsChanged(FontEncoding enc)
        {
            string signature = CreateGenerationSignature(enc);
            if (string.Equals(lastGenerationSignature, signature, StringComparison.Ordinal))
            {
                return;
            }

            Clear();
            lastGenerationSignature = signature;
        }

        internal void InvalidateGeneratedState()
        {
            lastGenerationSignature = "";
        }

        private string CreateGenerationSignature(FontEncoding enc)
        {
            StringBuilder builder = new StringBuilder(256);
            AppendFontSignature(builder, "font1", font1);
            AppendFontSignature(builder, "font2", font2);
            builder.Append("|import1=").Append(ImportFont1name ?? "");
            builder.Append("|import2=").Append(ImportFont2name ?? "");
            builder.Append("|link=").Append(DCfontLink.ToString(CultureInfo.InvariantCulture));
            builder.Append("|fixed=").Append(fixedFont ? "1" : "0");
            builder.Append("|fixedWidth=").Append(FontMaxWidth.ToString("R", CultureInfo.InvariantCulture));
            builder.Append("|glow=").Append(Glow.ToString(CultureInfo.InvariantCulture));
            builder.Append("|glowColor=").Append(GlowColor.ToArgb().ToString(CultureInfo.InvariantCulture));
            builder.Append("|outline=").Append(Outline.ToString(CultureInfo.InvariantCulture));
            builder.Append("|outlineColor=").Append(OutlineColor.ToArgb().ToString(CultureInfo.InvariantCulture));
            builder.Append("|fontColor=").Append(FontColor.ToArgb().ToString(CultureInfo.InvariantCulture));
            builder.Append("|manualBaseLine=").Append(UseManualBaseLine ? "1" : "0");
            builder.Append("|baseLine=").Append(ManualBaseLine.ToString("R", CultureInfo.InvariantCulture));

            if (enc != null)
            {
                builder.Append("|codePage=").Append(enc.enc.CodePage.ToString(CultureInfo.InvariantCulture));
                builder.Append("|ascii=").Append(enc.ASCII_Only ? "1" : "0");
                builder.Append("|tempCount=").Append(enc.Temp != null ? enc.Temp.Count.ToString(CultureInfo.InvariantCulture) : "0");
                builder.Append("|tempHash=").Append(GetEncodingTemplateHash(enc).ToString(CultureInfo.InvariantCulture));
            }

            return builder.ToString();
        }

        private static void AppendFontSignature(StringBuilder builder, string name, FontDescriptor font)
        {
            builder.Append('|').Append(name).Append('=');
            if (font == null)
            {
                builder.Append("<null>");
                return;
            }

            builder.Append(font.FamilyName ?? "");
            builder.Append(',').Append(font.SizePixels.ToString("R", CultureInfo.InvariantCulture));
            builder.Append(',').Append(font.Weight.ToString(CultureInfo.InvariantCulture));
            builder.Append(',').Append(font.Width.ToString(CultureInfo.InvariantCulture));
            builder.Append(',').Append(((int)font.Slant).ToString(CultureInfo.InvariantCulture));
            builder.Append(",idx=").Append(font.StyleSetIndex.ToString(CultureInfo.InvariantCulture));
            builder.Append(",style=").Append(font.StyleName ?? "");
        }

        private static int GetEncodingTemplateHash(FontEncoding enc)
        {
            if (enc.Temp == null)
            {
                return 0;
            }

            unchecked
            {
                int hash = 17;
                foreach (string item in enc.Temp)
                {
                    hash = (hash * 31) + (item != null ? item.GetHashCode() : 0);
                }

                return hash;
            }
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

                FontDescriptor itemFont;
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
                    UseFont2 = dc,
                    StyleDescriptor = GetStyleDescriptorForFont(dc)
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
                    if (IsOriginalSerializedBlankGlyph(item.Hex))
                    {
                        this.iFntFile.Add(CreateOriginalSerializedBlankGlyph(item.Hex), item.Hex, ID);
                    }
                    else
                    {
                        this.iFntFile.AddEmpty(item.Hex, ID);
                    }
                    continue;
                }

                this.iFntFile.Add(item.Fnt, item.Hex, ID);
                RegisterFontHeight(item.Height);
            }

            //修正同寬字
            if (fixedFont && ImportFont1name == "" && ImportFont2name == "")
            {
                FixedFont(fixedFont, this.FontMaxWidth);
			}
            if (ImportFont1name == "" && ImportFont2name == "")
            {

				//this.iFntFile.Header.fBaseLine = SysDraw.lineSpacingPixel;

				//this.iFntFile.Header.fBaseLine = (float)FontMaxHeight * 1.3f;
				//Fallout FontData::fBaseLine 是行 rise/ascent，不是 glyph 顶边 fTopEdge。

                SetGeneratedBaseLineFromFontMetrics();

            }
            ApplyGeneratedTopEdgeOffsets();
            QuantizeGeneratedGlyphHorizontalMetrics();
            QuantizeGeneratedGlyphVerticalMetrics();
            QuantizeGeneratedBaseLine();
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

                    if (FontMaxWidth > fnt.fWidth)
                    {
                        float shift = ((float)FontMaxWidth - fnt.fWidth) / 2f;

                        fnt.fLeadingEdge = shift; fnt.fLeadingEdgeFixed = 0;
                        fnt.fSpacing = shift; fnt.fSpacingFixed = 0;
                    }
                    else if (fnt.fWidth > FontMaxWidth)
                    {
                        float shift = (fnt.fWidth - (float)FontMaxWidth) / 2f;

                        fnt.fLeadingEdge = -shift; fnt.fLeadingEdgeFixed = 0;
                        fnt.fSpacing = -shift; fnt.fSpacingFixed = 0;
                    }
                    else
                    {
                        fnt.fLeadingEdge = 0; fnt.fLeadingEdgeFixed = 0;
                        fnt.fSpacing = 0; fnt.fSpacingFixed = 0;
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
            if (IsOriginalSerializedBlankGlyph(c))
            {
                height = 0;
                return CreateOriginalSerializedBlankGlyph(c, dc);
            }

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

                    fnt.GlyphImage = glyph.GlyphImage;

                    ViewSize = fnt.GlyphImage != null
                        ? new SizeF(fnt.GlyphImage.Width, fnt.GlyphImage.Height)
                        : SizeF.Empty;

                }
                else //製造空白
                {
                    ViewSize = new SizeF(renderer.SpaceWidth, 0);
                    

                }

//                ef.X += this.sc_i左上角.X;
                //ef.Y += this.sc_i左上角.Y;
                //ef.Width += this.sc_i右下角.X;
                //ef.Height += this.sc_i右下角.Y;

                fnt.fLeadingEdge = 0;
                fnt.fSpacing = 0;
                if (!this.fixedFont && !IsSpace)
                {
                    float rawAdvance = glyph.LayoutAdvance > 0f
                        ? glyph.LayoutAdvance
                        : glyph.OriginSize.Width + Math.Max(0f, glyph.RealSpace * 2f);
                    float desiredAdvance = Math.Max(1f, rawAdvance);

                    desiredAdvance += CalculateEffectAdvanceCompensation(glyph, dc);
                    int minimumAdvance = CalculateMinimumGameAdvance(glyph, dc);
                    int targetGameAdvance = GameFontMetricQuantizer.SelectAdvance(
                        desiredAdvance,
                        minimumAdvance,
                        dc);

                    float serializedWidth = RoundMetric(ViewSize.Width);
                    fnt.fLeadingEdge = 0;
                    fnt.fSpacing = GameFontMetricQuantizer.SpacingForAdvance(serializedWidth, targetGameAdvance);
                }
                else if (this.fixedFont && !IsSpace)
                {
                    int targetGlyphWidth = Math.Max(1, glyph.BakedLeftPad + glyph.BakedAdvance);
                    if (fnt.GlyphImage != null && fnt.GlyphImage.Width < targetGlyphWidth)
                    {
                        fnt.GlyphImage = PadGlyphWidth(fnt.GlyphImage, targetGlyphWidth, renderer.BackColor);
                        ViewSize = new SizeF(fnt.GlyphImage.Width, fnt.GlyphImage.Height);
                    }
                }
                else if (glyph.RealSpace > 0)
                {
                    fnt.fLeadingEdge = RoundMetric(glyph.RealSpace);
                    fnt.fSpacing = RoundMetric(glyph.RealSpace);
                }
                /*
                if (SysDraw.Glow > 0)
                {
                    float shift = ((float)ef.Width - DisplaySize.Width) / 4;
                    fnt.fLeadingEdge = shift;
                    fnt.fSpacing = shift;
                }
                */

                if (IsSpace)
                {
                    fnt.fLeadingEdge = 0;
                    fnt.fTopEdge = 0;
                    fnt.fHeight = 0;
                    fnt.fWidth = 0;
                    fnt.fSpacing = Math.Max(0f, RoundMetric(renderer.SpaceWidth));
                    fnt.Empty = true;
                    fnt.IsSpace = true;
                }
                else
                {
                    fnt.fTopEdge = FloorMetric(glyph.GetGeneratedTopEdge(this.UseManualBaseLine));
                    fnt.fHeight = RoundMetric(ViewSize.Height);  //顯示高度
                    fnt.fWidth = RoundMetric(ViewSize.Width);      //顯示寬度
                }
                height = fnt.fHeight;
            }

            return fnt;
        }

        private static Bgra32Image PadGlyphWidth(Bgra32Image image, int targetWidth, Color background)
        {
            if (image == null || image.Width >= targetWidth)
            {
                return image;
            }

            Bgra32Image padded = new Bgra32Image(targetWidth, image.Height);
            padded.Clear(background);
            image.CopyTo(padded, 0, 0);
            return padded;
        }

        private static int CalculateMinimumGameAdvance(DrawFont.GlyphRenderResult glyph, bool isDoubleByte)
        {
            if (glyph == null)
            {
                return 1;
            }

            float rawAdvance = glyph.LayoutAdvance > 0f
                ? glyph.LayoutAdvance
                : glyph.OriginSize.Width + Math.Max(0f, glyph.RealSpace * 2f);

            int minimumAdvance = isDoubleByte
                ? GameFontMetricQuantizer.ToNearestGameInt(rawAdvance)
                : GameFontMetricQuantizer.ToGameInt(rawAdvance);

            return Math.Max(1, minimumAdvance);
        }

        private static float CalculateEffectAdvanceCompensation(DrawFont.GlyphRenderResult glyph, bool isDoubleByte)
        {
            if (glyph == null || glyph.IsSpace || glyph.RightOverhang <= EffectAdvanceOverhangThreshold)
            {
                return 0f;
            }

            float factor = isDoubleByte ? DoubleByteEffectAdvanceFactor : SingleByteEffectAdvanceFactor;
            float max = isDoubleByte ? DoubleByteEffectAdvanceMax : SingleByteEffectAdvanceMax;
            float compensation = glyph.RightOverhang * factor;
            if (compensation < 0f)
            {
                return 0f;
            }

            compensation = compensation > max ? max : compensation;
            return RoundMetric(compensation);
        }

        private static bool IsOriginalSerializedBlankGlyph(char c)
        {
            return c < 0x20 || c == '\u007F' || c == '\u00A0';
        }

        private static bool IsOriginalSerializedBlankGlyph(string hex)
        {
            return string.Equals(hex, "007F", StringComparison.OrdinalIgnoreCase)
                || string.Equals(hex, "00A0", StringComparison.OrdinalIgnoreCase);
        }

        private static Fnt_char CreateOriginalSerializedBlankGlyph(char c, bool dc)
        {
            return new Fnt_char
            {
                c = c,
                IsDC = dc,
                Enable = false,
                Empty = true,
                IsSpace = true,
                LeftSpace = 0f,
                RightSpace = 2f,
                charViewWidth = 0f,
                charViewHeight = 0f,
                BottomAlign = 0f
            };
        }

        private static Fnt_char CreateOriginalSerializedBlankGlyph(string hex)
        {
            ushort code = ushort.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return CreateOriginalSerializedBlankGlyph((char)code, false);
        }

        private void RegisterFontHeight(float height)
        {
            //this.iFntFile.Header.fBaseLine = (this.iFntFile.Header.fBaseLine < Height) ? Height : this.iFntFile.Header.fBaseLine;

            
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

        private FontStyleDescriptor GetStyleDescriptorForFont(bool doubleByte)
        {
            FontStyleDescriptor descriptor = doubleByte ? font2StyleDescriptor : font1StyleDescriptor;
            if (descriptor != null)
            {
                return descriptor;
            }

            FontDescriptor font = doubleByte ? font2 : font1;
            if (font != null && font.HasExactStyleSetFace)
            {
                return new FontStyleDescriptor(
                    font.StyleName,
                    font.Weight,
                    font.Width,
                    font.Slant,
                    font.StyleSetIndex,
                    font.FamilyName);
            }

            return null;
        }

        private static bool SameStyleDescriptor(FontStyleDescriptor left, FontStyleDescriptor right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null) return false;
            return left.Matches(right);
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
            settings.RenderBackend = SelectRenderBackend(settings, buildItems);
            int maxParallelism = settings.RenderBackend == FontRenderBackend.Direct3D12
                ? 1
                : Math.Max(1, Math.Min(buildItems.Count, Environment.ProcessorCount - 1));
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
                            FontDescriptor selectedFont = item.UseFont2 ? renderState.Font2 : renderState.Font1;
                            FontStyleDescriptor selectedStyle = item.StyleDescriptor
                                ?? (item.UseFont2 ? renderState.Font2StyleDescriptor : renderState.Font1StyleDescriptor);
                            if (renderState.ActiveFont != selectedFont
                                || !SameStyleDescriptor(renderState.ActiveStyleDescriptor, selectedStyle))
                            {
                                renderState.Renderer.StyleDescriptor = selectedStyle;
                                renderState.Renderer.FontData = selectedFont;
                                renderState.ActiveFont = selectedFont;
                                renderState.ActiveStyleDescriptor = selectedStyle;
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

        private FontRenderBackend SelectRenderBackend(FontRenderSettings settings, List<FontBuildItem> buildItems)
        {
            FontRenderBackend requested = FontRenderBackendSelector.ReadRequestedBackend();
            if (requested == FontRenderBackend.Cpu || requested == FontRenderBackend.Direct3D12)
            {
                return requested;
            }

            TimeSpan cpuTime;
            if (!TryBenchmarkBackend(FontRenderBackend.Cpu, settings, buildItems, out cpuTime))
            {
                return FontRenderBackend.Cpu;
            }

            TimeSpan d3dTime;
            if (!TryBenchmarkBackend(FontRenderBackend.Direct3D12, settings, buildItems, out d3dTime))
            {
                return FontRenderBackend.Cpu;
            }

            return d3dTime < cpuTime ? FontRenderBackend.Direct3D12 : FontRenderBackend.Cpu;
        }

        private bool TryBenchmarkBackend(
            FontRenderBackend backend,
            FontRenderSettings settings,
            List<FontBuildItem> buildItems,
            out TimeSpan elapsed)
        {
            elapsed = TimeSpan.Zero;
            FontRenderSettings benchmarkSettings = new FontRenderSettings
            {
                BackColor = settings.BackColor,
                DrawMode = settings.DrawMode,
                Glow = settings.Glow,
                GlowColor = settings.GlowColor,
                Outline = settings.Outline,
                OutlineColor = settings.OutlineColor,
                FontColor = settings.FontColor,
                RenderBackend = backend
            };

            int sampleCount = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                using (DrawFont renderer = CreateDrawFontRenderer(benchmarkSettings))
                {
                    FontDescriptor activeFont = null;
                    FontStyleDescriptor activeStyle = null;
                    for (int i = 0; i < buildItems.Count && sampleCount < 64; i++)
                    {
                        FontBuildItem item = buildItems[i];
                        if (item.IsEmpty || IsOriginalSerializedBlankGlyph(item.Character))
                        {
                            continue;
                        }

                        FontDescriptor selectedFont = item.UseFont2 ? this.font2 : this.font1;
                        FontStyleDescriptor selectedStyle = item.StyleDescriptor ?? GetStyleDescriptorForFont(item.UseFont2);
                        if (activeFont != selectedFont || !SameStyleDescriptor(activeStyle, selectedStyle))
                        {
                            renderer.StyleDescriptor = selectedStyle;
                            renderer.FontData = selectedFont;
                            activeFont = selectedFont;
                            activeStyle = selectedStyle;
                        }

                        renderer.RenderGlyph(item.Character);
                        sampleCount++;
                    }
                }

                stopwatch.Stop();
                elapsed = stopwatch.Elapsed;
                return sampleCount > 0;
            }
            catch
            {
                stopwatch.Stop();
                elapsed = stopwatch.Elapsed;
                return false;
            }
        }

        private FontRenderState CreateFontRenderState(FontRenderSettings settings)
        {
            return new FontRenderState
            {
                Renderer = CreateDrawFontRenderer(settings),
                Font1 = this.font1,
                Font2 = this.font2,
                Font1StyleDescriptor = GetStyleDescriptorForFont(false),
                Font2StyleDescriptor = GetStyleDescriptorForFont(true)
            };
        }

        private DrawFont CreateDrawFontRenderer(FontRenderSettings settings)
        {
            DrawFont renderer = new DrawFont();
            renderer.RenderBackend = settings.RenderBackend;
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
            if (renderState.Renderer != null)
            {
                renderState.Renderer.Dispose();
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
                fnt.fTopEdge += shift;
                fnt.fTopEdgeFixed += shift;
            }

        }

        private void SetGeneratedBaseLineFromFontMetrics()
        {
            if (ImportFont1name != "" || ImportFont2name != "")
            {
                return;
            }

            float baseline = Math.Max(GetFontAscent(this.font1), GetFontAscent(this.font2));
            if (baseline <= 0f)
            {
                float lineHeight1 = this.font1 != null ? this.font1.GetLineSpacing() : 0f;
                float lineHeight2 = this.font2 != null ? this.font2.GetLineSpacing() : 0f;
                baseline = Math.Max(lineHeight1, lineHeight2);
            }

            if (this.UseManualBaseLine)
            {
                this.iFntFile.Header.fBaseLine = Math.Max(1f, this.ManualBaseLine) + this.iFntFile.Header.fBaseLineFixed;
                return;
            }

            this.iFntFile.Header.fBaseLine = baseline + GetEffectRisePadding() + this.iFntFile.Header.fBaseLineFixed;
        }

        private static float GetFontAscent(FontDescriptor font)
        {
            if (font == null)
            {
                return 0f;
            }

            float ascent = font.GetAscent();
            if (float.IsNaN(ascent) || float.IsInfinity(ascent) || ascent <= 0f)
            {
                return font.SizePixels;
            }

            return ascent;
        }

        private float GetEffectRisePadding()
        {
            return Math.Max(0, this.Outline) + Math.Max(0, this.Glow) + 0.5f;
        }

        private void ApplyGeneratedTopEdgeOffsets()
        {
            if (ImportFont1name != "" || ImportFont2name != "" || this.UseManualBaseLine)
            {
                return;
            }

            bool hasSingleByteOffset = TryCalculateTopEdgeOffset(false, this.font1, out int singleByteOffset);
            bool hasDoubleByteOffset = TryCalculateTopEdgeOffset(true, this.font2, out int doubleByteOffset);

            if (hasSingleByteOffset)
            {
                ApplyTopEdgeOffset(false, singleByteOffset);
            }

            if (hasDoubleByteOffset)
            {
                ApplyTopEdgeOffset(true, doubleByteOffset);
            }

            if (hasSingleByteOffset
                && hasDoubleByteOffset
                && TryGetSingleByteVisualCenter(out float singleByteCenter)
                && TryGetDoubleByteVisualCenter(out float doubleByteCenter))
            {
                float centerDelta = doubleByteCenter - singleByteCenter;
                if (Math.Abs(centerDelta) > 1f)
                {
                    int relativeOffset = ClampInt(RoundMetricToInt(centerDelta), -1, 1);
                    ApplyTopEdgeOffset(true, relativeOffset);
                }
            }
        }

        private bool TryCalculateTopEdgeOffset(bool doubleByte, FontDescriptor font, out int offset)
        {
            offset = 0;
            if (font == null)
            {
                return false;
            }

            bool hasCenter = doubleByte
                ? TryGetDoubleByteVisualCenter(out float actualCenter)
                : TryGetSingleByteVisualCenter(out actualCenter);
            if (!hasCenter)
            {
                return false;
            }

            FontVerticalMetrics metrics = font.GetVerticalMetrics();
            int maxOffset = CalculateMaxTopEdgeOffset(font);
            offset = ClampInt(RoundMetricToInt(actualCenter - metrics.TargetCenter), -maxOffset, maxOffset);
            return true;
        }

        private void ApplyTopEdgeOffset(bool doubleByte, int offset)
        {
            if (offset == 0)
            {
                return;
            }

            foreach (Fnt_char fnt in this.iFntFile.CharList)
            {
                if (IsGeneratedVerticalAlignmentCandidate(fnt, doubleByte))
                {
                    fnt.fTopEdge += offset;
                }
            }
        }

        private bool TryGetSingleByteVisualCenter(out float center)
        {
            char[] referenceChars = { 'H', 'M', 'W', 'A', '0', '8', 'B', 'E', 'N', 'T', 'X' };
            List<float> centers = new List<float>();
            foreach (char c in referenceChars)
            {
                Fnt_char fnt = this.iFntFile.GetFntFromChar(c);
                if (IsGeneratedVerticalAlignmentCandidate(fnt, false))
                {
                    centers.Add(GetVisualCenter(fnt));
                }
            }

            if (TryGetMedian(centers, out center))
            {
                return true;
            }

            return TryGetVisualCenter(false, false, out center);
        }

        private bool TryGetDoubleByteVisualCenter(out float center)
        {
            if (TryGetVisualCenter(true, true, out center))
            {
                return true;
            }

            return TryGetVisualCenter(true, false, out center);
        }

        private bool TryGetVisualCenter(bool doubleByte, bool cjkOnly, out float center)
        {
            List<float> centers = new List<float>();
            foreach (Fnt_char fnt in this.iFntFile.CharList)
            {
                if (!IsGeneratedVerticalAlignmentCandidate(fnt, doubleByte))
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

        private static float GetVisualCenter(Fnt_char fnt)
        {
            return (fnt.fHeight / 2f) - fnt.fTopEdge;
        }

        private static int CalculateMaxTopEdgeOffset(FontDescriptor font)
        {
            if (font == null)
            {
                return 1;
            }

            return ClampInt(RoundMetricToInt(font.SizePixels * 0.08f), 1, 3);
        }

        private static int RoundMetricToInt(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return 0;
            }

            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        private static int ClampInt(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        private static bool IsCjkIdeograph(char c)
        {
            return (c >= '\u3400' && c <= '\u4DBF')
                || (c >= '\u4E00' && c <= '\u9FFF')
                || (c >= '\uF900' && c <= '\uFAFF');
        }

        private void QuantizeGeneratedGlyphVerticalMetrics()
        {
            if (ImportFont1name != "" || ImportFont2name != "")
            {
                return;
            }

            foreach (Fnt_char fnt in this.iFntFile.CharList)
            {
                if (!fnt.Enable || fnt.IsSpace)
                {
                    continue;
                }

                fnt.fTopEdge = FloorMetric(fnt.fTopEdge);
            }
        }

        private void QuantizeGeneratedGlyphHorizontalMetrics()
        {
            if (ImportFont1name != "" || ImportFont2name != "" || fixedFont)
            {
                return;
            }

            foreach (Fnt_char fnt in this.iFntFile.CharList)
            {
                if (!fnt.Enable)
                {
                    continue;
                }

                fnt.fWidth = RoundMetric(fnt.fWidth);
                fnt.fLeadingEdge = RoundMetric(fnt.fLeadingEdge);
                fnt.fSpacing = RoundMetric(fnt.fSpacing);
            }
        }

        private void QuantizeGeneratedBaseLine()
        {
            if (ImportFont1name != "" || ImportFont2name != "")
            {
                return;
            }

            this.iFntFile.Header.fBaseLine = CeilingMetric(this.iFntFile.Header.fBaseLine);
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

        private static bool TryGetMedian(List<float> values, out float median)
        {
            if (values.Count == 0)
            {
                median = 0f;
                return false;
            }

            values.Sort();
            median = values[values.Count / 2];
            return true;
        }

        private static bool IsGeneratedLineDropCandidate(Fnt_char fnt)
        {
            return fnt != null
                && fnt.Enable
                && !fnt.Empty
                && !fnt.IsSpace
                && fnt.fWidth > 0
                && fnt.fHeight > 0;
        }

        private static bool IsGeneratedVerticalAlignmentCandidate(Fnt_char fnt, bool doubleByte)
        {
            return IsGeneratedLineDropCandidate(fnt)
                && fnt.IsDC == doubleByte;
        }

        #region Properties
        /// <summary>
        /// 設定字型
        /// </summary>
        private FontDescriptor NowFont
        {
            set
            {
                if (_nowFontDescriptor != value)
                {
                    _nowFontDescriptor = value;
                    SysDraw.StyleDescriptor = (_nowFontDescriptor == font1) ? font1StyleDescriptor :
                                             (_nowFontDescriptor == font2) ? font2StyleDescriptor : null;
                    SysDraw.FontData = _nowFontDescriptor;
                }
            }
            get
            {
                return _nowFontDescriptor;
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

        public FontDescriptor font1
        {
            get
            {
                return this._font1Descriptor;
            }
            set
            {
                this._font1Descriptor = value;
            }
        }

        public FontStyleDescriptor font1StyleDescriptor;
        public FontStyleDescriptor font2StyleDescriptor;

        public FontDescriptor font2
        {
            get
            {
                return this._font2Descriptor;
            }
            set
            {
                this._font2Descriptor = value;
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
            LastLoadedTexturePixels = result.TexturePixels;
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
            return TextureService.LoadBmp(path);
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
                return y.fHeight.CompareTo(x.fHeight);
            }
        }
        public class Fnt_char_Width : IComparer<Fnt_char>
        {
            //按照圖形寬度排序
            public int Compare(Fnt_char x, Fnt_char y)
            {
                return y.fWidth.CompareTo(x.fWidth);
            }
        }

        #endregion
    }
}
