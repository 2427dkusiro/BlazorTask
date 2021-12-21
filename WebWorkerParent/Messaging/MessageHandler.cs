using BlazorTask.Dispatch;
using BlazorTask.Tasks;

using System.Text.Json;

namespace BlazorTask.Messaging
{
    public abstract class MessageHandler
    {
        private IntPtr buffer;
        private int bufferLength;

        public HandlerId Id { get; protected set; }

        protected abstract void JSInvokeVoid(string name);

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
                    OnReceiveCallStaticSerialized(id);
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

        internal unsafe void OnReceiveCallStaticSerialized(int id)
        {
            long callId = ((long)Id << 32) + id;
            int* ptr = (int*)buffer.ToPointer();
            int length = ptr[0];
            if (length < 20)
            {
                throw new InvalidOperationException();
            }

            int nameAddr = ptr[1];
            int nameLength = ptr[2];
            int argAddr = ptr[3];
            int argLength = ptr[4];

            SerializedDispatcher.CallStatic(new Span<char>((void*)nameAddr, nameLength / sizeof(char)), new Span<byte>((void*)argAddr, argLength), callId);
        }

        internal unsafe void OnReceiveResult(int workerId)
        {
            int* bufferPtr = (int*)buffer.ToPointer();
            int bufferpayload = bufferPtr[0];
            if (bufferpayload < 12)
            {
                throw new InvalidOperationException("buffer too short.");
            }
            int dataAddr = bufferPtr[1];
            int dataLen = bufferPtr[2];

            int* dataPtr = (int*)dataAddr;
            int payload = dataPtr[0];
            int callId = dataPtr[1];
            int resultType = dataPtr[2];

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

        public unsafe void ReturnResultVoid(int id)
        {
            var ptr = (int*)buffer.ToPointer();
            ptr[0] = 0;
            ptr[1] = id;
            ptr[2] = 0;
            ptr[0] = 12;
            JSInvokeVoid("ReturnVoidResult");
        }

        public unsafe void ReturnResultSerialized<T>(T value, int id)
        {
            var ptr = (int*)buffer.ToPointer();
            ptr[0] = 0;
            var json = JsonSerializer.SerializeToUtf8Bytes(value);
            fixed (void* jsonPtr = json)
            {
                ptr[1] = id;
                ptr[2] = 2;
                ptr[3] = (int)jsonPtr;
                ptr[4] = json.Length;
                ptr[0] = 20;
                JSInvokeVoid("ReturnResult");
            }
        }

        public unsafe void ReturnException(Exception exception, int id)
        {
            var ptr = (int*)buffer.ToPointer();
            ptr[0] = 0;
            var wrapped = new WorkerException(exception.Message, exception.StackTrace, exception.Source, exception.GetType().FullName);
            var json = JsonSerializer.SerializeToUtf8Bytes(wrapped);
            fixed (void* jsonPtr = json)
            {
                ptr[1] = id;
                ptr[2] = 4;
                ptr[3] = (int)jsonPtr;
                ptr[4] = json.Length;
                ptr[0] = 20;
                JSInvokeVoid("ReturnResult");
            }
        }
    }
}

/**
 * Worker Messaging Protocol
 * 
 * Message has the type such as "Init" , "SCall"...
 * Type provide the definition of the way to transfer data.
 * 
 * Message body is JS object.(or null)
 *  field "t" : message Type. 
 *  field "d" : transfer Data(option).
 *  field "i" : message Id(option).
 *  
 * For JS<=>C# interop, use 2 buffer.
 *  general buffer: fixed size buffer to put interop argumetnts.
 *  data buffer: flex size buffer to put data.
 * These buffer is instance-shared. And first 4 byte must be payload length not to read unexpected field.
 * 
 * Init : Worker => Parent
 *  Notify worker INITialization completed. This message has no body.
 * 
 * SCall : Parent => Worker 
 *  CALL method from Serialized arguments.
 *  Body:
 *   i:number method call ID.
 *   d:arrayBuffer[]
 *    [0]:arrayBuffer UTF-16 encoded method mame string. 
 *    [1]:arrayBuffer UTF-8 encoded json arguments.
 *       
 *  C#=>JS: Use general buffer 20 bytes.
 *   [0]:Int32 payload length(20).
 *   [1]:Int32 pointer to method name string.
 *   [2]:Int32 method name length in bytes.(2x larger than string length)
 *   [3]:Int32 pointer to json arguments.
 *   [4]:Int32 json arguments length in bytes.
 *   
 *  JS=>C#: Use general buffer 20 bytes and use data buffer.
 *   general buffer(same to C#=>JS):
 *    [0]:Int32 payload length(20).
 *    [1]:Int32 pointer to method name string in data buffer.
 *    [2]:Int32 method name length in bytes.(2x larger than string length)
 *    [3]:Int32 pointer to json arguments in data buffer.
 *    [4]:Int32 json arguments length in bytes. 
 *   data buffer:
 *    + method name.
 *    + json args.
 *    
 * Res : Worker => Parent
 *  Return method call RESult.
 *  Body:
 *   d:arraybuffer[]
 *    [0]:Int32 payload size.
 *    [1]:Int32 call ID.
 *    [2]:Int32 result type. 
 *     { 
 *       0 = execution succeeded but returned nothing. 
 *       1 = allocated.
 *       2 = execution succeeded and returned json value.
 *       3 = exception occured but no information.
 *       4 = exception occured and re-throw it as json.
 *     }
 *    [3]:Any returned value.(flex length)
 *    
 *   C#=>JS:
 *    when return void: use general buffer 12 bytes.
 *     [0]:Int32 payload length.
 *     [1]:Int32 call ID.
 *     [2]:Int32 result type.
 *
 *    when return something: use general buffer 20 bytes.
 *     [0]:Int32 payload length.
 *     [1]:Int32 call ID.
 *     [2]:Int32 result type.
 *     [3]:Int32 pointer to return value.
 *     [4]:Int32 return value length.
 */