namespace Books.Models;

public sealed record AboutStats(
    int TotalBooks,
    int BooksAddedLast24Hours,
    int TotalPages);