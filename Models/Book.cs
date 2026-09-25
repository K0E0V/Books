using System.ComponentModel.DataAnnotations;

namespace Books.Models
{
    public class Book
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Укажите название.")]
        public string Title { get; set; } = string.Empty;
        [Required(ErrorMessage = "Укажите автора.")]
        public string Author { get; set; } = string.Empty;
        public int PublicationYear { get; set; }
        public string? Isbn { get; set; }
        [Required(ErrorMessage = "Укажите издательство.")]
        [StringLength(200)]
        public string? Publisher { get; set; }
        public string? Description { get; set; }

        /// <summary>Количество страниц. В БД колонка CountPages NOT NULL.</summary>
        [Range(1, int.MaxValue, ErrorMessage = "Количество страниц должно быть больше нуля.")]
        public int CountPages { get; set; }

        public List<Chapter> Chapters { get; set; } = new();

        // Справочниковые связи (не маппятся на колонки TblBooks, заполняются репозиторием)
        public List<int> GenreIds { get; set; } = new();
        public List<int> BookTypeIds { get; set; } = new();
        public List<string> Genres { get; set; } = new();
        public List<string> BookTypes { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
