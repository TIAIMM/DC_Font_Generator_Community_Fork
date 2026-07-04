namespace DC_Font_Generator
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Text;

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
        public string LastError = "";

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
            item.fTopEdge = 0f;
            item.fHeight = 0f;
            item.fWidth = 0f;
            item.fLeadingEdge = 0f;
            item.fSpacing = 0f;
            item.iTextureIndex = 0;
            item.pMapping[0].fU = 0f;
            item.pMapping[0].fV = 0f;
            item.pMapping[1].fU = 0f;
            item.pMapping[1].fV = 0f;
            item.pMapping[2].fU = 0f;
            item.pMapping[2].fV = 0f;
            item.pMapping[3].fU = 0f;
            item.pMapping[3].fV = 0f;
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
            WriteTo(writer, enc, ASCII_only);
            writer.Flush();
            writer.Close();
            return output.ToArray();
        }
        private byte[] getBytes_append()
        {
            MemoryStream output = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(output);
            WriteAppendTo(writer);
            writer.Flush();
            writer.Close();
            return output.ToArray();
        }

        private void WriteTo(BinaryWriter writer, Encoding enc, bool ASCII_only)
        {
            this.iHeader.WriteTo(writer);
            int max = ASCII_only ? Math.Min(256, this.iCharList.Count) : this.iCharList.Count;
            FntBinaryCodec.WriteRecords(writer, this.iCharList, 0, max);
        }

        private void WriteAppendTo(BinaryWriter writer)
        {
            FntBinaryCodec.WriteRecords(writer, this.iCharList, 256, this.iCharList.Count);
        }

        private void setBytes(string filename, Encoding enc, List<string> Temp,int ID)
        {
            try
            {
                using (FileStream input = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024))
                using (BinaryReader reader = new BinaryReader(input, enc))
                {
                    this.iHeader.setBytes(reader);
                    //開啟Tex
                    this.iCharList.Clear();
                    FntBinaryCodec.ReadRecords(input, reader, Temp, ID, this.iCharList, CharCode);
                }
            }
            catch (Exception ee)
            {
                LastError = ee.Message;
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
				using (FileStream output = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024))
				using (BinaryWriter writer = new BinaryWriter(output, enc))
				{
					WriteTo(writer, enc, ASCII_only);
				}
			}
			catch (Exception ee)
			{
				LastError = ee.Message;
			}
		}

		public void save_append(string filename)
        {
            try
            {
                using (FileStream output = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.None, 64 * 1024))
                using (BinaryWriter writer = new BinaryWriter(output))
                {
                    WriteAppendTo(writer);
                }
            }
            catch (Exception ee)
            {
                LastError = ee.Message;
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
                LastError = ee.ToString();
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
                LastError = "File read failure : " + filename;
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
                    float width = iCharList[i].fWidth + iCharList[i].fLeadingEdge + iCharList[i].fSpacing;
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

