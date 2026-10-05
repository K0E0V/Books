using System.Xml.Linq;
using Books.Models;

namespace Books.Data;

/// <summary>
/// Конвертер оглавления книги: List&lt;Chapter&gt; ⇄ XML колонки dbo.tblBooks.ContentsXml.
/// Формат сохраняется совместимым с существующими ХП (spBooksGetByIdV3 c .nodes()):
/// &lt;BookContents&gt;&lt;Chapter number="1" title="..." startPage="1" endPage="10"/&gt;&lt;/BookContents&gt;
/// Экранирование спецсимволов выполняет XDocument (замена ручного SecurityElement.Escape).
/// См. docs/ORM_MIGRATION_TZ.md, п. 3.2 (вариант A).
/// </summary>
public static class ContentsXmlConverter
{
    public const string EmptyXml = "<BookContents></BookContents>";

    /// Максимальный размер XML оглавления, символов (страховка от раздувания колонки xml, см. ТЗ п. 7).
    public const int MaxXmlLength = 64 * 1024;

    public static string ToXml(List<Chapter>? chapters)
    {
        if (chapters == null || chapters.Count == 0)
            return EmptyXml;

        var root = new XElement("BookContents");
        foreach (var ch in chapters)
        {
            root.Add(new XElement("Chapter",
                new XAttribute("number", ch.Number),
                new XAttribute("title", ch.Title ?? string.Empty),
                new XAttribute("startPage", ch.StartPage),
                new XAttribute("endPage", ch.EndPage)));
        }

        // Совпадение с форматом старого GenerateXmlFromChapters: по элементу на строку.
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<BookContents>");
        foreach (var el in root.Elements())
            sb.AppendLine("  " + OneLine(el));
        sb.Append("</BookContents>");

        var xml = sb.ToString();
        if (xml.Length > MaxXmlLength)
            throw new InvalidOperationException(
                $"Оглавление слишком большое ({xml.Length} символов, лимит {MaxXmlLength}).");
        return xml;
    }

    public static List<Chapter> FromXml(string? xml)
    {
        var result = new List<Chapter>();
        if (string.IsNullOrWhiteSpace(xml)) return result;

        XDocument doc;
        try
        {
            doc = XDocument.Parse(xml);
        }
        catch
        {
            return result; // повреждённый XML — читаем как пустое оглавление (см. поведение старых ХП)
        }

        foreach (var el in doc.Root?.Elements("Chapter") ?? Enumerable.Empty<XElement>())
        {
            result.Add(new Chapter
            {
                Number = AttrInt(el, "number"),
                Title = (string?)el.Attribute("title") ?? string.Empty,
                StartPage = AttrInt(el, "startPage"),
                EndPage = AttrInt(el, "endPage"),
            });
        }
        return result;
    }

    private static int AttrInt(XElement el, string name) =>
        int.TryParse((string?)el.Attribute(name), out var v) ? v : 0;

    private static string OneLine(XElement el) =>
        el.ToString(SaveOptions.DisableFormatting);
}
