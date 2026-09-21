namespace Books.Models;

public class Chapter
{
    public int Number { get; set; }
    public string Title { get; set; } = string.Empty;
    public int StartPage { get; set; }
    public int EndPage { get; set; }
}