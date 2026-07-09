using System;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace DC_Font_Generator
{
    internal static class DirectWriteGlyphPathService
    {
        private static readonly Guid IDWriteFactoryGuid = new Guid("b859ee5a-d838-4b5b-a2e8-1adc7d93db48");
        private static readonly Lazy<IntPtr> SharedFactory = new Lazy<IntPtr>(CreateFactory);

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

            IntPtr collection = IntPtr.Zero;
            IntPtr family = IntPtr.Zero;
            IntPtr matchingFont = IntPtr.Zero;
            IntPtr fontFace = IntPtr.Zero;

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

                int hr = Call<GetSystemFontCollectionDelegate>(SharedFactory.Value, 3)(SharedFactory.Value, out collection, false);
                if (hr < 0 || collection == IntPtr.Zero)
                {
                    FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: GetSystemFontCollection hr=0x{hr:X8}");
                    return false;
                }

                hr = Call<FindFamilyNameDelegate>(collection, 5)(collection, familyName, out uint familyIndex, out bool exists);
                if (hr < 0 || !exists)
                {
                    FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: FindFamilyName family={familyName}, exists={exists}, hr=0x{hr:X8}");
                    return false;
                }

                hr = Call<GetFontFamilyDelegate>(collection, 4)(collection, familyIndex, out family);
                if (hr < 0 || family == IntPtr.Zero)
                {
                    FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: GetFontFamily index={familyIndex}, hr=0x{hr:X8}");
                    return false;
                }

                bool exactFont = TryGetExactFont(family, weight, stretch, style, out matchingFont, out uint matchedIndex);
                if (!exactFont)
                {
                    hr = Call<GetFirstMatchingFontDelegate>(family, 7)(family, weight, stretch, style, out matchingFont);
                    if (hr < 0 || matchingFont == IntPtr.Zero)
                    {
                        FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: GetFirstMatchingFont hr=0x{hr:X8}");
                        return false;
                    }

                    matchedIndex = uint.MaxValue;
                }

                DWriteFontWeight resolvedWeight = Call<GetFontWeightDelegate>(matchingFont, 4)(matchingFont);
                DWriteFontStretch resolvedStretch = Call<GetFontStretchDelegate>(matchingFont, 5)(matchingFont);
                DWriteFontStyle resolvedStyle = Call<GetFontStyleDelegate>(matchingFont, 6)(matchingFont);
                DWriteFontSimulations simulations = Call<GetFontSimulationsDelegate>(matchingFont, 10)(matchingFont);
                FontRenderDebugLog.Add($"[font-debug] DW font U+{(int)c:X4}: exact={exactFont}, familyIndex={familyIndex}, fontIndex={(matchedIndex == uint.MaxValue ? "fallback" : matchedIndex.ToString())}, resolved w={(int)resolvedWeight}, stretch={(int)resolvedStretch}, style={resolvedStyle}, simulations={simulations}");

                hr = Call<CreateFontFaceDelegate>(matchingFont, 13)(matchingFont, out fontFace);
                if (hr < 0 || fontFace == IntPtr.Zero)
                {
                    FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: CreateFontFace hr=0x{hr:X8}");
                    return false;
                }

                uint[] codePoints = { c };
                ushort[] glyphIndices = new ushort[1];
                hr = Call<GetGlyphIndicesDelegate>(fontFace, 11)(fontFace, codePoints, 1, glyphIndices);
                DWriteFontFaceType faceType = Call<GetFontFaceTypeDelegate>(fontFace, 3)(fontFace);
                uint faceIndex = Call<GetFontFaceIndexDelegate>(fontFace, 5)(fontFace);
                if (hr < 0 || glyphIndices[0] == 0)
                {
                    FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: GetGlyphIndices hr=0x{hr:X8}, glyph={glyphIndices[0]}, faceType={faceType}, faceIndex={faceIndex}");
                    return false;
                }

                using (DirectWriteSkiaGeometrySink sink = new DirectWriteSkiaGeometrySink(originX, baseline))
                {
                    hr = Call<GetGlyphRunOutlineDelegate>(fontFace, 14)(
                        fontFace,
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
                        FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: GetGlyphRunOutline hr=0x{hr:X8}, glyph={glyphIndices[0]}, faceType={faceType}, faceIndex={faceIndex}");
                        return false;
                    }

                    path = sink.DetachPath();
                    if (path == null || path.IsEmpty)
                    {
                        FontRenderDebugLog.Add($"[font-debug] DW miss U+{(int)c:X4}: empty path, glyph={glyphIndices[0]}, faceType={faceType}, faceIndex={faceIndex}");
                        return false;
                    }

                    SKRect bounds = path.Bounds;
                    FontRenderDebugLog.Add($"[font-debug] DW hit U+{(int)c:X4}: glyph={glyphIndices[0]}, faceType={faceType}, faceIndex={faceIndex}, bounds={bounds.Width:0.##}x{bounds.Height:0.##}");
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
                ReleaseComPointer(fontFace);
                ReleaseComPointer(matchingFont);
                ReleaseComPointer(family);
                ReleaseComPointer(collection);
            }
        }

        private static bool TryGetExactFont(
            IntPtr family,
            DWriteFontWeight weight,
            DWriteFontStretch stretch,
            DWriteFontStyle style,
            out IntPtr matchingFont,
            out uint matchedIndex)
        {
            matchingFont = IntPtr.Zero;
            matchedIndex = uint.MaxValue;
            if (family == IntPtr.Zero)
            {
                return false;
            }

            uint count = Call<GetFontCountDelegate>(family, 4)(family);
            for (uint i = 0; i < count; i++)
            {
                IntPtr candidate = IntPtr.Zero;
                int hr = Call<GetFontDelegate>(family, 5)(family, i, out candidate);
                if (hr < 0 || candidate == IntPtr.Zero)
                {
                    FontRenderDebugLog.Add($"[font-debug] DW exact scan skip: index={i}, hr=0x{hr:X8}");
                    continue;
                }

                DWriteFontWeight candidateWeight = Call<GetFontWeightDelegate>(candidate, 4)(candidate);
                DWriteFontStretch candidateStretch = Call<GetFontStretchDelegate>(candidate, 5)(candidate);
                DWriteFontStyle candidateStyle = Call<GetFontStyleDelegate>(candidate, 6)(candidate);
                DWriteFontSimulations simulations = Call<GetFontSimulationsDelegate>(candidate, 10)(candidate);
                FontRenderDebugLog.Add($"[font-debug] DW exact scan: index={i}, w={(int)candidateWeight}, stretch={(int)candidateStretch}, style={candidateStyle}, simulations={simulations}");

                if (candidateWeight == weight
                    && candidateStretch == stretch
                    && candidateStyle == style)
                {
                    matchingFont = candidate;
                    matchedIndex = i;
                    return true;
                }

                ReleaseComPointer(candidate);
            }

            return false;
        }

        private static IntPtr CreateFactory()
        {
            Guid iid = IDWriteFactoryGuid;
            int hr = DWriteCreateFactory(DWriteFactoryType.Shared, ref iid, out IntPtr factory);
            if (hr < 0 || factory == IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            FontRenderDebugLog.Add($"[font-debug] DW factory created: ptr=0x{factory.ToInt64():X}");
            return factory;
        }

        private static T Call<T>(IntPtr comObject, int slot) where T : Delegate
        {
            if (comObject == IntPtr.Zero)
            {
                throw new InvalidOperationException("COM object pointer is null.");
            }

            IntPtr vtable = Marshal.ReadIntPtr(comObject);
            IntPtr function = Marshal.ReadIntPtr(vtable, slot * IntPtr.Size);
            return Marshal.GetDelegateForFunctionPointer<T>(function);
        }

        private static void ReleaseComPointer(IntPtr comObject)
        {
            if (comObject == IntPtr.Zero)
            {
                return;
            }

            try
            {
                Call<ReleaseDelegate>(comObject, 2)(comObject);
            }
            catch
            {
            }
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

        [DllImport("dwrite.dll", ExactSpelling = true, PreserveSig = true)]
        private static extern int DWriteCreateFactory(
            DWriteFactoryType factoryType,
            ref Guid iid,
            out IntPtr factory);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint ReleaseDelegate(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetSystemFontCollectionDelegate(
            IntPtr self,
            out IntPtr fontCollection,
            [MarshalAs(UnmanagedType.Bool)] bool checkForUpdates);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint GetFontFamilyCountDelegate(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetFontFamilyDelegate(IntPtr self, uint index, out IntPtr fontFamily);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int FindFamilyNameDelegate(
            IntPtr self,
            [MarshalAs(UnmanagedType.LPWStr)] string familyName,
            out uint index,
            [MarshalAs(UnmanagedType.Bool)] out bool exists);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint GetFontCountDelegate(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetFontDelegate(IntPtr self, uint index, out IntPtr font);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetFirstMatchingFontDelegate(
            IntPtr self,
            DWriteFontWeight weight,
            DWriteFontStretch stretch,
            DWriteFontStyle style,
            out IntPtr matchingFont);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate DWriteFontWeight GetFontWeightDelegate(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate DWriteFontStretch GetFontStretchDelegate(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate DWriteFontStyle GetFontStyleDelegate(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate DWriteFontSimulations GetFontSimulationsDelegate(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateFontFaceDelegate(IntPtr self, out IntPtr fontFace);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate DWriteFontFaceType GetFontFaceTypeDelegate(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint GetFontFaceIndexDelegate(IntPtr self);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetGlyphIndicesDelegate(
            IntPtr self,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] uint[] codePoints,
            uint codePointCount,
            [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] ushort[] glyphIndices);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GetGlyphRunOutlineDelegate(
            IntPtr self,
            float emSize,
            [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5)] ushort[] glyphIndices,
            IntPtr glyphAdvances,
            IntPtr glyphOffsets,
            uint glyphCount,
            [MarshalAs(UnmanagedType.Bool)] bool isSideways,
            [MarshalAs(UnmanagedType.Bool)] bool isRightToLeft,
            [MarshalAs(UnmanagedType.Interface)] ID2D1SimplifiedGeometrySink geometrySink);

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
