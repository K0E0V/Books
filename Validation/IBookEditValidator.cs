using Books.Models;

namespace Books.Validation;

public interface IBookEditValidator
{
    ValidationResult Validate(Book book, IReadOnlyList<Chapter> chapters);
}