namespace DC_Font_Generator
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Text;
	using System.Windows.Forms;

    public class FL_FONT
    {
		private static readonly byte[] SpecialBytes = HexStringToByteArray(
		"0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000040000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000040000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000040000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000040000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000040000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000040000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000040000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000040000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000040000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000040000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000004000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000400000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000040"
		);

		private List<Fnt_char> iCharList = new List<Fnt_char>();
        private Fnt_Header iHeader = new Fnt_Header();
        public Hashtable ht = new Hashtable();
        public Hashtable CharCode = new Hashtable(); //字碼對應CharList
        private Bitmap b_empty = new Bitmap(1, 1);
        public int EmptyDC = -1;
        public int EmptySC = -1;
        public float FixedWidth = 0; //等寬字型

		private static byte[] HexStringToByteArray(string hex)
		{
			hex = hex.Replace("\n", "").Replace(" ", "");
			int length = hex.Length;
			byte[] bytes = new byte[length / 2];
			for (int i = 0; i < length; i += 2)
			{
				bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			}
			return bytes;
		}

		public void Add(Fnt_char fnt,string hex,int ID)
        {
            fnt.ID = ID;
            fnt.HEX = hex;
            char c = fnt.c;
            int index = 0;
            //if (ht.Contains(c)) fnt.Empty = true; //重複char視為無字
            //if (fnt.Empty)
            //{
            //    if (fnt.IsDC)
            //    {
            //        if (EmptyDC == -1)
            //        {
            //            iCharList.Add(fnt);
            //            index = iCharList.Count - 1;
            //            EmptyDC = index;
            //        }
            //        else
            //        {
            //            Fnt_char fc = iCharList[EmptyDC];
            //            iCharList.Add(fc);
            //            index = iCharList.Count - 1;
            //        }
            //    }
            //    else
            //    {
            //        if (EmptySC == -1)
            //        {
            //            iCharList.Add(fnt);
            //            index = iCharList.Count - 1;
            //            EmptySC = index;
            //        }
            //        else
            //        {
            //            Fnt_char fc = iCharList[EmptySC];
            //            iCharList.Add(fc);
            //            index = iCharList.Count - 1;
            //        }
            //    }
            //    ht[c] = index;
            //    CharCode[hex] = iCharList[index];
            //    return;
            //}


            iCharList.Add(fnt);
            index = iCharList.Count - 1;
            ht[c] = index;
            CharCode[hex] = iCharList[index];
        }
        public bool HasCode(string index)
        {
            return CharCode.Contains(index);
        }
        public Fnt_char GetFntFromChar(char c)
        {
            Fnt_char fnt = new Fnt_char();
            if (ht.Contains(c))
            {
                int index = (int)ht[c];
                return iCharList[index];
            }
            fnt.Empty = true;
            return fnt;
        }
        public Fnt_char GetFntFromHEX(string hex)
        {
            Fnt_char fnt = new Fnt_char();
            foreach (Fnt_char f in iCharList)
            {
                if (f.HEX == hex)
                {
                    return f;
                }
            }
            fnt.Enable = true;
            fnt.Empty = true;
            return fnt;
        }

        public void AddEmpty(string hex,int ID)
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
            item.Empty = true;
            item.Enable = false;
            item.ID = ID;
            item.HEX = hex;
            iCharList.Add(item);
            int index=iCharList.Count-1;
            CharCode[hex] = iCharList[index];

        }
        private byte[] getBytes(Encoding enc, bool ASCII_only)
        {
            MemoryStream output = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(output,enc);
            writer.Write(this.iHeader.getBytes(enc));
            int count = 0;
            foreach (Fnt_char _char in this.iCharList)
            {
                if (ASCII_only && count > 255) break;
                writer.Write(_char.getBytes());
                count++;
            }
            writer.Flush();
            writer.Close();
            return output.ToArray();
        }
        private byte[] getBytes_append()
        {
            MemoryStream output = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(output);
            int count = 0;
            foreach (Fnt_char _char in this.iCharList)
            {
                if (count > 255)
                    writer.Write(_char.getBytes());
                count++;
            }
            writer.Flush();
            writer.Close();
            return output.ToArray();
        }

        private void setBytes(string filename, Encoding enc, List<string> Temp,int ID)
        {
            try
            {
                
                FileStream input = new FileStream(filename, FileMode.Open);
                BinaryReader reader = new BinaryReader(input, enc);
                this.iHeader.setBytes(reader);
                //開啟Tex
                this.iCharList.Clear();
                int count = 0;
                while (reader.PeekChar() != -1)
                {
                    Fnt_char fc = new Fnt_char();
                    fc.setBytes(reader);

                    string hex = Temp[count].Substring(2, 4);
                    fc.ID = ID;
                    fc.HEX = hex;
                    this.iCharList.Add(fc);
                    CharCode[hex] = this.iCharList[count];
                    count++;
                }
                reader.Close();
                input.Close();
            }
            catch (Exception ee)
            {
                System.Windows.Forms.MessageBox.Show(ee.Message);
            }
        }

        public void reset(bool KeepASCII)
        {
            if (KeepASCII)
            {
                this.iCharList.RemoveRange(256, this.iCharList.Count - 256);
                ht.Clear();
                CharCode.Clear();
                int index = 0;
                foreach (Fnt_char fnt in this.iCharList)
                {
                    if (!ht.Contains(fnt.c)) ht[fnt.c] = index;
                    CharCode[index.ToString("X4")] = this.iCharList[index];
                    index++;
                }
                EmptyDC = -1;
                return;
            }
            this.FixedWidth = 0;
            this.iCharList.Clear();
            ht.Clear();
            CharCode.Clear();
            EmptyDC = -1;
            EmptySC = -1;
        }

		public void save(string filename, Encoding enc, bool ASCII_only)
		{
			try
			{
				// 保存临时文件
				string tempFile = Path.GetTempFileName();
				using (FileStream output = new FileStream(tempFile, FileMode.Create))
				using (BinaryWriter writer = new BinaryWriter(output, enc))
				{
					// 写入头部和字符数据
					writer.Write(this.getBytes(enc, ASCII_only));
				}

				//Write glyph mapping data
				using (FileStream file = new FileStream(filename, FileMode.Create))
				using (MemoryStream mem = new MemoryStream(File.ReadAllBytes(tempFile)))
				{
					mem.Position = 0;
					mem.CopyTo(file);

					// 覆盖0x12C-0x823区域控制字符
					//file.Position = 0x12C;
					//file.Write(SpecialBytes, 0, Math.Min(SpecialBytes.Length, 0x823 - 0x12C + 1));
				}

				// 删除临时文件
				File.Delete(tempFile);
			}
			catch (Exception ee)
			{
				MessageBox.Show(ee.Message);
			}
		}

		public void save_append(string filename)
        {
            try
            {
                FileStream output = new FileStream(filename, FileMode.Append);
                BinaryWriter writer = new BinaryWriter(output);
                writer.Write(getBytes_append());
                writer.Flush();
                writer.Close();
                output.Close();
            }
            catch (Exception ee)
            {
                System.Windows.Forms.MessageBox.Show(ee.Message);
            }
        }

        public bool CheckFile(string filename, Encoding enc)
        {
            try
            {
                FileStream input = new FileStream(filename, FileMode.Open);
                BinaryReader reader = new BinaryReader(input, enc);
                this.iHeader.setBytes(reader);
                string Tex = Path.Combine(Path.GetDirectoryName(filename), this.iHeader.TexFileName + ".Tex");
                reader.Close();
                input.Close();
                return File.Exists(Tex);
            }
            catch(Exception ee)
            {
                System.Windows.Forms.MessageBox.Show(ee.ToString());
                return false;
            }
        }
        /// <summary>
        /// 完整載入fnt
        /// </summary>
        /// <param name="filename">檔案路徑名稱</param>
        /// <param name="enc">使用的編碼</param>
        /// <param name="Temp">使用的編碼樣版</param>
        /// <param name="ID">隸屬索引編號</param>
        public void load(string filename, Encoding enc,List<string> Temp,int ID)
        {
            reset(false);
            this.setBytes(filename, enc,Temp,ID); //讀取
            if (iCharList.Count == 0)
            {
                System.Windows.Forms.MessageBox.Show("File read failure : " + filename);
                return;
            }

            //統計
            float Width = 0;
            bool IsFixed = true;
            //建立字元關聯
            for (int i = 0; i <256; i++)
            {
                if (iCharList[i].Enable)
                {
                    byte[] b = new byte[1];
                    b[0] = (byte)i;
                    char c = enc.GetChars(b)[0];
                    CharCode[i.ToString("X4")] = iCharList[i];
                    if (!ht.Contains(c)) ht[c] = i;
                    iCharList[i].c = c;
                    float width = iCharList[i].charViewWidth + iCharList[i].LeftSpace + iCharList[i].RightSpace;
                    if (i < 0x7E && i>0x20)
                    {
                        if (Width == 0) Width = width;
                        if (Width != width)
                            IsFixed = false;
                    }
                }
            }
            if (IsFixed) this.FixedWidth = Width;
            if (iCharList.Count > 256)
            {
                int index = 256;
                for (int hh = 0x81; hh <= 0xFE; hh++) //81 FE //A1 F7
                {
                    for (int ll = 0x40; ll <= 0xFE; ll++) //40 FE //A1 FE
                    {
                        int hex = (hh * 256) + ll;
                        if (iCharList[index].Enable)
                        {
                            byte[] b = new byte[2];
                            b[0] = (byte)hh;
                            b[1] = (byte)ll;
                            char c = enc.GetChars(b)[0];
                            if (!ht.Contains(c)) ht[c] = index;
                            iCharList[index].c = c;
                            CharCode[hex.ToString("X4")] = iCharList[index];
                        }
                        index++;
                    }

                }
            }

        }
        public List<Fnt_char> CharList
        {
            get
            {
                return this.iCharList;
            }
            set
            {
                this.iCharList = value;
            }
        }

        public Fnt_Header Header
        {
            get
            {
                return this.iHeader;
            }
            set
            {
                this.iHeader = value;
            }
        }
    }
}

