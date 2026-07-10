using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DC_Font_Generator;
using INI_RW;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUIColor = Windows.UI.Color;

namespace DC_Font_Generator.WinUI.ViewModels;

internal sealed class MainWindowViewModel : ObservableObject
{
    private const string ToolTipFormat =
        "[{0}] Hex:[{1}]\n" +
        "宽度 (fWidth): {2}\n" +
        "高度 (fHeight): {3}\n" +
        "基线 (fBaseLine): {4}\n" +
        "基线偏移 (fBaseLineFixed): {5}\n" +
        "顶部边距 (fTopEdge): {6}\n" +
        "前导边距 (fLeadingEdge): {7}\n" +
        "字距 (fSpacing): {8}\n" +
        "图像宽度: {9}\n" +
        "图像高度: {10}\n" +
        "Font{11}";

    private readonly DispatcherQueue dispatcherQueue;
    private readonly List<int> texSizes = new List<int> { 128, 256, 512, 1024, 2048, 4096, 8192 };
    private int selectedFontIndex;
    private int encodingIndex;
    private int sizeXIndex;
    private int sizeYIndex;
    private int gap;
    private int arrangeMethod = 1;
    private WinUIColor backgroundColor = WinUIColor.FromArgb(0, 0, 0, 0);
    private Bgra32Image textPixels;
    private Bitmap textImage;
    private WriteableBitmap atlasBitmap;
    private string statusText = "请选择编码。";
    private string logText = "";
    private bool isBusy;
    private bool hasAtlas;
    private bool doubleByteFontEnabled;
    private WinUiFontProgress progress;
    private string sampleText = "Here is example !测试测试";
    private WriteableBitmap samplePreviewBitmap;
    private FontPerformanceStats performanceStats;
    private string projectTexName = "Font";
    private IniFile currentIni;
    private string currentIniPath = "";
    private string currentFontPath = "";
    private bool suppressGeneratedOutputInvalidation;

    public MainWindowViewModel(DispatcherQueue dispatcherQueue)
    {
        this.dispatcherQueue = dispatcherQueue;
        System.Text.Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding = new FontEncoding(System.Text.Encoding.Default, true);
        EncodingSelectionService.Select(Encoding, 0);
        doubleByteFontEnabled = false;

        Main section = FontSectionService.CreateSection(FontSections, 0, OnTextOverflow);
        FontSections.Add(section);
        CharIndex = new Array2D.List2D<Fnt_char>();
        Selection = new GlyphSelectionState();

        for (int i = 0; i < 8; i++)
        {
            IniSlots.Add(new IniSlotViewModel(i));
        }

        RefreshSectionState();
    }

    public List<Main> FontSections { get; } = new List<Main>();
    public FontEncoding Encoding { get; private set; }
    public Array2D.List2D<Fnt_char> CharIndex { get; private set; }
    internal GlyphSelectionState Selection { get; }
    public IReadOnlyList<int> TexSizes => texSizes;
    public ObservableCollection<IniSlotViewModel> IniSlots { get; } = new ObservableCollection<IniSlotViewModel>();

    public string[] EncodingItems { get; } =
    {
        "ANSI",
        "932 日文",
        "936 简体中文",
        "949 韩文",
        "950 繁体中文",
        "936 GBK",
        "1252 Windows"
    };

    public string[] ArrangeItems { get; } =
    {
        "按高度",
        "按宽度",
        "按编码"
    };

    public string[] AdjustmentItems { get; } =
    {
        "Leading Edge (fLeadingEdge)",
        "Spacing (fSpacing)",
        "Base Line (fBaseLine)",
        "Top Edge (fTopEdge)",
        "Scale"
    };

    public Main CurrentSection => FontSections.Count == 0 ? null : FontSections[SelectedFontIndex];

    public int SelectedFontIndex
    {
        get => selectedFontIndex;
        set
        {
            int clamped = FontSectionStateService.ClampSelectedIndex(FontSections, value);
            if (SetProperty(ref selectedFontIndex, clamped))
            {
                RefreshSectionState();
            }
        }
    }

