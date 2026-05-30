namespace ProjectLucy.Domain.Exceptions;

/// <summary>
/// Base exception for domain rule violations.
/// Caught by the global exception middleware and mapped to HTTP 400/422.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
