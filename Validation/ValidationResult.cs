namespace Books.Validation;

public class ValidationError
{
    public ValidationError(string field, string message)
    {
        Field = field;
        Message = message;
    }

    public string Field { get; }

    public string Message { get; }
}

public class ValidationResult
{
    private readonly List<ValidationError> _errors = new();

    public bool IsValid => _errors.Count == 0;

    public IReadOnlyCollection<ValidationError> Errors => _errors;

    public void AddError(string field, string message)
    {
        _errors.Add(new ValidationError(field, message));
    }

    public void AddError(string message)
    {
        _errors.Add(new ValidationError(string.Empty, message));
    }
}