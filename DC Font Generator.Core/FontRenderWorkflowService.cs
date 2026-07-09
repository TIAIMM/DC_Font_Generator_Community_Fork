using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DC_Font_Generator
{
    internal enum FontRenderWorkflowStatus
    {
        Success,
        AtlasOverflow
    }

    internal sealed class FontRenderWorkflowRequest
    {
        public IList<Main> FontSections { get; set; } = Array.Empty<Main>();
        public FontEncoding Encoding { get; set; }
        public GlyphSelectionState GlyphSelection { get; set; }
        public FontAtlasRequest AtlasRequest { get; set; }
        public IProgress<FontProgress> Progress { get; set; }
        public bool SaveBandFileWhenChanged { get; set; } = true;
    }

    internal sealed class FontRenderWorkflowResult
    {
        public FontRenderWorkflowStatus Status { get; set; }
        public bool BandListChanged { get; set; }
        public FontAtlasResult AtlasResult { get; set; }
        public FontPerformanceStats PerformanceStats { get; set; }
        public bool Success => Status == FontRenderWorkflowStatus.Success
            && AtlasResult != null
            && AtlasResult.Success;
    }

    internal static class FontRenderWorkflowService
    {
        public static FontRenderWorkflowResult Render(FontRenderWorkflowRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.AtlasRequest == null) throw new ArgumentNullException(nameof(request.AtlasRequest));

            FontRenderWorkflowResult result = new FontRenderWorkflowResult();
            FontPerformanceStats stats = new FontPerformanceStats();
            result.PerformanceStats = stats;
            request.AtlasRequest.PerformanceStats = stats;
            FontRenderDebugLog.Clear();

            if (request.GlyphSelection != null && request.Encoding != null)
            {
                result.BandListChanged = request.GlyphSelection.ApplyRemoved(request.Encoding);
                if (result.BandListChanged && request.SaveBandFileWhenChanged)
                {
                    request.Encoding.SaveBandFile();
                }
            }

            Stopwatch manufacturingWatch = Stopwatch.StartNew();
            foreach (Main section in request.FontSections)
            {
                stats.AddDebug($"[font-debug] section {section.ID}: font1={FormatFont(section.font1)}, style1={FormatStyle(section.font1StyleDescriptor)}, font2={FormatFont(section.font2)}, style2={FormatStyle(section.font2StyleDescriptor)}");
                section.ResetGeneratedStateIfRenderSettingsChanged(request.Encoding);
                section.NewDrawing(request.Encoding, request.Progress);
            }
            manufacturingWatch.Stop();
            stats.Add("Manufacturing", manufacturingWatch.Elapsed);
            stats.AddDebugRange(FontRenderDebugLog.Snapshot());

            FontAtlasResult atlasResult = FontGenerationServices.BuildAtlas(request.AtlasRequest, request.Progress);
            result.AtlasResult = atlasResult;
            if (!atlasResult.Success)
            {
                result.Status = FontRenderWorkflowStatus.AtlasOverflow;
                return result;
            }

            foreach (Main section in request.FontSections)
            {
                section.LinkClone();
            }

            result.Status = FontRenderWorkflowStatus.Success;
            return result;
        }

        private static string FormatFont(FontDescriptor font)
        {
            if (font == null)
            {
                return "<null>";
            }

            return $"{font.FamilyName}, size={font.SizePixels:0.##}, w={font.Weight}, wd={font.Width}, slant={font.Slant}, idx={font.StyleSetIndex}, style={font.StyleName ?? ""}";
        }

        private static string FormatStyle(FontStyleDescriptor style)
        {
            if (style == null)
            {
                return "<null>";
            }

            return $"{style.SourceFamilyName ?? ""}/{style.Name}, w={style.Weight}, wd={style.Width}, slant={style.Slant}, idx={style.StyleSetIndex}";
        }
    }
}
