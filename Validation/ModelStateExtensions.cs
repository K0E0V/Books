using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Books.Validation;

public static class ModelStateExtensions
{
    private static readonly string[] NumericSuffixes =
    {
        ".StartPage",
        ".EndPage",
        ".Number",
        ".PublicationYear"
    };

    public static void HumanizeNumericBindingErrors(this ModelStateDictionary modelState)
    {
        foreach (var entry in modelState)
        {
            if (entry.Value.ValidationState != ModelValidationState.Invalid)
                continue;

            if (!NumericSuffixes.Any(s => entry.Key.EndsWith(s, StringComparison.OrdinalIgnoreCase)))
                continue;

            var attempted = entry.Value.AttemptedValue;

            entry.Value.Errors.Clear();
            entry.Value.ValidationState = ModelValidationState.Invalid;

            entry.Value.Errors.Add(
                string.IsNullOrWhiteSpace(attempted)
                    ? "Поле обязательно для заполнения"
                    : $"Введите целое число. Значение \"{attempted}\" не распознано");
        }
    }

    public static void AddToModelState(this ValidationResult result, ModelStateDictionary modelState)
    {
        foreach (var error in result.Errors)
        {
            if (!string.IsNullOrWhiteSpace(error.Field) &&
                modelState.TryGetValue(error.Field, out var state) &&
                state.ValidationState == ModelValidationState.Invalid)
            {
                continue; // поле уже помечено ошибкой привязки
            }

            if (string.IsNullOrWhiteSpace(error.Field))
                modelState.AddModelError(string.Empty, error.Message);
            else
                modelState.AddModelError(error.Field, error.Message);
        }
    }
}