namespace ProjectLucy.Gateway.Middlewares;

/// <summary>
/// Gán/đọc X-Correlation-ID cho mỗi request, trả lại trong response header,
/// và mở logging scope để mọi log line của request có cùng CorrelationId.
/// Giá trị được đẩy xuống downstream qua YARP transform (xem Program.cs).
/// </summary>
public class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers.TryGetValue(HeaderName, out var incoming)
            && !string.IsNullOrWhiteSpace(incoming)
                ? incoming.ToString()
                : Guid.NewGuid().ToString();

        context.Items[ItemKey] = correlationId;

        // Set trước khi response bắt đầu (YARP có thể ghi response sớm).
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (_logger.BeginScope(new Dictionary<string, object> { [ItemKey] = correlationId }))
        {
            await _next(context);
        }
    }
}
