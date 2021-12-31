using BlazorTask.Dispatch;
using BlazorTask.Messaging;
using BlazorTask.Tasks;

using System.Reflection;

namespace BlazorTask;

public static class WorkerContext
{
    private static WorkerParent workerParent;
    public static WorkerParent Parent { get => workerParent ??= new WorkerParent(); }

    private static JSRuntime.WorkerJSRuntime workerJSRuntime;
    public static IJSUnmarshalledRuntime WorkerJSRuntime { get => workerJSRuntime ??= JSRuntime.WorkerJSRuntime.Singleton; }

    private static HttpClient httpClient;

    public static HttpClient HttpClient { get => httpClient ??= new HttpClient(); }
}

public class WorkerParent : ICallProvider
{
    private IJSUnmarshalledRuntime? _jSRuntime;
    private IJSUnmarshalledRuntime JSRuntime { get => _jSRuntime ??= BlazorTask.JSRuntime.WorkerJSRuntime.Singleton; }

    private MessageHandler? _messageHandler;
    private MessageHandler MessageHandler { get => _messageHandler ??= MessageHandlerManager.GetHandler(HandlerId.ThisContext); }

    public WorkerTask Call(MethodInfo methodInfo, params object?[] args)
    {
        return SerializedCall(methodInfo.ToIdentifier(), System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(args));
    }

    public WorkerTask<T> Call<T>(MethodInfo methodInfo, params object?[] args)
    {
        return SerializedCall<T>(methodInfo.ToIdentifier(), System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(args));
    }

    private SerializedCallWorkerTask SerializedCall(string methodName, byte[] arg)
    {
        var header = new CallHeader(CallHeader.CallType.Default);
        return new SerializedCallWorkerTask(JSRuntime, header, methodName, arg, 0, MessageHandler);
    }

    private SerializedCallWorkerTask<T> SerializedCall<T>(string methodName, byte[] arg)
    {
        var header = new CallHeader(CallHeader.CallType.Default);
        return new SerializedCallWorkerTask<T>(JSRuntime, header, methodName, arg, 0, MessageHandler);
    }
}
