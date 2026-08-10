namespace Demo.Models;

public class ReaderParagraph
{
    public string Text { get; init; } = "";
    public bool IsTitle { get; init; }
    public bool IsBody => !IsTitle;
}