    public int EncodingIndex
    {
        get => encodingIndex;
        set
        {
            if (SetProperty(ref encodingIndex, value))
            {
                EncodingSelectionResult result = EncodingSelectionService.Select(Encoding, value);
                DoubleByteFontEnabled = result.DoubleByteFontEnabled;
                StatusText = result.HasSelection
                    ? $"Characters count = {result.CharactersCount}"
                    : "请选择编码。";
                RefreshSectionState();
                InvalidateGeneratedOutput();
            }
        }
    }

    public int SizeXIndex
    {
        get => sizeXIndex;
        set
        {
            if (SetProperty(ref sizeXIndex, ClampIndex(value, texSizes.Count)))
            {
                OnPropertyChanged(nameof(TexSizeText));
                InvalidateGeneratedOutput();
            }
        }
    }

    public int SizeYIndex
    {
        get => sizeYIndex;
        set
        {
            if (SetProperty(ref sizeYIndex, ClampIndex(value, texSizes.Count)))
            {
                OnPropertyChanged(nameof(TexSizeText));
                InvalidateGeneratedOutput();
            }
        }
    }

    public int Gap
    {
        get => gap;
        set
        {
            if (SetProperty(ref gap, Math.Max(0, value)))
            {
                InvalidateGeneratedOutput();
            }
        }
    }

    public int ArrangeMethod
    {
        get => arrangeMethod;
        set
        {
            if (SetProperty(ref arrangeMethod, value))
            {
                InvalidateGeneratedOutput();
            }
        }
    }

    public WinUIColor BackgroundColor
    {
        get => backgroundColor;
        set
        {
            if (SetProperty(ref backgroundColor, value))
            {
                InvalidateGeneratedOutput();
            }
        }
    }

    public WriteableBitmap AtlasBitmap
    {
        get => atlasBitmap;
        private set => SetProperty(ref atlasBitmap, value);
    }

    public WriteableBitmap SamplePreviewBitmap
    {
        get => samplePreviewBitmap;
        private set => SetProperty(ref samplePreviewBitmap, value);
    }

    public string StatusText
    {
        get => statusText;
        set => SetProperty(ref statusText, value);
    }

    public string LogText
    {
        get => logText;
        set => SetProperty(ref logText, value);
    }

    public bool IsBusy
    {
        get => isBusy;
        private set => SetProperty(ref isBusy, value);
    }

    public bool HasAtlas
    {
        get => hasAtlas;
        private set => SetProperty(ref hasAtlas, value);
    }

    public bool DoubleByteFontEnabled
    {
        get => doubleByteFontEnabled;
        private set => SetProperty(ref doubleByteFontEnabled, value);
    }

    public WinUiFontProgress Progress
    {
        get => progress;
        private set => SetProperty(ref progress, value);
    }

    public string SampleText
    {
        get => sampleText;
        set
        {
            if (SetProperty(ref sampleText, value))
            {
                RenderSamplePreview(420, 160);
            }
        }
    }

    public string TexSizeText
    {
        get
        {
            int width = texSizes[SizeXIndex];
            int height = texSizes[SizeYIndex];
            decimal bytes = width * height * 4m;
            string suffix = " B";
            if (bytes > 1024m * 1024m)
            {
                bytes /= 1024m * 1024m;
                suffix = "MB";
            }
            else if (bytes > 1024m)
            {
                bytes /= 1024m;
                suffix = "kB";
            }

            return $"{width} x {height} ({bytes:N0}{suffix})";
        }
    }

    public Bgra32Image TextPixels => textPixels;
    public Bitmap TextImage => textImage;
    public Size TextImageSize { get; private set; } = new Size(128, 128);
    public string ProjectTexName
    {
        get => projectTexName;
        set => SetProperty(ref projectTexName, string.IsNullOrWhiteSpace(value) ? "Font" : value);
    }

    public async Task RenderAsync()
    {
        if (IsBusy) return;
        InvalidateGeneratedOutput();
        StatusText = "Manufacturing fonts...";
        IsBusy = true;
        Selection.ApplyRemoved(Encoding);
        try
        {
            WinUiRenderRequest request = new WinUiRenderRequest
            {
                FontSections = FontSections,
                Encoding = Encoding,
                GlyphSelection = Selection,
                AtlasRequest = CreateAtlasRequest(),
                Progress = CreateProgressAdapter()
            };

            WinUiRenderResult result = await WinUiRenderAdapter.RenderAsync(request);
            if (!result.Success || result.AtlasResult == null || !result.AtlasResult.Success)
            {
                string message = result.AtlasResult != null && !string.IsNullOrWhiteSpace(result.AtlasResult.FailureMessage)
                    ? result.AtlasResult.FailureMessage
                    : "Font file size exceeds the limit! Can not be processed.";
                StatusText = message;
                AppendLog(message);
                return;
            }

            BindAtlasResult(result.AtlasResult);
            performanceStats = result.PerformanceStats;
            AppendPerformanceLog(performanceStats);
            StatusText = "Render done.";
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
            AppendLog(ex.ToString());
        }
        finally
        {
            IsBusy = false;
            Progress = null;
        }
    }

