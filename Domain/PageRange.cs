namespace Books.Domain;

/// Диапазон страниц главы. Невозможно создать некорректный экземпляр.
public sealed record PageRange
{
    public int Start { get; }
    public int End { get; }

    private PageRange(int start, int end)
    {
        Start = start;
        End = end;
    }

    public static bool TryCreate(int? start, int? end, out PageRange? range)
    {
        range = null;

        if (start is null or <= 0) return false;
        if (end is null or <= 0) return false;
        if (end < start) return false;

        range = new PageRange(start.Value, end.Value);
        return true;
    }

    public bool Overlaps(PageRange other) => Start <= other.End && other.Start <= End;

    public override string ToString() => $"{Start}-{End}";
}