using BlazorTask.Dispatch;
using BlazorTask.Tasks;

using System.Runtime.CompilerServices;
using System.Text.Json;

namespace BlazorTask.Messaging;

/// <summary>
/// Represent a common implements of message handler.
/// </summary>
public abstract class MessageHandler
{
    private IntPtr buffer;
    private int bufferLength;

    /// <summary>
    /// Get a ID of this <see cref="MessageHandler">.
    /// </summary>
    public HandlerId Id { get; protected set; }

    /// <summary>
    /// If be override in inherited class, invoke javascript.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="arg0"></param>
    protected abstract void InvokeJSVoid(string name);

    /// <summary>
    /// If be override in inherited class, invoke javascript.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="arg0"></param>
    protected abstract void InvokeJSVoid(string name, int arg0);

    /// <summary>
    /// Set the buffer address and its length.
    /// </summary>
    /// <param name="buffer">Start address of buffer.</param>
    /// <param name="bufferLength">Length of buffer in bytes.</param>
    public void SetBuffer(IntPtr buffer, int bufferLength)
    {
        this.buffer = buffer;
        this.bufferLength = bufferLength;
    }

    private readonly Dictionary<int, WorkerAwaiter> workerInitAwaiters = new();

    /// <summary>
    /// Registor a awaiter in order to wait worker initialization.
    /// </summary>
    /// <param name="id">worker id to wait.</param>
    /// <param name="awaiter"></param>
    public void RegistorInitializeAwaiter(int id, WorkerAwaiter awaiter)
    {
        workerInitAwaiters.Add(id, awaiter);
    }

    private readonly Dictionary<int, IAsyncResultToken> callResultTokens = new();

    /// <summary>
    /// Registor a awaiter in order to wait method call.
    /// </summary>
    /// <param name="callId"></param>
    /// <param name="token"></param>
    public void RegisterCallResultToken(int callId, IAsyncResultToken token)
    {
        callResultTokens.Add(callId, token);
    }

