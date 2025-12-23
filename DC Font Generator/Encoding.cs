using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.IO;
using System.Windows.Forms;
using System.Globalization;

namespace DC_Font_Generator
{
    public class FontEncoding
    {
        public List<string> Temp;
        public Encoding enc = Encoding.Default;
        public Hashtable TempWith = new Hashtable();
        public bool ASCII_Only = false;
        public int count = 0;
        private List<string> BandList = new List<string>(); 
        public FontEncoding(Encoding Enc,bool ascii)
        {
            ASCII_Only = ascii;
            enc = Enc;
            LoadBandFile();
            MakeTemp();
        }
        public string AddBand
        {
            set
            {
                if (!BandList.Contains(value)) BandList.Add(value);
            }
        }
        public bool IsBand(string Band)
        {
            return BandList.Contains(Band);
        }
        public void LoadBandFile()
        {
            string AppPath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            string BandFile = Path.Combine(AppPath, "BandList.Txt");
            if (!File.Exists(BandFile)) return;
            try
            {
                FileStream myFile = File.Open(BandFile, FileMode.Open, FileAccess.Read);
                StreamReader br = new StreamReader(myFile); //讀取的文件
                String line;
                while ((line = br.ReadLine()) != null)
                {
                    BandList.Add(line);
                }

                br.Close();
            }
            catch (Exception ee)
            {
                System.Windows.Forms.MessageBox.Show(ee.Message);
            }


        }
        public void SaveBandFile()
        {
            string AppPath = System.AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            string BandFile = Path.Combine(AppPath, "BandList.Txt");
            try
            {
                using (StreamWriter sw = File.CreateText(BandFile))

                {
                    foreach(string l in BandList)
                        sw.WriteLine(l);

                    sw.Close();
                }
            }
            catch (Exception ee)
            {
                System.Windows.Forms.MessageBox.Show(ee.Message);
            }

        }
        public char CheckFontCode(string str,out bool IsDC,out bool IsError)
        {
            IsDC = false;
            IsError = false;
            byte[] buffer;
            char c = '\0';

            int intFontCode = int.Parse(str.Substring(2, 4), NumberStyles.AllowHexSpecifier);
            if ((intFontCode / 0x100) > 0)
            {
                
                buffer = new byte[] { (byte)(intFontCode / 0x100), (byte)(intFontCode % 0x100) };
                c = enc.GetChars(buffer)[0];
                
                IsDC = true;
                

                //檢查是否為錯誤字
                byte[] check = enc.GetBytes(new char[] { c });
                if (check[0] != buffer[0] || check[1] != buffer[1])
                {
                    IsError = true;
                    
                }
            }
            else
            {
                buffer = new byte[] { (byte)intFontCode };
                c = enc.GetChars(buffer)[0];

                //檢查是否為錯誤字
                byte[] check = enc.GetBytes(new char[] { c });
                if (check[0] != buffer[0])
                {
                    IsError = true;
                }

            }
            return c;
        }
        public int SwitchEnc(int index)
        {

            ASCII_Only = false;

            switch (index)
            {
                case (0): //ANSI
                    ASCII_Only = true;
                    count = this.ANSI();
                    enc = Encoding.Default;
                    break;
                case (1): //日文
                    count = this.SJIS();
                    enc = Encoding.GetEncoding(932);
                    break;
                case (2): //簡體中文
                    count = this.GB2312(false);
                    enc = Encoding.GetEncoding(936);
                    break;
                case (3): //韓文
                    count = this.Korea();
                    enc = Encoding.GetEncoding(949);
                    break;
                case (4): //繁體中文
                    count = this.Big5();
                    enc = Encoding.GetEncoding(950);
                    break;
                case (5): //GBK                   
                    count = this.GB2312(true);
                    enc = Encoding.GetEncoding(936);
                    break;
                case (6): //WIndows-1252
					ASCII_Only = true;
					count = this.Windows1252();
					enc = Encoding.GetEncoding(1252);
					break;
            }
            return count;
        }
        /// <summary>
        /// 匯入外部編碼
        /// </summary>
        /// <returns></returns>
        public int ImportEncoding()
        {
            OpenFileDialog open = new OpenFileDialog();
            //open.InitialDirectory = @"C:\";
            open.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
            open.FilterIndex = 1;
            open.RestoreDirectory = true;

            if (open.ShowDialog() != DialogResult.OK) return 0;

            if (!File.Exists(open.FileName)) return 0;

            FileStream myFile = File.Open(open.FileName, FileMode.Open, FileAccess.Read);
            StreamReader br = new StreamReader(myFile); //讀取的文件
            string allFile = br.ReadToEnd();
            br.Close();
            char[] ac = allFile.ToCharArray();

            count = MakeTemp();
            foreach (char c in ac)
            {
                string s = c.ToString();
                byte[] byteData = enc.GetBytes(s);
                if (byteData.Length > 1)
                {
                    int hex = ((int)byteData[0] * 256) + (int)byteData[1];
                    string shex = hex.ToString();
                    string sindex = (string)TempWith[shex];
                    int index = int.Parse(sindex);
                    string w = "0x" + byteData[0].ToString("X2") + byteData[1].ToString("X2");
                    if (w != Temp[index])
                        Temp[index] = w;
                    count++;
                }


            }
            return count;

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
            int count = 0;
            if (!ASCII_Only)
            {
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
            }

			for (byte b = 0x80; b < 0xFF; b++)
			{
				if (!BandList.Contains(b.ToString("X4")))
				{
					char c = Convert.ToChar(b);
					Temp[b] = "0x" + b.ToString("X4") + " " + c;
					count++;
				}
			}


			//Thai
			byte bb = 0xFF;
			if (!BandList.Contains(bb.ToString("X4")))
			{
				char cc = Convert.ToChar(bb);
				Temp[bb] = "0x" + bb.ToString("X4") + " " + cc;
				count++;
			}

			// 映射控制字符
			for (byte b = 0; b < 0x20; b++)
			{
				char c = Convert.ToChar(b);
				Temp[b] = "0x" + b.ToString("X4") + " " + c;
				count++;
			}

			//輸出ASCII
			for (byte b = 0x20; b < 0x7F; b++)
            {
                if (!BandList.Contains(b.ToString("X4")))
                {
                    char c = Convert.ToChar(b);
                    Temp[b] = "0x" + b.ToString("X4") + " " + c;
                    count++;
                }
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
                if (buffer[1] != 0x7F && buffer[1] != 0xFF)
                {
                    if (!BandList.Contains(i.ToString("X4")))
                    {
                        char c = enc.GetChars(buffer)[0];

                        Temp[index] = "0x" + i.ToString("X4") + " " + c.ToString();
                        count = count + 1;
                    }
                }

            }
            return count;
        }

