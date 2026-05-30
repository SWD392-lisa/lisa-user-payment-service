using MediatR;
using Microsoft.Extensions.Logging;

namespace ProjectLucy.Application.Common.Behaviors;

public class RequestLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<RequestLoggingBehavior<TRequest, TResponse>> _logger;

    public RequestLoggingBehavior(ILogger<RequestLoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Handling request {RequestName}: {@Request}", requestName, request);

        var response = await next();

        _logger.LogInformation("Handled request {RequestName}", requestName);

        return response;
    }
}
