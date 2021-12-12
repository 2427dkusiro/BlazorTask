using BlazorTask.Tasks;

using System.Text.Json;

namespace BlazorTask.Messaging
{
    public static class StaticMessageHandler
    {
        private static int handlerId = 0;

        private static readonly Dictionary<int, MessageHandler> handlers = new();

        public static int JSCreateNew(int buffer, int bufferLength)
        {
            var handler = new WorkerMessageHandler(handlerId);
            handler.SetBuffer((nint)buffer, bufferLength);
            handlers.Add(handlerId++, handler);
            return handler.Id;
        }

        public static MessageHandler CreateNew(IJSUnmarshalledObjectReference module)
        {
            var handler = new WindowMessageHandler(handlerId, module);
            handlers.Add(handlerId++, handler);
            return handler;
        }

        public static void ReceiveMessage(int targetHandler, string type, int id)
        {
            handlers[targetHandler].ReceiveMessage(type, id);
        }

        public static void ReturnResultVoid(long id)
        {
            var insId = (int)(id >> 32);
            var id2 = (int)(id & uint.MaxValue);
            handlers[insId].ReturnResultVoid(id2);
        }

        public static void ReturnResultSerialized<T>(T value, long id)
        {
            var insId = (int)(id >> 32);
            var id2 = (int)(id & uint.MaxValue);
            handlers[insId].ReturnResultSerialized(value, id2);
        }
    }

    public abstract class MessageHandler
    {
        private IntPtr buffer;
        private int bufferLength;

        public int Id { get; protected set; }

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

        public void ReceiveMessage(string type, int id)
        {
            switch (type)
            {
                case "Init":
                    Console.WriteLine("Init:" + id.ToString());
                    // 連打すると二回来ちゃうっぽい
                    workerInitAwaiters[id].SetResult();
                    workerInitAwaiters.Remove(id);
                    return;
                case "SCall":
                    long callId = ((long)Id << 32) + id;
                    CallStaticSerialized(callId);
                    return;
            }
        }

        public unsafe void CallStaticSerialized(long callId)
        {
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

            WorkerImplements.Dispatch.SerializedDispatcher.CallStatic(new Span<char>((void*)nameAddr, nameLength / sizeof(char)), new Span<byte>((void*)argAddr, argLength), callId);
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
                ptr[2] = 1;
                ptr[3] = (int)jsonPtr;
                ptr[4] = json.Length;
                ptr[0] = 20;
                JSInvokeVoid("ReturnSerializedResult");
            }
        }
    }

    public class WorkerMessageHandler : MessageHandler
    {
        private readonly WorkerImplements.JSRuntime.WorkerJSRuntime workerJSRuntime; // = WorkerImplements.JSRuntime.WorkerJSRuntime.Singleton;

        public WorkerMessageHandler(int id)
        {
            Id = id;
        }

        protected override void JSInvokeVoid(string name)
        {
            _ = workerJSRuntime.InvokeUnmarshalled<object?>(name);
        }
    }

    public class WindowMessageHandler : MessageHandler
    {
        private readonly IJSUnmarshalledObjectReference module;

        public WindowMessageHandler(int id, IJSUnmarshalledObjectReference jSUnmarshalledObjectReference)
        {
            module = jSUnmarshalledObjectReference;
            Id = id;
        }

        protected override void JSInvokeVoid(string name)
        {
            module.InvokeUnmarshalled<object?>(name);
        }
    }
}
