using BlazorTask.Messaging;

namespace BlazorTask.Tasks;

public sealed class SerializedCallWorkerTask : WorkerTask
{
    private readonly IJSUnmarshalledRuntime runtime;
    private readonly CallHeader callHeader;

    private readonly string methodName;
    private readonly byte[] args;

    private readonly int workerId;

    private readonly MessageHandler messageHandler;

    public SerializedCallWorkerTask(IJSUnmarshalledRuntime runtime, CallHeader callHeader, string methodName, byte[] args, int workerId, MessageHandler messageHandler)
    {
        this.runtime = runtime;
        this.callHeader = callHeader;
        this.methodName = methodName;
        this.args = args;
        this.workerId = workerId;
        this.messageHandler = messageHandler;
    }

    protected override unsafe void BeginAsyncInvoke(WorkerAwaiter workerAwaiter)
    {
        messageHandler.CallSerialized(callHeader, methodName, args, workerId, workerAwaiter);
    }

    protected override void BlockingInvoke()
    {
        throw new NotImplementedException();
    }
}

public sealed class SerializedCallWorkerTask<T> : WorkerTask<T>
{
    private readonly IJSUnmarshalledRuntime runtime;
    private readonly CallHeader callHeader;

    private readonly string methodName;
    private readonly byte[] args;

    private readonly int workerId;

    private readonly MessageHandler messageHandler;

    public SerializedCallWorkerTask(IJSUnmarshalledRuntime runtime, CallHeader callHeader, string methodName, byte[] args, int workerId, MessageHandler messageHandler)
    {
        this.runtime = runtime;
        this.callHeader = callHeader;
        this.methodName = methodName;
        this.args = args;
        this.workerId = workerId;
        this.messageHandler = messageHandler;
    }

    protected override unsafe void BeginAsyncInvoke(WorkerAwaiter<T> workerAwaiter)
    {
        messageHandler.CallSerialized(callHeader, methodName, args, workerId, workerAwaiter);
    }

    protected override T BlockingInvoke()
    {
        throw new NotImplementedException();
    }
}