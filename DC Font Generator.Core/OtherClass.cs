using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Drawing;
using System.Collections;
using System.Globalization;

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
            string LangINIPath = Path.Combine(System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase,"Language.INI");
            if (!File.Exists(LangINIPath)) return;
            Dictionary<string, List<string>> sections = ReadLanguageSections(LangINIPath);
            foreach (string sectionName in GetLanguageSectionCandidates(enc, sections))
            {
                if (sections.TryGetValue(sectionName, out List<string> langStr) && langStr.Count > 0)
                {
                    RecordCodePage(langStr);
                    return;
                }
            }

        }

        private static IEnumerable<string> GetLanguageSectionCandidates(Encoding enc, Dictionary<string, List<string>> sections)
        {
            if (enc != null)
            {
                yield return enc.WebName;
                yield return enc.CodePage.ToString(CultureInfo.InvariantCulture);
            }

            string uiName = CultureInfo.CurrentUICulture.Name;
            string uiTwoLetter = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
            if (uiName.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
            {
                if (uiName.IndexOf("TW", StringComparison.OrdinalIgnoreCase) >= 0
                    || uiName.IndexOf("HK", StringComparison.OrdinalIgnoreCase) >= 0
                    || uiName.IndexOf("MO", StringComparison.OrdinalIgnoreCase) >= 0
                    || uiName.IndexOf("Hant", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    yield return "big5";
                    yield return "950";
                }
                else
                {
                    yield return "gb2312";
                    yield return "936";
                }
            }
            else if (string.Equals(uiTwoLetter, "ja", StringComparison.OrdinalIgnoreCase))
            {
                yield return "shift_jis";
                yield return "932";
            }
            else if (string.Equals(uiTwoLetter, "ko", StringComparison.OrdinalIgnoreCase))
            {
                yield return "949";
            }

            yield return "gb2312";
            yield return "936";
            yield return "big5";
            yield return "950";
            yield return "shift_jis";
            yield return "932";

            if (sections.Count == 1)
            {
                foreach (string key in sections.Keys)
                {
                    yield return key;
                }
            }
        }

        private static Dictionary<string, List<string>> ReadLanguageSections(string path)
        {
            Dictionary<string, List<string>> sections = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            string currentSection = "";
            foreach (string rawLine in ReadLanguageLines(path))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith(";")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    if (!sections.ContainsKey(currentSection))
                    {
                        sections[currentSection] = new List<string>();
                    }
                    continue;
                }

                if (currentSection.Length == 0) continue;
                sections[currentSection].Add(line);
            }

            return sections;
        }

        private static string[] ReadLanguageLines(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            try
            {
                return SplitLanguageText(new UTF8Encoding(false, true).GetString(bytes));
            }
            catch (DecoderFallbackException)
            {
                return SplitLanguageText(Encoding.Default.GetString(bytes));
            }
        }

        private static string[] SplitLanguageText(string text)
        {
            if (text.Length > 0 && text[0] == '\uFEFF')
            {
                text = text.Substring(1);
            }

            return text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        }

        private void RecordCodePage(IList<string> LangStr)
        {
            for (int i = 0; i < LangStr.Count; i++)
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
        private float baseLine = 0;
        public bool IsEmpty = true;
        public List<string> index = new List<string>();

        public float fBaseLineFixed
        {
            get { return baseLine; }
            set { baseLine = value; IsEmpty = false; }
        }

        public float LineHeightFixed
        {
            get { return fBaseLineFixed; }
            set { fBaseLineFixed = value; }
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
        public float fLeadingEdgeFixed = 0;
        public float fSpacingFixed = 0;
        public float fTopEdgeFixed = 0;
        public float fHeightFixed = 0;
        public float fWidthFixed = 0;

        public float LeftSpaceFixed
        {
            get { return fLeadingEdgeFixed; }
            set { fLeadingEdgeFixed = value; }
        }

        public float RightSpaceFixed
        {
            get { return fSpacingFixed; }
            set { fSpacingFixed = value; }
        }

        public float BottomAlignFixed
        {
            get { return fTopEdgeFixed; }
            set { fTopEdgeFixed = value; }
        }

        public float CharViewHeightFixed
        {
            get { return fHeightFixed; }
            set { fHeightFixed = value; }
        }

        public float CharViewWidthFixed
        {
            get { return fWidthFixed; }
            set { fWidthFixed = value; }
        }
    }
}
