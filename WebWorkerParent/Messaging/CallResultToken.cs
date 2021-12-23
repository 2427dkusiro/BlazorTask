using BlazorTask.Dispatch;
using BlazorTask.Tasks;

namespace BlazorTask.Messaging;

public interface ICallResultToken
{
    void SetResultFromJson(in Span<byte> result);

    void SetException(in Span<byte> span);
}

public class CallResultToken<T> : ICallResultToken
{
    private readonly WorkerAwaiter<T> workerAwaiter;

    public CallResultToken(WorkerAwaiter<T> awaiter)
    {
        workerAwaiter = awaiter;
    }

    public void SetResultFromJson(in Span<byte> span)
    {
        T? data = System.Text.Json.JsonSerializer.Deserialize<T>(span);
        workerAwaiter.SetResult(data);
    }

    public void SetException(in Span<byte> span)
    {
        WorkerException? exception = System.Text.Json.JsonSerializer.Deserialize<WorkerException>(span) ?? throw new ArgumentException("Failed to deserialize exception.");
        workerAwaiter.SetException(exception);
    }
}

public class CallResultToken : ICallResultToken
{
    private readonly WorkerAwaiter workerAwaiter;

    public CallResultToken(WorkerAwaiter awaiter)
    {
        workerAwaiter = awaiter;
    }

    public void SetResultFromJson(in Span<byte> span)
    {
        workerAwaiter.SetResult();
    }

    public void SetException(in Span<byte> span)
    {
        Console.WriteLine(System.Text.Encoding.UTF8.GetString(span));
        WorkerException? exception = System.Text.Json.JsonSerializer.Deserialize<WorkerException>(span) ?? throw new ArgumentException("Failed to deserialize exception.");
        workerAwaiter.SetException(exception);
    }
}