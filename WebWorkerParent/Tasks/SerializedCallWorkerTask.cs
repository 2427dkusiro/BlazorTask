using BlazorTask.Dispatch;

namespace BlazorTask.Tasks;

internal static class CallIdManager
{
    private static int callId = 0;

    public static int CallId { get => callId++; }
}

public sealed class SerializedCallWorkerTask : WorkerTask
{
    private readonly IJSUnmarshalledRuntime runtime;
    private readonly IJSUnmarshalledObjectReference module;

    private readonly string methodName;
    private readonly byte[] args;

    private readonly int workerId;
    private readonly IntPtr buffer;
    private readonly int bufferLength;

    private readonly Messaging.MessageHandler messageHandler;

    public SerializedCallWorkerTask(IJSUnmarshalledRuntime runtime, IJSUnmarshalledObjectReference module, string methodName, byte[] args, int workerId, IntPtr buffer, int bufferLength, Messaging.MessageHandler messageHandler)
    {
        this.runtime = runtime;
        this.module = module;
        this.methodName = methodName;
        this.args = args;
        this.workerId = workerId;
        this.buffer = buffer;
        this.bufferLength = bufferLength;
        this.messageHandler = messageHandler;
    }

    protected override unsafe void BeginAsyncInvoke(WorkerAwaiter workerAwaiter)
    {
        if (bufferLength < 16)
        {
            throw new InvalidOperationException("Buffer too short");
        }
        var _callId = CallIdManager.CallId;
        fixed (char* methodNamePtr = methodName)
        {
            fixed (byte* argPtr = args)
            {
                nint* ptr = (nint*)buffer.ToPointer();
                ptr[0] = (nint)methodNamePtr;
                ptr[1] = methodName.Length * sizeof(char);
                ptr[2] = (nint)argPtr;
                ptr[3] = args.Length;

                _ = module.InvokeUnmarshalled<int, int, int, object?>("SCall", workerId, sizeof(nint) * 4, _callId);
            }
        }
        var token = new CallResultToken(workerAwaiter);
        messageHandler.RegisterCallResultToken(_callId, token);
    }

    protected override void BlockingInvoke()
    {
        throw new NotImplementedException();
    }
}

public sealed class SerializedCallWorkerTask<T> : WorkerTask<T>
{
    private readonly IJSUnmarshalledRuntime runtime;
    private readonly IJSUnmarshalledObjectReference module;

    private readonly string methodName;
    private readonly byte[] args;

    private readonly int workerId;
    private readonly IntPtr buffer;
    private readonly int bufferLength;

    private readonly Messaging.MessageHandler messageHandler;

    public SerializedCallWorkerTask(IJSUnmarshalledRuntime runtime, IJSUnmarshalledObjectReference module, string methodName, byte[] args, int workerId, IntPtr buffer, int bufferLength, Messaging.MessageHandler messageHandler)
    {
        this.runtime = runtime;
        this.module = module;
        this.methodName = methodName;
        this.args = args;
        this.workerId = workerId;
        this.buffer = buffer;
        this.bufferLength = bufferLength;
        this.messageHandler = messageHandler;
    }

    protected override unsafe void BeginAsyncInvoke(WorkerAwaiter<T> workerAwaiter)
    {
        int _callId = CallIdManager.CallId;
        fixed (char* methodNamePtr = methodName)
        {
            fixed (byte* argPtr = args)
            {
                nint* ptr = (nint*)buffer.ToPointer();
                ptr[0] = (nint)methodNamePtr;
                ptr[1] = methodName.Length * sizeof(char);
                ptr[2] = (nint)argPtr;
                ptr[3] = args.Length;

                _ = module.InvokeUnmarshalled<int, int, int, object?>("SCall", workerId, sizeof(nint) * 4, _callId);
            }
        }
        var token = new CallResultToken<T>(workerAwaiter);
        messageHandler.RegisterCallResultToken(_callId, token);
    }

    protected override T BlockingInvoke()
    {
        throw new NotImplementedException();
    }
}

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
        var data = System.Text.Json.JsonSerializer.Deserialize<T>(span);
        workerAwaiter.SetResult(data);
    }

    public void SetException(in Span<byte> span)
    {
        var exception = System.Text.Json.JsonSerializer.Deserialize<WorkerException>(span) ?? throw new ArgumentException("Failed to deserialize exception.");
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
        var exception = System.Text.Json.JsonSerializer.Deserialize<WorkerException>(span) ?? throw new ArgumentException("Failed to deserialize exception.");
        workerAwaiter.SetException(exception);
    }
}