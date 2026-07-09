using System;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace DC_Font_Generator
{
    internal static class DirectWriteGlyphPathService
    {
        private static readonly Guid IDWriteFactoryGuid = new Guid("b859ee5a-d838-4b5b-a2e8-1adc7d93db48");
        private static readonly Lazy<IDWriteFactory> SharedFactory = new Lazy<IDWriteFactory>(CreateFactory);

        public static bool TryGetGlyphPath(
            FontDescriptor font,
            FontStyleDescriptor descriptor,
            char c,
            float originX,
            float baseline,
            out SKPath path)
        {
            path = null;
            if (font == null || c < 32)
            {
                FontRenderDebugLog.Add($"[font-debug] DW skip char U+{(int)c:X4}: font={(font == null ? "<null>" : font.FamilyName)}, control={c < 32}");
                return false;
            }

            IDWriteFontCollection collection = null;
            IDWriteFontFamily family = null;
            IDWriteFont matchingFont = null;
            IDWriteFontFace fontFace = null;

            try
            {
                string familyName = !string.IsNullOrWhiteSpace(descriptor?.SourceFamilyName)
                    ? descriptor.SourceFamilyName
                    : font.FamilyName;
                if (string.IsNullOrWhiteSpace(familyName))
                {
                    FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: empty family, font={FormatFont(font)}, desc={FormatStyle(descriptor)}");
                    return false;
                }

                DWriteFontWeight weight = ToDWriteWeight(descriptor?.Weight ?? font.Weight);
                DWriteFontStretch stretch = ToDWriteStretch(descriptor?.Width ?? font.Width);
                DWriteFontStyle style = ToDWriteStyle(descriptor?.Slant ?? font.Slant);
                FontRenderDebugLog.Add($"[font-debug] DW request U+{(int)c:X4}: family={familyName}, requested w={(int)weight}, stretch={(int)stretch}, style={style}, font={FormatFont(font)}, desc={FormatStyle(descriptor)}");

                int hr = SharedFactory.Value.GetSystemFontCollection(out collection, false);
                if (hr < 0 || collection == null)
                {
                    FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: GetSystemFontCollection hr=0x{hr:X8}");
                    return false;
                }

                hr = collection.FindFamilyName(familyName, out uint familyIndex, out bool exists);
                if (hr < 0 || !exists)
                {
                    FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: FindFamilyName family={familyName}, exists={exists}, hr=0x{hr:X8}");
                    return false;
                }

                hr = collection.GetFontFamily(familyIndex, out family);
                if (hr < 0 || family == null)
                {
                    FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: GetFontFamily index={familyIndex}, hr=0x{hr:X8}");
                    return false;
                }

                bool exactFont = TryGetExactFont(family, weight, stretch, style, out matchingFont, out uint matchedIndex);
                if (!exactFont)
                {
                    hr = family.GetFirstMatchingFont(weight, stretch, style, out matchingFont);
                    if (hr < 0 || matchingFont == null)
                    {
                        FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: GetFirstMatchingFont hr=0x{hr:X8}");
                        return false;
                    }

                    matchedIndex = uint.MaxValue;
                }

                FontRenderDebugLog.Add($"[font-debug] DW font U+{(int)c:X4}: exact={exactFont}, familyIndex={familyIndex}, fontIndex={(matchedIndex == uint.MaxValue ? "fallback" : matchedIndex.ToString())}, resolved w={(int)matchingFont.GetWeight()}, stretch={(int)matchingFont.GetStretch()}, style={matchingFont.GetStyle()}, simulations={matchingFont.GetSimulations()}");

                hr = matchingFont.CreateFontFace(out fontFace);
                if (hr < 0 || fontFace == null)
                {
                    FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: CreateFontFace hr=0x{hr:X8}");
                    return false;
                }

                uint[] codePoints = { c };
                ushort[] glyphIndices = new ushort[1];
                hr = fontFace.GetGlyphIndices(codePoints, 1, glyphIndices);
                if (hr < 0 || glyphIndices[0] == 0)
                {
                    FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: GetGlyphIndices hr=0x{hr:X8}, glyph={glyphIndices[0]}, faceType={fontFace.GetType()}, faceIndex={fontFace.GetIndex()}");
                    return false;
                }

                using (DirectWriteSkiaGeometrySink sink = new DirectWriteSkiaGeometrySink(originX, baseline))
                {
                    hr = fontFace.GetGlyphRunOutline(
                        font.SizePixels,
                        glyphIndices,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        1,
                        false,
                        false,
                        sink);
                    if (hr < 0)
                    {
                        FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: GetGlyphRunOutline hr=0x{hr:X8}, glyph={glyphIndices[0]}, faceType={fontFace.GetType()}, faceIndex={fontFace.GetIndex()}");
                        return false;
                    }

                    path = sink.DetachPath();
                    if (path == null || path.IsEmpty)
                    {
                        FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: empty path, glyph={glyphIndices[0]}, faceType={fontFace.GetType()}, faceIndex={fontFace.GetIndex()}");
                        return false;
                    }

                    SKRect bounds = path.Bounds;
                    FontRenderDebugLog.Add($"[font-debug] DW hit U+{(int)c:X4}: glyph={glyphIndices[0]}, faceType={fontFace.GetType()}, faceIndex={fontFace.GetIndex()}, bounds={bounds.Width:0.##}x{bounds.Height:0.##}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                FontRenderDebugLog.AddException($"DW TryGetGlyphPath U+{(int)c:X4}", ex);
                path?.Dispose();
                path = null;
                return false;
            }
            finally
            {
                Release(fontFace);
                Release(matchingFont);
                Release(family);
                Release(collection);
            }
        }

        private static bool TryGetExactFont(
            IDWriteFontFamily family,
            DWriteFontWeight weight,
            DWriteFontStretch stretch,
            DWriteFontStyle style,
            out IDWriteFont matchingFont,
            out uint matchedIndex)
        {
            matchingFont = null;
            matchedIndex = uint.MaxValue;
            if (family == null)
            {
                return false;
            }

            uint count = family.GetFontCount();
            for (uint i = 0; i < count; i++)
            {
                IDWriteFont candidate = null;
                int hr = family.GetFont(i, out candidate);
                if (hr < 0 || candidate == null)
                {
                    FontRenderDebugLog.Add($"[font-debug] DW exact scan skip: index={i}, hr=0x{hr:X8}");
                    continue;
                }

                DWriteFontWeight candidateWeight = candidate.GetWeight();
                DWriteFontStretch candidateStretch = candidate.GetStretch();
                DWriteFontStyle candidateStyle = candidate.GetStyle();
                FontRenderDebugLog.Add($"[font-debug] DW exact scan: index={i}, w={(int)candidateWeight}, stretch={(int)candidateStretch}, style={candidateStyle}, simulations={candidate.GetSimulations()}");

                if (candidateWeight == weight
                    && candidateStretch == stretch
                    && candidateStyle == style)
                {
                    matchingFont = candidate;
                    matchedIndex = i;
                    return true;
                }

                Release(candidate);
            }

            return false;
        }

        private static IDWriteFactory CreateFactory()
        {
            Guid iid = IDWriteFactoryGuid;
            int hr = DWriteCreateFactory(DWriteFactoryType.Shared, ref iid, out IDWriteFactory factory);
            if (hr < 0 || factory == null)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            return factory;
        }

        private static DWriteFontWeight ToDWriteWeight(int weight)
        {
            if (weight < 1) weight = 400;
            if (weight > 999) weight = 999;
            return (DWriteFontWeight)weight;
        }

        private static DWriteFontStretch ToDWriteStretch(int width)
        {
            if (width < 1 || width > 9)
            {
                width = (int)SKFontStyleWidth.Normal;
            }

            return (DWriteFontStretch)width;
        }

        private static DWriteFontStyle ToDWriteStyle(SKFontStyleSlant slant)
        {
            return slant switch
            {
                SKFontStyleSlant.Italic => DWriteFontStyle.Italic,
                SKFontStyleSlant.Oblique => DWriteFontStyle.Oblique,
                _ => DWriteFontStyle.Normal
            };
        }

        private static string FormatFont(FontDescriptor font)
        {
            if (font == null)
            {
                return "<null>";
            }

            return $"{font.FamilyName}, style={font.StyleName ?? ""}, idx={font.StyleSetIndex}, w={font.Weight}, wd={font.Width}, slant={font.Slant}";
        }

        private static string FormatStyle(FontStyleDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return "<null>";
            }

            return $"{descriptor.SourceFamilyName ?? ""}/{descriptor.Name}, idx={descriptor.StyleSetIndex}, w={descriptor.Weight}, wd={descriptor.Width}, slant={descriptor.Slant}";
        }

        private static void Release(object comObject)
        {
            if (comObject != null && Marshal.IsComObject(comObject))
            {
                Marshal.ReleaseComObject(comObject);
            }
        }

        [DllImport("dwrite.dll", ExactSpelling = true)]
        private static extern int DWriteCreateFactory(
            DWriteFactoryType factoryType,
            ref Guid iid,
            [MarshalAs(UnmanagedType.Interface)] out IDWriteFactory factory);

        private enum DWriteFactoryType
        {
            Shared = 0,
            Isolated = 1
        }

        private enum DWriteFontStyle
        {
            Normal = 0,
            Oblique = 1,
            Italic = 2
        }

        private enum DWriteFontStretch
        {
            Undefined = 0,
            UltraCondensed = 1,
            ExtraCondensed = 2,
            Condensed = 3,
            SemiCondensed = 4,
            Normal = 5,
            SemiExpanded = 6,
            Expanded = 7,
            ExtraExpanded = 8,
            UltraExpanded = 9
        }

        private enum DWriteFontWeight
        {
            Thin = 100,
            ExtraLight = 200,
            Light = 300,
            SemiLight = 350,
            Normal = 400,
            Medium = 500,
            DemiBold = 600,
            Bold = 700,
            ExtraBold = 800,
            Black = 900,
            ExtraBlack = 950
        }

        private enum DWriteFontFaceType
        {
            Cff = 0,
            TrueType = 1,
            OpenTypeCollection = 2,
            Type1 = 3,
            Vector = 4,
            Bitmap = 5,
            Unknown = 6,
            RawCff = 7,
            TrueTypeCollection = OpenTypeCollection
        }

        [Flags]
        private enum DWriteFontSimulations
        {
            None = 0,
            Bold = 1,
            Oblique = 2
        }

        private enum DWriteInformationalStringId
        {
            None = 0
        }

        private enum D2D1FillMode
        {
            Alternate = 0,
            Winding = 1
        }

        [Flags]
        private enum D2D1PathSegment
        {
            None = 0,
            ForceUnstroked = 1,
            ForceRoundLineJoin = 2
        }

        private enum D2D1FigureBegin
        {
            Filled = 0,
            Hollow = 1
        }

        private enum D2D1FigureEnd
        {
            Open = 0,
            Closed = 1
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DWriteFontMetrics
        {
            public ushort DesignUnitsPerEm;
            public ushort Ascent;
            public ushort Descent;
            public short LineGap;
            public ushort CapHeight;
            public ushort XHeight;
            public short UnderlinePosition;
            public ushort UnderlineThickness;
            public short StrikethroughPosition;
            public ushort StrikethroughThickness;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DWriteGlyphMetrics
        {
            public int LeftSideBearing;
            public uint AdvanceWidth;
            public int RightSideBearing;
            public int TopSideBearing;
            public uint AdvanceHeight;
            public int BottomSideBearing;
            public int VerticalOriginY;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct D2DPoint2F
        {
            public float X;
            public float Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct D2DBezierSegment
        {
            public D2DPoint2F Point1;
            public D2DPoint2F Point2;
            public D2DPoint2F Point3;
        }

        [ComImport]
        [Guid("b859ee5a-d838-4b5b-a2e8-1adc7d93db48")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteFactory
        {
            [PreserveSig]
            int GetSystemFontCollection(
                [MarshalAs(UnmanagedType.Interface)] out IDWriteFontCollection fontCollection,
                [MarshalAs(UnmanagedType.Bool)] bool checkForUpdates);
        }

        [ComImport]
        [Guid("a84cee02-3eea-4eee-a827-87c1a02a0fcc")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteFontCollection
        {
            uint GetFontFamilyCount();

            [PreserveSig]
            int GetFontFamily(uint index, [MarshalAs(UnmanagedType.Interface)] out IDWriteFontFamily fontFamily);

            [PreserveSig]
            int FindFamilyName(
                [MarshalAs(UnmanagedType.LPWStr)] string familyName,
                out uint index,
                [MarshalAs(UnmanagedType.Bool)] out bool exists);

            [PreserveSig]
            int GetFontFromFontFace(
                [MarshalAs(UnmanagedType.Interface)] IDWriteFontFace fontFace,
                [MarshalAs(UnmanagedType.Interface)] out IDWriteFont font);
        }

        [ComImport]
        [Guid("da20d8ef-812a-4c43-9802-62ec4abd7add")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteFontFamily
        {
            [PreserveSig]
            int GetFontCollection([MarshalAs(UnmanagedType.Interface)] out IDWriteFontCollection fontCollection);

            uint GetFontCount();

            [PreserveSig]
            int GetFont(uint index, [MarshalAs(UnmanagedType.Interface)] out IDWriteFont font);

            [PreserveSig]
            int GetFamilyNames(out IntPtr names);

            [PreserveSig]
            int GetFirstMatchingFont(
                DWriteFontWeight weight,
                DWriteFontStretch stretch,
                DWriteFontStyle style,
                [MarshalAs(UnmanagedType.Interface)] out IDWriteFont matchingFont);
        }

        [ComImport]
        [Guid("acd16696-8c14-4f5d-877e-fe3fc1d32737")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteFont
        {
            [PreserveSig]
            int GetFontFamily([MarshalAs(UnmanagedType.Interface)] out IDWriteFontFamily fontFamily);

            DWriteFontWeight GetWeight();
            DWriteFontStretch GetStretch();
            DWriteFontStyle GetStyle();

            [return: MarshalAs(UnmanagedType.Bool)]
            bool IsSymbolFont();

            [PreserveSig]
            int GetFaceNames(out IntPtr names);

            [PreserveSig]
            int GetInformationalStrings(
                DWriteInformationalStringId informationalStringID,
                out IntPtr informationalStrings,
                [MarshalAs(UnmanagedType.Bool)] out bool exists);

            DWriteFontSimulations GetSimulations();
            void GetMetrics(out DWriteFontMetrics fontMetrics);

            [PreserveSig]
            int HasCharacter(uint unicodeValue, [MarshalAs(UnmanagedType.Bool)] out bool exists);

            [PreserveSig]
            int CreateFontFace([MarshalAs(UnmanagedType.Interface)] out IDWriteFontFace fontFace);
        }

        [ComImport]
        [Guid("5f49804d-7024-4d43-bfa9-d25984f53849")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDWriteFontFace
        {
            DWriteFontFaceType GetType();

            [PreserveSig]
            int GetFiles(ref uint numberOfFiles, IntPtr fontFiles);

            uint GetIndex();
            DWriteFontSimulations GetSimulations();

            [return: MarshalAs(UnmanagedType.Bool)]
            bool IsSymbolFont();

            void GetMetrics(out DWriteFontMetrics fontFaceMetrics);
            ushort GetGlyphCount();

            [PreserveSig]
            int GetDesignGlyphMetrics(
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ushort[] glyphIndices,
                uint glyphCount,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] DWriteGlyphMetrics[] glyphMetrics,
                [MarshalAs(UnmanagedType.Bool)] bool isSideways);

            [PreserveSig]
            int GetGlyphIndices(
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] uint[] codePoints,
                uint codePointCount,
                [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] ushort[] glyphIndices);

            [PreserveSig]
            int TryGetFontTable(
                uint openTypeTableTag,
                out IntPtr tableData,
                out uint tableSize,
                out IntPtr tableContext,
                [MarshalAs(UnmanagedType.Bool)] out bool exists);

            void ReleaseFontTable(IntPtr tableContext);

            [PreserveSig]
            int GetGlyphRunOutline(
                float emSize,
                [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] ushort[] glyphIndices,
                IntPtr glyphAdvances,
                IntPtr glyphOffsets,
                uint glyphCount,
                [MarshalAs(UnmanagedType.Bool)] bool isSideways,
                [MarshalAs(UnmanagedType.Bool)] bool isRightToLeft,
                [MarshalAs(UnmanagedType.Interface)] ID2D1SimplifiedGeometrySink geometrySink);
        }

        [ComVisible(true)]
        [Guid("2cd9069e-12e2-11dc-9fed-001143a055f9")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ID2D1SimplifiedGeometrySink
        {
            void SetFillMode(D2D1FillMode fillMode);
            void SetSegmentFlags(D2D1PathSegment vertexFlags);
            void BeginFigure(D2DPoint2F startPoint, D2D1FigureBegin figureBegin);
            void AddLines([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] D2DPoint2F[] points, uint pointsCount);
            void AddBeziers([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] D2DBezierSegment[] beziers, uint beziersCount);
            void EndFigure(D2D1FigureEnd figureEnd);

            [PreserveSig]
            int Close();
        }

        [ComVisible(true)]
        [ClassInterface(ClassInterfaceType.None)]
        private sealed class DirectWriteSkiaGeometrySink : ID2D1SimplifiedGeometrySink, IDisposable
        {
            private readonly float originX;
            private readonly float baseline;
            private SKPath path = new SKPath();

            public DirectWriteSkiaGeometrySink(float originX, float baseline)
            {
                this.originX = originX;
                this.baseline = baseline;
            }

            public void SetFillMode(D2D1FillMode fillMode)
            {
                if (path != null)
                {
                    path.FillType = fillMode == D2D1FillMode.Winding
                        ? SKPathFillType.Winding
                        : SKPathFillType.EvenOdd;
                }
            }

            public void SetSegmentFlags(D2D1PathSegment vertexFlags)
            {
            }

            public void BeginFigure(D2DPoint2F startPoint, D2D1FigureBegin figureBegin)
            {
                path?.MoveTo(originX + startPoint.X, baseline + startPoint.Y);
            }

            public void AddLines(D2DPoint2F[] points, uint pointsCount)
            {
                if (path == null || points == null)
                {
                    return;
                }

                int count = (int)Math.Min(pointsCount, (uint)points.Length);
                for (int i = 0; i < count; i++)
                {
                    path.LineTo(originX + points[i].X, baseline + points[i].Y);
                }
            }

            public void AddBeziers(D2DBezierSegment[] beziers, uint beziersCount)
            {
                if (path == null || beziers == null)
                {
                    return;
                }

                int count = (int)Math.Min(beziersCount, (uint)beziers.Length);
                for (int i = 0; i < count; i++)
                {
                    D2DBezierSegment bezier = beziers[i];
                    path.CubicTo(
                        originX + bezier.Point1.X,
                        baseline + bezier.Point1.Y,
                        originX + bezier.Point2.X,
                        baseline + bezier.Point2.Y,
                        originX + bezier.Point3.X,
                        baseline + bezier.Point3.Y);
                }
            }

            public void EndFigure(D2D1FigureEnd figureEnd)
            {
                if (figureEnd == D2D1FigureEnd.Closed)
                {
                    path?.Close();
                }
            }

            public int Close()
            {
                return 0;
            }

            public SKPath DetachPath()
            {
                SKPath result = path;
                path = null;
                return result;
            }

            public void Dispose()
            {
                path?.Dispose();
                path = null;
            }
        }
    }
}
