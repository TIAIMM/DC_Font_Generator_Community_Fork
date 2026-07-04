using System;
using System.Buffers.Binary;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace DC_Font_Generator
{

    public class Fnt_char
    {
        public const int SerializedSize = 56;

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void CopyMemory(IntPtr dest, IntPtr src, uint length);

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
        private Bitmap lazySourceImage;
        private Rectangle lazySourceRect;
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
            byte[] bytes = new byte[SerializedSize];
            WriteTo(bytes);
            return bytes;
        }
        public void WriteTo(BinaryWriter writer)
        {
            Span<byte> bytes = stackalloc byte[SerializedSize];
            WriteTo(bytes);
            writer.Write(bytes);
        }
        public void WriteTo(Span<byte> bytes)
        {
            if (bytes.Length < SerializedSize) throw new ArgumentException("Fnt_char record buffer is too small.", nameof(bytes));

            WriteSingle(bytes, 0, iConstant_0);
            WriteSingle(bytes, 4, this.ix1);
            WriteSingle(bytes, 8, this.iy1);
            WriteSingle(bytes, 12, this.ix2);
            WriteSingle(bytes, 16, this.iy2);
            WriteSingle(bytes, 20, this.ix3);
            WriteSingle(bytes, 24, this.iy3);
            WriteSingle(bytes, 28, this.ix4);
            WriteSingle(bytes, 32, this.iy4);
            WriteSingle(bytes, 36, this.icharViewWidth);
            WriteSingle(bytes, 40, this.icharViewHeight);
            WriteSingle(bytes, 44, this.iLeftSpace);
            WriteSingle(bytes, 48, this.iRightSpace);
            WriteSingle(bytes, 52, this.iBottomAlign);
        }
        public void setBytes(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(SerializedSize);
            if (bytes.Length != SerializedSize) throw new EndOfStreamException("Unexpected end of .fnt character record.");
            ReadFrom(bytes);
        }
        public void ReadFrom(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length < SerializedSize) throw new ArgumentException("Fnt_char record buffer is too small.", nameof(bytes));

            iConstant_0 = ReadSingle(bytes, 0);
            this.ix1 = ReadSingle(bytes, 4);
            this.iy1 = ReadSingle(bytes, 8);
            this.ix2 = ReadSingle(bytes, 12);
            this.iy2 = ReadSingle(bytes, 16);
            this.ix3 = ReadSingle(bytes, 20);
            this.iy3 = ReadSingle(bytes, 24);
            this.ix4 = ReadSingle(bytes, 28);
            this.iy4 = ReadSingle(bytes, 32);
            this.icharViewWidth = ReadSingle(bytes, 36);
            this.icharViewHeight = ReadSingle(bytes, 40);
            this.iLeftSpace = ReadSingle(bytes, 44);
            this.iRightSpace = ReadSingle(bytes, 48);
            this.iBottomAlign = ReadSingle(bytes, 52);
            if (this.icharViewHeight + this.iBottomAlign + this.icharViewWidth + this.iLeftSpace + this.iRightSpace == 0) Enable = false;
        }
        private static void WriteSingle(Span<byte> bytes, int offset, float value)
        {
            BinaryPrimitives.WriteSingleLittleEndian(bytes.Slice(offset, sizeof(float)), value);
        }
        private static float ReadSingle(ReadOnlySpan<byte> bytes, int offset)
        {
            return BinaryPrimitives.ReadSingleLittleEndian(bytes.Slice(offset, sizeof(float)));
        }
        public Bitmap FontImage
        {
            get
            {
                if (image == null)
                {
                    if (lazySourceImage != null && lazySourceRect.Width > 0 && lazySourceRect.Height > 0)
                    {
                        image = CopyBitmapRegion(lazySourceImage, lazySourceRect);
                        lazySourceImage = null;
                    }
                    if (image != null)
                    {
                        return image;
                    }
                    return new Bitmap(1, 1);
                }
                return image;
            }
            set
            {
                image = value;
                lazySourceImage = null;
                lazySourceRect = Rectangle.Empty;
            }
        }
        public void SetLazyFontImage(Bitmap sourceImage, Rectangle sourceRect)
        {
            image = null;
            lazySourceImage = sourceImage;
            lazySourceRect = sourceRect;
        }
        private static Bitmap CopyBitmapRegion(Bitmap source, Rectangle sourceRect)
        {
            Bitmap cropped = new Bitmap(sourceRect.Width, sourceRect.Height, PixelFormat.Format32bppArgb);
            BitmapData sourceData = source.LockBits(sourceRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData croppedData = cropped.LockBits(
                new Rectangle(0, 0, cropped.Width, cropped.Height),
                ImageLockMode.WriteOnly,
                PixelFormat.Format32bppArgb);

            try
            {
                int copyBytes = sourceRect.Width * 4;
                for (int y = 0; y < sourceRect.Height; y++)
                {
                    IntPtr sourcePtr = IntPtr.Add(sourceData.Scan0, y * sourceData.Stride);
                    IntPtr croppedPtr = IntPtr.Add(croppedData.Scan0, y * croppedData.Stride);
                    CopyMemory(croppedPtr, sourcePtr, (uint)copyBytes);
                }
            }
            finally
            {
                cropped.UnlockBits(croppedData);
                source.UnlockBits(sourceData);
            }

            return cropped;
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

