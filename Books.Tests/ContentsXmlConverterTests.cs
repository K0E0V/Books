using Books.Data;
using Books.Models;

namespace Books.Tests;

/// <summary>
/// Критерий приёмки №2 ТЗ (docs/ORM_MIGRATION_TZ.md, п. 6):
/// round-trip List&lt;Chapter&gt; → XML → List&lt;Chapter&gt; без потерь,
/// формат совместим с .nodes() старых ХП (spBooksGetByIdV3).
/// </summary>
public class ContentsXmlConverterTests
{
    [Fact]
    public void ToXml_EmptyOrNull_ReturnsEmptyBookContents()
    {
        Assert.Equal(ContentsXmlConverter.EmptyXml, ContentsXmlConverter.ToXml(null));
        Assert.Equal(ContentsXmlConverter.EmptyXml, ContentsXmlConverter.ToXml(new List<Chapter>()));
    }

    [Fact]
    public void ToXml_ProducesOldStoredProcFormat()
    {
        var chapters = new List<Chapter>
        {
            new() { Number = 1, Title = "Введение", StartPage = 1, EndPage = 10 },
            new() { Number = 2, Title = "Основная часть", StartPage = 11, EndPage = 100 },
        };

        var xml = ContentsXmlConverter.ToXml(chapters);

        // Формат для spBooksGetByIdV3 (.nodes('/BookContents/Chapter')):
        // корневой элемент + по одному <Chapter .../> на строку.
        Assert.StartsWith("<BookContents>", xml);
        Assert.EndsWith("</BookContents>", xml.TrimEnd());
        Assert.Contains("<Chapter number=\"1\" title=\"Введение\" startPage=\"1\" endPage=\"10\" />", xml);
        Assert.Contains("<Chapter number=\"2\" title=\"Основная часть\" startPage=\"11\" endPage=\"100\" />", xml);
    }

    [Fact]
    public void RoundTrip_PreservesAllFields()
    {
        var chapters = new List<Chapter>
        {
            new() { Number = 1, Title = "Глава \"в кавычках\" & спец<символы>", StartPage = 1, EndPage = 5 },
            new() { Number = 42, Title = "", StartPage = 0, EndPage = 0 },
            new() { Number = int.MaxValue, Title = "Финал", StartPage = 300, EndPage = 320 },
        };

        var restored = ContentsXmlConverter.FromXml(ContentsXmlConverter.ToXml(chapters));

        Assert.Equal(chapters.Count, restored.Count);
        for (var i = 0; i < chapters.Count; i++)
        {
            Assert.Equal(chapters[i].Number, restored[i].Number);
            Assert.Equal(chapters[i].Title, restored[i].Title);
            Assert.Equal(chapters[i].StartPage, restored[i].StartPage);
            Assert.Equal(chapters[i].EndPage, restored[i].EndPage);
        }
    }

    [Fact]
    public void FromXml_NullOrBroken_ReturnsEmptyList()
    {
        Assert.Empty(ContentsXmlConverter.FromXml(null));
        Assert.Empty(ContentsXmlConverter.FromXml(""));
        Assert.Empty(ContentsXmlConverter.FromXml("это не xml <<<"));
    }

    [Fact]
    public void FromXml_NonNumericAttributes_DefaultsToZero()
    {
        const string xml = "<BookContents><Chapter number=\"abc\" title=\"X\" startPage=\"zz\" endPage=\"7\"/></BookContents>";

        var chapters = ContentsXmlConverter.FromXml(xml);

        Assert.Single(chapters);
        Assert.Equal(0, chapters[0].Number);
        Assert.Equal(0, chapters[0].StartPage);
        Assert.Equal(7, chapters[0].EndPage);
    }

    [Fact]
    public void ToXml_AboveSizeLimit_Throws()
    {
        var huge = new List<Chapter>();
        var filler = new string('А', 200);
        while (ContentsXmlConverter.EmptyXml.Length == 0 || true)
        {
            huge.Add(new Chapter { Number = huge.Count + 1, Title = filler, StartPage = 1, EndPage = 2 });
            if (huge.Count * filler.Length > ContentsXmlConverter.MaxXmlLength) break;
        }

        Assert.Throws<InvalidOperationException>(() => ContentsXmlConverter.ToXml(huge));
    }
}