    public void BindAtlasResult(FontAtlasResult result)
    {
        DisposeTextImage();
        textPixels = result.TextPixels;
        textImage = result.TextImage ?? result.TextPixels?.ToBitmap();
        TextImageSize = result.TextImageSize;
        CharIndex = result.CharIndex;
        suppressGeneratedOutputInvalidation = true;
        try
        {
            SizeXIndex = result.SizeXIndex >= 0 ? result.SizeXIndex : SizeXIndex;
            SizeYIndex = result.SizeYIndex >= 0 ? result.SizeYIndex : SizeYIndex;
        }
        finally
        {
            suppressGeneratedOutputInvalidation = false;
        }
        AtlasBitmap = textPixels != null ? WinUiImageAdapter.ToAtlasPreviewWriteableBitmap(textPixels) : null;
        HasAtlas = AtlasBitmap != null;
        RenderSamplePreview(420, 160);
    }

    public async Task SaveFontAsync(string texPath, IList<string> fntPaths)
    {
        if (textPixels == null)
        {
            StatusText = "No atlas to save.";
            return;
        }

        IsBusy = true;
        try
        {
            FontSaveResult result = await Task.Run(() => FontSaveWorkflowService.Save(new FontSaveRequest
            {
                FontSections = FontSections,
                TextPixels = textPixels,
                TexPath = texPath,
                TexName = FontSaveWorkflowService.GetTexName(texPath),
                FntPaths = fntPaths,
                Encoding = Encoding.enc,
                Progress = CreateProgressAdapter(),
                PerformanceStats = performanceStats
            }));

            AppendPerformanceLog(result.PerformanceStats);
            ProjectTexName = FontSaveWorkflowService.GetTexName(texPath);
            RefreshSectionState();
            StatusText = "Save complete.";
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
            AppendLog(ex.ToString());
        }
        finally
        {
            IsBusy = false;
            Progress = null;
        }
    }

    public async Task SaveProjectAsync(string path, string texName)
    {
        try
        {
            await Task.Run(() => WinUiProjectAdapter.SaveProject(path, new WinUiProjectSaveOptions
            {
                EncodingIndex = EncodingIndex,
                SizeXIndex = SizeXIndex,
                SizeYIndex = SizeYIndex,
                TexFileName = texName,
                Gap = Gap,
                BackgroundColor = BackgroundColor,
                ArrangeMethod = ArrangeMethod,
                FontSections = FontSections
            }));
            StatusText = "Project has been saved.";
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
            AppendLog(ex.ToString());
        }
    }

