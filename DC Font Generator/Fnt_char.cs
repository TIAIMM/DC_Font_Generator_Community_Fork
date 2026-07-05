using System;
using System.Buffers.Binary;
using System.IO;
using System.Drawing;
using System.Collections.Generic;

namespace DC_Font_Generator
{

    public class Fnt_char
    {
        public const int SerializedSize = 56;

        // Matches tNVSE / Fallout FontLetter: iTextureIndex, UVMap[4], fWidth, fHeight, fLeadingEdge, fSpacing, fTopEdge.
        public struct UVMap
        {
            public float fU;
            public float fV;
        }

        public int ID = 0; //所屬的上層編號
        private int textureIndex;
        private readonly UVMap[] uvMapping = new UVMap[4];
        private float width;
        private float height;
        private float leadingEdge;
        private float spacing;
        private float topEdge;
        public bool Empty = false;
        private bool iEnable = true;
        public bool IsSpace = false;
        public char c;
        public bool IsDC = false;
        public float fLeadingEdgeFixed = 0;
        public float fSpacingFixed = 0;
        public float fTopEdgeFixed = 0;
        public float fHeightFixed = 0;
        public float fWidthFixed = 0;
        private Bgra32Image glyphImage;
        private Bitmap image;
        private Bgra32Image lazySourcePixels;
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

            WriteInt32(bytes, 0, this.textureIndex);
            WriteSingle(bytes, 4, this.uvMapping[0].fU);
            WriteSingle(bytes, 8, this.uvMapping[0].fV);
            WriteSingle(bytes, 12, this.uvMapping[1].fU);
            WriteSingle(bytes, 16, this.uvMapping[1].fV);
            WriteSingle(bytes, 20, this.uvMapping[2].fU);
            WriteSingle(bytes, 24, this.uvMapping[2].fV);
            WriteSingle(bytes, 28, this.uvMapping[3].fU);
            WriteSingle(bytes, 32, this.uvMapping[3].fV);
            WriteSingle(bytes, 36, this.width);
            WriteSingle(bytes, 40, this.height);
            WriteSingle(bytes, 44, this.leadingEdge);
            WriteSingle(bytes, 48, this.spacing);
            WriteSingle(bytes, 52, this.topEdge);
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

