using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Collections;

namespace DC_Font_Generator
{
    #region Fallout INI 設置類
    class FontFile
    {
        private string FontName = "";
        private string FontPath = "";
        public string intFontPath = "";
        public bool Enable = false;
        public string Err = "";
        public bool SysFont = false;
        public bool dc = false;
        public System.Windows.Forms.ComboBox p;
        public int id = 0;
        /// <summary>
        /// SystemFont
        /// </summary>
        /// <param name="path"></param>
        /// <param name="system"></param>
        public FontFile(string path, bool system,Encoding enc)
        {
            if (system)
            {
                FontName = path;
                Enable = true;
                SysFont = true;
            }
            else
            {
                FontName = Path.GetFileName(path);
                check(path,enc);
            }
            intFontPath = @"Textures\Fonts\" + FontName;
        }
        public FontFile(string path, Encoding enc)
        {
            FontName = Path.GetFileName(FontPath);
            intFontPath = @"Textures\Fonts\" + FontName;

            check(path,enc);
        }
        public FontFile(string path, bool system, System.Windows.Forms.ComboBox cb, int index, Encoding enc)
        {

            if (system)
            {
                FontName = path;
                Enable = true;
                SysFont = true;
            }
            else
            {
                FontName = Path.GetFileName(path);
                check(path,enc);
            }
            intFontPath = @"Textures\Fonts\" + FontName;
            p = cb;
            id = index;
        }
        public FontFile(string path, bool system, int index, Encoding enc)
        {
            if (system)
            {
                FontName = path;
                Enable = true;
                SysFont = true;
                
            }
            else
            {
                FontName = Path.GetFileName(path);
                check(path,enc);
            }
            intFontPath = @"Textures\Fonts\" + FontName;
            id = index;
        }
        /// <summary>
        /// 檢查檔案正確性
        /// </summary>
        /// <param name="path"></param>
        private void check(string path,Encoding enc)
        {
            FontPath = path;
            if (File.Exists(FontPath))
            {
                FileInfo info = new FileInfo(path);
                if (info.Length == 14632)
                {
                    Enable = true;
                }
                else if (info.Length == 1362328)
                {
                    Enable = true;
                    dc = true;
                }
                else
                {
                    Enable = false;
                    Err = Path.GetFileName(path) + " : File size error.";
                    return;
                }
            }
            else
            {
                Err = Path.GetFileName(path) + " : File does not exist.";
                Enable = false; return;
            }

            //讀fnt更詳細的檢驗
            FL_FONT ff = new FL_FONT();
            if (!ff.CheckFile(path, enc))
            {
                Err = Path.GetFileName(path) + " : Tex file '" + ff.Header.TexFileName + ".tex' does not exist.";
                Enable = false;
            }
            
        }
        public bool IsThis(string fontname)
        {
            if (fontname.ToLower() == FontName.ToLower())
                return true;
            else
                return false;
        }
        public void SetComboBox(System.Windows.Forms.ComboBox cb)
        {
            if (SysFont)
            {
                p.SelectedIndex = 0;
                return;
            }

            cb.SelectedIndex = id;
        }
        /// <summary>
        /// 取得FNT名稱
        /// </summary>
        public string FntName
        {
            get
            {
                return Path.GetFileNameWithoutExtension(FontName);
            }
        }
        public override string ToString()
        {
            if (SysFont)
                return "(System)" + Path.GetFileNameWithoutExtension(FontName);
            else
            {
                if (dc)
                    return "(dc) " + Path.GetFileNameWithoutExtension(FontName);
                else
                    return "(sc) " + Path.GetFileNameWithoutExtension(FontName);
            }
        }
    }
    #endregion

    class HashData
    {
        public char c = '\0';
        public Bitmap image;
        public int TempIndex = 0;
        public HashData(char C,Bitmap b,int index)
        {
            c = C;
            image = b;
            TempIndex = index;
        }

    }

    class TexSize
    {
        public int size = 0;
        public int pow = 0;
        private const string fileSizeFormat = "fs";
        private const Decimal OneKiloByte = 1024M;
        private const Decimal OneMegaByte = OneKiloByte * 1024M;
        private const Decimal OneGigaByte = OneMegaByte * 1024M;

        public TexSize(int _pow)
        {
            pow = _pow;
            size = (int)Math.Pow(2, (double)pow);

        }
        public string MergeSize(int size2)
        {
            Decimal filesize = (Decimal)(size*size2*4);


            string suffix;
            if (filesize > OneGigaByte)
            {
                filesize /= OneGigaByte;
                suffix = "GB";
            }
            else if (filesize > OneMegaByte)
            {
                filesize /= OneMegaByte;
                suffix = "MB";
            }
            else if (filesize > OneKiloByte)
            {
                filesize /= OneKiloByte;
                suffix = "kB";
            }
            else
            {
                suffix = " B";
            }


            //string precision = format.Substring(2);
            //if (String.IsNullOrEmpty(precision)) precision = "2";
            return String.Format("{0:N0}{1}", filesize, suffix);

            
        }
        public override string ToString()
        {
            return size.ToString();
        }
    }

    public class LanguageData
    {
        private Hashtable ht = new Hashtable();
        public LanguageData(Encoding enc)
        {
            string CodePageName = enc.WebName;
            string LangINIPath = Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase,"Language.INI");
            if (!File.Exists(LangINIPath)) return;
            INI_RW.IniFile lang_ini = new INI_RW.IniFile(LangINIPath);
            string[] LangStr = lang_ini.ReadSection(CodePageName);
            if (LangStr.Length > 0)
            {
                RecordCodePage(LangStr);
            }
            else
            {
                LangStr = lang_ini.ReadSection(enc.CodePage.ToString());
                if (LangStr.Length > 0) RecordCodePage(LangStr);
            }

        }
        private void RecordCodePage(string[] LangStr)
        {
            for (int i = 0; i < LangStr.Length; i++)
            {
                int FindS = LangStr[i].IndexOf('='); //尋找第一個出現的索引
                if (FindS > -1)
                {
                    string index = LangStr[i].Substring(0, FindS).Trim();
                    string value = LangStr[i].Substring(FindS + 1, LangStr[i].Length - FindS - 1).Trim();
                    ht[index] = value;
                }
            }
        }
        public string GetString(string key)
        {
            if (ht[key] == null)
                return key;
            else
                return ht[key].ToString();
        }
    }

    /// <summary>
    /// 讀檔的後製修正
    /// </summary>
    public class PostAmendment
    {
        public int ID = 0;
        private Hashtable FntFixeds = new Hashtable();
        private float LineHeight = 0;
        public bool IsEmpty = true;
        public List<string> index = new List<string>();

        public float LineHeightFixed
        {
            get { return LineHeight; }
            set { LineHeight = value; IsEmpty = false; }
        }
        public FntFixed this[string hex]
        {
            get
            {
                if (!FntFixeds.Contains(hex)) return new FntFixed();
                return (FntFixed)FntFixeds[hex];
            }
            set
            {
                if (!FntFixeds.Contains(hex))
                {
                    FntFixeds.Add(hex, value);
                    index.Add(hex);
                }
                else
                    FntFixeds[hex] = value;
                IsEmpty = false;
            }
        }

    }
    public class FntFixed
    {
        public string hex = "";
        public float LeftSpaceFixed = 0; //曾經修正過的底部對齊
        public float RightSpaceFixed = 0; //曾經修正過的底部對齊
        public float BottomAlignFixed = 0; //曾經修正過的底部對齊
        public float CharViewHeightFixed = 0;
        public float CharViewWidthFixed = 0;
    }
}
