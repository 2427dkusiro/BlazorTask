using BlazorTask.Configure;
using BlazorTask.Messaging;
using BlazorTask.Tasks;

using Microsoft.JSInterop.WebAssembly;

using System.Reflection;

namespace BlazorTask;
/// <summary>
/// Represents a worker. 
/// </summary>
public class Worker : IDisposable
{
    private readonly WebAssemblyJSRuntime jSRuntime;
    private readonly IJSUnmarshalledObjectReference module;
    private readonly WorkerInitializeSetting workerInitializeSetting;

    private readonly IntPtr buffer;
    private readonly int bufferLength;

    private readonly MessageHandler messageHandler;

    /// <summary>
    /// Create a new instance of <see cref="Worker"/>.
    /// </summary>
    /// <param name="jSRuntime"></param>
    /// <param name="module"></param>
    /// <param name="workerInitializeSetting"></param>
    public Worker(WebAssemblyJSRuntime jSRuntime, IJSUnmarshalledObjectReference module, WorkerInitializeSetting workerInitializeSetting, IntPtr buffer, int bufferLength, MessageHandler messageHandler)
    {
        this.jSRuntime = jSRuntime;
        this.module = module;
        this.workerInitializeSetting = workerInitializeSetting;
        this.buffer = buffer;
        this.bufferLength = bufferLength;
        this.messageHandler = messageHandler;
    }

    private int workerId = -1;
    private bool disposedValue;

    /// <summary>
    /// Start this worker.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public WorkerTask Start()
    {
        if (workerId >= 0)
        {
            throw new InvalidOperationException("Worker is already started.");
        }
        StartWorkerTask task = new(jSRuntime, module, workerInitializeSetting, id => workerId = id, messageHandler);
        return task;
    }

    /// <summary>
    /// Stop this worker.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public void Terminate()
    {
        module.InvokeUnmarshalled<int, object?>("TerminateWorker", workerId);
    }

    /// <summary>
    /// Call a method at the worker context.
    /// </summary>
    /// <param name="methodInfo">method to call.</param>
    /// <param name="args">Arguments to pass to worker. This arguments will be json-serialized.</param>
    /// <returns></returns>
    public WorkerTask Call(MethodInfo methodInfo, params object?[] args)
    {
        var json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(args);
        var name = GetMethodName(methodInfo);
        return SerializedCall(name, json);
    }

    /// <summary>
    /// Call a method at the worker context.
    /// </summary>
    /// <param name="methodInfo">method to call.</param>
    /// <param name="args">Arguments to pass to worker. This arguments will be json-serialized.</param>
    /// <returns></returns>
    public WorkerTask<T> Call<T>(MethodInfo methodInfo, params object?[] args)
    {
        var json = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(args);
        var name = GetMethodName(methodInfo);
        return SerializedCall<T>(name, json);
    }

    private static string GetMethodName(MethodInfo methodInfo)
    {
        var type = methodInfo.DeclaringType ?? throw new InvalidOperationException("Method had no declaring type.");
        var asm = type.Assembly;
        return $"[{asm.GetName().Name}]{type.FullName}:{methodInfo.Name}";
    }

    private SerializedCallWorkerTask SerializedCall(string methodName, byte[] arg)
    {
        return new SerializedCallWorkerTask(jSRuntime, module, methodName, arg, workerId, buffer, bufferLength, messageHandler);
    }

    private SerializedCallWorkerTask<T> SerializedCall<T>(string methodName, byte[] arg)
    {
        return new SerializedCallWorkerTask<T>(jSRuntime, module, methodName, arg, workerId, buffer, bufferLength, messageHandler);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {

            }
            Terminate();

            disposedValue = true;
        }
    }

    ~Worker()
    {
        // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}