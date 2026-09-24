using Books.Data;
using Books.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Books.Pages;

public class AboutModel : PageModel
{
    // Средние оценки для офсетной бумаги. Крути здесь, если захочешь другую физику.
    private const double PageThicknessMm = 0.12;  // толщина корешка на одну страницу
    private const double CoverThicknessMm = 4;    // корешок: крышки переплёта на одну книгу
    private const double PageWeightG = 2;         // вес одной страницы (65 г/м², формат 60x90/16)
    private const double CoverWeightG = 150;      // крышки + клей на одну книгу

    private readonly BookRepository _repo;

    public AboutModel(BookRepository repo)
    {
        _repo = repo;
    }

    public AboutStats Stats { get; private set; } = new(0, 0, 0);
    public string ShelfLengthLabel { get; private set; } = "0 см";
    public string ShelfWeightLabel { get; private set; } = "0 г";

    public async Task OnGetAsync()
    {
        Stats = await _repo.GetAboutStatsAsync();

        var lengthMm = Stats.TotalPages * PageThicknessMm
                       + Stats.TotalBooks * CoverThicknessMm;
        var weightG = Stats.TotalPages * PageWeightG
                      + Stats.TotalBooks * CoverWeightG;

        ShelfLengthLabel = FormatLength(lengthMm / 1000.0);
        ShelfWeightLabel = FormatWeight(weightG / 1000.0);
    }

    private static string FormatLength(double meters) =>
        meters < 1 ? $"{meters * 100:0} см" : $"{meters:0.#} м";

    private static string FormatWeight(double kg) =>
        kg < 1 ? $"{kg * 1000:0} г" : $"{kg:0.#} кг";
}
