namespace ProjectLucy.Application.Common.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }
    public IReadOnlyCollection<string>? Errors { get; }

    public AppException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public AppException(int statusCode, string message, IReadOnlyCollection<string> errors)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}
