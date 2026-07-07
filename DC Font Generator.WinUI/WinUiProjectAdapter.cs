using System;
using System.Collections.Generic;
using DC_Font_Generator;

namespace DC_Font_Generator.WinUI;

public sealed class WinUiProjectSaveOptions
{
    public int EncodingIndex { get; set; }
    public int SizeXIndex { get; set; }
    public int SizeYIndex { get; set; }
    public string TexFileName { get; set; }
    public decimal Gap { get; set; }
    public Windows.UI.Color BackgroundColor { get; set; } = Windows.UI.Color.FromArgb(0, 0, 0, 0);
    public int ArrangeMethod { get; set; }
    public IList<Main> FontSections { get; set; } = Array.Empty<Main>();
}

public static class WinUiProjectAdapter
{
    public static ProjectDocument LoadProject(string selectedPath)
    {
        return ProjectFileWorkflowService.Load(selectedPath);
    }

    public static void SaveProject(string selectedPath, WinUiProjectSaveOptions options)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        ProjectFileWorkflowService.Save(selectedPath, new ProjectSaveRequest
        {
            EncodingIndex = options.EncodingIndex,
            SizeXIndex = options.SizeXIndex,
            SizeYIndex = options.SizeYIndex,
            TexFileName = options.TexFileName,
            Gap = options.Gap,
            BackGroundColorArgb = WinUiColorAdapter.ToDrawingColor(options.BackgroundColor).ToArgb(),
            ArrangeMethod = options.ArrangeMethod,
            FontSections = options.FontSections
        });
    }

    public static string GetProjectSavePath(string selectedPath)
    {
        return ProjectSerializationService.GetSavePath(selectedPath);
    }

    public static string GetProjectLoadPath(string selectedPath)
    {
        return ProjectSerializationService.GetLoadPath(selectedPath);
    }
}