            this.textureIndex = ReadInt32(bytes, 0);
            this.uvMapping[0].fU = ReadSingle(bytes, 4);
            this.uvMapping[0].fV = ReadSingle(bytes, 8);
            this.uvMapping[1].fU = ReadSingle(bytes, 12);
            this.uvMapping[1].fV = ReadSingle(bytes, 16);
            this.uvMapping[2].fU = ReadSingle(bytes, 20);
            this.uvMapping[2].fV = ReadSingle(bytes, 24);
            this.uvMapping[3].fU = ReadSingle(bytes, 28);
            this.uvMapping[3].fV = ReadSingle(bytes, 32);
            this.width = ReadSingle(bytes, 36);
            this.height = ReadSingle(bytes, 40);
            this.leadingEdge = ReadSingle(bytes, 44);
            this.spacing = ReadSingle(bytes, 48);
            this.topEdge = ReadSingle(bytes, 52);
            if (this.height + this.topEdge + this.width + this.leadingEdge + this.spacing == 0) Enable = false;
        }
        private static void WriteInt32(Span<byte> bytes, int offset, int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(bytes.Slice(offset, sizeof(int)), value);
        }
        private static int ReadInt32(ReadOnlySpan<byte> bytes, int offset)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(bytes.Slice(offset, sizeof(int)));
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
                    if (glyphImage == null && lazySourcePixels != null && lazySourceRect.Width > 0 && lazySourceRect.Height > 0)
                    {
                        glyphImage = lazySourcePixels.Crop(lazySourceRect);
                        lazySourcePixels = null;
                        lazySourceRect = Rectangle.Empty;
                    }
                    if (glyphImage != null)
                    {
                        image = glyphImage.ToBitmap();
                        return image;
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
                glyphImage = value != null ? Bgra32Image.FromBitmap(value) : null;
                lazySourcePixels = null;
                lazySourceRect = Rectangle.Empty;
            }
        }
        public Bgra32Image GlyphImage
        {
            get
            {
                if (glyphImage == null)
                {
                    if (lazySourcePixels != null && lazySourceRect.Width > 0 && lazySourceRect.Height > 0)
                    {
                        glyphImage = lazySourcePixels.Crop(lazySourceRect);
                        lazySourcePixels = null;
                        lazySourceRect = Rectangle.Empty;
                    }
                    else if (image != null)
                    {
                        glyphImage = Bgra32Image.FromBitmap(image);
                    }
                }

                return glyphImage;
            }
            set
            {
                glyphImage = value;
                if (image != null)
                {
                    image.Dispose();
                    image = null;
                }
                lazySourcePixels = null;
                lazySourceRect = Rectangle.Empty;
            }
        }
        public void SetLazyFontImage(Bitmap sourceImage, Rectangle sourceRect)
        {
            image = null;
            glyphImage = null;
            lazySourcePixels = sourceImage != null ? Bgra32Image.FromBitmap(sourceImage) : null;
            lazySourceRect = sourceRect;
        }
        public void SetLazyGlyphImage(Bgra32Image sourceImage, Rectangle sourceRect)
        {
            image = null;
            glyphImage = null;
            lazySourcePixels = sourceImage;
            lazySourceRect = sourceRect;
        }
        public int iTextureIndex
        {
            get { return this.textureIndex; }
            set { this.textureIndex = value; }
        }

        public UVMap[] pMapping
        {
            get { return this.uvMapping; }
        }

        public float fWidth
        {
            get { return this.width; }
            set { this.width = value; }
        }

        public float fHeight
        {
            get { return this.height; }
            set { this.height = value; }
        }

        public float fLeadingEdge
        {
            get { return this.leadingEdge; }
            set { this.leadingEdge = value; }
        }

        public float fSpacing
        {
            get { return this.spacing; }
            set { this.spacing = value; }
        }

        public float fTopEdge
        {
            get { return this.topEdge; }
            set { this.topEdge = value; }
        }

        public float BottomAlign
        {
            get { return this.fTopEdge; }
            set { this.fTopEdge = value; }
        }

        public float charViewHeight
        {
            get { return this.fHeight; }
            set { this.fHeight = value; }
        }

        public float charViewWidth
        {
            get { return this.fWidth; }
            set { this.fWidth = value; }
        }

        public float LeftSpace
        {
            get { return this.fLeadingEdge; }
            set { this.fLeadingEdge = value; }
        }

        public float RightSpace
        {
            get { return this.fSpacing; }
            set { this.fSpacing = value; }
        }

        public float LeftSpaceFixed
        {
            get { return this.fLeadingEdgeFixed; }
            set { this.fLeadingEdgeFixed = value; }
        }

        public float RightSpaceFixed
        {
            get { return this.fSpacingFixed; }
            set { this.fSpacingFixed = value; }
        }

        public float BottomAlignFixed
        {
            get { return this.fTopEdgeFixed; }
            set { this.fTopEdgeFixed = value; }
        }

        public float charViewHeightFixed
        {
            get { return this.fHeightFixed; }
            set { this.fHeightFixed = value; }
        }

        public float charViewWidthFixed
        {
            get { return this.fWidthFixed; }
            set { this.fWidthFixed = value; }
        }

        public float x1
        {
            get
            {
                return this.uvMapping[0].fU;
            }
            set
            {
                this.uvMapping[0].fU = value;
            }
        }

        public float x2
        {
            get
            {
                return this.uvMapping[1].fU;
            }
            set
            {
                this.uvMapping[1].fU = value;
            }
        }

        public float x3
        {
            get
            {
                return this.uvMapping[2].fU;
            }
            set
            {
                this.uvMapping[2].fU = value;
            }
        }

        public float x4
        {
            get
            {
                return this.uvMapping[3].fU;
            }
            set
            {
                this.uvMapping[3].fU = value;
            }
        }

        public float y1
        {
            get
            {
                return this.uvMapping[0].fV;
            }
            set
            {
                this.uvMapping[0].fV = value;
            }
        }

        public float y2
        {
            get
            {
                return this.uvMapping[1].fV;
            }
            set
            {
                this.uvMapping[1].fV = value;
            }
        }

        public float y3
        {
            get
            {
                return this.uvMapping[2].fV;
            }
            set
            {
                this.uvMapping[2].fV = value;
            }
        }

        public float y4
        {
            get
            {
                return this.uvMapping[3].fV;
            }
            set
            {
                this.uvMapping[3].fV = value;
            }
        }

    }


}

