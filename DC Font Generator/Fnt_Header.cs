namespace DC_Font_Generator
{
    using System;
    using System.IO;
    using System.Text;

    public class Fnt_Header
    {
        private static int iConstant_0 = 1;
        private static int iConstant_1 = 1;
        private float iLineHeight;
        public float LineHeightFixed = 0; //曾經修過的行高
        private char[] iTexFileName = new char[0x11c];

        public byte[] getBytes(Encoding enc)
        {
            MemoryStream output = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(output, enc);
            writer.Write(this.iLineHeight);
            writer.Write(iConstant_0);
            writer.Write(iConstant_1);
            writer.Write(this.iTexFileName);
            writer.Flush();
            writer.Close();
            return output.ToArray();
        }
        public void setBytes(BinaryReader reader)
        {
            this.iLineHeight=reader.ReadSingle();
            iConstant_0=reader.ReadInt32();
            iConstant_1=reader.ReadInt32();
            this.iTexFileName = reader.ReadChars(0x11c);

        }

        public float LineHeight
        {
            get
            {
                return this.iLineHeight;
            }
            set
            {
                this.iLineHeight = value;
            }
        }

        public string TexFileName
        {
            get
            {
                string FileName = "";
                foreach (char c in iTexFileName)
                {
                    if (c == (char)0) break;
                    FileName += c;
                }
                return FileName;
            }
            set
            {
                if (value.Length <= 0x11c)
                {
                    char[] chArray = value.ToCharArray();
                    for (int i = 0; i < this.iTexFileName.Length; i++)
                    {
                        if (i < chArray.Length)
                        {
                            this.iTexFileName[i] = chArray[i];
                        }
                        else
                        {
                            this.iTexFileName[i] = '\0';
                        }
                    }
                }
            }
        }
    }
}

