using INI_RW;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;

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
        public Size TextImageSize = new Size(128, 128);
        public Array2D.List2D<Fnt_char> CharIndex = new Array2D.List2D<Fnt_char>();
        private string ToolTipFormat = "";
        public FontEncoding fenc;

        private bool TexEnable = false;
        private List<Fnt_char> SelectFnt = new List<Fnt_char>(); //選取容器
        private List<Fnt_char> SelectFnt2 = new List<Fnt_char>(); //選取容器
        private List<Fnt_char> RemoveFnt = new List<Fnt_char>(); //移除字型容器
        private Graphics mask;
        private Bitmap AdjPreview = new Bitmap(207, 96);
        private List<ToolStripMenuItem> tsb = new List<ToolStripMenuItem>(8);

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
            this.fontDialog1.ShowColor=false;
            this.fontDialog1.ShowEffects=false;
            this.fontDialog1.ShowHelp=false;
            this.fontDialog1.AllowScriptChange=true;
            this.fontDialog1.AllowVectorFonts=true;
            this.fontDialog1.AllowVerticalFonts=false;
            
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
            GamePath = GetGamePath(); //取得遊戲資安裝料夾
            
            //setup font select

            //set font path
            if (GamePath != "")
            {
                if (Directory.Exists(GamePath))
                {

                    FontPath = Path.Combine(GamePath , @"Data\textures\Fonts\");
                    if (!Directory.Exists(GamePath))
                    {
                        Directory.CreateDirectory(FontPath); //建立Fonts資料夾
                    }


                }
                else
                {
                    GamePath = "";
                }

            }
            if (GamePath == "")
            {
                toolStripStatusLabel1.Text += " [" + GetString("Fallout3 not installed.") + "]";
                tableLayoutPanel6.Enabled = false;

            }
            saveFileDialog1.InitialDirectory = FontPath;
            openFileDialog1.InitialDirectory = FontPath;
            //MyDocuments
            if (tableLayoutPanel6.Enabled)
            {
                string MyDocumentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string MyGamesPath = Path.Combine(MyDocumentsPath, "My Games");
                MyGamesPath = Path.Combine(MyGamesPath, "Fallout3");  
                if (!Directory.Exists(MyGamesPath))
                {
                    tableLayoutPanel6.Enabled = false;
                    StatusText += " [" + GetString("FALLOUT.INI Not Found.") + "]";
                }
                else
                {
                    INIPath = Path.Combine(MyGamesPath, "FALLOUT.INI");
                    if (!File.Exists(INIPath))
                    {
                        INIPath = "";
                        tableLayoutPanel6.Enabled = false;
                        StatusText += " [" + GetString("FALLOUT.INI Not Found.") + "]";
                    }
                }
            }
            if (tableLayoutPanel6.Enabled) ini = new IniFile(INIPath);
                
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

            this.ToolTipFormat = "[{0}] Hex:[{1}]\n" + GetString("View Width") +
                ": {2}\n" + GetString("View Height") +
				": {3}\n" + GetString("Line Height") +
				": {4}\n" + GetString("Line Height Fixed") +
				": {5}\n" + GetString("Bottom Align") +
                ": {6}\n" + GetString("Left Space") +
                ": {7}\n" + GetString("Right Space") +
                ": {8}\n" + GetString("Image Width") +
                ": {9}\n" + GetString("Image Height")+
                ": {10}\nFont{11}";

            buttonLoadPrj.Text = GetString("Load Project");
            buttonSavePrj.Text = GetString("Save Project");
            buttonLink.Text = GetString("Link Font");
            groupBox6.Text = GetString("Function");
            groupBox7.Text = GetString("Select");
            radioButton_LeftSpacing.Text = GetString("Left Spacing");
            radioButton_RightSpacing.Text = GetString("Right Spacing");
            radioButtonLineSpacing.Text = GetString("Line Spacing");
            radioButton_BottomAlign.Text = GetString("Bottom Align");
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
            if (!Directory.Exists(FontPath))
            {
                Directory.CreateDirectory(FontPath);
            }
            comboBox1.Items.Clear(); comboBox2.Items.Clear(); comboBox3.Items.Clear(); comboBox4.Items.Clear();
            comboBox5.Items.Clear(); comboBox6.Items.Clear(); comboBox7.Items.Clear(); comboBox8.Items.Clear();
            List<FontFile> FontFiles = new List<FontFile>();
            //建立預設font
            FontFile ff = new FontFile("Glow_Monofonto_Large.fnt", true, comboBox1, 0, fenc.enc);
            comboBox1.Items.Add(ff); FontFiles.Add(ff);
            ff = new FontFile("Monofonto_Large.fnt", true, comboBox2, 0, fenc.enc);
            comboBox2.Items.Add(ff); FontFiles.Add(ff);
            ff = new FontFile("Glow_Monofonto_Medium.fnt", true, comboBox3, 0, fenc.enc);
            comboBox3.Items.Add(ff); FontFiles.Add(ff);
            ff = new FontFile("Monofonto_VeryLarge02_Dialogs2.fnt", true, comboBox4, 0, fenc.enc);
            comboBox4.Items.Add(ff); FontFiles.Add(ff);
            ff = new FontFile("Fixedsys_Comp_uniform_width.fnt", true, comboBox5, 0, fenc.enc);
            comboBox5.Items.Add(ff); FontFiles.Add(ff);
            ff = new FontFile("Glow_Monofonto_VL_dialogs.fnt", true, comboBox6, 0, fenc.enc);
            comboBox6.Items.Add(ff); FontFiles.Add(ff);
            ff = new FontFile("Baked-in_Monofonto_Large.fnt", true, comboBox7, 0, fenc.enc);
            comboBox7.Items.Add(ff); FontFiles.Add(ff);
            ff = new FontFile("Glow_Futura_Caps_Large.fnt", true, comboBox8, 0, fenc.enc);
            comboBox8.Items.Add(ff); FontFiles.Add(ff);
            //搜尋已有的Fonts
            int index=1;
            foreach (string dir in Directory.GetFiles(FontPath, "*.fnt"))
            {
                ff = new FontFile(dir, false, index++, fenc.enc);
                if (ff.Enable)
                {
                    comboBox1.Items.Add(ff); comboBox2.Items.Add(ff); comboBox3.Items.Add(ff); comboBox4.Items.Add(ff);
                    comboBox5.Items.Add(ff); comboBox6.Items.Add(ff); comboBox7.Items.Add(ff); comboBox8.Items.Add(ff);
                    FontFiles.Add(ff);
                }
                else
                {
                    this.OutputLog(ff.Err); StatusText = GetString("Fallout.ini Font has error.");
                    index--;
                }
                
            }
            //讀取ini對應combobox
            
            string[] Font = new string[8];
            Font[0] = ini.IniReadValue("Fonts", "sFontFile_1");
            Font[1] = ini.IniReadValue("Fonts", "sFontFile_2");
            Font[2] = ini.IniReadValue("Fonts", "sFontFile_3");
            Font[3] = ini.IniReadValue("Fonts", "sFontFile_4");
            Font[4] = ini.IniReadValue("Fonts", "sFontFile_5");
            Font[5] = ini.IniReadValue("Fonts", "sFontFile_6");
            Font[6] = ini.IniReadValue("Fonts", "sFontFile_7");
            Font[7] = ini.IniReadValue("Fonts", "sFontFile_8");
            ComboBox[] cb = new ComboBox[8];
            cb[0] = comboBox1; cb[1] = comboBox2; cb[2] = comboBox3; cb[3] = comboBox4;
            cb[4] = comboBox5; cb[5] = comboBox6; cb[6] = comboBox7; cb[7] = comboBox8;
            index = 0;
            ready = false;
            for (int i = 0; i < 8; i++)
            {
                int pos = Font[i].LastIndexOf(@"\") + 1;
                string fn = Font[i].Substring(pos);
                bool find = false;
                foreach (FontFile f in FontFiles)
                {
                    if (f.IsThis(fn))
                    {
                        find = true;
                        f.SetComboBox(cb[i]);
                    }
                }
                if (!find)
                {
                    OutputLog(fn + " : File does not exist."); StatusText = GetString("Fallout.ini Font has error.");
                }
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

            labelFnt.Text = this.MainList[MainSelect].name;

            numericUpDown_MaxWidth.Value = (decimal)this.MainList[MainSelect].FontMaxWidth;
            numericUpDown_MaxWidth.Visible = this.MainList[MainSelect].fixedFont;

            numericUpDown1.Value = this.MainList[MainSelect].Glow; //glow
            numericUpDown_Outline.Value = this.MainList[MainSelect].Outline; //outline

            checkBox_fixed.Checked = this.MainList[MainSelect].fixedFont; //等寬字

            button_GlowColor.BackColor = this.MainList[MainSelect].GlowColor;
            button_Outline.BackColor = this.MainList[MainSelect].OutlineColor;
            buttonFontColor.BackColor = this.MainList[MainSelect].FontColor;

            label2.Font = this.MainList[MainSelect].font1;
            if (this.MainList[MainSelect].ImportFont1name != "")
                label2.Text = this.MainList[MainSelect].ImportFont1name;
            else
                label2.Text = this.MainList[MainSelect].font1.Name + "," + this.MainList[MainSelect].font1.Size;
            if (this.MainList[MainSelect].ImportFont2name != "")
                label4.Text = this.MainList[MainSelect].ImportFont2name;
            else
                label4.Text = this.MainList[MainSelect].font2.Name + "," + this.MainList[MainSelect].font2.Size;
            label4.Font = this.MainList[MainSelect].font2;

            this.textBoxFntName.Text = this.MainList[this.MainSelect].name;
            this.labelFnt.Text = "Fnt " + (this.MainSelect + 1) + "/" + (this.MainList.Count);

            if (this.MainSelect == 0) buttonFntUp.Enabled = false; else { if (this.MainList.Count > 1) buttonFntUp.Enabled = true; }
            if (this.MainSelect >= this.MainList.Count - 1) buttonFntDown.Enabled = false; else { if (this.MainList.Count > 1) buttonFntDown.Enabled = true; }
            if (this.MainList.Count == 1) this.buttonFntRemove.Enabled = false; else this.buttonFntRemove.Enabled = true;
            if (this.MainList.Count >= 8) this.buttonFntNew.Enabled = false; else this.buttonFntNew.Enabled = true;

            if (MainList[MainSelect].Fallout3INI.Count > 0)
            {

            }
            for (int i = 0; i < 8; i++)
            {
                if (MainList[MainSelect].Fallout3INI[i])
                {
                    tsb[i].Checked = true;
                }
                else
                {
                    tsb[i].Checked = false;
                }
                tsb[i].Enabled = true;
            }
            List<int> INIstat = new List<int>(8); for (int i = 0; i < 8; i++) INIstat.Add(-1);
            int index = 0;
            foreach (Main m in MainList)
            {
                 for (int i = 0; i < 8; i++)
                     if (MainList[index].Fallout3INI[i])
                     {
                         INIstat[i] = index;
                     }
                index++;
            }
            for (int i = 0; i < 8; i++)
            {
                if (INIstat[i] > -1)
                {
                    if (INIstat[i] != MainSelect) tsb[i].Enabled = false;
                }
            }

            this.EnableLink();
            if (tabControl1.SelectedTab == tabControl1.TabPages[1])
            {
                ReflashAdjustPreview();
            }

            if (checkBox_fixed.Checked) //等寬字
            {
                radioButton_LeftSpacing.Enabled = false;
                radioButton_RightSpacing.Enabled = false;
                radioButtonLineSpacing.Enabled = false;
            }
            else
            {
                radioButton_LeftSpacing.Enabled = true;
                radioButton_RightSpacing.Enabled = true;
                radioButtonLineSpacing.Enabled = true;
            }

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

            toolStripProgressBar1.Value++;
            if (dt.Second != DateTime.Now.Second) //每秒更新
            {
                statusStrip1.Refresh();
                dt = DateTime.Now;
            }

        }
        public int ProgressBar
        {
            set
            {
                toolStripProgressBar1.Value = value;

            }
        }
        public int ProgressBarMax
        {
            set { toolStripProgressBar1.Maximum = value; }
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
            if (((Main)sender).isTextOverFlow)
            {
                this.errorProvider1.SetError(this.label7, "");
                //StatusText = "请扩大字库尺寸";
            }
        }

        /// <summary>
        /// 取得遊戲資料夾
        /// </summary>
        /// <returns></returns>
        private string GetGamePath()
        {
            string path = "";
            try
            {
                // Auslesen des Oblivion-Installationsordners aus der Registry
                RegistryKey userKey = Registry.LocalMachine;

                userKey = userKey.OpenSubKey(@"SOFTWARE\Bethesda Softworks\Fallout3", false);
                if (userKey != null) // gefunden
                {
                    path = userKey.GetValue("Installed Path").ToString();
                    // Data-Ordner anhängen
                    //path = Path.Combine(path, "Data");
                }
                else //x64
                {
                    userKey = Registry.LocalMachine;
                    userKey = userKey.OpenSubKey(@"SOFTWARE\Wow6432Node\Bethesda Softworks\Fallout3", false);
                    if (userKey != null) // gefunden
                    {
                        path = userKey.GetValue("Installed Path").ToString();
                        // Data-Ordner anhängen
                        //path = Path.Combine(path, "Data");
                    }
                }

                return path;

            }
            catch
            {
                return path;
            }

        }

        /// <summary>
        /// 檢查Fnt資料
        /// </summary>
        /// <param name="path"></param>
        /// <param name="IsDC"></param>
        /// <returns></returns>
        private bool CheckFnt(string path, out bool IsDC)
        {
            IsDC = false;
            bool Enable = false;
            if (File.Exists(path))
            {
                FileInfo info = new FileInfo(path);
                if (info.Length == 14632)
                {
                    Enable = true;
                    IsDC = false;
                }
                else if (info.Length == 1362328)
                {
                    Enable = true;
                    IsDC = true;
                }
                else
                    Enable = false;
            }
            else
                Enable = false;
            return Enable;
        }

		#endregion

		public string GetUserInput()
		{
			string prompt = "Font Size(px):";
			string title = "Font Size";
			string defaultValue = "18";
			int xPos = -1; // 使用默认水平位置
			int yPos = -1; // 使用默认垂直位置

			return Interaction.InputBox(prompt, title, defaultValue, xPos, yPos);
		}

		#region Font Page Event
		/// <summary>
		/// 選擇字型
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void label_Click(object sender, EventArgs e)
        {
            FontDialog fd = new FontDialog();
            fd.ShowColor = false;
            fd.ShowEffects = false;
            fd.ShowHelp = false;
            fd.AllowScriptChange = true;
            fd.AllowVectorFonts = true;
            fd.AllowVerticalFonts = false;

            string Tag = ((Label)sender).Tag.ToString();
            switch (Tag)
            {
                case ("Font1"):
                    Font font1=this.MainList[MainSelect].font1;
                    fd.Font = font1;

                    break;
                case ("Font2"):
                    Font font2=this.MainList[MainSelect].font2;
                    fd.Font = font2;
                    break;
            }

            try
            {
                if (fd.ShowDialog() == DialogResult.OK)
                {
                    Font font = new Font(fd.Font.FontFamily, int.Parse(GetUserInput()), fd.Font.Style, GraphicsUnit.Pixel);
                    
                    
                    
                    ((Label)sender).Text = font.Name + "," + font.Size + "," + font.Height;
                    switch (Tag)
                    {
                        case("Font1"):
                            this.MainList[MainSelect].font1 = font;
                            this.MainList[MainSelect].ImportFont1name = "";
                            break;
                        case("Font2"):
                            this.MainList[MainSelect].font2 = font;
                            this.MainList[MainSelect].ImportFont2name = "";
                            this.MainList[MainSelect].DCfontLink = -1;
                            break;
                    }

                    this.MainList[MainSelect].Clear(); //清除已經繪製的字
                    this.button1.Enabled = false;

                    if (font.Size>27)
                        font = new Font(this.fontDialog1.Font.FontFamily, 28f, this.fontDialog1.Font.Style, GraphicsUnit.Pixel);
                    ((Label)sender).Font = font;
                    

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
            FontListSelect fs = new FontListSelect(this.MainList, this.MainSelect, lang);
            if (fs.Enable)
            {
                fs.ShowDialog();
                if (fs.SelectIndex > -1)
                {
                    label4.Text = "Link to : Fnt" + (MainList[MainSelect].DCfontLink + 1);
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
            switch (Tag)
            {
                case ("Glow"):
                    this.MainList[MainSelect].Glow = (int)value;
                    if (ready) this.MainList[MainSelect].Clear();
                    break;
                case ("Outline"):
                    this.MainList[MainSelect].Outline = (int)value;
                    if (ready) this.MainList[MainSelect].Clear();
                    break;
                case ("SC_BA"):
                    if (ready) this.MainList[MainSelect].Clear();
                    break;
                case ("DC_BA"):
                    if (ready) this.MainList[MainSelect].Clear();
                    break;
                
            }
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
                switch (Tag)
                {
                    case ("Glow"):
                        this.MainList[MainSelect].GlowColor = color;
                        this.MainList[MainSelect].Clear();
                        break;
                    case ("Outline"):
                        this.MainList[MainSelect].OutlineColor = color;
                        this.MainList[MainSelect].Clear();
                        break;
                    case ("FontColor"):
                        this.MainList[MainSelect].FontColor = color;
                        this.MainList[MainSelect].Clear();
                        break;
                    case ("Gap"):

                        break;
                }

            }

        }
        /// <summary>
        /// 等寬字體
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBox_fixed_CheckedChanged(object sender, EventArgs e)
        {

            if (checkBox_fixed.Checked)
            {
                if (ready) this.MainList[MainSelect].DrawMode = 0;
            }
            else
            {
                if (ready) this.MainList[MainSelect].DrawMode = 1;
            }

            if (ready) numericUpDown_MaxWidth.Visible = checkBox_fixed.Checked;
            if (ready) this.MainList[MainSelect].FixedFont(checkBox_fixed.Checked, (float)numericUpDown_MaxWidth.Value);
        }

        /// <summary>
        /// 等寬字體調整
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void numericUpDown_MaxWidth_ValueChanged(object sender, EventArgs e)
        {
            if (ready) this.MainList[MainSelect].FixedFont(checkBox_fixed.Checked, (float)numericUpDown_MaxWidth.Value);

            if (ready) this.MainList[MainSelect].Clear();
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

            string filename = Path.GetFileNameWithoutExtension(this.openFileDialog1.FileName);
            string path = Path.GetDirectoryName(this.openFileDialog1.FileName) + @"\" + filename + ".fnt";

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

            bool IsDC;
            if (!CheckFnt(path, out IsDC))
            {
                toolStripProgressBar1.Visible = false;
                return false;
            }

            //關閉字型選取
            label2.Text = filename;
            label2.Font = Control.DefaultFont;
            this.MainList[MainSelect].ImportFont1name = filename;
            if (IsDC)
            {
                label4.Text = filename;
                label4.Font = Control.DefaultFont;
                this.MainList[MainSelect].ImportFont2name = filename;
            }
            

            StatusText = GetString("Please wait...");
            if (this.TextImage != null) this.TextImage.Dispose();
            if (!this.MainList[MainSelect].LoadFnt(path, true, CharIndex, out this.TextImage, fenc))
            {
                toolStripProgressBar1.Visible = false;
                return false;
            }
            //檢查band
            foreach (Fnt_char fnt in this.MainList[MainSelect].FntFile.CharList)
            {
                if (fnt.Enable)
                {
                    if (fenc.IsBand(fnt.HEX))
                        fnt.Enable = false;
                }
            }

            SetNowData();
            buttonClear.Enabled = true;
            toolStripProgressBar1.Visible = false;
            return true;
        }
        /// <summary>
        /// 啟用字型連結
        /// </summary>
        private void EnableLink()
        {
            buttonLink.Enabled = false;
            if (Encoding_comboBox.SelectedIndex < 1) return;
            if (MainList.Count <= 1) return;

            //計算有效的選取
            int index=0;
            foreach (Main m in MainList)
            {
                if (m.DCfontLink == -1 && index != MainSelect)
                {
                    buttonLink.Enabled = true; break;
                }
                index++;
            }
            if (MainList[MainSelect].DCfontLink > -1)
            {
                label4.Text = "Link to : Fnt" + (MainList[MainSelect].DCfontLink + 1);
                buttonLink.Enabled = true;
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

            TexName = Path.GetFileNameWithoutExtension(this.saveFileDialog1.FileName);
            textBoxTexName.Text = TexName;
            //save tex
            toolStripProgressBar1.Visible = true;
            string path = Path.GetDirectoryName(this.saveFileDialog1.FileName) + @"\" + TexName + ".Tex";
            this.MainList[MainSelect].SaveTex(path, this.TextImage);

            //save fnt
            int index = 1;
            foreach (Main main in this.MainList)
            {
                if (this.MainList.Count > 1)
                {
                    this.saveFileDialog1.InitialDirectory = Path.GetDirectoryName(path);
                    this.saveFileDialog1.FileName = main.name;
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
                string FntName = Path.GetFileNameWithoutExtension(this.saveFileDialog1.FileName);
                main.name = FntName;

                //save fnt
                main.PictureFileName = TexName;
                path = Path.GetDirectoryName(this.saveFileDialog1.FileName) + @"\" + FntName + ".fnt";
                main.SaveFnt(path, fenc.enc);

                //設定ini
                InitFontSelector(); //ini選取重設
                SetFallout3INI(main.Fallout3INI, FntName);
                
                

                if (index - 1 == this.MainSelect)
                {
                    textBoxFntName.Text = FntName;
                }
                index++;

            }
            toolStripProgressBar1.Visible = false;
            StatusText = GetString("Save complete.");
            this.saveFileDialog1.InitialDirectory = FontPath;
            
        }
        /// <summary>
        /// 存檔後直接改變ini設置
        /// </summary>
        /// <param name="sfont"></param>
        /// <param name="FntName"></param>
        private void SetFallout3INI(List<bool> Fallout3INI, string FntName)
        {
            for (int i = 0; i < 8; i++)
            {
                if (Fallout3INI[i])
                {

                    switch (i + 1)
                    {
                        case 1:
                            SetFallout3INI_sub(comboBox1, FntName);
                            break;
                        case 2:
                            SetFallout3INI_sub(comboBox2, FntName);
                            break;
                        case 3:
                            SetFallout3INI_sub(comboBox3, FntName);
                            break;
                        case 4:
                            SetFallout3INI_sub(comboBox4, FntName);
                            break;
                        case 5:
                            SetFallout3INI_sub(comboBox5, FntName);
                            break;
                        case 6:
                            SetFallout3INI_sub(comboBox6, FntName);
                            break;
                        case 7:
                            SetFallout3INI_sub(comboBox7, FntName);
                            break;
                        case 8:
                            SetFallout3INI_sub(comboBox8, FntName);
                            break;
                    }
                }
            }
        }
        private void SetFallout3INI_sub(ComboBox cb, string FntName)
        {
            int index = 0;
            foreach (FontFile ff in cb.Items)
            {
                if (ff.FntName.ToLower() == FntName.ToLower())
                {
                    cb.SelectedIndex = index;
                    break;
                }
                index++;
            }

        }
        /// <summary>
        /// 繪製文字
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button2_Click(object sender, EventArgs e)
        {
            //移除RemoveFnt
            bool AddBandList = false;
            foreach (Fnt_char fnt in RemoveFnt)
            {
                if (!fenc.IsBand(fnt.HEX)) { fenc.AddBand=fnt.HEX; AddBandList = true; }
                fnt.Enable = false;
            }
            RemoveFnt.Clear();
            SelectFnt.Clear();
            SelectFnt2.Clear();
            if (AddBandList) fenc.SaveBandFile();

            MakeFonts(); //製造字元

            DrawFonts(); //繪製文字

            //關聯文字複製
            foreach (Main m in MainList)
            {
                m.LinkClone();
            }

        }
        private void MakeFonts()
        {
            toolStripProgressBar1.Visible = true;
            StatusText = GetString("Manufacturing fonts...");
            //製作fnt
            foreach (Main m in MainList)
            {
                m.NewDrawing(fenc);
            }
            //開放清除
            this.buttonClear.Enabled = true;
            toolStripProgressBar1.Visible = false;
        }
        private bool DrawFonts()
        {
            toolStripProgressBar1.Visible = true;
            MaskReset();

            Rectangle p = new Rectangle(0, 0, 0, 0);
            int LineShift = 0;

            //集合fnt

            List<Fnt_char> sort = new List<Fnt_char>();
            foreach (Main m in this.MainList)
            {
                foreach (Fnt_char fnt in m.FntFile.CharList)
                {
                    if (fnt.Enable && !fenc.IsBand(fnt.HEX))
                        sort.Add(fnt);
                }
            }

            bool Vertical = false; //縱向排列
            if (radioButtonArrangeHeight.Checked)
            {
                sort.Sort(new DC_Font_Generator.Main.Fnt_char_Height()); //用高度排序
            }
            else if (radioButtonWidthArrange.Checked)
            {
                sort.Sort(new DC_Font_Generator.Main.Fnt_char_Width()); //用寬度排序
                Vertical = true;
            }

            this.errorProvider1.SetError(this.label7, "");
            this.StatusText = GetString("Drawing...");
            DateTime dt = DateTime.Now;
            bool redraw = true;
            while (redraw)
            {
                redraw = false;
                //製作Tex
                this.pictureBox1.Invalidate();
                CharIndex.Clear();
                if (this.TextImage != null) this.TextImage.Dispose();
                this.TextImage = new Bitmap(this.TextImageSize.Width, this.TextImageSize.Height);
                this.pictureBox1.SetImage = this.TextImage;

                Graphics graphics = Graphics.FromImage(this.TextImage);
                graphics.PageUnit = GraphicsUnit.Pixel;
                int Draw_Mode = 1;
                if (Draw_Mode == 1)
                {
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
				}
                else
                {
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bilinear;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                    graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                }

                int RGB = Color.FromArgb(0xFF, Color.Black).ToArgb();
                int ARGB = Color.FromArgb(0, Color.Black).ToArgb();
                int BackColor = button5.BackColor.ToArgb();
                if (BackColor == RGB || BackColor == ARGB)
                    graphics.Clear(Color.FromArgb(ARGB));
                else
                    graphics.Clear(Color.FromArgb(BackColor));

                p = new Rectangle(0, 0, 0, 0);
                LineShift = 0;

                ProgressBar = 0;
                ProgressBarMax = sort.Count;

                foreach (Fnt_char fnt in sort)
                {
                    int id = fnt.ID;
                    if (this.MainList[id].DrawToScreen(this.TextImage, ref p, ref LineShift, fnt, CharIndex, (int)numericUpDownGap.Value, Vertical, graphics))
                    {
                        redraw = true;
                        break;
                    }
                    ProgressBarAdd();
                }
                if (!redraw)
                {
                    string format = GetString("done.") + " {0} " + GetString("sec.");
                    StatusText = string.Format(format, DateTime.Now - dt);
                    this.TexEnable = true;
                    break;
                }
                else
                {
                    //自動擴增
                    int x = comboBoxSizeX.SelectedIndex; //((TexSize)comboBoxSizeX.SelectedItem).size;
                    int y = comboBoxSizeY.SelectedIndex; //((TexSize)comboBoxSizeY.SelectedItem).size; 
                    //如果爆掉
                    if (x + 1 == comboBoxSizeX.Items.Count && y + 1 == comboBoxSizeY.Items.Count)
                    {
                        StatusText = GetString("Font file size exceeds the limit! Can not be processed.");
                        this.pictureBox1.SetImage = this.TextImage;
                        toolStripProgressBar1.Visible = false;
                        this.TexEnable = false;
                        return false;
                    }

                    if (comboBoxSizeX.SelectedIndex <= comboBoxSizeY.SelectedIndex)
                    {
                        if (x < comboBoxSizeX.Items.Count)
                        {
                            comboBoxSizeX.SelectedIndex++;
                            comboBoxSizeX.Refresh();
                        }
                        else
                        {
                            comboBoxSizeY.SelectedIndex++;
                            comboBoxSizeY.Refresh();
                        }

                    }
                    else
                    {
                        if (y < comboBoxSizeY.Items.Count)
                        {
                            comboBoxSizeY.SelectedIndex++;
                            comboBoxSizeY.Refresh();
                        }
                        else
                        {
                            comboBoxSizeX.SelectedIndex++;
                            comboBoxSizeX.Refresh();
                        }
                    }
                    StatusText = GetString("Auto-amplification font size...");
                    this.label7.Refresh();
                }
            }
            this.button1.Enabled = true; //開放save
            this.buttonSavePrj.Enabled = true; //開放save project
            this.tableLayoutPanelAdjust.Enabled = true; //開放調整
            this.pictureBox1.SetImage = this.TextImage;
            this.tableLayoutPanelAdjust.Enabled = true;
            toolStripProgressBar1.Visible = false;
            return true;
        }
        /// <summary>
        /// 編碼選擇
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Encoding_comboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = Encoding_comboBox.SelectedIndex;
            int count = fenc.SwitchEnc(index);
            if (index >= 0)
            {
                if (index == 0)//dc選取字型
                {
                    label4.Enabled = false;

                }
                else
                {
                    label4.Enabled = true; 
                }
                this.button1.Enabled = false;
                this.button2.Enabled = true;
                this.button3.Enabled = true;
                this.button4.Enabled = true;
                this.buttonOpenFNT.Enabled = true;
                this.toolStrip1.Enabled = true;
                this.EnableLink(); //更動字型Link狀態
                //toolStripProgressBar1.Visible = false;
                //StatusText = string.Format("char count={0} , 重複字={1}", count, repcount);
                StatusText = string.Format(GetString("Characters count") + " = {0}", count);
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
            int X = ((TexSize)comboBoxSizeX.SelectedItem).size;
            int Y = ((TexSize)comboBoxSizeY.SelectedItem).size;
            this.TextImageSize = new Size(X, Y);
            this.button1.Enabled = false;
            this.errorProvider1.SetError(this.label7, "");
            label_TexSize.Text = ((TexSize)comboBoxSizeX.SelectedItem).MergeSize(Y);

        }
        /// <summary>
        /// 改變ImageSize時調整ComboBox的選取
        /// </summary>
        private bool ChangeImageSize()
        {
            this.TextImageSize.Width = this.TextImage.Width;
            this.TextImageSize.Height = this.TextImage.Height;

            ready = false;
            bool xfind = false;
            int index = 0;
            foreach (TexSize tx in comboBoxSizeX.Items)
            {
                if (tx.size == this.TextImageSize.Width)
                {
                    comboBoxSizeX.SelectedIndex = index;
                    xfind = true;
                    break;
                }
                index++;
            }
            bool yfind = false;
            index = 0;
            foreach (TexSize ty in comboBoxSizeY.Items)
            {
                if (ty.size == this.TextImageSize.Height)
                {
                    comboBoxSizeY.SelectedIndex = index;
                    yfind = true;
                    break;
                }
                index++;
            }

            ready = true;

            if (!xfind || !yfind)
            {
                StatusText = string.Format("Image Size error ({0},{1}).", this.TextImageSize.Width, this.TextImageSize.Height);
                return false;
            }
            return true;
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

            SelectFnt.Clear();
            SelectFnt2.Clear();
            RemoveFnt.Clear();

            this.TexEnable = false;
            CharIndex.Clear();
            this.MainList.Clear();
            MainSelect = 0;
            Main main = new Main(this, this.MainList, 0);
            main.TextOverFlow += new EventHandler(this.DealWithTextFLow);
            this.MainList.Add(main);

            this.buttonClear.Enabled = false;
            Font f = main.font1;
            this.label2.Text = f.Name + "," + f.Size;
            this.label2.Font = f;

            f = main.font2;
            this.label4.Text = f.Name + "," + f.Size;
            this.label4.Font = f;

            fontDialog1.Font = f;
            if (ready)
            {
                comboBoxSizeX.SelectedIndex = 0;
                comboBoxSizeY.SelectedIndex = 0;
            }
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

            int count = fenc.ImportEncoding();
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
            switch (Tag)
            {
                case ("ImportTex"):
                    openFileDialog1.Title = "Import Tex";
                    openFileDialog1.Filter = "Tex File|*.Tex";
                    openFileDialog1.FilterIndex = 1;
                    pass = true;
                    break;
                case ("ImportBmp"):
                    openFileDialog1.Title = "Import PNG";
                    openFileDialog1.Filter = "PNG File|*.PNG";
                    openFileDialog1.FilterIndex = 1;
                    pass = true;
                    break;
            }
            if (!pass) return;
            if (this.openFileDialog1.ShowDialog() == DialogResult.Cancel) return;

            string path = Path.GetDirectoryName(this.openFileDialog1.FileName) + @"\" + Path.GetFileNameWithoutExtension(this.openFileDialog1.FileName);
            toolStripProgressBar1.Visible = true;
            StatusText = GetString("Please wait...");
            switch (Tag)
            {
                case ("ImportTex"):
                    this.MainList[MainSelect].LoadTex(path + ".Tex");
                    this.pictureBox1.SetImage = this.TextImage;
                    if (ChangeImageSize())
                        StatusText = "Import Tex done.";
                    break;
                case ("ImportBmp"):
                    this.MainList[MainSelect].LoadBmp(path + ".png");
                    this.pictureBox1.SetImage = this.TextImage;
                    if (ChangeImageSize())
                        StatusText = "Import PNG done.";
                    break;
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
            string Tag = ((ComboBox)sender).Tag.ToString();
            FontFile ff = (FontFile)((ComboBox)sender).SelectedItem;
            string key = "";
            switch (Tag)
            {
                case ("Font1"):
                    key = "sFontFile_1";
                    break;
                case ("Font2"):
                    key = "sFontFile_2";
                    break;
                case ("Font3"):
                    key = "sFontFile_3";
                    break;
                case ("Font4"):
                    key = "sFontFile_4";
                    break;
                case ("Font5"):
                    key = "sFontFile_5";
                    break;
                case ("Font6"):
                    key = "sFontFile_6";
                    break;
                case ("Font7"):
                    key = "sFontFile_7";
                    break;
                case ("Font8"):
                    key = "sFontFile_8";
                    break;


            }
            if (key!="")
                ini.IniWriteValue("Fonts", key, ff.intFontPath);
        }
        /// <summary>
        /// Fallout3 Default Fonts
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button7_Click(object sender, EventArgs e)
        {
            comboBox1.SelectedIndex = 0; comboBox2.SelectedIndex = 0; comboBox3.SelectedIndex = 0; comboBox4.SelectedIndex = 0;
            comboBox5.SelectedIndex = 0; comboBox6.SelectedIndex = 0; comboBox7.SelectedIndex = 0; comboBox8.SelectedIndex = 0;
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
            IniFile fini = new IniFile(path);
            for (int i = 1; i <= 8; i++)
            {
                string s = fini.IniReadValue("Fonts", "sFontFile_" + i);
                ini.IniWriteValue("Fonts", "sFontFile_" + i, s);
            }
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
                using (StreamWriter sw = File.CreateText(path))
                {
                    
                    sw.WriteLine("[Fonts]");
                    FontFile ff = (FontFile)comboBox1.SelectedItem;
                    sw.WriteLine("sFontFile_1=" + ff.intFontPath);
                    ff = (FontFile)comboBox2.SelectedItem;
                    sw.WriteLine("sFontFile_2=" + ff.intFontPath);
                    ff = (FontFile)comboBox3.SelectedItem;
                    sw.WriteLine("sFontFile_3=" + ff.intFontPath);
                    ff = (FontFile)comboBox4.SelectedItem;
                    sw.WriteLine("sFontFile_4=" + ff.intFontPath);
                    ff = (FontFile)comboBox5.SelectedItem;
                    sw.WriteLine("sFontFile_5=" + ff.intFontPath);
                    ff = (FontFile)comboBox6.SelectedItem;
                    sw.WriteLine("sFontFile_6=" + ff.intFontPath);
                    ff = (FontFile)comboBox7.SelectedItem;
                    sw.WriteLine("sFontFile_7=" + ff.intFontPath);
                    ff = (FontFile)comboBox8.SelectedItem;
                    sw.WriteLine("sFontFile_8=" + ff.intFontPath);
                    sw.Close();
                }
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
        /// <summary>
        /// 滑鼠離開時
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            MouseLeftClick(-1, -1, false, false);
        }

        public string MouseLeftClick(int x, int y,bool selected,bool remove)
        {
            if (this.pictureBox1.SizeNormal()) return "";
            if (CharIndex[x, y] == null)
            {
                MaskReset();
                return "";
            }

            char c = CharIndex[x, y].c;

            byte[] b = fenc.enc.GetBytes(c.ToString());
            string hexOutput = CharIndex[x, y].HEX;

            //對文字畫上方塊
            
            

            //取得文字座標
            Fnt_char fnt = CharIndex[x, y];
            float rx = fnt.x1 * ((float)this.TextImageSize.Width);
            float ry = fnt.y1 * ((float)this.TextImageSize.Height);
            float bx = fnt.x4 * ((float)this.TextImageSize.Width);
            float by = fnt.y4 * ((float)this.TextImageSize.Height);

            if (fnt.ID != MainSelect) //link fnt
            {
                fnt = MainList[MainSelect].FntFile.GetFntFromHEX(hexOutput);
                if (!fnt.Enable)
                    fnt = CharIndex[x, y];
            }
            if (selected)
            {
                if (!remove) //加入選取容器
                {
                    if (SelectFnt.Contains(fnt))
                        SelectFnt.Remove(fnt);
                    else
                        SelectFnt.Add(fnt);
                }
                else //加入移除容器
                {
                    if (RemoveFnt.Contains(fnt))
                        RemoveFnt.Remove(fnt);
                    else
                        RemoveFnt.Add(fnt);
                }
            }

            MaskReset();
            //繪製圖像大小方框
            Pen red = new Pen(Color.Red, 1f);
            mask.DrawRectangle(red, rx, ry, bx - rx, by - ry);
            //繪製底部對齊
            Pen yellow = new Pen(Color.Yellow, 1f);
            mask.DrawLine(Pens.Yellow, rx + 1, ry + fnt.BottomAlign, bx - 1, ry + fnt.BottomAlign);

            string s = string.Format(this.ToolTipFormat, c, hexOutput, fnt.charViewWidth, fnt.charViewHeight, MainList[MainSelect].FntFile.Header.LineHeight, MainList[MainSelect].FntFile.Header.LineHeightFixed, fnt.BottomAlign, fnt.LeftSpace, fnt.RightSpace, bx - rx, by - ry, fnt.ID);

            this.pictureBox1.ToolTip = s;
            return string.Format("[{0}] Hex:[{1}]", c, hexOutput);
        }
        public void MaskReset()
        {
            if (this.TextImageMask != null) this.TextImageMask.Dispose();
            if (mask != null) mask.Dispose();

            this.TextImageMask = (Bitmap)this.TextImage.Clone();
            mask = Graphics.FromImage(this.TextImageMask);
            //mask.Clear(Color.FromArgb(0, Color.Black));
            mask.PageUnit = GraphicsUnit.Pixel;

            mask.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            mask.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bicubic;
            mask.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
            mask.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            mask.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;


            //繪製選取方塊
            foreach (Fnt_char fnt in SelectFnt)
            {
                float rx = fnt.x1 * ((float)this.TextImageSize.Width);
                float ry = fnt.y1 * ((float)this.TextImageSize.Height);
                float bx = fnt.x4 * ((float)this.TextImageSize.Width);
                float by = fnt.y4 * ((float)this.TextImageSize.Height);
                //繪製圖像大小方框
                Pen red = new Pen(Color.Red, 1f);
                mask.DrawRectangle(red, rx, ry, bx - rx, by - ry);
                //繪製底部對齊
            }
            //繪製選取方塊
            foreach (Fnt_char fnt in SelectFnt2)
            {
                float rx = fnt.x1 * ((float)this.TextImageSize.Width);
                float ry = fnt.y1 * ((float)this.TextImageSize.Height);
                float bx = fnt.x4 * ((float)this.TextImageSize.Width);
                float by = fnt.y4 * ((float)this.TextImageSize.Height);
                //繪製圖像大小方框
                Pen red = new Pen(Color.Red, 1f);
                mask.DrawRectangle(red, rx, ry, bx - rx, by - ry);
                //繪製底部對齊
            }

            //繪製移除方塊
            foreach (Fnt_char fnt in RemoveFnt)
            {
                float rx = fnt.x1 * ((float)this.TextImageSize.Width);
                float ry = fnt.y1 * ((float)this.TextImageSize.Height);
                float bx = fnt.x4 * ((float)this.TextImageSize.Width);
                float by = fnt.y4 * ((float)this.TextImageSize.Height);
                //繪製X
                Pen red = new Pen(Color.Red, 1f);
                mask.DrawLine(red, new PointF(rx, ry), new PointF(bx, by));
                mask.DrawLine(red, new PointF(bx, ry),new PointF( rx, by));
                //繪製底部對齊
            }

            //置換mask
            this.pictureBox1.ChangeImage = this.TextImageMask;

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

            string path = Path.GetDirectoryName(this.openFileDialog1.FileName) + @"\" + Path.GetFileNameWithoutExtension(this.openFileDialog1.FileName);
            toolStripProgressBar1.Visible = true;
            StatusText = GetString("Please wait...");
            Bitmap b = this.MainList[MainSelect].LoadTex(path + ".Tex");
            this.saveFileDialog1.Title = "Save PNG";
            this.saveFileDialog1.FileName = path;
            this.saveFileDialog1.Filter = "PNG File|*.PNG";
            this.saveFileDialog1.FilterIndex = 1;
            if (this.saveFileDialog1.ShowDialog() == DialogResult.Cancel) return;

            b.Save(this.saveFileDialog1.FileName, ImageFormat.Png); //save bmp
            
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
            string path = Path.GetDirectoryName(this.openFileDialog1.FileName) + @"\" + Path.GetFileNameWithoutExtension(this.openFileDialog1.FileName);
            //if (File.Exists(path + ".Tex"))
            //{
            //    MessageBoxResult result = MessageBox.Show(message, caption, MessageBoxButton.OKCancel);

            //}
            
            toolStripProgressBar1.Visible = true;
            StatusText = GetString("Please wait...");
            Bitmap b = this.MainList[MainSelect].LoadBmp(path + ".png");
            this.saveFileDialog1.Title = "Save Tex";
            this.saveFileDialog1.FileName = path;
            this.saveFileDialog1.Filter = "Tex File|*.Tex";
            this.saveFileDialog1.FilterIndex = 1;
            if (this.saveFileDialog1.ShowDialog() == DialogResult.Cancel) return;

            this.MainList[MainSelect].SaveTex(this.saveFileDialog1.FileName, b);
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
            int befor = this.MainSelect;

            switch (Tag)
            {
                case ("Up"):
                    this.MainSelect--; if (this.MainSelect < 0) this.MainSelect = 0;
                    SetNowData();
                    break;
                case ("Down"):
                    this.MainSelect++; if (this.MainSelect >= this.MainList.Count) this.MainSelect = this.MainList.Count - 1;
                    SetNowData();
                    break;
                case ("+"):
                    Main newMain = new Main(this, this.MainList, this.MainList.Count);
                    
                    
                    newMain.TextOverFlow += new EventHandler(this.DealWithTextFLow);
                    this.MainList.Add(newMain);
                    this.MainSelect = this.MainList.Count - 1;
                    SetNowData();
                    break;
                case ("-"):
                    if (this.MainList.Count > 0)
                    {
                        int DelID = this.MainList[this.MainSelect].ID;
                        Font DelF2 = this.MainList[this.MainSelect].font2;
                        this.MainList.Remove(this.MainList[this.MainSelect]);
                        foreach (Main m in this.MainList)
                        {
                            if (m.ID > this.MainSelect)
                            {
                                m.ID--;
                                foreach(Fnt_char fnt in m.FntFile.CharList)
                                {
                                    fnt.ID = m.ID;
                                }
                            }
                            if (m.DCfontLink == DelID)
                            {
                                m.DCfontLink = -1; //不關聯
                                m.font2 = DelF2;//拿回字型
                                m.FntFile.reset(true);
                            }
                        }
                        CharIndex.Clear();
                        this.MainSelect=0;
                        
                        SetNowData();
                    }
                    break;
            }
            SelectFnt.Clear();
            SelectFnt2.Clear();
            ready = false;
            checkBox_SelectAllSC.Checked = false;
            checkBox_SelectAllDC.Checked = false;
            ready = true;
            MaskReset();
        }

        private void textBoxFntName_TextChanged(object sender, EventArgs e)
        {
            this.MainList[this.MainSelect].name = this.textBoxFntName.Text;
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
            MainList[MainSelect].Fallout3INI[index] = value;

            
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
            string hexOutput = "";
            if (Text != "")
            {
                char[] c = Text.ToCharArray();
                byte[] b = fenc.enc.GetBytes(c[0].ToString());
                
                if (b.Length == 1)
                {
                    hexOutput = String.Format("{0:X2}", b[0]);
                }
                else if (b.Length > 1)
                {
                    hexOutput = String.Format("{0:X2}{1:X2}", b[0], b[1]);
                }
            }            
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
            if (e.KeyChar == (Char)8) return; //backspace
            ready = false;
            if (Text.Length > 3) { ((TextBox)sender).Text = ""; }
            ready = true;
            bool handled = true;
            #region HexCode
            switch (e.KeyChar.ToString().ToUpper())
            {
                case ("0"):
                case ("4"):
                case ("1"):
                case ("2"):
                case ("3"):
                case ("5"):
                case ("6"):
                case ("7"):
                case ("8"):
                case ("9"):
                case ("A"):
                case ("B"):
                case ("C"):
                case ("D"):
                case ("E"):
                case ("F"):
                    handled = false;
                    break;
            }
            #endregion
            e.Handled = handled;
            if (!handled)
            {
                string convert = e.KeyChar.ToString().ToUpper();
                char[] c = convert.ToCharArray();
                e.KeyChar = c[0];
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

            string TextOutput = "";
            int len = Text.Length;
            if (len == 4 || len == 2)
            {
                int num2 = int.Parse(Text, NumberStyles.AllowHexSpecifier);
                byte[] buffer = new byte[1];

                char c = '\0';
                if (len == 4)
                {
                    buffer = new byte[] { (byte)(num2 / 0x100), (byte)(num2 % 0x100) };
                }
                else if (len == 2)
                {
                    buffer = new byte[] { (byte)(num2 % 0x100) };
                }
                c = fenc.enc.GetChars(buffer)[0];
                TextOutput = c.ToString();
            }
            
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
            SelectFnt2.Clear();
            string startHex = textBox_FromHex.Text;
            string endHex = textBox_ToHex.Text;
            if (startHex == "") startHex = endHex;
            if (endHex == "") endHex = startHex;
            if (startHex == "" && endHex == "" && !checkBox_SelectAllSC.Checked && !checkBox_SelectAllDC.Checked) return;

            int start = int.Parse(startHex, NumberStyles.AllowHexSpecifier);
            int end = int.Parse(endHex, NumberStyles.AllowHexSpecifier);
            if (start > end)
            {
                int tmp = start;
                start = end; end = tmp;
            }
            foreach (Fnt_char fnt in MainList[MainSelect].FntFile.CharList)
            {
                if (!fnt.Enable) continue;
                int hex = int.Parse(fnt.HEX, NumberStyles.AllowHexSpecifier);

                bool s = false;
                if (hex>=start && hex<=end) 
                {
                    s = true;
                }
                else if (checkBox_SelectAllSC.Checked && hex >= 0 && hex <= 255)
                {
                    s = true;
                }
                else if (checkBox_SelectAllDC.Checked && hex >= 256 && hex <= 65535)
                {
                    s = true;
                }

                if (s && !SelectFnt.Contains(fnt) && !SelectFnt2.Contains(fnt))
                {
                    SelectFnt2.Add(fnt);
                }
            }
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
            if (SelectFnt.Count == 0 && SelectFnt2.Count == 0)
            {
                StatusText = GetString("Has not selected any font.");
                if (!radioButtonLineSpacing.Checked)
                    return;
            }
            if (StatusText == GetString("Has not selected any font.")) StatusText = "";
            string Tag = ((Button)sender).Tag.ToString();

            float av = 0;
            switch (Tag)
            {
                case("Add"):
                    av = (float)numericUpDown_Increment.Value;
                    break;
                case("Dec"):
                    av = -(float)numericUpDown_Increment.Value;
                    break;
            }
            

            int adj_traget = 0;
            if (radioButton_LeftSpacing.Checked && !checkBox_fixed.Checked) adj_traget = 1;
            if (radioButton_RightSpacing.Checked && !checkBox_fixed.Checked) adj_traget = 2;
            if (radioButtonLineSpacing.Checked && !checkBox_fixed.Checked)
            {

                MainList[MainSelect].FntFile.Header.LineHeight += av;
                MainList[MainSelect].FntFile.Header.LineHeightFixed += av;
                ReflashAdjustPreview();
                return;
            }
            if (radioButton_BottomAlign.Checked) adj_traget = 3;
            if (radioButtonScale.Checked) adj_traget = 4;
            
            if (adj_traget == 0) return;

            foreach (Fnt_char fnt in SelectFnt)
            {
                switch (adj_traget)
                {
                    case(1):
                        fnt.LeftSpace += av;
                        fnt.LeftSpaceFixed += av;
                        break;
                    case(2):
                        fnt.RightSpace += av;
                        fnt.RightSpaceFixed += av;
                        break;
                    case(3):
                        fnt.BottomAlign += av;
                        fnt.BottomAlignFixed += av;
                        break;
                    case(4):
                        if (fnt.charViewHeight + av > 0 && fnt.charViewWidth + av > 0)
                        {
                            fnt.charViewHeight += av;
                            fnt.charViewWidth += av;
                            fnt.charViewHeightFixed += av;
                            fnt.charViewWidthFixed += av;
                            fnt.BottomAlign += av;
                            fnt.BottomAlignFixed += av;

                            if (!checkBox_fixed.Checked && MainList[MainSelect].FntFile.Header.LineHeight < fnt.charViewHeight)
                            {
                                //MainList[MainSelect].FntFile.Header.LineHeight += av;
                                //MainList[MainSelect].FntFile.Header.LineHeightFixed += av;
                            }
                        }
                        break;
                }
            }
            foreach (Fnt_char fnt in SelectFnt2)
            {
                switch (adj_traget)
                {
                    case (1):
                        fnt.LeftSpace += av;
                        fnt.LeftSpaceFixed += av;
                        break;
                    case (2):
                        fnt.RightSpace += av;
                        fnt.RightSpaceFixed += av;
                        break;
                    case (3):
                        fnt.BottomAlign += av;
                        fnt.BottomAlignFixed += av;
                        break;
                    case (4):
                        if (fnt.charViewHeight + av > 0 && fnt.charViewWidth + av > 0)
                        {
                            fnt.charViewHeight += av;
                            fnt.charViewWidth += av;
                            fnt.charViewHeightFixed += av;
                            fnt.charViewWidthFixed += av;
                            fnt.BottomAlign += av;
                            fnt.BottomAlignFixed += av;

                            if (!checkBox_fixed.Checked && MainList[MainSelect].FntFile.Header.LineHeight < fnt.charViewHeight)
                            {
                                //MainList[MainSelect].FntFile.Header.LineHeight += av;
                                //MainList[MainSelect].FntFile.Header.LineHeightFixed += av;
                            }
                        }
                        break;

                }
            }
            //修正等寬字
            if(MainList[MainSelect].fixedFont)
            {
                MainList[MainSelect].FixedFont(MainList[MainSelect].fixedFont, MainList[MainSelect].FontMaxWidth);
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
            MainList[MainSelect].FntFile.Header.LineHeight -= MainList[MainSelect].FntFile.Header.LineHeightFixed;
            MainList[MainSelect].FntFile.Header.LineHeightFixed = 0;
            foreach (Fnt_char fnt in SelectFnt)
            {
                if (fnt.Enable)
                {
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
            foreach (Fnt_char fnt in SelectFnt2)
            {
                if (fnt.Enable)
                {
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
            mask = Graphics.FromImage(AdjPreview);
            mask.PageUnit = GraphicsUnit.Pixel;

            mask.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighSpeed;
            mask.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.Bicubic;
            mask.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.None;
            mask.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            mask.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
			mask.Clear(Color.FromArgb(0, Color.Black));
            if (textBox_TypeTest.Text == "") return;

            float LineH = MainList[MainSelect].FntFile.Header.LineHeight;
            PointF p=new PointF(0,0);
            char[] c = textBox_TypeTest.Text.ToCharArray();
            Fnt_char LastFnt=MainList[MainSelect].FntFile.GetFntFromChar(' ');

            int LinePoint = (int)LineH;
            for (int i = 0; i < c.Length; i++)
            {
                p.X += LastFnt.charViewWidth + LastFnt.RightSpace;
                Fnt_char fnt = MainList[MainSelect].FntFile.GetFntFromChar(c[i]);

                p.X += fnt.LeftSpace;
                if (p.X > AdjPreview.Width)
                {
                    p.X = 0;
                    LinePoint += (int)LineH;
                }
                p.Y = LinePoint - fnt.BottomAlign;
                if (p.Y > AdjPreview.Height) break;

                Bitmap fntimage = null;
                if (fnt.IsDC && MainList[MainSelect].DCfontLink > -1) //link fnt
                {
                    int link = MainList[MainSelect].DCfontLink;
                    Fnt_char f = MainList[link].FntFile.GetFntFromChar(c[i]);
                    fntimage = f.FontImage;
                }
                else
                    fntimage = fnt.FontImage;
                if (p.X < 0) p.X = 0;
                if (p.Y < LineH - fnt.BottomAlign) p.Y = LineH - fnt.BottomAlign;
                if (p.Y < 0) p.Y = 0;
                mask.DrawImage(fntimage, p.X,p.Y, fnt.charViewWidth, fnt.charViewHeight);
                LastFnt = fnt;
                
                
                

            }
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
            XmlWriterSettings mySettings = new XmlWriterSettings();
            mySettings.Indent = true;
            mySettings.IndentChars = ("    ");

            string filename = Path.GetFileNameWithoutExtension(this.saveFileDialog1.FileName);
            if (filename.Length > 8 && filename.Substring(filename.Length - 8).ToLower() == ".project")
                filename += ".xml";
            else
                filename += ".Project.xml";
            
            
            string path = Path.GetDirectoryName(this.saveFileDialog1.FileName);
            try
            {
                XmlWriter myWriter = XmlWriter.Create(Path.Combine(path, filename), mySettings);


                myWriter.WriteStartElement("main");
                myWriter.WriteElementString("Encoding", this.Encoding_comboBox.SelectedIndex.ToString());
                myWriter.WriteElementString("SizeX", this.comboBoxSizeX.SelectedIndex.ToString());
                myWriter.WriteElementString("SizeY", this.comboBoxSizeY.SelectedIndex.ToString());
                myWriter.WriteElementString("TexFileName", this.textBoxTexName.Text);
                myWriter.WriteElementString("Gap", this.numericUpDownGap.Value.ToString());
                myWriter.WriteElementString("BackGroundColor", this.button5.BackColor.ToArgb().ToString());
                
                int select = 0;
                if (radioButtonWidthArrange.Checked) select = 1;
                if (radioButtonCodeOrdered.Checked) select = 2;
                myWriter.WriteElementString("ArrangeMethod", select.ToString());
                myWriter.WriteElementString("FontLists", MainList.Count.ToString());
                int index = 1;
                foreach (Main m in MainList)
                {
                    myWriter.WriteStartElement("font", index.ToString());
                    if (m.ImportFont1name == "")
                    {

                        myWriter.WriteElementString("SCFontName", m.font1.FontFamily.Name);
                        myWriter.WriteElementString("SCFontSize", m.font1.Size.ToString());
                        myWriter.WriteElementString("SCFontStyle", m.font1.Style.ToString());

                    }
                    else
                    {
                        myWriter.WriteElementString("import_font", m.ImportFont1name);
                        
                    }
                    if (m.ImportFont2name == "")
                    {
                        if (m.DCfontLink > -1)
                        {
                            myWriter.WriteElementString("DCFontLink", m.DCfontLink.ToString());
                        }
                        else if (this.Encoding_comboBox.SelectedIndex != 0)
                        {
                            myWriter.WriteElementString("DCFontName", m.font2.FontFamily.Name);
                            myWriter.WriteElementString("DCFontSize", m.font2.Size.ToString());
                            myWriter.WriteElementString("DCFontStyle", m.font2.Style.ToString());
                        }
                    }

                    myWriter.WriteElementString("FntName", m.name);
                    myWriter.WriteElementString("Glow", m.Glow.ToString());
                    myWriter.WriteElementString("GlowColor", m.GlowColor.ToArgb().ToString());
                    myWriter.WriteElementString("Outline", m.Outline.ToString());
                    myWriter.WriteElementString("OutlineColor", m.OutlineColor.ToArgb().ToString());
                    myWriter.WriteElementString("FontColor", m.FontColor.ToArgb().ToString());
                    for (int i = 0; i < 8; i++)
                    {
                        myWriter.WriteElementString("LinkINI"+(i+1), m.Fallout3INI[i].ToString());
                    }
                    myWriter.WriteElementString("LineHeight", m.FntFile.Header.LineHeightFixed.ToString());
                    if (m.fixedFont)
                    {
                        myWriter.WriteElementString("FontMaxWidth", m.FontMaxWidth.ToString());
                    }

                    //調整參數
                    myWriter.WriteStartElement("Adjust");
                    foreach (Fnt_char fnt in m.FntFile.CharList)
                    {
                        if (!fnt.Enable) continue;
                        if(fnt.LeftSpaceFixed!=0)
                            myWriter.WriteElementString("LeftSpacing",fnt.HEX, fnt.LeftSpaceFixed.ToString());
                        if(fnt.RightSpaceFixed!=0)
                            myWriter.WriteElementString("RightSpacing", fnt.HEX, fnt.RightSpaceFixed.ToString());
                        if (fnt.BottomAlignFixed!=0)
                            myWriter.WriteElementString("BottomAlign", fnt.HEX, fnt.BottomAlignFixed.ToString());
                        if (fnt.charViewHeightFixed!=0)
                            myWriter.WriteElementString("CharViewHeight", fnt.HEX, fnt.charViewHeightFixed.ToString());
                        if (fnt.charViewWidthFixed != 0)
                            myWriter.WriteElementString("CharViewWidth", fnt.HEX, fnt.charViewWidthFixed.ToString());
                    }
                    myWriter.WriteEndElement();


                    myWriter.WriteEndElement();
                    index++;
                }
                myWriter.WriteEndElement();
                myWriter.Flush();
                myWriter.Close();
                StatusText = GetString("Project has been saved.");
            }
            catch (Exception ee)
            {
                System.Windows.Forms.MessageBox.Show(ee.Message);
            }
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
            string filename = Path.GetFileNameWithoutExtension(this.openFileDialog1.FileName) + ".xml";
            string path = Path.GetDirectoryName(this.openFileDialog1.FileName);

            try
            {
                
                XmlReaderSettings settings = new XmlReaderSettings();
                settings.IgnoreComments = true; // 不處理註解
                settings.IgnoreWhitespace = true; // 跳過空白
                settings.ValidationType = ValidationType.None; // 不驗證任何資料

                XmlReader myReader = XmlReader.Create(Path.Combine(path, filename), settings);
                Clear();
                int ChangeSizeX = -1; int ChangeSizeY = -1;

                //循環變數
                string CFontName = ""; float CFontSize = 0; string HEX = ""; FntFixed fx = new FntFixed(); FontStyle CFontStyle = FontStyle.Regular;

                List<PostAmendment> pas = new List<PostAmendment>(); //後製修正
                int pa = -1;
                // 進入讀取主要部分
                string value = "";
                bool err = false;
                while (myReader.Read())
                {

                    switch (myReader.NodeType)
                    {
                        case XmlNodeType.Element:
                            string LocalName = myReader.LocalName; // 取得標籤名稱
                            switch (LocalName)
                            {
                                case ("Encoding"):
                                    myReader.Read();
                                    value = myReader.Value;
                                    this.Encoding_comboBox.SelectedIndex = int.Parse(value);
                                    break;
                                case("SizeX"):
                                    myReader.Read(); value = myReader.Value;
                                    ChangeSizeX = int.Parse(value);
                                    break;
                                case ("SizeY"):
                                    myReader.Read(); value = myReader.Value;
                                    ChangeSizeY = int.Parse(value);
                                    break;
                                case ("Gap"):
                                    myReader.Read(); value = myReader.Value;
                                    this.numericUpDownGap.Value = decimal.Parse(value);
                                    break;
                                case("BackGroundColor"):
                                    myReader.Read(); value = myReader.Value;
                                    if (value=="-16777216")
                                        this.button5.BackColor = Color.FromArgb(0,Color.Black);
                                    else
                                        this.button5.BackColor = Color.FromArgb(int.Parse(value));
                                    break;
                                case ("TexFileName"):
                                    myReader.Read(); value = myReader.Value;
                                    this.textBoxTexName.Text = value;
                                    break;
                                case ("ArrangeMethod"):
                                    myReader.Read(); value = myReader.Value;
                                    if (value == "0") radioButtonArrangeHeight.Checked = true;
                                    if (value == "1") radioButtonWidthArrange.Checked = true;
                                    if (value == "2") radioButtonCodeOrdered.Checked = true;
                                    break;
                                case("FontLists"):
                                    myReader.Read(); value = myReader.Value;
                                    int MainListCount = int.Parse(value);
                                    if (MainListCount > 1)
                                    {
                                        for (int li = 1; li < MainListCount; li++)
                                        {
                                            Main newMain = new Main(this, this.MainList, li);
                                            newMain.TextOverFlow += new EventHandler(this.DealWithTextFLow);
                                            this.MainList.Add(newMain);
                                            this.MainSelect = li;
                                        }

                                    }
                                    else
                                    {
                                        this.MainSelect = 0;
                                    }
                                    break;
                                case("font"):
                                    this.MainSelect = int.Parse(myReader.NamespaceURI) - 1;
                                    PostAmendment postAmendment = new PostAmendment();
                                    postAmendment.ID = this.MainSelect;
                                    pas.Add(postAmendment);
                                    pa++;

                                    break;
                                case ("Adjust"):
                                    break;

                                case ("SCFontName"):
                                case ("DCFontName"):
                                    myReader.Read(); value = myReader.Value;
                                    CFontName = value;
                                    break;
                                case("SCFontSize"):
                                case ("DCFontSize"):
                                    myReader.Read(); value = myReader.Value;
                                    CFontSize = float.Parse(value);
                                    break;
                                case("SCFontStyle"):
                                    myReader.Read(); value = myReader.Value;
                                    CFontStyle=ConvertFontStyle(value);
                                    Font SCF = new Font(CFontName, CFontSize, CFontStyle);
                                    if (SCF.FontFamily.IsStyleAvailable(CFontStyle))
                                        MainList[MainSelect].font1 = SCF;
                                    else
                                    { OutputLog(GetString("Project font error : Missing Font.") + "(" + CFontName + ")"); err = true; }
                                    break;
                                case ("DCFontStyle"):
                                    myReader.Read(); value = myReader.Value;
                                    CFontStyle = ConvertFontStyle(value);
                                    Font DCF = new Font(CFontName, CFontSize, CFontStyle);

                                    if (DCF.FontFamily.IsStyleAvailable(CFontStyle))
                                        MainList[MainSelect].font2 = DCF;
                                    else
                                    { OutputLog(GetString("Project font error : Missing Font.") + "(" + CFontName + ")"); err = true; }
                                    break;
                                case("DCFontLink"):
                                    myReader.Read(); value = myReader.Value;
                                    MainList[MainSelect].DCfontLink = int.Parse(value);
                                    break;
                                case("import_font"):
                                    myReader.Read(); value = myReader.Value;
                                    if (!ImportFntAndTex(Path.Combine(FontPath, value + ".fnt"), value))
                                    {
                                        OutputLog(GetString("Project font error : Missing Fallout3 Font file.") + "(" + value + ".fnt)"); err = true;
                                    }
                                    break;
                                case("Glow"):
                                    myReader.Read(); value = myReader.Value;
                                    MainList[MainSelect].Glow = int.Parse(value);
                                    break;
                                case("GlowColor"):
                                    myReader.Read(); value = myReader.Value;
                                    MainList[MainSelect].GlowColor = Color.FromArgb(int.Parse(value));
                                    break;
                                case ("Outline"):
                                    myReader.Read(); value = myReader.Value;
                                    MainList[MainSelect].Outline = int.Parse(value);
                                    break;
                                case ("OutlineColor"):
                                    myReader.Read(); value = myReader.Value;
                                    MainList[MainSelect].OutlineColor = Color.FromArgb(int.Parse(value));
                                    break;
                                case("FontMaxWidth"):
                                    myReader.Read(); value = myReader.Value;
                                    this.MainList[this.MainSelect].fixedFont = true;
                                    this.MainList[this.MainSelect].FontMaxWidth = int.Parse(value);
                                    break;
                                case("FontColor"):
                                    myReader.Read(); value = myReader.Value;
                                    MainList[MainSelect].FontColor = Color.FromArgb(int.Parse(value));
                                    break;
                                case("LinkINI1"):
                                    myReader.Read(); value = myReader.Value;
                                    if (value.ToLower() == "true")
                                        MainList[MainSelect].Fallout3INI[0] = true;
                                    else
                                        MainList[MainSelect].Fallout3INI[0] = false;
                                    break;
                                case ("LinkINI2"):
                                    myReader.Read(); value = myReader.Value;
                                    if (value.ToLower() == "true")
                                        MainList[MainSelect].Fallout3INI[1] = true;
                                    else
                                        MainList[MainSelect].Fallout3INI[1] = false;
                                    break;
                                case ("LinkINI3"):
                                    myReader.Read(); value = myReader.Value;
                                    if (value.ToLower() == "true")
                                        MainList[MainSelect].Fallout3INI[2] = true;
                                    else
                                        MainList[MainSelect].Fallout3INI[2] = false;
                                    break;
                                case ("LinkINI4"):
                                    myReader.Read(); value = myReader.Value;
                                    if (value.ToLower() == "true")
                                        MainList[MainSelect].Fallout3INI[3] = true;
                                    else
                                        MainList[MainSelect].Fallout3INI[3] = false;
                                    break;
                                case ("LinkINI5"):
                                    myReader.Read(); value = myReader.Value;
                                    if (value.ToLower() == "true")
                                        MainList[MainSelect].Fallout3INI[4] = true;
                                    else
                                        MainList[MainSelect].Fallout3INI[4] = false;
                                    break;
                                case ("LinkINI6"):
                                    myReader.Read(); value = myReader.Value;
                                    if (value.ToLower() == "true")
                                        MainList[MainSelect].Fallout3INI[5] = true;
                                    else
                                        MainList[MainSelect].Fallout3INI[5] = false;
                                    break;
                                case ("LinkINI7"):
                                    myReader.Read(); value = myReader.Value;
                                    if (value.ToLower() == "true")
                                        MainList[MainSelect].Fallout3INI[6] = true;
                                    else
                                        MainList[MainSelect].Fallout3INI[6] = false;
                                    break;
                                case ("LinkINI8"):
                                    myReader.Read(); value = myReader.Value;
                                    if (value.ToLower() == "true")
                                        MainList[MainSelect].Fallout3INI[7] = true;
                                    else
                                        MainList[MainSelect].Fallout3INI[7] = false;
                                    break;
                                case ("FntName"):
                                    myReader.Read(); value = myReader.Value;
                                    MainList[MainSelect].name = value;
                                    break;
                                case ("LineHeight"):
                                    myReader.Read(); value = myReader.Value;
                                    pas[pa].LineHeightFixed = float.Parse(value);
                                    break;
                                case("LeftSpacing"):
                                    HEX=myReader.NamespaceURI;
                                    myReader.Read(); value = myReader.Value;
                                    fx = pas[pa][HEX];
                                    fx.hex = HEX;
                                    fx.LeftSpaceFixed = float.Parse(value);
                                    pas[pa][HEX] = fx;
                                    break;
                                case ("RightSpacing"):
                                    HEX = myReader.NamespaceURI;
                                    myReader.Read(); value = myReader.Value;
                                    fx = pas[pa][HEX];
                                    fx.hex = HEX;
                                    fx.RightSpaceFixed = float.Parse(value);
                                    pas[pa][HEX] = fx;
                                    break;
                                case ("BottomAlign"):
                                    HEX = myReader.NamespaceURI;
                                    myReader.Read(); value = myReader.Value;
                                    fx = pas[pa][HEX];
                                    fx.hex = HEX;
                                    fx.BottomAlignFixed = float.Parse(value);
                                    pas[pa][HEX] = fx;
                                    break;
                                case"CharViewHeight":
                                    HEX = myReader.NamespaceURI;
                                    myReader.Read(); value = myReader.Value;
                                    fx = pas[pa][HEX];
                                    fx.hex = HEX;
                                    fx.CharViewHeightFixed = float.Parse(value);
                                    pas[pa][HEX] = fx;
                                    break;
                                case "CharViewWidth":
                                    HEX = myReader.NamespaceURI;
                                    myReader.Read(); value = myReader.Value;
                                    fx = pas[pa][HEX];
                                    fx.hex = HEX;
                                    fx.CharViewWidthFixed = float.Parse(value);
                                    pas[pa][HEX] = fx;
                                    break;
                            }
                            break;
                    }
                }
                myReader.Close();
                SetNowData();



                MakeFonts(); //製造字元

              if (ChangeSizeX != -1) this.comboBoxSizeX.SelectedIndex = ChangeSizeX;
                if (ChangeSizeY != -1) this.comboBoxSizeY.SelectedIndex = ChangeSizeY;

                if (DrawFonts()) //繪製
                {
                    if (!err)
                        StatusText = GetString("Project has been opened. Please remember to save font.");
                    else
                        StatusText = GetString("Project error : Please refer to the log");
                }
                //關聯字型
                //關聯文字複製
                foreach (Main m in MainList)
                {
                    m.LinkClone();
                }

                //後置修正
                foreach (PostAmendment p in pas)
                {
                    if (p.IsEmpty) continue;
                    int id = p.ID;
                    if (p.LineHeightFixed != 0)
                        MainList[id].FntFile.Header.LineHeight += p.LineHeightFixed;
                    foreach (string hex in p.index)
                    {
                        FntFixed ff = p[hex];
                        Fnt_char fnt = (Fnt_char)MainList[id].FntFile.CharCode[hex];
                        if (fnt == null)
                        {
                            OutputLog("Project Load : (" + hex + ") " + GetString("Code does not exist!")); err = true;
                            continue;
                        }
                        if (!fnt.Enable) continue;
                        fnt.BottomAlign += ff.BottomAlignFixed;
                        fnt.BottomAlignFixed = ff.BottomAlignFixed;
                        fnt.charViewHeight += ff.CharViewHeightFixed;
                        fnt.charViewHeightFixed = ff.CharViewHeightFixed;
                        fnt.charViewWidth += ff.CharViewWidthFixed;
                        fnt.charViewWidthFixed = ff.CharViewWidthFixed;
                        if (MainList[id].fixedFont) continue;
                        fnt.LeftSpace += ff.LeftSpaceFixed;
                        fnt.LeftSpaceFixed = ff.LeftSpaceFixed;
                        fnt.RightSpace += ff.RightSpaceFixed;
                        fnt.RightSpaceFixed = ff.RightSpaceFixed;
                    }
                }
                //修正等寬字
                foreach (Main m in MainList)
                {
                    m.FixedFont(m.fixedFont, m.FontMaxWidth);
                }
            }
            catch (System.Xml.XmlException ee)
            {
                System.Windows.Forms.MessageBox.Show(ee.Message);
                StatusText = GetString("file error.");
            }
        }
        private FontStyle ConvertFontStyle(string fs)
        {
            FontStyle FS = FontStyle.Regular;
            switch (fs)
            {
                case("Bold"):
                    FS = FontStyle.Bold;
                    break;
                case("Italic"):
                    FS = FontStyle.Italic;
                    break;
                case ("Strikeout"):
                    FS = FontStyle.Strikeout;
                    break;
                case ("Underline"):
                    FS = FontStyle.Underline;
                    break;

            }
            return FS;
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
            if (MainList[MainSelect].FntFile.CharList.Count<256) return;
            bool check=((CheckBox)sender).Checked;
            if (check)
            {
                for (int i = 0; i < 256; i++)
                {
                    Fnt_char fnt = MainList[MainSelect].FntFile.CharList[i];
                    if (fnt.Enable && !SelectFnt2.Contains(fnt)) SelectFnt2.Add(fnt);
                }
            }
            else
            {
                for (int i = 0; i < 256; i++)
                {
                    Fnt_char fnt = MainList[MainSelect].FntFile.CharList[i];
                    if (fnt.Enable && SelectFnt2.Contains(fnt)) SelectFnt2.Remove(fnt);
                }

            }
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
            if (MainList[MainSelect].FntFile.CharList.Count < 257) return;
            bool check = ((CheckBox)sender).Checked;
            if (check)
            {
                for (int i = 256; i < MainList[MainSelect].FntFile.CharList.Count; i++)
                {
                    Fnt_char fnt = MainList[MainSelect].FntFile.CharList[i];
                    if (fnt.Enable && !SelectFnt2.Contains(fnt)) SelectFnt2.Add(fnt);
                }
            }
            else
            {
                for (int i = 256; i < MainList[MainSelect].FntFile.CharList.Count; i++)
                {
                    Fnt_char fnt = MainList[MainSelect].FntFile.CharList[i];
                    if (fnt.Enable && SelectFnt2.Contains(fnt)) SelectFnt2.Remove(fnt);
                }

            }
            MaskReset();
        }



    }
}
