using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DC_Font_Generator;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DC_Font_Generator.WinUI;

public enum WinUiRenderStatus
{
    Success,
    AtlasOverflow
}

public sealed class WinUiRenderRequest
{
    public IList<Main> FontSections { get; set; } = Array.Empty<Main>();
    public FontEncoding Encoding { get; set; }
    internal GlyphSelectionState GlyphSelection { get; set; }
    public FontAtlasRequest AtlasRequest { get; set; }
    public IProgress<FontProgress> Progress { get; set; }
    public bool SaveBandFileWhenChanged { get; set; } = true;
}

public sealed class WinUiRenderResult
{
    public WinUiRenderStatus Status { get; set; }
    public bool Success { get; set; }
    public bool BandListChanged { get; set; }
    public FontAtlasResult AtlasResult { get; set; }
    public FontPerformanceStats PerformanceStats { get; set; }
    public Bgra32Image AtlasPixels => AtlasResult?.TextPixels;

    public WriteableBitmap CreateAtlasBitmap()
    {
        return AtlasPixels == null ? null : WinUiImageAdapter.ToWriteableBitmap(AtlasPixels);
    }
}

public static class WinUiRenderAdapter
{
    public static Task<WinUiRenderResult> RenderAsync(
        WinUiRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            FontRenderWorkflowResult result = FontRenderWorkflowService.Render(new FontRenderWorkflowRequest
            {
                FontSections = request.FontSections,
                Encoding = request.Encoding,
                GlyphSelection = request.GlyphSelection,
                AtlasRequest = request.AtlasRequest,
                Progress = request.Progress,
                SaveBandFileWhenChanged = request.SaveBandFileWhenChanged
            });

            cancellationToken.ThrowIfCancellationRequested();

            return new WinUiRenderResult
            {
                Status = ToWinUiStatus(result.Status),
                Success = result.Success,
                BandListChanged = result.BandListChanged,
                AtlasResult = result.AtlasResult,
                PerformanceStats = result.PerformanceStats
            };
        }, cancellationToken);
    }

    private static WinUiRenderStatus ToWinUiStatus(FontRenderWorkflowStatus status)
    {
        return status == FontRenderWorkflowStatus.AtlasOverflow
            ? WinUiRenderStatus.AtlasOverflow
            : WinUiRenderStatus.Success;
    }
}
