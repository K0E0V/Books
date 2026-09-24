using System.Text.RegularExpressions;

namespace Books.Validation;

public static class IsbnValidator
{
    public static bool IsValidStrict(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var isbn = value.Trim().ToUpperInvariant();

        if (!Regex.IsMatch(isbn, @"^[0-9X-]+$"))
        {
            return false;
        }

        var digits = isbn.Replace("-", "");

        if (digits.Length == 13)
        {
            if (!isbn.Contains('-'))
            {
                return false;
            }

            if (!Regex.IsMatch(isbn, @"^97[89]-\d{1,5}-\d{1,7}-\d{1,7}-\d$"))
            {
                return false;
            }

            return IsValid13(digits);
        }

        if (digits.Length == 10)
        {
            if (!isbn.Contains('-'))
            {
                return false;
            }

            if (!Regex.IsMatch(isbn, @"^\d{1,5}-\d{1,7}-\d{1,7}-[\dX]$"))
            {
                return false;
            }

            return IsValid10(digits);
        }

        return false;
    }

    private static bool IsValid13(string digits)
    {
        if (!Regex.IsMatch(digits, @"^\d{13}$"))
        {
            return false;
        }

        if (!digits.StartsWith("978") && !digits.StartsWith("979"))
        {
            return false;
        }

        int sum = 0;

        for (int i = 0; i < 12; i++)
        {
            int digit = digits[i] - '0';
            sum += digit * (i % 2 == 0 ? 1 : 3);
        }

        int check = (10 - (sum % 10)) % 10;
        int last = digits[12] - '0';

        return check == last;
    }

    private static bool IsValid10(string digits)
    {
        if (!Regex.IsMatch(digits, @"^\d{9}[\dX]$"))
        {
            return false;
        }

        int sum = 0;

        for (int i = 0; i < 9; i++)
        {
            int digit = digits[i] - '0';
            sum += (10 - i) * digit;
        }

        int last = digits[9] == 'X' ? 10 : digits[9] - '0';
        sum += last;

        return sum % 11 == 0;
    }
}