    public async Task LoadProjectAsync(string path)
    {
        IsBusy = true;
        try
        {
            ProjectDocument document = await Task.Run(() => WinUiProjectAdapter.LoadProject(path));
            Clear();
            suppressGeneratedOutputInvalidation = true;
            ProjectTexName = document.TexFileName;
            EncodingIndex = document.EncodingIndex;
            Gap = (int)document.Gap;
            BackgroundColor = WinUiColorAdapter.ToWinUiColor(ProjectRequestFactory.GetBackgroundColor(document.BackGroundColorArgb));
            ArrangeMethod = document.ArrangeMethod;
            if (document.SizeXIndex >= 0) SizeXIndex = document.SizeXIndex;
            if (document.SizeYIndex >= 0) SizeYIndex = document.SizeYIndex;
            suppressGeneratedOutputInvalidation = false;

            ProjectOpenWorkflowResult result = await Task.Run(() => ProjectOpenWorkflowService.Open(new ProjectOpenWorkflowRequest
            {
                Document = document,
                FontSections = FontSections,
                FontPath = Path.GetDirectoryName(path) ?? AppContext.BaseDirectory,
                Encoding = Encoding,
                CharIndex = CharIndex,
                CreateMain = CreateSection,
                AtlasRequest = CreateAtlasRequest(),
                Progress = CreateProgressAdapter(),
                Localize = value => value
            }));

            foreach (string log in result.Logs)
            {
                AppendLog(log);
            }

            SelectedFontIndex = FontSectionStateService.ClampSelectedIndex(FontSections, result.SelectedMainIndex);
            if (!result.Success)
            {
                StatusText = result.Status == ProjectOpenWorkflowStatus.AtlasOverflow
                    ? "Font file size exceeds the limit! Can not be processed."
                    : "Project error : Please refer to the log";
                return;
            }

            BindAtlasResult(result.AtlasResult);
            RefreshSectionState();
            StatusText = "Project has been opened. Please remember to save font.";
        }
        catch (Exception ex)
        {
            suppressGeneratedOutputInvalidation = false;
            StatusText = ex.Message;
            AppendLog(ex.ToString());
        }
        finally
        {
            IsBusy = false;
            Progress = null;
        }
    }

    public async Task ImportFntAsync(string path)
    {
        IsBusy = true;
        try
        {
            ImportedFontResult result = await Task.Run(() => FontImportWorkflowService.Import(new ImportedFontRequest
            {
                Path = path,
                FontName = FontImportWorkflowService.GetImportName(path),
                FontSections = FontSections,
                SelectedFontIndex = SelectedFontIndex,
                Encoding = Encoding,
                CharIndex = CharIndex,
                Progress = CreateProgressAdapter()
            }));

            if (!result.Success)
            {
                StatusText = "file size error.";
                return;
            }

            DisposeTextImage();
            textPixels = result.TexturePixels;
            textImage = result.Texture;
            TextImageSize = textPixels != null
                ? new Size(textPixels.Width, textPixels.Height)
                : textImage?.Size ?? Size.Empty;
            AtlasBitmap = textPixels != null
                ? WinUiImageAdapter.ToAtlasPreviewWriteableBitmap(textPixels)
                : WinUiImageAdapter.ToAtlasPreviewWriteableBitmap(Bgra32Image.FromBitmap(textImage));
            HasAtlas = AtlasBitmap != null;
            RefreshSectionState();
            StatusText = "Open fnt and tex done.";
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
            AppendLog(ex.ToString());
        }
        finally
        {
            IsBusy = false;
            Progress = null;
        }
    }

    public async Task ImportTextureAsync(string path, TextureWorkflowFormat format)
    {
        IsBusy = true;
        try
        {
            TextureImportResult result = await Task.Run(() => TextureWorkflowService.Import(path, format));
            DisposeTextImage();
            textPixels = result.ImagePixels;
            textImage = result.Image;
            TextImageSize = result.ImageSize;
            AtlasBitmap = WinUiImageAdapter.ToAtlasPreviewWriteableBitmap(textPixels);
            HasAtlas = true;
            StatusText = format == TextureWorkflowFormat.Tex ? "Import Tex done." : "Import PNG done.";
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
            AppendLog(ex.ToString());
        }
        finally
        {
            IsBusy = false;
            Progress = null;
        }
    }

    public async Task ConvertTexToPngAsync(string texPath, string pngPath)
    {
        await RunBackgroundAsync(() => TextureWorkflowService.ConvertTexToPng(texPath, pngPath), "Convert Tex to PNG : done.");
    }

    public async Task ConvertPngToTexAsync(string pngPath, string texPath)
    {
        await RunBackgroundAsync(() => TextureWorkflowService.ConvertPngToTex(pngPath, texPath, CreateProgressAdapter()), "Convert PNG to Tex : done.");
    }

    public void LoadIniState(string iniPath, string fontPath)
    {
        currentIniPath = iniPath ?? "";
        currentFontPath = string.IsNullOrWhiteSpace(fontPath) ? AppContext.BaseDirectory : fontPath;
        currentIni = File.Exists(currentIniPath) ? new IniFile(currentIniPath) : null;
        FontSelectorLoadResult result = FontIniWorkflowService.LoadSelectorState(currentFontPath, currentIni, Encoding.enc);
        for (int i = 0; i < IniSlots.Count; i++)
        {
            IniSlots[i].Items.Clear();
            IniSlots[i].Items.AddRange(result.SlotItems[i]);
            IniSlots[i].SelectedIndex = result.SelectedIndices[i];
        }

        foreach (string error in result.Errors)
        {
            AppendLog(error);
        }

        StatusText = "INI loaded.";
    }

