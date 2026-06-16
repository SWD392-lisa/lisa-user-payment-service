using System.Text.Json.Serialization;

namespace ProjectLucy.Application.Common;

/// <summary>
/// Generic result wrapper that replaces the old ServiceResult.
/// Serializes to the same JSON shape: { status, message, data, errors }
/// so existing API response contracts are preserved.
/// </summary>
public class Result<T>
{
    [JsonPropertyName("status")]
    public int Status { get; private set; }
    [JsonPropertyName("message")]
    public string Message { get; private set; } = null!;
    [JsonPropertyName("data")]
    public T? Data { get; private set; }

    [JsonPropertyName("errors")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Errors { get; private set; }

    [JsonIgnore]
    public bool IsSuccess => Status is >= 200 and < 300;

    protected Result() { }

    public static Result<T> Success(T data, string message = "Success", int statusCode = 200)
        => new() { Status = statusCode, Message = message, Data = data };

    public static Result<T> Failure(int statusCode, string message)
        => new() { Status = statusCode, Message = message };

    public static Result<T> ValidationFailure(List<string> errors)
        => new() { Status = 422, Message = "Validation failed", Errors = errors };

    /// <summary>
    /// Convenience: create a non-generic result for operations that return no data.
    /// </summary>
    public static Result<T> Created(T data, string message = "Created")
        => new() { Status = 201, Message = message, Data = data };
}

/// <summary>
/// Non-generic shorthand for commands that return no payload.
/// </summary>
public class Result : Result<object?>
{
    public static Result<object?> Success(string message = "Success")
        => Result<object?>.Success(null, message);

    public new static Result<object?> Failure(int statusCode, string message)
        => Result<object?>.Failure(statusCode, message);
}
