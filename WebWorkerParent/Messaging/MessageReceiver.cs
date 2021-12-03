using WebWorkerParent.Tasks;

namespace WebWorkerParent.Messaging
{
    public static class MessageReceiver
    {
        [JSInvokable]
        public static void ReceiveMessageFromJS(int id, string message)
        {
            Console.WriteLine($"message received,id={id},body={message}");
        }

        private static readonly Dictionary<int, WorkerAwaiter> workerInitCallback = new();

        public static void RegisterInitializedAwait(int id, WorkerAwaiter awaiter)
        {
            workerInitCallback.Add(id, awaiter);
        }

        [JSInvokable]
        public static void NotifyWorkerInitialized(int id)
        {
            workerInitCallback[id].SetResult();
            workerInitCallback.Remove(id);
        }
    }
}
