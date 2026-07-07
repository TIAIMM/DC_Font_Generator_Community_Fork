using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace DC_Font_Generator.WinUI;

internal sealed class WinUiFilePickerService
{
    private readonly Window window;

    public WinUiFilePickerService(Window window)
    {
        this.window = window ?? throw new ArgumentNullException(nameof(window));
    }

    public async Task<string> OpenFileAsync(string title, params string[] extensions)
    {
        FileOpenPicker picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary
        };
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
        if (!string.IsNullOrWhiteSpace(title))
        {
            picker.CommitButtonText = title;
        }

        AddExtensions(picker.FileTypeFilter, extensions);
        Windows.Storage.StorageFile file = await picker.PickSingleFileAsync();
        return file?.Path;
    }

    public async Task<string> SaveFileAsync(string title, string suggestedName, params string[] extensions)
    {
        FileSavePicker picker = new FileSavePicker
        {
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SuggestedFileName = string.IsNullOrWhiteSpace(suggestedName) ? "" : suggestedName
        };
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
        if (!string.IsNullOrWhiteSpace(title))
        {
            picker.CommitButtonText = title;
        }

        string label = extensions.Length == 0 ? "All files" : string.Join(", ", extensions);
        List<string> fileTypes = new List<string>();
        AddExtensions(fileTypes, extensions);
        picker.FileTypeChoices.Add(label, fileTypes);

        Windows.Storage.StorageFile file = await picker.PickSaveFileAsync();
        return file?.Path;
    }

    private static void AddExtensions(ICollection<string> target, params string[] extensions)
    {
        if (extensions == null || extensions.Length == 0)
        {
            target.Add("*");
            return;
        }

        foreach (string extension in extensions)
        {
            if (string.IsNullOrWhiteSpace(extension))
            {
                continue;
            }

            target.Add(extension.StartsWith(".") ? extension : "." + extension);
        }
    }
}