    /// <summary>
    /// This method is expected to be called from JS only.
    /// Should not call this method from C# code.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="id"></param>
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
                OnReceiveResult();
                return;
        }
    }

    /// <summary>
    /// Handle a message which notify completion of worker initialization.
    /// </summary>
    /// <param name="id"></param>
    internal void OnReceiveInitialized(int id)
    {
        workerInitAwaiters[id].SetResult();
        workerInitAwaiters.Remove(id);
    }

    private int _callReceiveId = 0;

    /// <summary>
    /// Get a unique id of received method call request.
    /// </summary>
    internal int CallReceiveId { get => _callReceiveId++; }

    private readonly Dictionary<int, (int sourceId, CallHeader header)> headers = new();

    /// <summary>
    /// Handle a message which requests calling method.
    /// </summary>
    /// <param name="sourceId">ID which represent the source of this request.</param>
    /// <exception cref="InvalidOperationException"></exception>
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

    /// <summary>
    /// Handle a message which notify method call result received.
    /// </summary>
    /// <param name="workerId">source of this message.</param>
    /// <exception cref="InvalidOperationException"></exception>
    internal unsafe void OnReceiveResult()
    {
        switch (OnReceiveResultCommon(out Span<byte> resultType, out var callId))
        {
            case CallResultTypes.SuccessedVoid:
                callResultTokens[callId].SetResultFromJson(resultType);
                return;
            case CallResultTypes.SuccessedJson:
                callResultTokens[callId].SetResultFromJson(resultType);
                return;
            case CallResultTypes.Exception:
                callResultTokens[callId].SetException(resultType);
                return;
        }
    }

    private unsafe CallResultTypes OnReceiveResultCommon(out Span<byte> span, out int callId)
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
        callId = dataPtr[1];
        var resultType = (CallResultTypes)dataPtr[2];

        switch (resultType)
        {
            case CallResultTypes.SuccessedVoid:
                span = null;
                break;
            case CallResultTypes.SuccessedJson:
            case CallResultTypes.Exception:
                span = new Span<byte>(dataPtr + 3, payload - 12);
                break;
            default:
                throw new NotSupportedException();
        }
        return resultType;
    }

    private unsafe void CallCommom(CallHeader callHeader, string methodName, byte[] args, int workerId)
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

                InvokeJSVoid("SCall", workerId);
            }
        }
    }

    /// <summary>
    /// Call method from json serialized arguments.
    /// </summary>
    /// <param name="callHeader"></param>
    /// <param name="methodName"></param>
    /// <param name="args"></param>
    /// <param name="workerId"></param>
    /// <param name="workerAwaiter"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void CallSerializedSync(CallHeader callHeader, string methodName, byte[] args, int workerId)
    {
        CallCommom(callHeader, methodName, args, workerId);
        GetSyncCallResult(callHeader.callId);
    }

    /// <summary>
    /// Call method from json serialized arguments.
    /// </summary>
    /// <param name="callHeader"></param>
    /// <param name="methodName"></param>
    /// <param name="args"></param>
    /// <param name="workerId"></param>
    /// <param name="workerAwaiter"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public T CallSerializedSync<T>(CallHeader callHeader, string methodName, byte[] args, int workerId)
    {
        CallCommom(callHeader, methodName, args, workerId);
        return GetSyncCallResult<T>(callHeader.callId);
    }

    /// <summary>
    /// Call method from json serialized arguments.
    /// </summary>
    /// <param name="callHeader"></param>
    /// <param name="methodName"></param>
    /// <param name="args"></param>
    /// <param name="workerId"></param>
    /// <param name="workerAwaiter"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public unsafe void CallSerialized(CallHeader callHeader, string methodName, byte[] args, int workerId, WorkerAwaiter workerAwaiter)
    {
        CallCommom(callHeader, methodName, args, workerId);
        var token = new CallResultToken(workerAwaiter);
        RegisterCallResultToken(callHeader.callId, token);
    }

    public unsafe void CallSerialized<T>(CallHeader callHeader, string methodName, byte[] args, int workerId, WorkerAwaiter<T?> workerAwaiter)
    {
        CallCommom(callHeader, methodName, args, workerId);
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
        ptr[2] = (int)CallResultTypes.SuccessedVoid;
        ptr[0] = 12;
        if (((int)header.callType & (int)CallHeader.CallType.Sync) == 0)
        {
            DebugHelper.Debugger.CheckPoint();
            InvokeJSVoid("ReturnVoidResult", source);
        }
        else
        {
            DebugHelper.Debugger.CheckPoint();
            InvokeJSVoid("ReturnVoidResultSync");
        }
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
            ptr[2] = (int)CallResultTypes.SuccessedJson;
            ptr[3] = (int)jsonPtr;
            ptr[4] = json.Length;
            ptr[0] = 20;
            if (((int)header.callType & (int)CallHeader.CallType.Sync) == 0)
            {
                DebugHelper.Debugger.CheckPoint();
                InvokeJSVoid("ReturnResult", source);
            }
            else
            {
                DebugHelper.Debugger.CheckPoint();
                InvokeJSVoid("ReturnResultSync");
            }
        }
    }

    public unsafe void ReturnException(Exception exception, int resultId)
    {
        DebugHelper.Debugger.WriteMessage($"a exception was thrown:{exception}");

        if (bufferLength < 20)
        {
            throw new InvalidOperationException("Buffer too short");
        }
        (var source, CallHeader header) = headers[resultId];

        var ptr = (int*)buffer.ToPointer();
        ptr[0] = 0;
        var wrapped = new WorkerException(exception.Message, exception.StackTrace, null, exception.GetType().FullName);
        var json = JsonSerializer.SerializeToUtf8Bytes(wrapped);
        fixed (void* jsonPtr = json)
        {
            ptr[1] = header.callId;
            ptr[2] = (int)CallResultTypes.Exception;
            ptr[3] = (int)jsonPtr;
            ptr[4] = json.Length;
            ptr[0] = 20;
            if (((int)header.callType & (int)CallHeader.CallType.Sync) == 0)
            {
                DebugHelper.Debugger.CheckPoint();
                InvokeJSVoid("ReturnResult", source);
            }
            else
            {
                DebugHelper.Debugger.CheckPoint();
                InvokeJSVoid("ReturnResultSync");
            }
        }
    }

    public unsafe int GetSyncCallSourceId()
    {
        InvokeJSVoid("AssignSyncCallSourceId");
        var ptr = (int*)buffer.ToPointer();
        var length = ptr[0];
        if (length < 8)
        {
            throw new InvalidOperationException("Buffer too short");
        }

        return ptr[1];
    }

    public void GetSyncCallResult(int id)
    {
        InvokeJSVoid("WaitSyncCall", id);
        CallResultTypes type = OnReceiveResultCommon(out Span<byte> span, out var callId);
        DebugHelper.Debugger.Assert(id == callId);
        switch (type)
        {
            case CallResultTypes.SuccessedVoid:
            case CallResultTypes.SuccessedJson:
                return;
            case CallResultTypes.Exception:
                throw JsonSerializer.Deserialize<WorkerException>(span)!;
            default:
                throw new NotSupportedException();
        }
    }

    public T GetSyncCallResult<T>(int id)
    {
        InvokeJSVoid("WaitSyncCall", id);
        CallResultTypes type = OnReceiveResultCommon(out Span<byte> span, out var callId);
        DebugHelper.Debugger.Assert(id == callId);
        return type switch
        {
            CallResultTypes.SuccessedVoid => throw new InvalidOperationException("Worker returned void result."),
            CallResultTypes.SuccessedJson => JsonSerializer.Deserialize<T>(span)!,
            CallResultTypes.Exception => throw JsonSerializer.Deserialize<WorkerException>(span)!,
            _ => throw new NotSupportedException(),
        };
    }
}