    public void CopyIniSlotsFrom(string sourceIniPath)
    {
        if (string.IsNullOrWhiteSpace(sourceIniPath) || !File.Exists(sourceIniPath))
        {
            return;
        }

        if (currentIni == null)
        {
            LoadIniState(sourceIniPath, currentFontPath);
            return;
        }

        FontIniWorkflowService.CopySlots(sourceIniPath, currentIni);
        LoadIniState(currentIniPath, currentFontPath);
        StatusText = "INI loaded.";
    }

    public void SetDefaultIniSelections()
    {
        int[] selections = FontIniWorkflowService.GetDefaultSelections(IniSlots.Count);
        for (int i = 0; i < IniSlots.Count && i < selections.Length; i++)
        {
            IniSlots[i].SelectedIndex = FontIniWorkflowService.ClampSelectedIndex(selections[i], IniSlots[i].Items.Count);
            WriteIniSlot(i);
        }

        StatusText = "Default INI fonts selected.";
    }

    public void WriteIniSlot(int slotIndex)
    {
        if (currentIni == null || slotIndex < 0 || slotIndex >= IniSlots.Count)
        {
            return;
        }

        FontIniWorkflowService.WriteSlot(currentIni, slotIndex, IniSlots[slotIndex].SelectedFont);
    }

    public void SaveIniSlots(string path)
    {
        List<FontFile> selected = new List<FontFile>();
        foreach (IniSlotViewModel slot in IniSlots)
        {
            selected.Add(slot.SelectedFont);
        }

        FontIniWorkflowService.SaveSlots(path, selected);
        StatusText = "INI saved.";
    }

    public void ExportCodepageDebug()
    {
        Encoding.WriteToFile();
        StatusText = "Output CodepageDebug.txt done.";
    }

    public void SetSectionFont(bool doubleByte, FontDescriptor font, FontStyleDescriptor style)
    {
        FontSectionStateService.ApplySelectedFont(FontSections, SelectedFontIndex, doubleByte, font, style);
        RefreshSectionState();
        InvalidateGeneratedOutput();
    }

    public void SetEffectValue(string tag, float value)
    {
        FontSectionStateService.ApplyNumericChange(FontSections, SelectedFontIndex, tag, value, true);
        RefreshSectionState();
        InvalidateGeneratedOutput();
    }

    public void SetEffectColor(string tag, WinUIColor color)
    {
        FontSectionStateService.ApplyEffectColor(FontSections, SelectedFontIndex, tag, WinUiColorAdapter.ToDrawingColor(color));
        RefreshSectionState();
        InvalidateGeneratedOutput();
    }

    public void SetFixedFont(bool enabled, float width)
    {
        FontSectionStateService.ApplyFixedFont(FontSections, SelectedFontIndex, enabled, width);
        RefreshSectionState();
        InvalidateGeneratedOutput();
    }

    public void SetFixedFontWidth(bool enabled, float width)
    {
        FontSectionStateService.ApplyFixedFontWidth(FontSections, SelectedFontIndex, enabled, width);
        RefreshSectionState();
        InvalidateGeneratedOutput();
    }

    public void SetProportionalDoubleByteSpacing(bool enabled)
    {
        FontSectionStateService.ApplyProportionalDoubleByteSpacing(FontSections, SelectedFontIndex, enabled);
        RefreshSectionState();
        InvalidateGeneratedOutput();
    }

    public void SetManualBaseLine(bool auto, float value)
    {
        FontSectionStateService.ApplyManualBaseLine(FontSections, SelectedFontIndex, !auto, value, true);
        RefreshSectionState();
        InvalidateGeneratedOutput();
    }

    public void SetFntName(string name)
    {
        FontSectionStateService.SetName(FontSections, SelectedFontIndex, name ?? "");
        RefreshSectionState();
    }

    public void SetIniLink(int zeroBasedSlot, bool value)
    {
        FontSectionStateService.SetIniLink(FontSections, SelectedFontIndex, zeroBasedSlot, value);
        RefreshSectionState();
    }

