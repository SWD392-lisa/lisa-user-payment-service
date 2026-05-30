namespace ProjectLucy.Application.Common.Exceptions;

public class UnauthorizedException : AppException
{
    public UnauthorizedException(string message)
        : base(401, message)
    {
    }
}
