using BlazorTask.Configure;

namespace BlazorTask.Tasks;

public sealed class StartWorkerTask : WorkerTask
{
    private readonly IJSUnmarshalledRuntime runtime;
    private readonly IJSUnmarshalledObjectReference module;
    private readonly WorkerInitializeSetting workerInitOption;
    private readonly int workerId;
    private readonly Messaging.MessageHandler messageHandler;

    public StartWorkerTask(IJSUnmarshalledRuntime runtime, IJSUnmarshalledObjectReference module, WorkerInitializeSetting workerInitOption, int workerId, Messaging.MessageHandler messageHandler)
    {
        this.runtime = runtime;
        this.module = module;
        this.workerInitOption = workerInitOption;
        this.workerId = workerId;
        this.messageHandler = messageHandler;
    }

    protected override void BeginAsyncInvoke(WorkerAwaiter workerAwaiter)
    {
        module.InvokeVoidUnmarshalledJson("CreateWorker", workerInitOption, workerId);
        messageHandler.RegisterInitializeAwaiter(workerId, workerAwaiter);
    }

    protected override void BlockingInvoke()
    {
        throw new NotImplementedException();
    }
}
