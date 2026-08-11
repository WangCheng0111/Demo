using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Demo.Services;

public static class BookImporter
{
    public static async Task<StorageFile?> PickTxtFileAsync(Window window)
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(window));
        picker.FileTypeFilter.Add(".txt");
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        return await picker.PickSingleFileAsync();
    }

    public static async Task<Exception?> ImportAsync(string path)
    {
        try
        {
            await BookLibrary.Instance.ImportBookAsync(path);
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
