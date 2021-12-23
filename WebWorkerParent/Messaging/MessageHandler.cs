using BlazorTask.Dispatch;
using BlazorTask.Tasks;

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace BlazorTask.Messaging;

public abstract class MessageHandler
{
    private IntPtr buffer;
    private int bufferLength;

    public HandlerId Id { get; protected set; }

    protected abstract void JSInvokeVoid(string name, int arg0);

    public void SetBuffer(IntPtr buffer, int bufferLength)
    {
        this.buffer = buffer;
        this.bufferLength = bufferLength;
    }

    private readonly Dictionary<int, WorkerAwaiter> workerInitAwaiters = new();
    public void RegisterInitializeAwaiter(int id, WorkerAwaiter awaiter)
    {
        workerInitAwaiters.Add(id, awaiter);
    }

    private readonly Dictionary<int, ICallResultToken> callResultTokens = new();
    public void RegisterCallResultToken(int callId, ICallResultToken token)
    {
        callResultTokens.Add(callId, token);
    }

    public void ReceiveMessage(string type, int id)
    {
        switch (type)
        {
            case "Init":
                OnReceiveInitialized(id);
                return;
            case "SCall":
                OnReceiveCall(id);
                return;
            case "Res":
                OnReceiveResult(id);
                return;
        }
    }

    internal void OnReceiveInitialized(int id)
    {
        workerInitAwaiters[id].SetResult();
        workerInitAwaiters.Remove(id);
    }

    private int _callReceiveId = 0;
    internal int CallReceiveId { get => _callReceiveId++; }

    private Dictionary<int, (int sourceId, CallHeader header)> headers = new();

    internal unsafe void OnReceiveCall(int sourceId)
    {
        var callId = ((long)Id << 32) + sourceId;
        var ptr = (int*)buffer.ToPointer();
        var length = ptr[0];
        if (length < 20)
        {
            throw new InvalidOperationException("Buffer too short");
        }

        var headerAddr = ptr[1];
        var headerLength = ptr[2];
        var argAddr = ptr[3];
        var argLength = ptr[4];

        var headerPtr = (byte*)headerAddr;
        ref CallHeader header = ref Unsafe.AsRef<CallHeader>(headerPtr);
        var nameBin = new Span<char>(headerPtr + header.payloadLength, (headerLength - header.payloadLength) / sizeof(char));

        var resultId = CallReceiveId;
        headers.Add(resultId, (sourceId, header));
        var id = (((long)Id) << 32) + resultId;

        SerializedDispatcher.CallStatic(ref header, nameBin, new Span<byte>((void*)argAddr, argLength), id);
    }

    internal unsafe void OnReceiveResult(int workerId)
    {
        var bufferPtr = (int*)buffer.ToPointer();
        var bufferpayload = bufferPtr[0];
        if (bufferpayload < 12)
        {
            throw new InvalidOperationException("buffer too short.");
        }
        var dataAddr = bufferPtr[1];
        var dataLen = bufferPtr[2];

        var dataPtr = (int*)dataAddr;
        var payload = dataPtr[0];
        var callId = dataPtr[1];
        var resultType = dataPtr[2];

        switch (resultType)
        {
            case 0:
                callResultTokens[callId].SetResultFromJson(null);
                return;
            case 2:
                callResultTokens[callId].SetResultFromJson(new Span<byte>(dataPtr + 3, payload - 12));
                return;
            case 4:
                callResultTokens[callId].SetException(new Span<byte>(dataPtr + 3, payload - 12));
                return;
        }
    }

    public unsafe void CallSerialized(CallHeader callHeader, string methodName, byte[] args, int workerId, WorkerAwaiter workerAwaiter)
    {
        if (bufferLength < 28)
        {
            throw new InvalidOperationException("Buffer too short");
        }
        fixed (char* methodNamePtr = methodName)
        {
            fixed (byte* argPtr = args)
            {
                var ptr = (int*)buffer.ToPointer();
                ptr[0] = 0;
                ptr[1] = (int)&callHeader;
                ptr[2] = callHeader.payloadLength;
                ptr[3] = (int)methodNamePtr;
                ptr[4] = methodName.Length * sizeof(char);
                ptr[5] = (int)argPtr;
                ptr[6] = args.Length;
                ptr[0] = 28;

                JSInvokeVoid("SCall", workerId);
            }
        }
        var token = new CallResultToken(workerAwaiter);
        RegisterCallResultToken(callHeader.callId, token);
    }

    public unsafe void CallSerialized<T>(CallHeader callHeader, string methodName, byte[] args, int workerId, WorkerAwaiter<T> workerAwaiter)
    {
        if (bufferLength < 28)
        {
            throw new InvalidOperationException("Buffer too short");
        }
        fixed (char* methodNamePtr = methodName)
        {
            fixed (byte* argPtr = args)
            {
                var ptr = (int*)buffer.ToPointer();
                ptr[0] = 0;
                ptr[1] = (int)&callHeader;
                ptr[2] = callHeader.payloadLength;
                ptr[3] = (int)methodNamePtr;
                ptr[4] = methodName.Length * sizeof(char);
                ptr[5] = (int)argPtr;
                ptr[6] = args.Length;
                ptr[0] = 28;

                JSInvokeVoid("SCall", workerId);
            }
        }
        var token = new CallResultToken<T>(workerAwaiter);
        RegisterCallResultToken(callHeader.callId, token);
    }

    public unsafe void ReturnResultVoid(int resultId)
    {
        if (bufferLength < 12)
        {
            throw new InvalidOperationException("Buffer too short");
        }
        (var source, CallHeader header) = headers[resultId];

        var ptr = (int*)buffer.ToPointer();
        ptr[0] = 0;
        ptr[1] = header.callId;
        ptr[2] = 0;
        ptr[0] = 12;
        JSInvokeVoid("ReturnVoidResult", source);
    }

    public unsafe void ReturnResultSerialized<T>(T value, int resultId)
    {
        if (bufferLength < 20)
        {
            throw new InvalidOperationException("Buffer too short");
        }
        (var source, CallHeader header) = headers[resultId];

        var ptr = (int*)buffer.ToPointer();
        ptr[0] = 0;
        var json = JsonSerializer.SerializeToUtf8Bytes(value);
        fixed (void* jsonPtr = json)
        {
            ptr[1] = header.callId;
            ptr[2] = 2;
            ptr[3] = (int)jsonPtr;
            ptr[4] = json.Length;
            ptr[0] = 20;
            JSInvokeVoid("ReturnResult", source);
        }
    }

    public unsafe void ReturnException(Exception exception, int resultId)
    {
        Console.WriteLine($"a exception was thrown:{exception}");

        if (bufferLength < 20)
        {
            throw new InvalidOperationException("Buffer too short");
        }
        (var source, CallHeader header) = headers[resultId];

        var ptr = (int*)buffer.ToPointer();
        ptr[0] = 0;
        var wrapped = new WorkerException(exception.Message, exception.StackTrace, exception.Source, exception.GetType().FullName);
        var json = JsonSerializer.SerializeToUtf8Bytes(wrapped);
        fixed (void* jsonPtr = json)
        {
            ptr[1] = header.callId;
            ptr[2] = 4;
            ptr[3] = (int)jsonPtr;
            ptr[4] = json.Length;
            ptr[0] = 20;
            JSInvokeVoid("ReturnResult", source);
        }
    }
}