using Demo.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Demo.Services;

public static class TxtParser
{
    static TxtParser()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    private static readonly Regex ChapterTitleRegex = new(
        @"^\s*(" +
        @"第\s*[0-9零一二三四五六七八九十百千万两]+\s*[章回节卷集部篇][^\n\r]{0,60}" +
        @"|序章|楔子|前言|引言|卷首|终章|尾声|后记|番外|外传" +
        @"|(?:Chapter|Part|Episode|Volume|Act)\s*[0-9IVXLC]+[^\n\r]{0,60}" +
        @")$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static bool IsChapterTitle(string line)
    {
        return !string.IsNullOrWhiteSpace(line) && ChapterTitleRegex.IsMatch(line);
    }

    public static Encoding DetectEncoding(string path)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        Span<byte> head = stackalloc byte[4];
        int read = fs.Read(head);

        if (read >= 3 && head[0] == 0xEF && head[1] == 0xBB && head[2] == 0xBF)
        {
            return new UTF8Encoding(false);
        }
        if (read >= 2 && head[0] == 0xFF && head[1] == 0xFE)
        {
            return Encoding.Unicode;
        }
        if (read >= 2 && head[0] == 0xFE && head[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode;
        }

        const int sampleSize = 65536;
        var length = fs.Length;
        var offsets = new List<long> { 0 };
        if (length > sampleSize * 4)
        {
            offsets.Add(length / 4);
            offsets.Add(length / 2);
            offsets.Add(length * 3 / 4);
        }

        int valid = 0;
        foreach (var offset in offsets)
        {
            if (IsValidUtf8Sample(fs, offset, sampleSize))
            {
                valid++;
            }
        }
        if (valid > offsets.Count / 2)
        {
            return new UTF8Encoding(false);
        }
        return Encoding.GetEncoding("GB18030");
    }

    private static bool IsValidUtf8Sample(FileStream fs, long offset, int sampleSize)
    {
        byte[] buffer = new byte[sampleSize];
        fs.Seek(Math.Max(0, offset - 4), SeekOrigin.Begin);
        int n = 0;
        while (n < buffer.Length)
        {
            int r = fs.Read(buffer, n, buffer.Length - n);
            if (r <= 0) break;
            n += r;
        }
        if (n == 0) return true;

        try
        {
            var decoder = new UTF8Encoding(false, true).GetDecoder();
            char[] chars = new char[n];
            decoder.Convert(buffer, 0, n, chars, 0, chars.Length, false, out _, out _, out _);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    public static List<BookChapter> ParseChapters(string path, Encoding encoding)
    {
        var chapters = new List<BookChapter>();

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs, encoding, detectEncodingFromByteOrderMarks: false);

        var terminator = DetectLineTerminator(path, encoding);
        var terminatorBytes = encoding.GetByteCount(terminator);

        long byteOffset = 0;
        long lineNumber = 0;
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (IsChapterTitle(line))
            {
                chapters.Add(new BookChapter
                {
                    Title = line.Trim(),
                    ByteOffset = byteOffset,
                    LineNumber = lineNumber
                });
            }
            byteOffset += encoding.GetByteCount(line) + terminatorBytes;
            lineNumber++;
        }

        return chapters;
    }

    private static string DetectLineTerminator(string path, Encoding encoding)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var buffer = new byte[8192];
        bool utf16 = encoding.CodePage == 1200 || encoding.CodePage == 1201;
        int secondLast = -1;
        int last = -1;
        int read;
        while ((read = fs.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++)
            {
                var b = buffer[i];
                if (b == (byte)'\n')
                {
                    var hasCr = utf16 ? secondLast == (byte)'\r' : last == (byte)'\r';
                    return hasCr ? "\r\n" : "\n";
                }
                secondLast = last;
                last = b;
            }
        }
        return "\n";
    }

    public static List<ReaderParagraph> ReadChapterParagraphs(Book book, int chapterIndex)
    {
        var paragraphs = new List<ReaderParagraph>();
        if (book.Chapters.Count == 0) return paragraphs;

        var chapter = book.Chapters[chapterIndex];
        var encoding = Encoding.GetEncoding(book.EncodingName);

        paragraphs.Add(new ReaderParagraph { Text = chapter.Title, IsTitle = true });

        using var fs = new FileStream(book.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs, encoding, detectEncodingFromByteOrderMarks: false);

        fs.Seek(Math.Max(0, chapter.ByteOffset), SeekOrigin.Begin);
        reader.DiscardBufferedData();

        bool collecting = book.Chapters.Count == 1;
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (!collecting)
            {
                if (line.Trim() == chapter.Title) collecting = true;
                continue;
            }
            if (book.Chapters.Count > 1 && IsChapterTitle(line)) break;

            var text = line.Trim();
            if (text.Length > 0)
            {
                paragraphs.Add(new ReaderParagraph { Text = text });
            }
        }

        return paragraphs;
    }
}
