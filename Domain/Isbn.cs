using System.Text.RegularExpressions;

namespace Books.Domain;

public sealed record Isbn
{
    public string Value { get; }

    private Isbn(string value) => Value = value;

    /// Проверка формата: только допустимые символы и длина (13 или 10 знаков).
    /// Контрольная сумма здесь НЕ проверяется — см. HasValidChecksum().
    public static bool TryParse(string? raw, out Isbn? isbn)
    {
        isbn = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var normalized = raw.Trim().ToUpperInvariant()
                            .Replace("-", "").Replace(" ", "");

        if (normalized.Length == 13)
        {
            if (!Regex.IsMatch(normalized, @"^\d{13}$")) return false;
        }
        else if (normalized.Length == 10)
        {
            if (!Regex.IsMatch(normalized, @"^\d{9}[\dX]$")) return false;
        }
        else
        {
            return false;
        }

        isbn = new Isbn(normalized);
        return true;
    }

    /// Проверка контрольного разряда. В тестовом режиме валидатором не вызывается,
    /// сохранена для включения после теста и для аудита базы.
    public bool HasValidChecksum()
    {
        return Value.Length == 13
            ? Checksum13Ok(Value)
            : Checksum10Ok(Value);
    }

    private static bool Checksum13Ok(string digits)
    {
        int sum = 0;
        for (int i = 0; i < 12; i++)
            sum += (digits[i] - '0') * (i % 2 == 0 ? 1 : 3);

        return (10 - sum % 10) % 10 == digits[12] - '0';
    }

    private static bool Checksum10Ok(string digits)
    {
        int sum = 0;
        for (int i = 0; i < 9; i++)
            sum += (10 - i) * (digits[i] - '0');

        sum += digits[9] == 'X' ? 10 : digits[9] - '0';
        return sum % 11 == 0;
    }
}