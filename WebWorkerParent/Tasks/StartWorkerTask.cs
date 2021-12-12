using BlazorTask.Configure;

namespace BlazorTask.Tasks;

public sealed class StartWorkerTask : WorkerTask
{
    private readonly IJSUnmarshalledRuntime runtime;
    private readonly IJSUnmarshalledObjectReference module;
    private readonly WorkerInitializeSetting workerInitOption;
    private readonly Action<int> onIdAssigned;
    private readonly Messaging.MessageHandler messageHandler;

    public StartWorkerTask(IJSUnmarshalledRuntime runtime, IJSUnmarshalledObjectReference module, WorkerInitializeSetting workerInitOption, Action<int> onIdAssigned, Messaging.MessageHandler messageHandler)
    {
        this.runtime = runtime;
        this.module = module;
        this.workerInitOption = workerInitOption;
        this.onIdAssigned = onIdAssigned;
        this.messageHandler = messageHandler;
    }

    protected override void BeginAsyncInvoke(WorkerAwaiter workerAwaiter)
    {
        var id = module.InvokeUnmarshalledJson<int, WorkerInitializeSetting>("CreateWorker", workerInitOption);
        messageHandler.RegisterInitializeAwaiter(id, workerAwaiter);
        onIdAssigned.Invoke(id);
    }

    protected override void BlockingInvoke()
    {
        throw new NotImplementedException();
    }
}
