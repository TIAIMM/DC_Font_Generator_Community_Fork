using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;

namespace DC_Font_Generator
{

    public class Fnt_char
    {
        public int ID = 0; //所屬的上層編號
        private float iBottomAlign;
        private float icharViewHeight;
        private float icharViewWidth;
        private static float iConstant_0 = 0;
        private float iLeftSpace;
        private float iRightSpace;
        private float ix1;
        private float ix2;
        private float ix3;
        private float ix4;
        private float iy1;
        private float iy2;
        private float iy3;
        private float iy4;
        public bool Empty = false;
        private bool iEnable = true;
        public bool IsSpace = false;
        public char c;
        public bool IsDC = false;
        public float LeftSpaceFixed = 0; //曾經修正過的底部對齊
        public float RightSpaceFixed = 0; //曾經修正過的底部對齊
        public float BottomAlignFixed = 0; //曾經修正過的底部對齊
        public float charViewHeightFixed = 0;
        public float charViewWidthFixed = 0;
        private Bitmap image;
        public float FixedWidth = 0; //等寬修正
        public string HEX = "";

        public Fnt_char()
        {
        }
        /// <summary>
        /// 釋放
        /// </summary>
        ~Fnt_char()
        {
            if (image != null) image.Dispose();
        }
        public bool Enable
        {
            get { return iEnable; }
            set
            {
                iEnable = value;
            }
        }
        public byte[] getBytes()
        {
            MemoryStream output = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(output);
            writer.Write(iConstant_0);
            writer.Write(this.ix1);
            writer.Write(this.iy1);
            writer.Write(this.ix2);
            writer.Write(this.iy2);
            writer.Write(this.ix3);
            writer.Write(this.iy3);
            writer.Write(this.ix4);
            writer.Write(this.iy4);
            writer.Write(this.icharViewWidth);
            writer.Write(this.icharViewHeight);
            writer.Write(this.iLeftSpace);
            writer.Write(this.iRightSpace);
            writer.Write(this.iBottomAlign);
            writer.Flush();
            writer.Close();
            return output.ToArray();
        }
        public void setBytes(BinaryReader reader)
        {
            iConstant_0 = reader.ReadSingle();
            this.ix1 = reader.ReadSingle();
            this.iy1 = reader.ReadSingle();
            this.ix2 = reader.ReadSingle();
            this.iy2 = reader.ReadSingle();
            this.ix3 = reader.ReadSingle();
            this.iy3 = reader.ReadSingle();
            this.ix4 = reader.ReadSingle();
            this.iy4 = reader.ReadSingle();
            this.icharViewWidth = reader.ReadSingle();
            this.icharViewHeight = reader.ReadSingle();
            this.iLeftSpace = reader.ReadSingle();
            this.iRightSpace = reader.ReadSingle();
            this.iBottomAlign = reader.ReadSingle();
            if (this.icharViewHeight + this.iBottomAlign + this.icharViewWidth + this.iLeftSpace + this.iRightSpace == 0) Enable = false;
        }
        public Bitmap FontImage
        {
            get
            {
                if (image == null)
                {
                    return new Bitmap(1, 1);
                }
                return image;
            }
            set { image = value; }
        }
        public float BottomAlign
        {
            get
            {
                return this.iBottomAlign;
            }
            set
            {
                this.iBottomAlign = value;
            }
        }

        public float charViewHeight
        {
            get
            {
                return this.icharViewHeight;
            }
            set
            {
                this.icharViewHeight = value;
            }
        }

        public float charViewWidth
        {
            get
            {
                return this.icharViewWidth;
            }
            set
            {
                this.icharViewWidth = value;
            }
        }

        public float LeftSpace
        {
            get
            {
                return this.iLeftSpace;
            }
            set
            {
                this.iLeftSpace = value;
            }
        }

        public float RightSpace
        {
            get
            {
                return this.iRightSpace;
            }
            set
            {
                this.iRightSpace = value;
            }
        }

        public float x1
        {
            get
            {
                return this.ix1;
            }
            set
            {
                this.ix1 = value;
            }
        }

        public float x2
        {
            get
            {
                return this.ix2;
            }
            set
            {
                this.ix2 = value;
            }
        }

        public float x3
        {
            get
            {
                return this.ix3;
            }
            set
            {
                this.ix3 = value;
            }
        }

        public float x4
        {
            get
            {
                return this.ix4;
            }
            set
            {
                this.ix4 = value;
            }
        }

        public float y1
        {
            get
            {
                return this.iy1;
            }
            set
            {
                this.iy1 = value;
            }
        }

        public float y2
        {
            get
            {
                return this.iy2;
            }
            set
            {
                this.iy2 = value;
            }
        }

        public float y3
        {
            get
            {
                return this.iy3;
            }
            set
            {
                this.iy3 = value;
            }
        }

        public float y4
        {
            get
            {
                return this.iy4;
            }
            set
            {
                this.iy4 = value;
            }
        }

    }


}

