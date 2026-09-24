namespace Books.Models;

/// <summary>
/// Элемент справочника (издательство, жанр, тип книги).
/// </summary>
public class RefItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
