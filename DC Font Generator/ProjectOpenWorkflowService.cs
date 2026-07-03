using System;
using System.Collections.Generic;
using System.Drawing;

namespace DC_Font_Generator
{
    internal enum ProjectOpenWorkflowStatus
    {
        Success,
        ProjectError,
        AtlasOverflow
    }

    internal sealed class ProjectOpenWorkflowRequest
    {
        public ProjectDocument Document { get; set; }
        public IList<Main> FontSections { get; set; } = Array.Empty<Main>();
        public string FontPath { get; set; }
        public FontEncoding Encoding { get; set; }
        public Array2D.List2D<Fnt_char> CharIndex { get; set; }
        public Func<int, Main> CreateMain { get; set; }
        public FontAtlasRequest AtlasRequest { get; set; }
        public IProgress<FontProgress> Progress { get; set; }
        public Func<string, string> Localize { get; set; } = value => value;
    }

    internal sealed class ProjectOpenWorkflowResult
    {
        public ProjectOpenWorkflowStatus Status { get; set; }
        public int SelectedMainIndex { get; set; }
        public FontAtlasResult AtlasResult { get; set; }
        public List<string> Logs { get; } = new List<string>();
        public bool Success => Status == ProjectOpenWorkflowStatus.Success
            && AtlasResult != null
            && AtlasResult.Success;
    }

    internal static class ProjectOpenWorkflowService
    {
        public static ProjectOpenWorkflowResult Open(ProjectOpenWorkflowRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.Document == null) throw new ArgumentNullException(nameof(request.Document));
            if (request.AtlasRequest == null) throw new ArgumentNullException(nameof(request.AtlasRequest));

            ProjectOpenWorkflowResult result = new ProjectOpenWorkflowResult();
            ProjectApplyResult applyResult = ProjectApplicationService.ApplyFontSections(new ProjectApplyRequest
            {
                Document = request.Document,
                FontSections = request.FontSections,
                FontPath = request.FontPath,
                Encoding = request.Encoding,
                CharIndex = request.CharIndex,
                CreateMain = request.CreateMain,
                Progress = request.Progress,
                Localize = request.Localize
            });

            result.SelectedMainIndex = applyResult.SelectedMainIndex;
            result.Logs.AddRange(applyResult.Logs);

            try
            {
                FontRenderWorkflowResult renderResult = FontRenderWorkflowService.Render(new FontRenderWorkflowRequest
                {
                    FontSections = request.FontSections,
                    Encoding = request.Encoding,
                    AtlasRequest = request.AtlasRequest,
                    Progress = request.Progress,
                    SaveBandFileWhenChanged = false
                });

                result.AtlasResult = renderResult.AtlasResult;
                if (!renderResult.Success)
                {
                    result.Status = ProjectOpenWorkflowStatus.AtlasOverflow;
                    return result;
                }

                if (!ProjectApplicationService.ApplyPostAmendments(
                    request.FontSections,
                    request.Document.PostAmendments,
                    result.Logs,
                    request.Localize))
                {
                    applyResult.Success = false;
                }

                ProjectApplicationService.ApplyFixedFonts(request.FontSections);
                result.Status = applyResult.Success
                    ? ProjectOpenWorkflowStatus.Success
                    : ProjectOpenWorkflowStatus.ProjectError;
                return result;
            }
            finally
            {
                DisposeImportedTextures(applyResult.ImportedTextures);
            }
        }

        private static void DisposeImportedTextures(IEnumerable<Bitmap> textures)
        {
            foreach (Bitmap texture in textures)
            {
                texture.Dispose();
            }
        }
    }
}
