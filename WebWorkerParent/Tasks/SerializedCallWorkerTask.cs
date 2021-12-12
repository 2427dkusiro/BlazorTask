namespace BlazorTask.Tasks;

public sealed class SerializedCallWorkerTask : WorkerTask
{
    private static uint callId = 0;

    private readonly IJSUnmarshalledRuntime runtime;
    private readonly IJSUnmarshalledObjectReference module;

    private readonly string methodName;
    private readonly byte[] args;

    private readonly int workerId;
    private readonly IntPtr buffer;

    public SerializedCallWorkerTask(IJSUnmarshalledRuntime runtime, IJSUnmarshalledObjectReference module, string methodName, byte[] args, int workerId, IntPtr buffer)
    {
        this.runtime = runtime;
        this.module = module;
        this.methodName = methodName;
        this.args = args;
        this.workerId = workerId;
        this.buffer = buffer;
    }

    protected override unsafe void BeginAsyncInvoke(WorkerAwaiter workerAwaiter)
    {
        fixed (char* methodNamePtr = methodName)
        {
            fixed (byte* argPtr = args)
            {
                nint* ptr = (nint*)buffer.ToPointer();
                ptr[0] = (nint)methodNamePtr;
                ptr[1] = methodName.Length * sizeof(char);
                ptr[2] = (nint)argPtr;
                ptr[3] = args.Length;

                _ = module.InvokeUnmarshalled<int, int, uint, object?>("SCall", workerId, sizeof(nint) * 4, callId++);
            }
        }
        workerAwaiter.SetResult();
    }

    protected override void BlockingInvoke()
    {
        throw new NotImplementedException();
    }
}