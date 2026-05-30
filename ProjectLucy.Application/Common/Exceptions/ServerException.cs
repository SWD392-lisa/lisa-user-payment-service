namespace ProjectLucy.Application.Common.Exceptions;

public class ServerException : AppException
{
    public ServerException(string message)
        : base(500, message)
    {
    }
}