        public int ANSI()
        {
            enc = Encoding.Default;
            int count = MakeTemp();
            return count;

        }
		public int Windows1252()
		{
			enc = Encoding.GetEncoding(1252);
			int count = MakeTemp();
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


        public int GB2312(bool GBK)
        {
            enc = Encoding.GetEncoding(936);
            int count = MakeTemp();
            if (GBK)
            {
                for (uint i = 0x81; i < 0xA1; i++)
                {
                    uint s = (i * 256) + 0x40;
                    uint e = (i * 256) + 0xFE;
                    count += output((ushort)s, (ushort)e);
                }
                count += output(0xA6A1, 0xA6B8); count += output(0xA6C1, 0xA6D8); count += output(0xA6E0, 0xA6EB); count += output(0xA6EE, 0xA6F2); count += output(0xA6F4, 0xA6F5);
                count += output(0xA7A1, 0xA7C1); count += output(0xA7D1, 0xA7F1);
                count += output(0xA840, 0xA87F); count += output(0xA880, 0xA895); count += output(0xA8A1, 0xA8BB); count += output(0xA8BD, 0xA8BE); count += output(0xA8C1, 0xA8C1); count += output(0xA8C5, 0xA8E9);
                count += output(0xA940, 0xA957); count += output(0xA959, 0xA95A); count += output(0xA95C, 0xA95C); count += output(0xA960, 0xA97E); count += output(0xA980, 0xA988); count += output(0xA996, 0xA996);

                for (uint i = 0xAA; i < 0xFD; i++)
                {
                    uint s = (i * 256) + 0x40;
                    uint e = (i * 256) + 0xA0;
                    count += output((ushort)s, (ushort)e);
                }
                count += output(0xFE40, 0xFE4F);
            }

            count += output(0xA1A1, 0xA1FE);
            //count += output(0xA2A1, 0xA2AA); count += output(0xA2B1, 0xA2E2); count += output(0xA2E5, 0xA2EE); count += output(0xA2F1, 0xA2FC);

            count += output(0xA3A1, 0xA3FE);
            count += output(0xA4A1, 0xA4F3);
            count += output(0xA5A1, 0xA5F6);

            count += output(0xB0A1, 0xB0FE);
            count += output(0xB1A1, 0xB1FE);
            count += output(0xB2A1, 0xB2FE);
            count += output(0xB3A1, 0xB3FE);
            count += output(0xB4A1, 0xB4FE);
            count += output(0xB5A1, 0xB5FE);
            count += output(0xB6A1, 0xB6FE);
            count += output(0xB7A1, 0xB7FE);
            count += output(0xB8A1, 0xB8FE);
            count += output(0xB9A1, 0xB9FE);
            count += output(0xBAA1, 0xBAFE);
            count += output(0xBBA1, 0xBBFE);
            count += output(0xBCA1, 0xBCFE);
            count += output(0xBDA1, 0xBDFE);
            count += output(0xBEA1, 0xBEFE);
            count += output(0xBFA1, 0xBFFE);

            count += output(0xC0A1, 0xC0FE);
            count += output(0xC1A1, 0xC1FE);
            count += output(0xC2A1, 0xC2FE);
            count += output(0xC3A1, 0xC3FE);
            count += output(0xC4A1, 0xC4FE);
            count += output(0xC5A1, 0xC5FE);
            count += output(0xC6A1, 0xC6FE);
            count += output(0xC7A1, 0xC7FE);
            count += output(0xC8A1, 0xC8FE);
            count += output(0xC9A1, 0xC9FE);
            count += output(0xCAA1, 0xCAFE);
            count += output(0xCBA1, 0xCBFE);
            count += output(0xCCA1, 0xCCFE);
            count += output(0xCDA1, 0xCDFE);
            count += output(0xCEA1, 0xCEFE);
            count += output(0xCFA1, 0xCFFE);

            count += output(0xD0A1, 0xD0FE);
            count += output(0xD1A1, 0xD1FE);
            count += output(0xD2A1, 0xD2FE);
            count += output(0xD3A1, 0xD3FE);
            count += output(0xD4A1, 0xD4FE);
            count += output(0xD5A1, 0xD5FE);
            count += output(0xD6A1, 0xD6FE);
            count += output(0xD7A1, 0xD7FE);
            count += output(0xD8A1, 0xD8FE);
            count += output(0xD9A1, 0xD9FE);
            count += output(0xDAA1, 0xDAFE);
            count += output(0xDBA1, 0xDBFE);
            count += output(0xDCA1, 0xDCFE);
            count += output(0xDDA1, 0xDDFE);
            count += output(0xDEA1, 0xDEFE);
            count += output(0xDFA1, 0xDFFE);

            count += output(0xE0A1, 0xE0FE);
            count += output(0xE1A1, 0xE1FE);
            count += output(0xE2A1, 0xE2FE);
            count += output(0xE3A1, 0xE3FE);
            count += output(0xE4A1, 0xE4FE);
            count += output(0xE5A1, 0xE5FE);
            count += output(0xE6A1, 0xE6FE);
            count += output(0xE7A1, 0xE7FE);
            count += output(0xE8A1, 0xE8FE);
            count += output(0xE9A1, 0xE9FE);
            count += output(0xEAA1, 0xEAFE);
            count += output(0xEBA1, 0xEBFE);
            count += output(0xECA1, 0xECFE);
            count += output(0xEDA1, 0xEDFE);
            count += output(0xEEA1, 0xEEFE);
            count += output(0xEFA1, 0xEFFE);

            count += output(0xF0A1, 0xF0FE);
            count += output(0xF1A1, 0xF1FE);
            count += output(0xF2A1, 0xF2FE);
            count += output(0xF3A1, 0xF3FE);
            count += output(0xF4A1, 0xF4FE);
            count += output(0xF5A1, 0xF5FE);
            count += output(0xF6A1, 0xF6FE);
            count += output(0xF7A1, 0xF7FE);
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

            count += output(0x889F, 0x88FC);

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

            count += output(0xEA40, 0xEA7E); count += output(0xEA80, 0xEAA4);

            count += output(0xED40, 0xED7E); count += output(0xED80, 0xEDEC); count += output(0xEDEF, 0xEDFC);
            count += output(0xEE40, 0xEE7E); count += output(0xEE80, 0xEEFC);

            count += output(0xFA40, 0xFA7E); count += output(0xFA80, 0xFAFC);
            count += output(0xFB40, 0xFB7E); count += output(0xFB80, 0xFBFC);
            count += output(0xFC40, 0xFC4B);
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

            //漢字
            /*
            for (uint i = 0xCA; i <= 0xFD; i++)
            {
                uint a = (i * 256) + 0xA1;
                uint b = (i * 256) + 0xFE;
                count += output((ushort)a, (ushort)b);
            }
            */

            return count;
        }
        /// <summary>
        /// 輸出Codepage
        /// </summary>
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
