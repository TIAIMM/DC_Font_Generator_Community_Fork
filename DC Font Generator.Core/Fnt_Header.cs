namespace DC_Font_Generator
{
    using System;
    using System.IO;
    using System.Text;

    public class Fnt_Header
    {
        public const int SerializedSize = 0x128;

        // Matches the FontData prefix: fBaseLine, iTextureCount, then TextureFile data.
        private int textureCount = 1;
        private int textureType = 1;
        private float baseLine;
        public float fBaseLineFixed = 0;
        private char[] iTexFileName = new char[0x11c];

        public byte[] getBytes(Encoding enc)
        {
            MemoryStream output = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(output, enc);
            WriteTo(writer);
            writer.Flush();
            writer.Close();
            return output.ToArray();
        }
        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(this.baseLine);
            writer.Write(textureCount);
            writer.Write(textureType);
            writer.Write(this.iTexFileName);
        }
        public void setBytes(BinaryReader reader)
        {
            this.baseLine=reader.ReadSingle();
            textureCount=reader.ReadInt32();
            textureType=reader.ReadInt32();
            this.iTexFileName = reader.ReadChars(0x11c);

        }

        public float fBaseLine
        {
            get
            {
                return this.baseLine;
            }
            set
            {
                this.baseLine = value;
            }
        }

        public int iTextureCount
        {
            get
            {
                return textureCount;
            }
            set
            {
                textureCount = value;
            }
        }

        public int iTextureType
        {
            get
            {
                return textureType;
            }
            set
            {
                textureType = value;
            }
        }

        public int TextureCount
        {
            get { return this.iTextureCount; }
            set { this.iTextureCount = value; }
        }

        public int TextureType
        {
            get { return this.iTextureType; }
            set { this.iTextureType = value; }
        }

        public float LineHeightFixed
        {
            get
            {
                return this.fBaseLineFixed;
            }
            set
            {
                this.fBaseLineFixed = value;
            }
        }

        // Compatibility alias for old project/UI code. The .fnt header field is FontData::fBaseLine.
        public float LineHeight
        {
            get
            {
                return this.fBaseLine;
            }
            set
            {
                this.fBaseLine = value;
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

