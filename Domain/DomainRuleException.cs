namespace Books.Domain;

public class DomainRuleException : Exception
{
    public DomainRuleException(string message) : base(message)
    {
    }
}