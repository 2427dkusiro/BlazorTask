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
            HandlerId handler = GetHandlerFromId((int)(id >> 32));
            var resultId = (int)(id & uint.MaxValue);
            GetHandler(handler).ReturnResultVoid(resultId);
        }

        public static void ReturnResultSerialized<T>(T value, long id)
        {
            HandlerId handler = GetHandlerFromId((int)(id >> 32));
            var resultId = (int)(id & uint.MaxValue);
            GetHandler(handler).ReturnResultSerialized(value, resultId);
        }

        public static void ReturnException(Exception exception, long id)
        {
            HandlerId handler = GetHandlerFromId((int)(id >> 32));
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