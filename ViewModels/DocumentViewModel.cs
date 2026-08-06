using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace Demo.ViewModels;

public partial class DocumentViewModel : ObservableObject
{
    private const string SaveFileName = "last-document.md";

    private readonly DispatcherQueueTimer? _saveTimer;
    private bool _saving;

    public IntPtr Hwnd { get; set; }

    [ObservableProperty]
    public partial string Markdown { get; set; } = "";

    public DocumentViewModel()
    {
        _saveTimer = DispatcherQueue.GetForCurrentThread()?.CreateTimer();
        if (_saveTimer != null)
        {
            _saveTimer.Interval = TimeSpan.FromMilliseconds(800);
            _saveTimer.Tick += async (_, _) =>
            {
                _saveTimer.Stop();
                await SaveAsync();
            };
        }
    }

    partial void OnMarkdownChanged(string value)
    {
        if (_saveTimer != null)
        {
            _saveTimer.Stop();
            _saveTimer.Start();
        }
    }

    public void UpdateFromEditor(string markdown)
    {
        Markdown = markdown;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var file = await ApplicationData.Current.LocalFolder.GetFileAsync(SaveFileName);
            if (file != null)
            {
                var text = await FileIO.ReadTextAsync(file);
                Markdown = text;
            }
        }
        catch
        {
            // no saved document yet
        }
    }

    public async Task FlushSaveAsync()
    {
        if (_saveTimer != null)
        {
            _saveTimer.Stop();
        }
        await SaveAsync();
    }

    private async Task SaveAsync()
    {
        if (_saving) return;
        _saving = true;
        try
        {
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                SaveFileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, Markdown);
        }
        catch
        {
            // ignore save failures
        }
        finally
        {
            _saving = false;
        }
    }

    [RelayCommand]
    private async Task OpenAsync()
    {
        var picker = new FileOpenPicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, Hwnd);
        picker.FileTypeFilter.Add(".md");
        picker.FileTypeFilter.Add(".markdown");

        var file = await picker.PickSingleFileAsync();
        if (file == null) return;

        var text = await FileIO.ReadTextAsync(file);
        Markdown = text;
    }

    [RelayCommand]
    private async Task ExportAsync()
    {
        var picker = new FileSavePicker();
        WinRT.Interop.InitializeWithWindow.Initialize(picker, Hwnd);
        picker.SuggestedFileName = "document.md";
        picker.FileTypeChoices.Add("Markdown", new System.Collections.Generic.List<string> { ".md" });

        var file = await picker.PickSaveFileAsync();
        if (file == null) return;

        await FlushSaveAsync();
        await FileIO.WriteTextAsync(file, Markdown);
    }
}
