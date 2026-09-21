namespace Books.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int PublicationYear { get; set; }
        public string? Isbn { get; set; }
        public string? Publisher { get; set; }
        public string? Description { get; set; }
        public List<Chapter> Chapters { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
