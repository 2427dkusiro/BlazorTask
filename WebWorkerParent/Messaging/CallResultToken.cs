using BlazorTask.Dispatch;
using BlazorTask.Tasks;

namespace BlazorTask.Messaging;

/// <summary>
/// Defines token object which accepts async method result.
/// </summary>
public interface IAsyncResultToken
{
    /// <summary>
    /// Return success result value as json binary.
    /// </summary>
    /// <param name="result">UTF-8 encoded json data.</param>
    void SetResultFromJson(in Span<byte> result);

    /// <summary>
    /// Return exception result as json binary.
    /// </summary>
    /// <param name="span">UTF-8 encoded json data.</param>
    void SetException(in Span<byte> span);
}

/// <summary>
/// Represent a token to return calling result which has return-value.
/// </summary>
/// <typeparam name="T"></typeparam>
public class CallResultToken<T> : IAsyncResultToken
{
    private readonly WorkerAwaiter<T?> workerAwaiter;

    /// <summary>
    /// Create new instance of <see cref="CallResultToken{T}"/>.
    /// </summary>
    /// <param name="awaiter"></param>
    public CallResultToken(WorkerAwaiter<T?> awaiter)
    {
        workerAwaiter = awaiter;
    }

    /// <inheritdoc />
    public void SetResultFromJson(in Span<byte> span)
    {
        T? data = System.Text.Json.JsonSerializer.Deserialize<T>(span);
        workerAwaiter.SetResult(data);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException" />
    public void SetException(in Span<byte> span)
    {
        WorkerException? exception = System.Text.Json.JsonSerializer.Deserialize<WorkerException>(span) ?? throw new ArgumentException("Failed to deserialize exception.");
        workerAwaiter.SetException(exception);
    }
}

/// <summary>
/// Represent a token to return calling result which returns <see langword="void" />.
/// </summary>
public class CallResultToken : IAsyncResultToken
{
    private readonly WorkerAwaiter workerAwaiter;

    /// <summary>
    /// Create new instance of <see cref="CallResultToken"/>.
    /// </summary>
    /// <param name="awaiter"></param>
    public CallResultToken(WorkerAwaiter awaiter)
    {
        workerAwaiter = awaiter;
    }

    /// <inheritdoc />
    public void SetResultFromJson(in Span<byte> span)
    {
        workerAwaiter.SetResult();
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException" />
    public void SetException(in Span<byte> span)
    {
        Console.WriteLine(System.Text.Encoding.UTF8.GetString(span));
        WorkerException? exception = System.Text.Json.JsonSerializer.Deserialize<WorkerException>(span) ?? throw new ArgumentException("Failed to deserialize exception.");
        workerAwaiter.SetException(exception);
    }
}