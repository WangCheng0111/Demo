using Demo.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace Demo.Services;

public class BookLibrary
{
    public static BookLibrary Instance { get; } = new();

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _storagePath =
        Path.Combine(ApplicationData.Current.LocalFolder.Path, "books.json");

    private readonly string _booksDir =
        Path.Combine(ApplicationData.Current.LocalFolder.Path, "Books");

    public ObservableCollection<Book> Books { get; } = new();
    public Book? CurrentBook { get; private set; }

    public event EventHandler? CurrentBookChanged;

    private BookLibrary()
    {
        Load();
    }

    public async Task<Book> ImportBookAsync(string path)
    {
        var existing = Books.FirstOrDefault(b =>
            string.Equals(b.SourcePath, path, StringComparison.OrdinalIgnoreCase) ||
            (string.IsNullOrEmpty(b.SourcePath) && string.Equals(b.FilePath, path, StringComparison.OrdinalIgnoreCase)));
        if (existing != null)
        {
            SetCurrentBook(existing);
            return existing;
        }

        var encoding = await Task.Run(() => TxtParser.DetectEncoding(path));
        var chapters = await Task.Run(() => TxtParser.ParseChapters(path, encoding));
        var cachePath = await CopyToCacheAsync(path);

        var book = new Book
        {
            Title = Path.GetFileNameWithoutExtension(path),
            FilePath = cachePath,
            SourcePath = path,
            EncodingName = encoding.WebName
        };

        if (chapters.Count == 0)
        {
            chapters.Add(new BookChapter { Title = book.Title, ByteOffset = 0, LineNumber = 0 });
        }
        book.Chapters = chapters;

        Books.Add(book);
        SetCurrentBook(book);
        return book;
    }

    private async Task<string> CopyToCacheAsync(string sourcePath)
    {
        Directory.CreateDirectory(_booksDir);
        var fileName = Path.GetFileName(sourcePath);
        var dest = Path.Combine(_booksDir, fileName);
        if (File.Exists(dest))
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            var ext = Path.GetExtension(fileName);
            for (int i = 1; i < 1000; i++)
            {
                var candidate = Path.Combine(_booksDir, $"{name} ({i}){ext}");
                if (!File.Exists(candidate))
                {
                    dest = candidate;
                    break;
                }
            }
        }
        await Task.Run(() => File.Copy(sourcePath, dest, overwrite: false));
        return dest;
    }

    public void SetCurrentBook(Book book)
    {
        if (CurrentBook == book) return;
        CurrentBook = book;
        book.LastReadAt = DateTime.Now;
        CurrentBookChanged?.Invoke(this, EventArgs.Empty);
        Save();
    }

    public void RemoveBook(Book book)
    {
        if (book == null || !Books.Contains(book)) return;

        Books.Remove(book);

        if (CurrentBook == book)
        {
            CurrentBook = Books.OrderByDescending(b => b.LastReadAt).FirstOrDefault();
            CurrentBookChanged?.Invoke(this, EventArgs.Empty);
        }
        TryDeleteCacheFile(book);
        Save();
    }

    private static void TryDeleteCacheFile(Book book)
    {
        try
        {
            if (File.Exists(book.FilePath))
            {
                File.Delete(book.FilePath);
            }
        }
        catch
        {
        }
    }

    private Timer? _debounceTimer;
    private readonly object _saveLock = new();

    public void SaveDebounced()
    {
        lock (_saveLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = new Timer(_ => { try { Save(); } catch { } }, null, 800, Timeout.Infinite);
        }
    }

    public void Save()
    {
        lock (_saveLock)
        {
            try
            {
                var data = new LibraryData
                {
                    Books = Books.ToList(),
                    CurrentBookPath = CurrentBook?.FilePath
                };
                File.WriteAllText(_storagePath, JsonSerializer.Serialize(data, JsonOptions));
            }
            catch
            {
            }
        }
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_storagePath)) return;
            var json = File.ReadAllText(_storagePath);
            var data = JsonSerializer.Deserialize<LibraryData>(json, JsonOptions);
            if (data?.Books != null)
            {
                foreach (var book in data.Books)
                {
                    Books.Add(book);
                }
            }
            CurrentBook = Books.FirstOrDefault(b => b.FilePath == data?.CurrentBookPath)
                ?? Books.OrderByDescending(b => b.LastReadAt).FirstOrDefault();
        }
        catch
        {
        }
    }
}

public class LibraryData
{
    public List<Book> Books { get; set; } = new();
    public string? CurrentBookPath { get; set; }
}
