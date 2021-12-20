namespace BlazorTask.Messaging
{
    public static class MessageHandlerManager
    {
        // message handler to parent. In worker context, message to parent. otherwise(in window context), null.
        private static MessageHandler? parentMessageHandler;

        private static MessageHandler? workerModuleMessageHandler;

        public static int CreateAtThisContext(int buffer, int bufferLength)
        {
            if (parentMessageHandler is not null)
            {
                throw new InvalidOperationException("Message handler already exists.");
            }

            var handler = new WorkerMessageHandler(HandlerId.ThisContext);
            handler.SetBuffer((nint)buffer, bufferLength);
            parentMessageHandler = handler;
            return (int)HandlerId.ThisContext;
        }

        public static HandlerId CreateAtWorkerModuleContext(IJSUnmarshalledObjectReference module)
        {
            if (workerModuleMessageHandler is not null)
            {
                throw new InvalidOperationException("Message handler already exists.");
            }

            var handler = new WindowMessageHandler(HandlerId.WorkerContext, module);
            workerModuleMessageHandler = handler;
            return HandlerId.WorkerContext;
        }

        public static MessageHandler GetHandler(HandlerId handlerId)
        {
            return handlerId switch
            {
                HandlerId.ThisContext => parentMessageHandler ?? throw new InvalidOperationException("MessageHandler is not set."),
                HandlerId.WorkerContext => workerModuleMessageHandler ?? throw new InvalidOperationException("MessageHandler is not set."),
                _ => throw new NotSupportedException(),
            };
        }

        public static void ReceiveMessage(int targetHandler, string type, int id)
        {
#if DEBUG
            Console.WriteLine($"Message type:{type},target:{targetHandler},id:{id}");
#endif
            GetHandler(GetHandlerFromId(targetHandler)).ReceiveMessage(type, id);
        }

        public static void ReturnResultVoid(long id)
        {
            var handler = GetHandlerFromId((int)(id >> 32));
            var resultId = (int)(id & uint.MaxValue);
            GetHandler(handler).ReturnResultVoid(resultId);
        }

        public static void ReturnResultSerialized<T>(T value, long id)
        {
            Console.WriteLine($"return id={id.ToString("x")}");
            Console.WriteLine(value);

            var handler = GetHandlerFromId((int)(id >> 32));
            var resultId = (int)(id & uint.MaxValue);
            GetHandler(handler).ReturnResultSerialized(value, resultId);
        }

        public static void ReturnException(Exception exception, long id)
        {
            var handler = GetHandlerFromId((int)(id >> 32));
            var resultId = (int)(id & uint.MaxValue);
            GetHandler(handler).ReturnException(exception, resultId);
        }

        private static HandlerId GetHandlerFromId(int id)
        {
            return id switch
            {
                0 => throw new NullReferenceException("passed id is null value(0)."),
                1 => HandlerId.ThisContext,
                2 => HandlerId.WorkerContext,
                _ => throw new ArgumentOutOfRangeException(nameof(id), "Invalid id. Id is out of enum range."),
            };
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