    public void AddSection()
    {
        FontSectionService.AddSection(FontSections, CreateSection);
        SelectedFontIndex = FontSections.Count - 1;
        RefreshSectionState();
        InvalidateGeneratedOutput();
    }

    public void RemoveSection()
    {
        FontSectionRemoveResult result = FontSectionService.RemoveSection(FontSections, SelectedFontIndex, CharIndex);
        SelectedFontIndex = result.SelectedIndex;
        RefreshSectionState();
        InvalidateGeneratedOutput();
    }

    public void MoveSection(string command)
    {
        FontSectionControlResult result = FontSectionService.ApplyControlCommand(FontSections, SelectedFontIndex, command, CharIndex, CreateSection);
        SelectedFontIndex = result.SelectedIndex;
        RefreshSectionState();
        InvalidateGeneratedOutput();
    }

    public void SelectRange(string startHex, string endHex, bool sbcs, bool dbcs)
    {
        GlyphSelectionWorkflowService.SelectRange(new GlyphRangeSelectionRequest
        {
            FontSections = FontSections,
            SelectedFontIndex = SelectedFontIndex,
            Selection = Selection,
            StartHex = startHex ?? "",
            EndHex = endHex ?? "",
            IncludeSingleByte = sbcs,
            IncludeDoubleByte = dbcs
        });
    }

    public void SetSingleByteSelection(bool selected)
    {
        GlyphSelectionWorkflowService.SetSingleByteSelection(new GlyphSetSelectionRequest
        {
            FontSections = FontSections,
            SelectedFontIndex = SelectedFontIndex,
            Selection = Selection,
            Selected = selected
        });
    }

    public void SetDoubleByteSelection(bool selected)
    {
        GlyphSelectionWorkflowService.SetDoubleByteSelection(new GlyphSetSelectionRequest
        {
            FontSections = FontSections,
            SelectedFontIndex = SelectedFontIndex,
            Selection = Selection,
            Selected = selected
        });
    }

    public GlyphAdjustmentWorkflowResult AdjustGlyphs(int adjustmentIndex, string command, float increment)
    {
        GlyphAdjustmentWorkflowResult result = GlyphSelectionWorkflowService.ApplyAdjustment(new GlyphAdjustmentWorkflowRequest
        {
            FontSections = FontSections,
            SelectedFontIndex = SelectedFontIndex,
            Selection = Selection,
            FixedFont = CurrentSection?.fixedFont ?? false,
            LeftSpacing = adjustmentIndex == 0,
            RightSpacing = adjustmentIndex == 1,
            LineSpacing = adjustmentIndex == 2,
            TopEdge = adjustmentIndex == 3,
            Scale = adjustmentIndex == 4,
            Command = command,
            Increment = increment
        });

        RenderSamplePreview(420, 160);
        return result;
    }

    public void RestoreAdjustment()
    {
        GlyphSelectionWorkflowService.RestoreAdjustment(FontSections, SelectedFontIndex, Selection);
        RenderSamplePreview(420, 160);
    }

    public GlyphInteractionResult HandleGlyphPointer(int x, int y, bool toggle, bool remove, bool createMask)
    {
        if (!HasAtlas || textImage == null || CharIndex == null)
        {
            return new GlyphInteractionResult();
        }

        GlyphInteractionResult result = GlyphInteractionService.Handle(new GlyphInteractionRequest
        {
            TextImage = textImage,
            TextImageSize = TextImageSize,
            CharIndex = CharIndex,
            FontSections = FontSections,
            SelectedFontIndex = SelectedFontIndex,
            Selection = Selection,
            X = x,
            Y = y,
            ToggleSelection = toggle,
            Remove = remove,
            CreateMask = createMask,
            HitTolerance = toggle ? 8 : 3,
            ToolTipFormat = ToolTipFormat
        });

        if (result.MaskImage != null)
        {
            AtlasBitmap = WinUiImageAdapter.ToAtlasPreviewWriteableBitmap(Bgra32Image.FromBitmap(result.MaskImage));
            result.MaskImage.Dispose();
        }

        if (!string.IsNullOrEmpty(result.StatusText))
        {
            StatusText = result.StatusText;
        }

        return result;
    }

