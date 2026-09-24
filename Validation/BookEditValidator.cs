using Books.Domain;
using Books.Models;
using Microsoft.Extensions.Configuration;

namespace Books.Validation;

public class BookEditValidator : IBookEditValidator
{
    private readonly bool _checksumEnabled;

    public BookEditValidator(IConfiguration configuration)
    {
        _checksumEnabled = configuration.GetValue<bool>("Validation:IsbnChecksumEnabled");
    }

    //  точка входа: то, что требует интерфейс
    public ValidationResult Validate(Book book, IReadOnlyList<Chapter> chapters)
    {
        var result = new ValidationResult();

        ValidateBook(book, result);
        ValidateChapters(chapters, result);
        ValidateChapterOrder(chapters, result);

        return result;
    }

    private void ValidateBook(Book book, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(book.Title))
            result.AddError("Book.Title", "Укажите название книги");

        if (string.IsNullOrWhiteSpace(book.Author))
            result.AddError("Book.Author", "Укажите автора");

        if (book.PublicationYear <= 0)
            result.AddError("Book.PublicationYear", "Укажите год издания");

        if (string.IsNullOrWhiteSpace(book.Isbn))
        {
            result.AddError("Book.Isbn", "Укажите ISBN");
        }
        else if (!Isbn.TryParse(book.Isbn, out var isbn))
        {
            result.AddError("Book.Isbn",
                "ISBN должен содержать 13 цифр (или 10 знаков для старого формата). Дефисы допускаются.");
        }
        else if (_checksumEnabled && !isbn!.HasValidChecksum())
        {
            result.AddError("Book.Isbn", "ISBN не проходит проверку контрольного разряда.");
        }

        if (string.IsNullOrWhiteSpace(book.Publisher))
            result.AddError("Book.Publisher", "Укажите издателя");

        if (string.IsNullOrWhiteSpace(book.Description))
            result.AddError("Book.Description", "Заполните описание");
    }

    private static void ValidateChapterOrder(IReadOnlyList<Chapter> chapters, ValidationResult result)
    {
        for (int i = 1; i < chapters.Count; i++)
        {
            var prev = chapters[i - 1];
            var cur = chapters[i];

            var prevRangeValid = prev.StartPage > 0 && prev.EndPage >= prev.StartPage;
            var curStartValid = cur.StartPage > 0;

            if (prevRangeValid && curStartValid && cur.StartPage <= prev.EndPage)
            {
                var message =
                    $"Глава {i + 1} начинается на стр. {cur.StartPage}, а глава {i} заканчивается на стр. {prev.EndPage}. " +
                    "Главы должны идти по порядку: следующая начинается строго после конца предыдущей.";

                result.AddError($"Chapters[{i}].StartPage", message);
                result.AddError(message); // дубль в сводку сверху, чтобы ошибку невозможно было не заметить
            }
        }
    }
    private static void ValidateChapters(IReadOnlyList<Chapter> chapters, ValidationResult result)
    {
        var validRanges = new List<(int Index, PageRange Range)>();

        for (int i = 0; i < chapters.Count; i++)
        {
            var chapter = chapters[i];
            var prefix = $"Chapters[{i}]";

            if (chapter.Number <= 0)
                result.AddError($"{prefix}.Number", "Укажите номер главы");

            if (string.IsNullOrWhiteSpace(chapter.Title))
                result.AddError($"{prefix}.Title", "Укажите название главы");

            if (!PageRange.TryCreate(chapter.StartPage, chapter.EndPage, out var range))
            {
                if (chapter.StartPage <= 0)
                    result.AddError($"{prefix}.StartPage", "Укажите начальную страницу");

                if (chapter.EndPage <= 0)
                    result.AddError($"{prefix}.EndPage", "Укажите конечную страницу: второе число диапазона обязательно");
                else if (chapter.StartPage > 0 && chapter.EndPage < chapter.StartPage)
                    result.AddError($"{prefix}.EndPage", "Конечная страница меньше начальной");

                continue;
            }

            validRanges.Add((i, range!));
        }

        var ordered = validRanges
            .OrderBy(x => x.Range.Start)
            .ThenBy(x => x.Index)
            .ToList();

        for (int i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].Range.Overlaps(ordered[i - 1].Range))
            {
                var message =
                    $"Диапазоны пересекаются: глава {ordered[i - 1].Index + 1} ({ordered[i - 1].Range}) " +
                    $"и глава {ordered[i].Index + 1} ({ordered[i].Range}). Одна страница не может принадлежать двум главам.";

                result.AddError($"Chapters[{ordered[i].Index}].EndPage", message);
                result.AddError(message); // и в сводку сверху
            }
        }



    }
}