namespace ProjectLucy.Application.Common.Exceptions;

public class BadRequestException : AppException
{
    public BadRequestException(string message)
        : base(400, message)
    {
    }
}