    public void RestoreAtlasDisplay()
    {
        AtlasBitmap = textPixels != null ? WinUiImageAdapter.ToAtlasPreviewWriteableBitmap(textPixels) : null;
    }

    public void RefreshSelectionOverlay()
    {
        if (HasAtlas)
        {
            HandleGlyphPointer(-1, -1, false, false, true);
        }
    }

    public FontAtlasRequest CreateAtlasRequest()
    {
        return new FontAtlasRequest
        {
            FontSections = FontSections,
            Encoding = Encoding,
            CandidateWidths = texSizes,
            CandidateHeights = texSizes,
            CurrentWidthIndex = SizeXIndex,
            CurrentHeightIndex = SizeYIndex,
            Gap = Gap,
            ArrangeMode = TextureSizeSelectionService.ToFontArrangeMode(ArrangeMethod),
            BackgroundColor = WinUiColorAdapter.ToDrawingColor(BackgroundColor),
            PerformanceStats = performanceStats
        };
    }

    public FontSectionViewState GetSectionState()
    {
        return FontSectionStateService.CreateViewState(FontSections, SelectedFontIndex);
    }

    public void RefreshSectionState()
    {
        OnPropertyChanged(nameof(CurrentSection));
        OnPropertyChanged(nameof(SelectedFontIndex));
        OnPropertyChanged(nameof(FontSections));
    }

    public void Clear()
    {
        Selection.Clear();
        DisposeTextImage();
        CharIndex.Clear();
        FontSectionService.ResetSections(FontSections, CharIndex, CreateSection);
        SelectedFontIndex = 0;
        AtlasBitmap = null;
        SamplePreviewBitmap = null;
        HasAtlas = false;
        RefreshSectionState();
    }

    public void AppendLog(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        LogText += text + Environment.NewLine;
    }

    private Main CreateSection(int id)
    {
        return FontSectionService.CreateSection(FontSections, id, OnTextOverflow);
    }

    private WinUiProgressAdapter CreateProgressAdapter()
    {
        return new WinUiProgressAdapter(dispatcherQueue, p => Progress = p);
    }

    private void RenderSamplePreview(int width, int height)
    {
        if (width <= 0 || height <= 0 || CurrentSection == null)
        {
            return;
        }

        using Bitmap bitmap = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

        GlyphSelectionWorkflowService.RenderPreview(bitmap, SampleText, FontSections, SelectedFontIndex);
        SamplePreviewBitmap = WinUiImageAdapter.ToAtlasPreviewWriteableBitmap(Bgra32Image.FromBitmap(bitmap));
    }

    private async Task RunBackgroundAsync(Action action, string successStatus)
    {
        IsBusy = true;
        try
        {
            await Task.Run(action);
            StatusText = successStatus;
        }
        catch (Exception ex)
        {
            StatusText = ex.Message;
            AppendLog(ex.ToString());
        }
        finally
        {
            IsBusy = false;
            Progress = null;
        }
    }

    private void AppendPerformanceLog(FontPerformanceStats stats)
    {
        if (stats == null)
        {
            return;
        }

        string text = stats.ToLogLine();
        if (!string.IsNullOrWhiteSpace(text))
        {
            AppendLog(text);
        }
    }

    public void InvalidateGeneratedOutput()
    {
        if (suppressGeneratedOutputInvalidation)
        {
            return;
        }

        if (!HasAtlas && AtlasBitmap == null && textImage == null && textPixels == null)
        {
            return;
        }

        DisposeTextImage();
        CharIndex.Clear();
        TextImageSize = new Size(texSizes[SizeXIndex], texSizes[SizeYIndex]);
        AtlasBitmap = null;
        SamplePreviewBitmap = null;
        HasAtlas = false;
        StatusText = "设置已更改，请重新 Render。";
    }

    private void DisposeTextImage()
    {
        if (textImage != null)
        {
            textImage.Dispose();
            textImage = null;
        }

        textPixels = null;
    }

    private void OnTextOverflow(object sender, EventArgs e)
    {
        if (FontSectionStateService.IsTextOverflowSender(sender))
        {
            StatusText = "Image Size error.";
        }
    }

    private static int ClampIndex(int value, int count)
    {
        if (count <= 0) return 0;
        if (value < 0) return 0;
        if (value >= count) return count - 1;
        return value;
    }
}
