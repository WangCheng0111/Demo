using Demo.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Demo.Views;

public sealed partial class MarkdownEditorView : UserControl
{
    private bool _initialized;
    private bool _syncingFromEditor;
    private string _pendingMarkdown = "";
    private readonly DispatcherQueueTimer? _debounceTimer;

    public static readonly DependencyProperty DocumentProperty =
        DependencyProperty.Register(nameof(Document), typeof(DocumentViewModel), typeof(MarkdownEditorView),
            new PropertyMetadata(null, OnDocumentChanged));

    public static readonly DependencyProperty IsDarkThemeProperty =
        DependencyProperty.Register(nameof(IsDarkTheme), typeof(bool), typeof(MarkdownEditorView),
            new PropertyMetadata(false, OnIsDarkThemeChanged));

    public DocumentViewModel? Document
    {
        get => (DocumentViewModel?)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    public bool IsDarkTheme
    {
        get => (bool)GetValue(IsDarkThemeProperty);
        set => SetValue(IsDarkThemeProperty, value);
    }

    public MarkdownEditorView()
    {
        InitializeComponent();
        EditorWebView.DefaultBackgroundColor = Microsoft.UI.Colors.Transparent;
        _debounceTimer = DispatcherQueue.GetForCurrentThread()?.CreateTimer();
        if (_debounceTimer != null)
        {
            _debounceTimer.Interval = TimeSpan.FromMilliseconds(150);
            _debounceTimer.Tick += (_, _) =>
            {
                _debounceTimer.Stop();
                PushToViewModel(_pendingMarkdown);
            };
        }
        Loaded += MarkdownEditorView_Loaded;
    }

    private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var view = (MarkdownEditorView)d;
        if (e.OldValue is DocumentViewModel oldVm)
        {
            oldVm.PropertyChanged -= view.DocumentViewModel_PropertyChanged;
        }
        if (e.NewValue is DocumentViewModel newVm)
        {
            newVm.PropertyChanged += view.DocumentViewModel_PropertyChanged;
        }
    }

    private static void OnIsDarkThemeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((MarkdownEditorView)d).SendTheme();
    }

    private void SendTheme()
    {
        if (EditorWebView.CoreWebView2 == null) return;
        var payload = JsonSerializer.Serialize(new { type = "theme", dark = IsDarkTheme });
        EditorWebView.CoreWebView2.PostWebMessageAsString(payload);
    }

    private void DocumentViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DocumentViewModel.Markdown) && !_syncingFromEditor && Document != null)
        {
            LoadMarkdown(Document.Markdown);
        }
    }

    private async Task InitializeAsync()
    {
        if (_initialized) return;
        _initialized = true;

        try
        {
            await EditorWebView.EnsureCoreWebView2Async();
        }
        catch (Exception)
        {
            _initialized = false;
            return;
        }

        var core = EditorWebView.CoreWebView2;
        if (core == null) return;

        var editorPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Editor");
        if (Directory.Exists(editorPath))
        {
            core.SetVirtualHostNameToFolderMapping(
                "editor.local",
                editorPath,
                CoreWebView2HostResourceAccessKind.Allow);
        }

        core.WebMessageReceived += Core_WebMessageReceived;

        EditorWebView.Source = new Uri("https://editor.local/index.html");
        EditorWebView.NavigationCompleted += (_, _) => EditorWebView.Focus(FocusState.Programmatic);
    }

    public void LoadMarkdown(string markdown)
    {
        if (EditorWebView.CoreWebView2 == null) return;
        var payload = JsonSerializer.Serialize(new { type = "load", markdown });
        EditorWebView.CoreWebView2.PostWebMessageAsString(payload);
    }

    private async void MarkdownEditorView_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeAsync();
    }

    private void Core_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            using var doc = JsonDocument.Parse(args.TryGetWebMessageAsString());
            var root = doc.RootElement;
            if (root.TryGetProperty("type", out var typeProp) && typeProp.GetString() is string type)
            {
                if (type == "content")
                {
                    var markdown = root.TryGetProperty("markdown", out var mdProp) ? mdProp.GetString() ?? "" : "";
                    if (_debounceTimer != null)
                    {
                        _pendingMarkdown = markdown;
                        _debounceTimer.Stop();
                        _debounceTimer.Start();
                    }
                    else
                    {
                        PushToViewModel(markdown);
                    }
                }
                else if (type == "ready")
                {
                    SendTheme();
                    if (Document != null && !string.IsNullOrEmpty(Document.Markdown))
                    {
                        LoadMarkdown(Document.Markdown);
                    }
                }
            }
        }
        catch (Exception)
        {
            // ignore malformed messages
        }
    }

    private void PushToViewModel(string markdown)
    {
        if (Document == null) return;
        _syncingFromEditor = true;
        Document.UpdateFromEditor(markdown);
        _syncingFromEditor = false;
    }
}
