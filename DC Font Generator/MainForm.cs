using INI_RW;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace DC_Font_Generator
{
    public partial class MainForm : Form
    {
        #region Members

        private DateTime dt;
        private bool ready = false;
        private List<Main> MainList=new List<Main>();
        private int MainSelect = 0;
        private string GamePath = "";
        private string FontPath = "";
        private string INIPath = "";
        private IniFile ini;
        private LanguageData lang;

        public Bitmap TextImage;
        public Bitmap TextImageMask;
        private Bitmap TextImageSelectionMask;
        public Size TextImageSize = new Size(128, 128);
        public Array2D.List2D<Fnt_char> CharIndex = new Array2D.List2D<Fnt_char>();
        private string ToolTipFormat = "";
        public FontEncoding fenc;

        private bool TexEnable = false;
        private GlyphSelectionState glyphSelection = new GlyphSelectionState();
        private Fnt_char hoverGlyph;
        private bool hoverHasGlyph;
        private int hoverSelectionVersion = -1;
        private Rectangle hoverFocusBounds = Rectangle.Empty;
        private Bitmap AdjPreview = new Bitmap(207, 96);
        private List<ToolStripMenuItem> tsb = new List<ToolStripMenuItem>(8);
        private string progressStage = "";

        #endregion


        #region Constructors

        public MainForm()
        {
            InitializeComponent();

            tsb.Add(sFont1ToolStripMenuItem);
            tsb.Add(sFont2ToolStripMenuItem);
            tsb.Add(sFont3ToolStripMenuItem);
            tsb.Add(sFont4ToolStripMenuItem);
            tsb.Add(sFont5ToolStripMenuItem);
            tsb.Add(sFont6ToolStripMenuItem);
            tsb.Add(sFont7ToolStripMenuItem);
            tsb.Add(sFont8ToolStripMenuItem);

            Clear();
            ready = false;
            tabControl1.TabPages.Remove(tabControl1.TabPages[4]); //測試用移除


            fenc = new FontEncoding(Encoding.Default, true);

            this.lang = new LanguageData(Encoding.Default);

            LangSetup();
            this.Text = string.Format("{0} {1} [Version: {2}]", base.ProductName,GetString("by aabby & Artaud"), base.ProductVersion);
            FontPickerForm.BeginWarmup();
            
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            buttonClear.Enabled = false;
            buttonOpenFNT.Enabled = false;
            this.toolStrip1.Enabled = false;

            button5.BackColor = Color.FromArgb(0, Color.Black); //預設背景色為透明

            toolStripStatusLabel1.Text = GetString("Please select Encoding.") + " [CodePage:"+ Encoding.Default.WebName+"]";
            toolStripProgressBar1.Visible = false;
            dt = DateTime.Now;
            InitializeFalloutEnvironment();
            saveFileDialog1.InitialDirectory = FontPath;
            openFileDialog1.InitialDirectory = FontPath;
                
            InitFontSelector();
            ready = false;

            //初始化size combobox
            comboBoxSizeX.Items.Clear();
            comboBoxSizeY.Items.Clear();
            for (int i = 7; i < 14; i++) //128~8192
            {
                comboBoxSizeX.Items.Add(new TexSize(i));
                comboBoxSizeY.Items.Add(new TexSize(i));
            }
            comboBoxSizeX.SelectedIndex = 0;
            comboBoxSizeY.SelectedIndex = 0;
            label_TexSize.Text = ((TexSize)comboBoxSizeX.SelectedItem).MergeSize(((TexSize)comboBoxSizeX.SelectedItem).size);

            this.TextImageMask = new Bitmap(1, 1);
            pictureBoxPrview.Image = AdjPreview;
            
            
            ready = true;
        }

        private void InitializeFalloutEnvironment()
        {
            FalloutEnvironmentInfo environment = FalloutEnvironmentService.Detect();
            GamePath = environment.GamePath;
            FontPath = environment.FontPath;
            INIPath = environment.IniPath;

            if (!environment.GameInstalled)
            {
                toolStripStatusLabel1.Text += " [" + GetString("Fallout3 not installed.") + "]";
                tableLayoutPanel6.Enabled = false;
                return;
            }

            if (!environment.IniAvailable)
            {
                tableLayoutPanel6.Enabled = false;
                StatusText += " [" + GetString("FALLOUT.INI Not Found.") + "]";
                return;
            }

            ini = new IniFile(INIPath);
        }

        private ComboBox[] GetIniComboBoxes()
        {
            return new[]
            {
                comboBox1,
                comboBox2,
                comboBox3,
                comboBox4,
                comboBox5,
                comboBox6,
                comboBox7,
                comboBox8
            };
        }

        private void LangSetup()
        {
            label7.Text = GetString("Font file size:");
            label11.Text = GetString("Encoding:");
            tabControl1.TabPages["tabPage1"].Text = GetString("Font");
            tabControl1.TabPages["tabPage2"].Text = GetString("Advance");
            tabControl1.TabPages["tabPage5"].Text = GetString("Adjust");
            //tabControl1.TabPages["tabPage4"].Text = GetString("");
            groupBox4.Text = GetString("Single Byte Character Set Font");
            groupBox3.Text = GetString("Double Byte Character Set Font");
            groupBox2.Text = GetString("effect");
            button2.Text = GetString("Render");
            button3.Text = GetString("Import Encoding Text");
            button1.Text = GetString("Save Font");
            label24.Text = GetString("Glow");
            label25.Text = GetString("Outline");
            label1.Text = GetString("Font Color");
            checkBox_fixed.Text = GetString("Fixedsys Font");
            label10.Text = GetString("Backgroung Color");

            label12.Text = GetString("1.Glow Monofonto Large"); 
            label13.Text = GetString("2.Monofonto Large (PIP-Boy)");
            label14.Text = GetString("3.Glow Monofonto Medium");
            label19.Text = GetString("4.Monofonto VeryLarge02 Dialogs2");
            label20.Text = GetString("5.Fixedsys Comp uniform width (terminals)");
            label21.Text = GetString("6.Glow Monofonto VL dialogs");
            label23.Text = GetString("7.Baked-in Monofonto Large");
            label22.Text = GetString("8.Glow Futura Caps Large");

            button7.Text = GetString("Fallout3 Default");

            buttonConvertTex2Png.Text = GetString("Convert Tex to PNG");
            buttonConvertPNG2Tex.Text = GetString("Convert PNG to Tex");
            buttonOpenFNT.Text = GetString("Open");
            buttonClear.Text = GetString("Clear");

            this.ToolTipFormat = "[{0}] Hex:[{1}]\n" + GetString("Width") +
                ": {2}\n" + GetString("Height") +
				": {3}\n" + GetString("Base Line") +
				": {4}\n" + GetString("Base Line Fixed") +
				": {5}\n" + GetString("Top Edge") +
                ": {6}\n" + GetString("Leading Edge") +
                ": {7}\n" + GetString("Spacing") +
                ": {8}\n" + GetString("Image Width") +
                ": {9}\n" + GetString("Image Height")+
                ": {10}\nFont{11}";

            buttonLoadPrj.Text = GetString("Load Project");
            buttonSavePrj.Text = GetString("Save Project");
            buttonLink.Text = GetString("Link Font");
            groupBox6.Text = GetString("Function");
            groupBox7.Text = GetString("Select");
            radioButton_LeftSpacing.Text = GetString("Leading Edge");
            radioButton_RightSpacing.Text = GetString("Spacing");
            radioButtonLineSpacing.Text = GetString("Base Line");
            radioButton_BottomAlign.Text = GetString("Top Edge");
            checkBox_SelectAllSC.Text = GetString("Select Single Byte Character Set");
            checkBox_SelectAllDC.Text = GetString("Select Double Byte Character Set");
            label26.Text = GetString("From");
            label27.Text = GetString("To");
            label5.Text = GetString("Character");
            label6.Text = GetString("Hex code");
            label32.Text = GetString("Font Gap");
            groupBox5.Text = GetString("Arrange Method");
            radioButtonArrangeHeight.Text = GetString("Height ordered");
            radioButtonWidthArrange.Text = GetString("Width ordered");
            radioButtonCodeOrdered.Text = GetString("Code ordered");
            radioButtonScale.Text = GetString("Scale");
            label28.Text = GetString("Increment");
            this.toolStripDropDownButtonLinkINI.ToolTipText = GetString("Select the font settings under the Fallout3.ini,When the saved will be automatically set to Fallout.ini.");
            this.toolTip1.SetToolTip(this.checkBox_fixed, GetString("To the terminal used.Fixed-width is 17."));
            this.toolTip1.SetToolTip(this.buttonLink, GetString("You can use shared fonts, is a space-saving method."));
            this.toolTip1.SetToolTip(this.buttonOpenFNT, GetString("You can use the original game font."));
            this.buttonFntNew.ToolTipText = GetString("At the same one to add a new font map is a space-saving way to.");
            this.toolTip1.SetToolTip(this.button3, GetString("You can even list the text in TXT file, after import only use these words."));

            this.buttonLoadINI.Text = GetString("Load INI");
            this.buttonSaveINI.Text = GetString("Save INI");

            tsb[0].Text = label12.Text;
            tsb[1].Text = label13.Text;
            tsb[2].Text = label14.Text;
            tsb[3].Text = label19.Text;
            tsb[4].Text = label20.Text;
            tsb[5].Text = label21.Text;
            tsb[6].Text = label23.Text;
            tsb[7].Text = label22.Text;

            buttonFntUp.ToolTipText = GetString("Previous FNT");
            buttonFntDown.ToolTipText = GetString("Next FNT");
            buttonFntRemove.ToolTipText = GetString("Remove the current FNT");
            
        }
        public void OutputLog(string log)
        {
            textBoxLog.Text += log + Environment.NewLine;
        }
        /// <summary>
        /// 初始化Font選擇
        /// </summary>
        private void InitFontSelector()
        {
            if (!tableLayoutPanel6.Enabled) return;

            FontSelectorLoadResult selector = FontIniWorkflowService.LoadSelectorState(FontPath, ini, fenc.enc);
            ComboBox[] cb = GetIniComboBoxes();
            ready = false;
            for (int i = 0; i < 8; i++)
            {
                cb[i].Items.Clear();
                foreach (FontFile font in selector.SlotItems[i])
                {
                    cb[i].Items.Add(font);
                }

                if (cb[i].Items.Count > 0)
                {
                    cb[i].SelectedIndex = FontIniWorkflowService.ClampSelectedIndex(
                        selector.SelectedIndices[i],
                        cb[i].Items.Count);
                }
            }

            foreach (string error in selector.Errors)
            {
                OutputLog(error);
                StatusText = GetString("Fallout.ini Font has error.");
            }
            ready = true;

        }
        /// <summary>
        /// 重設欄位內容
        /// </summary>
        private void SetNowData()
        {
            ready = false;
            this.TextImageSize.Width = this.TextImage.Width;
            this.TextImageSize.Height = this.TextImage.Height;
            FontSectionViewState state = FontSectionStateService.CreateViewState(this.MainList, this.MainSelect);

            labelFnt.Text = state.FntName;

            numericUpDown_MaxWidth.Value = (decimal)state.FontMaxWidth;
            numericUpDown_MaxWidth.Visible = state.FixedFont;

            numericUpDown1.Value = state.Glow; //glow
            numericUpDown_Outline.Value = state.Outline; //outline

            checkBox_fixed.Checked = state.FixedFont; //等寬字

            button_GlowColor.BackColor = state.GlowColor;
            button_Outline.BackColor = state.OutlineColor;
            buttonFontColor.BackColor = state.FontColor;

            label2.Font = state.SingleByteFont.ToGdiFont();
            label2.Text = state.SingleByteFontText;
            label4.Text = state.DoubleByteFontText;
            label4.Font = state.DoubleByteFont.ToGdiFont();

            this.textBoxFntName.Text = state.FntName;
            this.labelFnt.Text = state.FontLabel;

            buttonFntUp.Enabled = state.CanMoveUp;
            buttonFntDown.Enabled = state.CanMoveDown;
            this.buttonFntRemove.Enabled = state.CanRemove;
            this.buttonFntNew.Enabled = state.CanAdd;
            for (int i = 0; i < 8; i++)
            {
                tsb[i].Checked = state.IniLinks[i].Checked;
                tsb[i].Enabled = state.IniLinks[i].Enabled;
            }

            ApplyLinkState(state);
            if (tabControl1.SelectedTab == tabControl1.TabPages[1])
            {
                ReflashAdjustPreview();
            }

            radioButton_LeftSpacing.Enabled = state.LeftSpacingEnabled;
            radioButton_RightSpacing.Enabled = state.RightSpacingEnabled;
            radioButtonLineSpacing.Enabled = state.LineSpacingEnabled;

            //主表單重繪
            tableLayoutPanel4.Refresh();
            toolStrip1.Refresh();
            tabControl1.Refresh(); 
            ready = true;
        }
        #endregion

        #region Properties
        /// <summary>
        /// 設定訊息
        /// </summary>
        public string StatusText
        {
            set
            {
                toolStripStatusLabel1.Text = value;
                statusStrip1.Refresh();
            }
            get
            {
                return toolStripStatusLabel1.Text;
            }
        }
        
        public void ProgressBarAdd()
        {

            if (toolStripProgressBar1.Value < toolStripProgressBar1.Maximum)
            {
                toolStripProgressBar1.Value++;
            }
            ProgressBarRefresh();

        }
        public void ProgressBarRefresh()
        {
            if ((DateTime.Now - dt).TotalMilliseconds >= 50)
            {
                statusStrip1.Refresh();
                dt = DateTime.Now;
            }
        }
        public int ProgressBar
        {
            set
            {
                if (value < toolStripProgressBar1.Minimum) value = toolStripProgressBar1.Minimum;
                if (value > toolStripProgressBar1.Maximum) value = toolStripProgressBar1.Maximum;
                toolStripProgressBar1.Value = value;
                ProgressBarRefresh();

            }
        }
        public int ProgressBarMax
        {
            set
            {
                if (value < toolStripProgressBar1.Minimum) value = toolStripProgressBar1.Minimum;
                toolStripProgressBar1.Maximum = value;
                if (toolStripProgressBar1.Value > toolStripProgressBar1.Maximum)
                {
                    toolStripProgressBar1.Value = toolStripProgressBar1.Maximum;
                }
                ProgressBarRefresh();
            }
        }

        private IProgress<FontProgress> CreateFontProgress()
        {
            progressStage = "";
            return new DirectFontProgress(ReportFontProgress);
        }

        private void ReportFontProgress(FontProgress progress)
        {
            if (progress == null) return;

            if (progress.Stage != progressStage)
            {
                progressStage = progress.Stage;
                string statusText = GetProgressStatusText(progress.Stage);
                if (statusText != "")
                {
                    StatusText = statusText;
                }
            }

            if (progress.Maximum >= toolStripProgressBar1.Minimum
                && toolStripProgressBar1.Maximum != progress.Maximum)
            {
                ProgressBarMax = progress.Maximum;
            }

            ProgressBar = progress.Value;
        }

        private string GetProgressStatusText(string stage)
        {
            switch (stage)
            {
                case "Manufacturing":
                    return GetString("Manufacturing fonts...");
                case "Drawing":
                    return GetString("Drawing...");
                default:
                    return "";
            }
        }

        private sealed class DirectFontProgress : IProgress<FontProgress>
        {
            private readonly Action<FontProgress> report;

            public DirectFontProgress(Action<FontProgress> report)
            {
                this.report = report;
            }

            public void Report(FontProgress value)
            {
                report(value);
            }
        }

        #endregion

        #region Other Methods

        /// <summary>
        /// 取得ini設定的文字
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetString(string key)
        {
            return lang.GetString(key);
        }

        #endregion

        #region Other Event

        /// <summary>
        /// 錯誤事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DealWithTextFLow(object sender, EventArgs e)
        {
            if (FontSectionStateService.IsTextOverflowSender(sender))
            {
                this.errorProvider1.SetError(this.label7, "");
                //StatusText = "请扩大字库尺寸";
            }
        }

		#endregion

		#region Font Page Event
		/// <summary>
		/// 選擇字型
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
        private void label_Click(object sender, EventArgs e)
        {
            string Tag = ((Label)sender).Tag.ToString();
            bool editingDoubleByteFont = Tag == "Font2";
            FontSectionPickerState pickerState = FontSectionStateService.CreatePickerState(
                this.MainList,
                MainSelect,
                editingDoubleByteFont,
                fenc);

            try
            {
                using (FontPickerForm picker = new FontPickerForm(
                    pickerState.CurrentFont,
                    pickerState.SingleByteFont,
                    pickerState.DoubleByteFont,
                    pickerState.EditingDoubleByteFont,
                    pickerState.AsciiOnly,
                    pickerState.EncodingCodePage,
                    pickerState.Glow,
                    pickerState.GlowColor,
                    pickerState.Outline,
                    pickerState.OutlineColor,
                    pickerState.FontColor))
                {
                    if (picker.ShowDialog(this) == DialogResult.OK)
                    {
                        FontDescriptor font = picker.SelectedFont;
                        string styleLabel = picker.SelectedFontStyleDescriptor != null
                            ? picker.SelectedFontStyleDescriptor.Name : FontStyleDescriptor.StyleNameFromValues(font.Weight, font.Slant);
                        ((Label)sender).Text = font.FamilyName + "," + font.SizePixels + "," + styleLabel;
                        FontSectionStateService.ApplySelectedFont(this.MainList, MainSelect, editingDoubleByteFont, font, picker.SelectedFontStyleDescriptor);
                        this.button1.Enabled = false;

                        ((Label)sender).Font = font.ToGdiFont();
                    }
                }
            }
            catch
            {

            }

        }

        /// <summary>
        /// 連結共用字型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonLink_Click(object sender, EventArgs e)
        {
            FontListSelect fs = new FontListSelect(FontLinkService.GetCandidates(this.MainList, this.MainSelect), lang);
            if (fs.Enable)
            {
                fs.ShowDialog();
                if (fs.SelectIndex > -1)
                {
                    FontLinkService.ApplyLink(this.MainList, this.MainSelect, fs.SelectIndex);
                    ApplyLinkState(FontSectionStateService.CreateViewState(this.MainList, this.MainSelect));
                }

            }
            fs.Dispose();
        }

        /// <summary>
        /// 數值調整事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void numericUpDown_ValueChanged(object sender, EventArgs e)
        {
            string Tag = ((NumericUpDown)sender).Tag.ToString();
            float value = (float)((NumericUpDown)sender).Value;
            FontSectionStateService.ApplyNumericChange(this.MainList, MainSelect, Tag, value, ready);
            this.button1.Enabled = false;
        }
        /// <summary>
        /// 設定顏色按鈕
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_effect_Click(object sender, EventArgs e)
        {
            if (!ready) return;
            string Tag = ((Button)sender).Tag.ToString();
            DialogResult dr = colorDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                Color color = colorDialog1.Color;
                ((Button)sender).BackColor = color;
                FontSectionStateService.ApplyEffectColor(this.MainList, MainSelect, Tag, color);

            }

        }
        /// <summary>
        /// 等寬字體
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox_fixed_CheckedChanged(object sender, EventArgs e)
        {
            if (ready) numericUpDown_MaxWidth.Visible = checkBox_fixed.Checked;
            if (ready) FontSectionStateService.ApplyFixedFont(this.MainList, MainSelect, checkBox_fixed.Checked, (float)numericUpDown_MaxWidth.Value);
        }

        /// <summary>
        /// 等寬字體調整
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void numericUpDown_MaxWidth_ValueChanged(object sender, EventArgs e)
        {
            if (ready) FontSectionStateService.ApplyFixedFontWidth(this.MainList, MainSelect, checkBox_fixed.Checked, (float)numericUpDown_MaxWidth.Value);
        }
        /// <summary>
        /// 匯入Fallout3字型
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonOpenFNT_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Open Fallout3 Fnt and Tex file";
            openFileDialog1.FileName = "";
            openFileDialog1.Filter = "Fnt File|*.Fnt";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.InitialDirectory = FontPath;
            if (this.openFileDialog1.ShowDialog() == DialogResult.Cancel) return;

            string filename = FontImportWorkflowService.GetImportName(this.openFileDialog1.FileName);
            string path = FontImportWorkflowService.GetImportPath(this.openFileDialog1.FileName);

            if (!ImportFntAndTex(path, filename))
            {
                StatusText = path + ".fnt " + GetString("file error.");
                return;
            }
            SetNowData();
            this.pictureBox1.SetImage = this.TextImage;
            if (ChangeImageSize())
            {
                StatusText = GetString("Open fnt and tex done.");

            }
            else
            {
                StatusText = GetString("file size error.");
            }

        }

        private bool ImportFntAndTex(string path,string filename)
        {
            toolStripProgressBar1.Visible = true;

            StatusText = GetString("Please wait...");
            DisposeTextImageMasks();
            if (this.TextImage != null) this.TextImage.Dispose();
            ImportedFontResult result = FontImportWorkflowService.Import(new ImportedFontRequest
            {
                Path = path,
                FontName = filename,
                FontSections = this.MainList,
                SelectedFontIndex = MainSelect,
                Encoding = fenc,
                CharIndex = CharIndex,
                Progress = CreateFontProgress()
            });
            if (!result.Success)
            {
                toolStripProgressBar1.Visible = false;
                return false;
            }

            this.TextImage = result.Texture;
            SetNowData();
            buttonClear.Enabled = true;
            toolStripProgressBar1.Visible = false;
            return true;
        }
        private void ApplyLinkState(FontSectionViewState state)
        {
            buttonLink.Enabled = Encoding_comboBox.SelectedIndex >= 1 && MainList.Count > 1 && state.LinkButtonEnabled;
            if (!string.IsNullOrEmpty(state.LinkLabelText))
            {
                label4.Text = state.LinkLabelText;
            }
        }
        #endregion

        #region 主要按鈕

        /// <summary>
        /// Save Font
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (!this.TexEnable)
            {
                return;
            }
            StatusText = GetString("Please wait...");
            if (this.MainList.Count > 1)
                this.saveFileDialog1.Title = "Save Tex";
            else
                this.saveFileDialog1.Title = "Save Fnt and Tex";
            this.saveFileDialog1.FileName = textBoxTexName.Text;
            this.saveFileDialog1.Filter = "Tex File|*.Tex";
            this.saveFileDialog1.FilterIndex = 1;
            this.saveFileDialog1.InitialDirectory = FontPath;

            string TexName = "";
            if (this.saveFileDialog1.ShowDialog() == DialogResult.Cancel)
            {
                toolStripProgressBar1.Visible = false;
                StatusText = GetString("Save Cancel.");
                return;
            }

            TexName = FontSaveWorkflowService.GetTexName(this.saveFileDialog1.FileName);
            textBoxTexName.Text = TexName;

            string texPath = FontSaveWorkflowService.GetTexPath(this.saveFileDialog1.FileName);
            List<string> fntPaths = new List<string>();
            int index = 1;
            foreach (string fontName in FontSaveWorkflowService.GetSuggestedFontNames(this.MainList))
            {
                if (this.MainList.Count > 1)
                {
                    this.saveFileDialog1.InitialDirectory = FontSaveWorkflowService.GetDirectory(texPath);
                    this.saveFileDialog1.FileName = fontName;
                    this.saveFileDialog1.Filter = "Fnt & Tex File|*.Fnt";
                    this.saveFileDialog1.FilterIndex = 1;
                    this.saveFileDialog1.Title = "Save Fnt " + index;
                    if (this.saveFileDialog1.ShowDialog() == DialogResult.Cancel)
                    {
                        toolStripProgressBar1.Visible = false;
                        StatusText = GetString("Save Cancel.");
                        return;
                    }
                }
                fntPaths.Add(FontSaveWorkflowService.GetFntPath(this.saveFileDialog1.FileName));
                index++;
            }

            toolStripProgressBar1.Visible = true;
            FontSaveResult saveResult = FontSaveWorkflowService.Save(new FontSaveRequest
            {
                FontSections = this.MainList,
                TextImage = this.TextImage,
                TexPath = texPath,
                TexName = TexName,
                FntPaths = fntPaths,
                Encoding = fenc.enc,
                Progress = CreateFontProgress()
            });
            string savePerformanceLog = saveResult.PerformanceStats?.ToLogLine();
            if (!string.IsNullOrEmpty(savePerformanceLog))
            {
                OutputLog(savePerformanceLog);
            }

            InitFontSelector();
            ApplySavedFontIniSelections(
                FontSaveWorkflowService.FindSavedFontIniSelections(
                    this.MainList,
                    saveResult.FontNames,
                    GetIniSlotItems()));

            if (this.MainSelect >= 0 && this.MainSelect < saveResult.FontNames.Count)
            {
                textBoxFntName.Text = saveResult.FontNames[this.MainSelect];
            }

            toolStripProgressBar1.Visible = false;
            StatusText = GetString("Save complete.");
            this.saveFileDialog1.InitialDirectory = FontPath;
            
        }

        private List<IEnumerable<FontFile>> GetIniSlotItems()
        {
            List<IEnumerable<FontFile>> slotItems = new List<IEnumerable<FontFile>>();
            foreach (ComboBox comboBox in GetIniComboBoxes())
            {
                slotItems.Add(comboBox.Items.Cast<FontFile>().ToList());
            }

            return slotItems;
        }

        private void ApplySavedFontIniSelections(IEnumerable<SavedFontIniSelection> selections)
        {
            ComboBox[] comboBoxes = GetIniComboBoxes();
            foreach (SavedFontIniSelection selection in selections)
            {
                if (selection.SlotIndex < 0 || selection.SlotIndex >= comboBoxes.Length)
                {
                    continue;
                }

                ComboBox comboBox = comboBoxes[selection.SlotIndex];
                if (selection.SelectedIndex >= 0 && selection.SelectedIndex < comboBox.Items.Count)
                {
                    comboBox.SelectedIndex = selection.SelectedIndex;
                }
            }
        }

        private void ApplyIniComboBoxSelections(IList<int> selectedIndices)
        {
            ComboBox[] comboBoxes = GetIniComboBoxes();
            for (int i = 0; i < comboBoxes.Length && i < selectedIndices.Count; i++)
            {
                int selectedIndex = FontIniWorkflowService.ClampSelectedIndex(
                    selectedIndices[i],
                    comboBoxes[i].Items.Count);
                if (selectedIndex >= 0)
                {
                    comboBoxes[i].SelectedIndex = selectedIndex;
                }
            }
        }
        /// <summary>
        /// 繪製文字
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            RunRenderWorkflow();
        }

        private bool RunRenderWorkflow()
        {
            toolStripProgressBar1.Visible = true;
            MaskReset();

            this.errorProvider1.SetError(this.label7, "");
            this.StatusText = GetString("Manufacturing fonts...");
            DateTime startTime = DateTime.Now;

            FontRenderWorkflowResult result = FontRenderWorkflowService.Render(new FontRenderWorkflowRequest
            {
                FontSections = this.MainList,
                Encoding = fenc,
                GlyphSelection = glyphSelection,
                AtlasRequest = CreateFontAtlasRequest(),
                Progress = CreateFontProgress()
            });

            if (!result.Success)
            {
                StatusText = GetString("Font file size exceeds the limit! Can not be processed.");
                toolStripProgressBar1.Visible = false;
                this.TexEnable = false;
                return false;
            }

            BindAtlasResult(result.AtlasResult, startTime);
            string performanceLog = result.PerformanceStats?.ToLogLine();
            if (!string.IsNullOrEmpty(performanceLog))
            {
                OutputLog(performanceLog);
            }
            this.buttonClear.Enabled = true;
            return true;
        }

        private void BindAtlasResult(FontAtlasResult result, DateTime startTime)
        {
            bool oldReady = ready;
            ready = false;
            comboBoxSizeX.SelectedIndex = result.SizeXIndex;
            comboBoxSizeY.SelectedIndex = result.SizeYIndex;
            ready = oldReady;
            this.TextImageSize = result.TextImageSize;
            label_TexSize.Text = ((TexSize)comboBoxSizeX.SelectedItem).MergeSize(result.TextImageSize.Height);

            //製作Tex
            DisposeTextImageMasks();
            this.pictureBox1.Invalidate();
            if (this.TextImage != null) this.TextImage.Dispose();
            this.TextImage = result.TextImage;
            this.CharIndex = result.CharIndex;
            this.pictureBox1.SetImage = this.TextImage;

            string format = GetString("done.") + " {0} " + GetString("sec.");
            StatusText = string.Format(format, DateTime.Now - startTime);
            this.TexEnable = true;
            this.button1.Enabled = true; //開放save
            this.buttonSavePrj.Enabled = true; //開放save project
            this.tableLayoutPanelAdjust.Enabled = true; //開放調整
            this.pictureBox1.SetImage = this.TextImage;
            this.tableLayoutPanelAdjust.Enabled = true;
            toolStripProgressBar1.Visible = false;
        }

        private FontAtlasRequest CreateFontAtlasRequest()
        {
            return TextureSizeSelectionService.CreateAtlasRequest(
                this.MainList,
                fenc,
                GetTexSizeItems(comboBoxSizeX),
                GetTexSizeItems(comboBoxSizeY),
                comboBoxSizeX.SelectedIndex,
                comboBoxSizeY.SelectedIndex,
                (int)numericUpDownGap.Value,
                GetProjectArrangeMethod(),
                button5.BackColor);
        }

        /// <summary>
        /// 編碼選擇
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Encoding_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = Encoding_comboBox.SelectedIndex;
            EncodingSelectionResult result = EncodingSelectionService.Select(fenc, index);
            if (result.HasSelection)
            {
                label4.Enabled = result.DoubleByteFontEnabled;
                this.button1.Enabled = false;
                this.button2.Enabled = true;
                this.button3.Enabled = true;
                this.button4.Enabled = true;
                this.buttonOpenFNT.Enabled = true;
                this.toolStrip1.Enabled = true;
                ApplyLinkState(FontSectionStateService.CreateViewState(this.MainList, this.MainSelect)); //更動字型Link狀態
                //toolStripProgressBar1.Visible = false;
                //StatusText = string.Format("char count={0} , 重複字={1}", count, repcount);
                StatusText = string.Format(GetString("Characters count") + " = {0}", result.CharactersCount);
            }
        }
        /// <summary>
        /// Tex大小選擇
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TexSizeChanged(object sender, EventArgs e)
        {
            if (!ready) return;
            TexSize selectedWidth = (TexSize)comboBoxSizeX.SelectedItem;
            TexSize selectedHeight = (TexSize)comboBoxSizeY.SelectedItem;
            this.TextImageSize = TextureSizeSelectionService.GetSelectedSize(selectedWidth, selectedHeight);
            this.button1.Enabled = false;
            this.errorProvider1.SetError(this.label7, "");
            label_TexSize.Text = selectedWidth.MergeSize(selectedHeight.size);

        }
        /// <summary>
        /// 改變ImageSize時調整ComboBox的選取
        /// </summary>
        private bool ChangeImageSize()
        {
            this.TextImageSize = new Size(this.TextImage.Width, this.TextImage.Height);

            ready = false;
            TextureSizeSelectionResult result = TextureSizeSelectionService.FindSize(
                GetTexSizeItems(comboBoxSizeX),
                GetTexSizeItems(comboBoxSizeY),
                this.TextImageSize);
            if (result.SizeXIndex >= 0) comboBoxSizeX.SelectedIndex = result.SizeXIndex;
            if (result.SizeYIndex >= 0) comboBoxSizeY.SelectedIndex = result.SizeYIndex;

            ready = true;

            if (!result.Success)
            {
                StatusText = string.Format("Image Size error ({0},{1}).", this.TextImageSize.Width, this.TextImageSize.Height);
                return false;
            }
            return true;
        }

        private static List<TexSize> GetTexSizeItems(ComboBox comboBox)
        {
            return comboBox.Items.Cast<TexSize>().ToList();
        }

        private void ApplyTextImage(Bitmap image, bool disposeOldImage)
        {
            DisposeTextImageMasks();
            if (disposeOldImage && this.TextImage != null && !object.ReferenceEquals(this.TextImage, image))
            {
                this.TextImage.Dispose();
            }

            this.TextImage = image;
            this.pictureBox1.SetImage = this.TextImage;
        }

        /// <summary>
        /// 清除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonClear_Click(object sender, EventArgs e)
        {
            Clear();
        }
        private void Clear()
        {
            ready = false;

            glyphSelection.Clear();

            this.TexEnable = false;
            MainSelect = 0;
            FontSectionService.ResetSections(this.MainList, CharIndex, CreateProjectMainSection);

            this.buttonClear.Enabled = false;

            if (ready)
            {
                comboBoxSizeX.SelectedIndex = 0;
                comboBoxSizeY.SelectedIndex = 0;
            }
            DisposeTextImageMasks();
            TextImage = new Bitmap(128, 128);
            pictureBox1.SetImage = TextImage;

            buttonFntUp.Enabled = false;
            buttonFntDown.Enabled = false;
            buttonFntRemove.Enabled = false;
            buttonFntNew.Enabled = true;
            buttonSavePrj.Enabled = false;
            labelFnt.Text = "Fnt1";
            //button_LinkINI.Text = "ini";
            
            buttonClear.Enabled = false;
            this.button1.Enabled = false; //save
            SetNowData();
            this.tableLayoutPanelAdjust.Enabled = false;
            ready = true;
        }

        #endregion

        #region 測試用

        /// <summary>
        /// 匯入外部設定文件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button3_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = GetString("Import Encoding Text");
            openFileDialog1.FileName = "";
            openFileDialog1.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.RestoreDirectory = true;
            if (this.openFileDialog1.ShowDialog() == DialogResult.Cancel) return;

            int count = fenc.ImportEncoding(this.openFileDialog1.FileName);
            StatusText = GetString("Import characters count") + " = " + count;
            this.button1.Enabled = false;

        }
        /// <summary>
        /// Codepage Debug
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button4_Click(object sender, EventArgs e)
        {
            fenc.WriteToFile();
            StatusText = "Output CodepageDebug.txt done.";
        }



        /// <summary>
        /// 單獨匯入檔案
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonImport_Click(object sender, EventArgs e)
        {
            if (!ready) return;
            string Tag = ((Button)sender).Tag.ToString();
            bool pass = false;
            TextureWorkflowFormat format = TextureWorkflowFormat.Tex;
            switch (Tag)
            {
                case ("ImportTex"):
                    openFileDialog1.Title = "Import Tex";
                    openFileDialog1.Filter = "Tex File|*.Tex";
                    openFileDialog1.FilterIndex = 1;
                    format = TextureWorkflowFormat.Tex;
                    pass = true;
                    break;
                case ("ImportBmp"):
                    openFileDialog1.Title = "Import PNG";
                    openFileDialog1.Filter = "PNG File|*.PNG";
                    openFileDialog1.FilterIndex = 1;
                    format = TextureWorkflowFormat.Png;
                    pass = true;
                    break;
            }
            if (!pass) return;
            if (this.openFileDialog1.ShowDialog() == DialogResult.Cancel) return;

            toolStripProgressBar1.Visible = true;
            StatusText = GetString("Please wait...");
            TextureImportResult importResult = TextureWorkflowService.Import(this.openFileDialog1.FileName, format);
            ApplyTextImage(importResult.Image, true);
            if (ChangeImageSize())
            {
                StatusText = format == TextureWorkflowFormat.Tex
                    ? "Import Tex done."
                    : "Import PNG done.";
            }

            //設定ComboBox Size

            toolStripProgressBar1.Visible = false;

        }
        #endregion

        #region 進階設定

        /// <summary>
        /// back color
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button5_Click(object sender, EventArgs e)
        {
            DialogResult dr= colorDialog1.ShowDialog();
            if (dr == DialogResult.OK)
            {
                button5.BackColor = colorDialog1.Color;
                //pictureBox1.BackColor = Color.FromArgb(0xFF, colorDialog1.Color);
                //pictureBox3.BackColor = Color.FromArgb(0xFF, colorDialog1.Color);
            }
        }

        #endregion

        #region Fallout3INI

        private void INI_Font_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!ready) return;
            if (!tableLayoutPanel6.Enabled) return;
            ComboBox comboBox = (ComboBox)sender;
            FontFile ff = (FontFile)comboBox.SelectedItem;
            int slot = Array.IndexOf(GetIniComboBoxes(), comboBox);
            if (slot >= 0)
            {
                FontIniWorkflowService.WriteSlot(ini, slot, ff);
            }
        }
        /// <summary>
        /// Fallout3 Default Fonts
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
            ApplyIniComboBoxSelections(FontIniWorkflowService.GetDefaultSelections(GetIniComboBoxes().Length));
        }
        /// <summary>
        /// 讀取ini設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonLoadINI_Click(object sender, EventArgs e)
        {
            string AppPath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            this.openFileDialog1.InitialDirectory = AppPath;
            this.openFileDialog1.Title = "Load INI";
            this.openFileDialog1.FileName = "";
            this.openFileDialog1.Filter = "INI File|*.ini";
            this.openFileDialog1.FilterIndex = 1;
            if (this.openFileDialog1.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }
            string path = this.openFileDialog1.FileName;
            FontIniWorkflowService.CopySlots(path, ini);
            InitFontSelector();
        }
        /// <summary>
        /// 保存ini設定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonSaveINI_Click(object sender, EventArgs e)
        {
            string AppPath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            saveFileDialog1.InitialDirectory = AppPath;

            this.saveFileDialog1.Title = "Save INI";
            this.saveFileDialog1.FileName = "";
            this.saveFileDialog1.Filter = "INI File|*.ini";
            this.saveFileDialog1.FilterIndex = 1;

            if (this.saveFileDialog1.ShowDialog() == DialogResult.Cancel)
            {
                StatusText = GetString("Save Cancel.");
                return;
            }
            string path = this.saveFileDialog1.FileName;

            

            try
            {
                List<FontFile> selectedFonts = new List<FontFile>();
                foreach (ComboBox comboBox in GetIniComboBoxes())
                {
                    selectedFonts.Add(comboBox.SelectedItem as FontFile);
                }
                FontIniWorkflowService.SaveSlots(path, selectedFonts);
            }
            catch (Exception ee)
            {
                System.Windows.Forms.MessageBox.Show(ee.Message);
            }

        }

        #endregion

        #region PictureBOX控制

        private void pictureBox1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Clicks == 0)
            {
                string status = UpdateGlyphHover(e.X, e.Y);
                if (status != null)
                {
                    StatusText = status;
                }
                return;
            }

            switch (e.Button)
            {
                case(MouseButtons.Left):
                    StatusText = this.MouseLeftClick(e.X, e.Y, true, false);
                    break;
                case(MouseButtons.Right):
                    StatusText = this.MouseLeftClick(e.X, e.Y, true, true);
                    break;
                default:
                    StatusText = this.MouseLeftClick(e.X, e.Y, false,false);
                    break;
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            string status = UpdateGlyphHover(e.X, e.Y);
            if (status != null)
            {
                StatusText = status;
            }
        }

        /// <summary>
        /// 滑鼠離開時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            ResetGlyphHoverCache();
            MouseLeftClick(-1, -1, false, false);
        }

        private string UpdateGlyphHover(int x, int y)
        {
            if (this.pictureBox1.SizeNormal()) return "";
            GlyphHitResult hit = GlyphSelectionService.HitTest(
                this.CharIndex,
                x,
                y,
                this.TextImageSize,
                this.MainList,
                this.MainSelect);

            Fnt_char glyph = hit.HasGlyph ? hit.EditableGlyph : null;
            if (hit.HasGlyph == hoverHasGlyph &&
                object.ReferenceEquals(glyph, hoverGlyph) &&
                hoverSelectionVersion == glyphSelection.Version)
            {
                return null;
            }

            hoverHasGlyph = hit.HasGlyph;
            hoverGlyph = glyph;
            hoverSelectionVersion = glyphSelection.Version;
            return this.MouseLeftClick(x, y, false, false);
        }

        private void ResetGlyphHoverCache()
        {
            hoverGlyph = null;
            hoverHasGlyph = false;
            hoverSelectionVersion = -1;
        }

        public string MouseLeftClick(int x, int y,bool selected,bool remove)
        {
            if (this.pictureBox1.SizeNormal()) return "";
            GlyphInteractionResult result = GlyphInteractionService.Handle(new GlyphInteractionRequest
            {
                TextImage = this.TextImage,
                TextImageSize = this.TextImageSize,
                CharIndex = this.CharIndex,
                FontSections = this.MainList,
                SelectedFontIndex = this.MainSelect,
                Selection = glyphSelection,
                X = x,
                Y = y,
                ToggleSelection = selected,
                Remove = remove,
                CreateMask = false,
                HitTolerance = selected ? 3 : 0,
                ToolTipFormat = this.ToolTipFormat
            });

            if (selected && result.Hit != null && result.Hit.HasGlyph)
            {
                ResetGlyphHoverCache();
                RebuildTextImageSelectionMask();
            }
            else
            {
                EnsureTextImageMasks();
            }

            UpdateGlyphFocus(result.Hit);
            this.pictureBox1.ToolTip = result.ToolTip;
            return result.StatusText;
        }

        private void EnsureTextImageMasks()
        {
            if (this.TextImage == null) return;
            if (this.TextImageSelectionMask == null)
            {
                this.TextImageSelectionMask = GlyphInteractionService.CreateMask(
                    this.TextImage,
                    this.TextImageSize,
                    glyphSelection);
            }
            if (this.TextImageMask == null && this.TextImageSelectionMask != null)
            {
                this.TextImageMask = CloneTextImageMask(this.TextImageSelectionMask);
                this.pictureBox1.ChangeImage = this.TextImageMask;
            }
        }

        private void RebuildTextImageSelectionMask()
        {
            hoverFocusBounds = Rectangle.Empty;
            if (this.TextImageSelectionMask != null)
            {
                this.TextImageSelectionMask.Dispose();
                this.TextImageSelectionMask = null;
            }
            if (this.TextImage == null)
            {
                ResetTextImageMaskFromSelection();
                return;
            }

            this.TextImageSelectionMask = GlyphInteractionService.CreateMask(
                this.TextImage,
                this.TextImageSize,
                glyphSelection);
            ResetTextImageMaskFromSelection();
        }

        private void ResetTextImageMaskFromSelection()
        {
            if (this.TextImageMask != null)
            {
                this.TextImageMask.Dispose();
                this.TextImageMask = null;
            }
            if (this.TextImageSelectionMask == null) return;

            this.TextImageMask = CloneTextImageMask(this.TextImageSelectionMask);
            this.pictureBox1.ChangeImage = this.TextImageMask;
        }

        private void UpdateGlyphFocus(GlyphHitResult hit)
        {
            EnsureTextImageMasks();
            RestoreGlyphFocus();
            if (this.TextImageMask == null)
            {
                return;
            }

            if (hit != null && hit.HasGlyph)
            {
                GlyphOverlayRenderer.DrawFocus(this.TextImageMask, hit);
                hoverFocusBounds = GlyphOverlayRenderer.GetFocusDirtyBounds(hit, this.TextImageSize);
            }

            this.pictureBox1.ChangeImage = this.TextImageMask;
            this.pictureBox1.Refresh();
        }

        private void RestoreGlyphFocus()
        {
            if (hoverFocusBounds.IsEmpty)
            {
                return;
            }

            GlyphOverlayRenderer.RestoreRegion(this.TextImageMask, this.TextImageSelectionMask, hoverFocusBounds);
            hoverFocusBounds = Rectangle.Empty;
        }

        private static Bitmap CloneTextImageMask(Bitmap source)
        {
            if (source == null) return null;
            Bitmap clone = new Bitmap(source.Width, source.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            SkiaBitmapInterop.CopyBitmapRegion(source, clone, new Rectangle(0, 0, source.Width, source.Height));
            return clone;
        }

        private void DisposeTextImageMasks()
        {
            ResetGlyphHoverCache();
            hoverFocusBounds = Rectangle.Empty;
            if (this.TextImageMask != null)
            {
                this.TextImageMask.Dispose();
                this.TextImageMask = null;
            }
            if (this.TextImageSelectionMask != null)
            {
                this.TextImageSelectionMask.Dispose();
                this.TextImageSelectionMask = null;
            }
        }

        public void MaskReset()
        {
            ResetGlyphHoverCache();
            RebuildTextImageSelectionMask();
        }
        #endregion

        #region 進階控制

        /// <summary>
        /// 轉換Tex->PNG
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonConvertTex2Png_Click(object sender, EventArgs e)
        {
            
            this.openFileDialog1.Title = "Open Tex file";
            this.openFileDialog1.FileName = "";
            this.openFileDialog1.Filter = "Tex File|*.Tex";
            this.openFileDialog1.FilterIndex = 1;
            if (this.openFileDialog1.ShowDialog() == DialogResult.Cancel) return;

            this.saveFileDialog1.Title = "Save PNG";
            this.saveFileDialog1.FileName = TextureWorkflowService.GetPngOutputPath(this.openFileDialog1.FileName);
            this.saveFileDialog1.Filter = "PNG File|*.PNG";
            this.saveFileDialog1.FilterIndex = 1;
            if (this.saveFileDialog1.ShowDialog() == DialogResult.Cancel) return;

            toolStripProgressBar1.Visible = true;
            StatusText = GetString("Please wait...");
            TextureWorkflowService.ConvertTexToPng(this.openFileDialog1.FileName, this.saveFileDialog1.FileName);
            
            toolStripProgressBar1.Visible = false;
            StatusText = GetString("Convert Tex to PNG") + " : " + GetString("done.");
        }
        /// <summary>
        /// 轉換PNG->Tex
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonConvertPNG2Tex_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.Title = "Open PNG file";
            this.openFileDialog1.FileName = "";
            this.openFileDialog1.Filter = "PNG File|*.PNG";
            this.openFileDialog1.FilterIndex = 1;
            if (this.openFileDialog1.ShowDialog() == DialogResult.Cancel) return;
            //if (File.Exists(path + ".Tex"))
            //{
            //    MessageBoxResult result = MessageBox.Show(message, caption, MessageBoxButton.OKCancel);

            //}
            
            this.saveFileDialog1.Title = "Save Tex";
            this.saveFileDialog1.FileName = TextureWorkflowService.GetTexOutputPath(this.openFileDialog1.FileName);
            this.saveFileDialog1.Filter = "Tex File|*.Tex";
            this.saveFileDialog1.FilterIndex = 1;
            if (this.saveFileDialog1.ShowDialog() == DialogResult.Cancel) return;

            toolStripProgressBar1.Visible = true;
            StatusText = GetString("Please wait...");
            TextureWorkflowService.ConvertPngToTex(this.openFileDialog1.FileName, this.saveFileDialog1.FileName, CreateFontProgress());
            toolStripProgressBar1.Visible = false;
            StatusText = GetString("Convert PNG to Tex") + " : " + GetString("done.");
        }

        #endregion

        #region Font陣列控制

        /// <summary>
        /// Fnt控制
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonFntCtrl(object sender, EventArgs e)
        {

            string Tag = ((ToolStripButton)sender).Tag.ToString();

            FontSectionControlResult result = FontSectionService.ApplyControlCommand(
                this.MainList,
                this.MainSelect,
                Tag,
                CharIndex,
                CreateProjectMainSection);

            if (result.Changed)
            {
                this.MainSelect = result.SelectedIndex;
                SetNowData();
            }
            glyphSelection.ClearSelection();
            ready = false;
            checkBox_SelectAllSC.Checked = false;
            checkBox_SelectAllDC.Checked = false;
            ready = true;
            MaskReset();
        }

        private void textBoxFntName_TextChanged(object sender, EventArgs e)
        {
            FontSectionStateService.SetName(this.MainList, this.MainSelect, this.textBoxFntName.Text);
        }
        /// <summary>
        /// 關聯INI
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_LinkINI_Click(object sender, EventArgs e)
        {
            string Tag = ((ToolStripMenuItem)sender).Tag.ToString();
            bool value=((ToolStripMenuItem)sender).Checked;
            int index = int.Parse(Tag) - 1;
            FontSectionStateService.SetIniLink(MainList, MainSelect, index, value);

            
        }

        #endregion

        #region 字型調整
        /// <summary>
        /// TextBox限制輸入單字
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxInputText_TextChanged(object sender, EventArgs e)
        {
            if (!ready) return;
            string Text = ((TextBox)sender).Text;
            string Tag = ((TextBox)sender).Tag.ToString();
            string hexOutput = EncodingInputService.TextToHex(Text, fenc.enc);
            ready = false;
            switch (Tag)
            {
                case("FromText"):
                    textBox_FromHex.Text = hexOutput;
                    break;
                case("ToText"):
                    textBox_ToHex.Text = hexOutput;
                    break;
            }
            ready = true;
            RangeSelect();

        }

        /// <summary>
        /// 限制單字輸入
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxImputText1Char_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!ready) return;
            if (e.KeyChar == (Char)8) return; //backspace
            string Text = ((TextBox)sender).Text;
            ready = false;
            if (Text.Length > 0) ((TextBox)sender).Text = "";
            ready = true;
        }

        /// <summary>
        /// 限制16進位4字
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxImputHex_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!ready) return;
            string Text = ((TextBox)sender).Text;
            HexKeyInputResult input = EncodingInputService.EvaluateHexKey(e.KeyChar, Text);
            if (e.KeyChar == (Char)8) return; //backspace
            ready = false;
            if (input.ClearExistingText) { ((TextBox)sender).Text = ""; }
            ready = true;
            e.Handled = input.Handled;
            if (!input.Handled)
            {
                e.KeyChar = input.KeyChar;
            }
        }
        /// <summary>
        /// 輸入16進位後轉文字
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox_InputHex_TextChanged(object sender, EventArgs e)
        {
            if (!ready) return;
            string Text = ((TextBox)sender).Text;
            string Tag = ((TextBox)sender).Tag.ToString();

            string TextOutput = EncodingInputService.HexToText(Text, fenc.enc);
            
            ready = false;
            switch (Tag)
            {
                case ("FromHex"):
                    textBoxFromText.Text = TextOutput;
                    break;
                case ("ToHex"):
                    textBox_ToText.Text = TextOutput;
                    break;

            }

            ready = true;
            RangeSelect();
        }
        /// <summary>
        /// 範圍選取
        /// </summary>
        private void RangeSelect()
        {
            GlyphSelectionWorkflowService.SelectRange(new GlyphRangeSelectionRequest
            {
                FontSections = MainList,
                SelectedFontIndex = MainSelect,
                Selection = glyphSelection,
                StartHex = textBox_FromHex.Text,
                EndHex = textBox_ToHex.Text,
                IncludeSingleByte = checkBox_SelectAllSC.Checked,
                IncludeDoubleByte = checkBox_SelectAllDC.Checked
            });
            MaskReset();
        }
        private void FunctionChange_CheckedChanged(object sender, EventArgs e)
        {
            this.tableLayoutPanelAdjustButton.Enabled = true;
        }

        private void tabControl1_Enter(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab.Tag.ToString() == "Adjust")
            {
                if (radioButton_LeftSpacing.Checked || radioButton_RightSpacing.Checked || radioButtonLineSpacing.Checked || radioButton_BottomAlign.Checked)
                {
                    this.tableLayoutPanelAdjustButton.Enabled = true;
                }
                else
                    this.tableLayoutPanelAdjustButton.Enabled = false;
                ReflashAdjustPreview();
            }
        }
        /// <summary>
        /// 調整數值增減
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonAdjust_Click(object sender, EventArgs e)
        {
            GlyphAdjustmentWorkflowResult result = GlyphSelectionWorkflowService.ApplyAdjustment(new GlyphAdjustmentWorkflowRequest
            {
                FontSections = MainList,
                SelectedFontIndex = MainSelect,
                Selection = glyphSelection,
                FixedFont = checkBox_fixed.Checked,
                LeftSpacing = radioButton_LeftSpacing.Checked,
                RightSpacing = radioButton_RightSpacing.Checked,
                LineSpacing = radioButtonLineSpacing.Checked,
                TopEdge = radioButton_BottomAlign.Checked,
                Scale = radioButtonScale.Checked,
                Command = ((Button)sender).Tag.ToString(),
                Increment = (float)numericUpDown_Increment.Value
            });

            if (result.MissingSelection)
            {
                StatusText = GetString("Has not selected any font.");
                return;
            }
            if (StatusText == GetString("Has not selected any font.")) StatusText = "";

            if (!result.Applied)
            {
                return;
            }
            ReflashAdjustPreview();
        }

        /// <summary>
        /// 還原調整值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonRestoreAdjust_Click(object sender, EventArgs e)
        {
            GlyphSelectionWorkflowService.RestoreAdjustment(MainList, MainSelect, glyphSelection);
            ReflashAdjustPreview();
        }
        /// <summary>
        /// 重畫調整範例
        /// </summary>
        private void ReflashAdjustPreview()
        {
            ready = false;
            pictureBoxPrview.Image = AdjPreview;
            ready = true;
            GlyphSelectionWorkflowService.RenderPreview(AdjPreview, textBox_TypeTest.Text, MainList, MainSelect);
            pictureBoxPrview.Refresh();
        }
        private void textBox_TypeTest_TextChanged(object sender, EventArgs e)
        {
            ReflashAdjustPreview();
        }
        private void pictureBoxPrview_Resize(object sender, EventArgs e)
        {
            if (!ready) return;
            int newX = pictureBoxPrview.Width;
            int newY = pictureBoxPrview.Height;
            AdjPreview = new Bitmap(newX, newY);
        }

        #endregion

        #region Project
        private void buttonSavePrj_Click(object sender, EventArgs e)
        {
            string AppPath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            saveFileDialog1.InitialDirectory = AppPath;

            this.saveFileDialog1.Title = "Save Project";
            this.saveFileDialog1.FileName = "";
            this.saveFileDialog1.Filter = "Project.xml|*.project.xml";
            this.saveFileDialog1.FilterIndex = 1;
            
            if (this.saveFileDialog1.ShowDialog() == DialogResult.Cancel)
            {
                StatusText = GetString("Save Cancel.");
                return;
            }
            try
            {
                ProjectFileWorkflowService.Save(this.saveFileDialog1.FileName, CreateProjectSaveRequest());
                StatusText = GetString("Project has been saved.");
            }
            catch (Exception ee)
            {
                System.Windows.Forms.MessageBox.Show(ee.Message);
            }
        }

        private ProjectSaveRequest CreateProjectSaveRequest()
        {
            return ProjectRequestFactory.CreateSaveRequest(
                this.Encoding_comboBox.SelectedIndex,
                this.comboBoxSizeX.SelectedIndex,
                this.comboBoxSizeY.SelectedIndex,
                this.textBoxTexName.Text,
                this.numericUpDownGap.Value,
                this.button5.BackColor.ToArgb(),
                GetProjectArrangeMethod(),
                this.MainList);
        }

        private int GetProjectArrangeMethod()
        {
            return ProjectRequestFactory.GetArrangeMethod(
                radioButtonWidthArrange.Checked,
                radioButtonCodeOrdered.Checked);
        }


        private void buttonLoadPrj_Click(object sender, EventArgs e)
        {
            string AppPath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            this.openFileDialog1.InitialDirectory = AppPath;
            this.openFileDialog1.Title = "Load Project";
            this.openFileDialog1.FileName = "";
            this.openFileDialog1.Filter = "Project.xml|*.project.xml";
            this.openFileDialog1.FilterIndex = 1;
            if (this.openFileDialog1.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }

            try
            {
                ProjectDocument document = ProjectFileWorkflowService.Load(this.openFileDialog1.FileName);
                ApplyProjectDocument(document);
            }
            catch (System.Xml.XmlException ee)
            {
                System.Windows.Forms.MessageBox.Show(ee.Message);
                StatusText = GetString("file error.");
            }
        }

        private void ApplyProjectDocument(ProjectDocument document)
        {
            Clear();

            this.Encoding_comboBox.SelectedIndex = document.EncodingIndex;
            this.numericUpDownGap.Value = document.Gap;
            SetProjectBackgroundColor(document.BackGroundColorArgb);
            this.textBoxTexName.Text = document.TexFileName;
            SetProjectArrangeMethod(document.ArrangeMethod);
            if (document.SizeXIndex != -1) this.comboBoxSizeX.SelectedIndex = document.SizeXIndex;
            if (document.SizeYIndex != -1) this.comboBoxSizeY.SelectedIndex = document.SizeYIndex;

            toolStripProgressBar1.Visible = true;
            this.errorProvider1.SetError(this.label7, "");
            this.StatusText = GetString("Manufacturing fonts...");
            DateTime startTime = DateTime.Now;

            ProjectOpenWorkflowResult result = ProjectOpenWorkflowService.Open(new ProjectOpenWorkflowRequest
            {
                Document = document,
                FontSections = this.MainList,
                FontPath = this.FontPath,
                Encoding = fenc,
                CharIndex = CharIndex,
                CreateMain = CreateProjectMainSection,
                AtlasRequest = CreateFontAtlasRequest(),
                Progress = CreateFontProgress(),
                Localize = GetString
            });

            this.MainSelect = FontSectionStateService.ClampSelectedIndex(this.MainList, result.SelectedMainIndex);

            if (result.AtlasResult == null || !result.AtlasResult.Success)
            {
                StatusText = GetString("Font file size exceeds the limit! Can not be processed.");
                toolStripProgressBar1.Visible = false;
                this.TexEnable = false;
                foreach (string log in result.Logs)
                {
                    OutputLog(log);
                }
                return;
            }

            BindAtlasResult(result.AtlasResult, startTime);
            SetNowData();

            foreach (string log in result.Logs)
            {
                OutputLog(log);
            }

            if (result.Status == ProjectOpenWorkflowStatus.Success)
                StatusText = GetString("Project has been opened. Please remember to save font.");
            else
                StatusText = GetString("Project error : Please refer to the log");
        }

        private Main CreateProjectMainSection(int id)
        {
            return FontSectionService.CreateSection(this.MainList, id, this.DealWithTextFLow);
        }

        private void SetProjectBackgroundColor(int argb)
        {
            this.button5.BackColor = ProjectRequestFactory.GetBackgroundColor(argb);
        }

        private void SetProjectArrangeMethod(int arrangeMethod)
        {
            ProjectArrangeSelection selection = ProjectRequestFactory.GetArrangeSelection(arrangeMethod);
            radioButtonArrangeHeight.Checked = selection.HeightOrdered;
            radioButtonWidthArrange.Checked = selection.WidthOrdered;
            radioButtonCodeOrdered.Checked = selection.CodeOrdered;
        }


        #endregion
        /// <summary>
        /// 選取全SC
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox_SelectAllSC_CheckedChanged(object sender, EventArgs e)
        {
            if (!ready) return;
            GlyphSelectionWorkflowService.SetSingleByteSelection(new GlyphSetSelectionRequest
            {
                FontSections = MainList,
                SelectedFontIndex = MainSelect,
                Selection = glyphSelection,
                Selected = ((CheckBox)sender).Checked
            });
            MaskReset();
        }
        /// <summary>
        /// 選取全DC
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox_SelectAllDC_CheckedChanged(object sender, EventArgs e)
        {
            if (!ready) return;
            GlyphSelectionWorkflowService.SetDoubleByteSelection(new GlyphSetSelectionRequest
            {
                FontSections = MainList,
                SelectedFontIndex = MainSelect,
                Selection = glyphSelection,
                Selected = ((CheckBox)sender).Checked
            });
            MaskReset();
        }



    }
}
