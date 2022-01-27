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

    private static int sourceId = -1;
    protected override void BlockingInvoke()
    {
        var callId = callHeader.callId;
        CallHeader.CallType option = callHeader.callType;

        sourceId = messageHandler.GetSyncCallSourceId();

        if (sourceId == -1)
        {
            throw new PlatformNotSupportedException("service-worker not available or not configured.");
        }
        if (sourceId > byte.MaxValue)
        {
            throw new OverflowException();
        }
        if (callId >= (1 << 24))
        {
            throw new OverflowException();
        }
        callId = (sourceId << 24) | callId;
        option |= CallHeader.CallType.Sync;
        var newHeader = new CallHeader(callId, option);
        messageHandler.CallSerializedSync(newHeader, methodName, args, workerId);
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
        var callId = callHeader.callId;
        CallHeader.CallType option = callHeader.callType;

        var sourceId = messageHandler.GetSyncCallSourceId();

        if (sourceId == -1)
        {
            throw new PlatformNotSupportedException("service-worker not available or not configured.");
        }
        if (sourceId > byte.MaxValue)
        {
            throw new OverflowException();
        }
        if (callId >= (1 << 24))
        {
            throw new OverflowException();
        }
        callId = (sourceId << 24) | callId;
        option |= CallHeader.CallType.Sync;
        var newHeader = new CallHeader(callId, option);
        return messageHandler.CallSerializedSync<T>(newHeader, methodName, args, workerId);
    }
}