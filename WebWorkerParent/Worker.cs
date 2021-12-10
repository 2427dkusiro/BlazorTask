using Microsoft.JSInterop.WebAssembly;

using System.Reflection;

using WebWorkerParent.Configure;
using WebWorkerParent.Tasks;

namespace WebWorkerParent;
/// <summary>
/// Web Worker APIによるワーカーを表現します。
/// </summary>
public class Worker : IAsyncDisposable
{
    private readonly WebAssemblyJSRuntime jSRuntime;
    private readonly IJSUnmarshalledObjectReference module;
    private readonly WorkerInitializeSetting workerInitializeSetting;

    const int bufferLength = 256;
    private IntPtr buffer = IntPtr.Zero;

    public Worker(WebAssemblyJSRuntime jSRuntime, IJSUnmarshalledObjectReference module, WorkerInitializeSetting workerInitializeSetting)
    {
        this.jSRuntime = jSRuntime;
        this.module = module;
        this.workerInitializeSetting = workerInitializeSetting;
    }

    private int workerId = -1;

    public WorkerTask Start()
    {
        if (workerId >= 0)
        {
            throw new InvalidOperationException("Worker is already started.");
        }
        StartWorkerTask task = new(jSRuntime, module, workerInitializeSetting, id => workerId = id);
        return task;
    }

    public Task Terminate()
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        //TODO: terminate worker and release related js resource.
        throw new NotImplementedException();
    }

    public SerializedCallWorkerTask Call(MethodInfo methodInfo, params object?[] args)
    {
        var json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(args);
        var name = GetMethodName(methodInfo);
        return SerializedCall(name, json);
    }

    private static string GetMethodName(MethodInfo methodInfo)
    {
        var type = methodInfo.DeclaringType;
        var asm = type.Assembly;
        return $"[{asm.GetName().Name}]{type.FullName}:{methodInfo.Name}";
    }

    private SerializedCallWorkerTask SerializedCall(string methodName, byte[] arg)
    {
        if (buffer == IntPtr.Zero)
        {
            buffer = module.InvokeUnmarshalled<int, int, object?, IntPtr>("AllocBuffer", workerId, bufferLength, null);
            if (buffer == IntPtr.Zero)
            {
                throw new InvalidOperationException("failed to alloc buffer");
            }
        }
        return new SerializedCallWorkerTask(jSRuntime, module, methodName, arg, workerId, buffer);
    }
}