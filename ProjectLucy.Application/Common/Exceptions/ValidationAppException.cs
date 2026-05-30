namespace ProjectLucy.Application.Common.Exceptions;

public class ValidationAppException : AppException
{
    public ValidationAppException(IReadOnlyCollection<string> errors)
        : base(422, "Validation failed", errors)
    {
    }
}
