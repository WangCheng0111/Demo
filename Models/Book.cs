using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Serialization;

namespace Demo.Models;

public class Book
{
    public string Title { get; set; } = "";
    public string FilePath { get; set; } = "";
    public string SourcePath { get; set; } = "";
    public string EncodingName { get; set; } = "utf-8";
    public List<BookChapter> Chapters { get; set; } = new();
    public int CurrentChapterIndex { get; set; }
    public int CurrentParagraphIndex { get; set; }
    public DateTime LastReadAt { get; set; }

    [JsonIgnore]
    public string FileName => Path.GetFileName(FilePath);
}
