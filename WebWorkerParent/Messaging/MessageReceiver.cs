using Microsoft.JSInterop;

namespace WebWorkerParent.Messaging
{
    public static class MessageReceiver
    {
        [JSInvokable]
        public static void ReceiveMessageFromJS(int id, string message)
        {
            Console.WriteLine($"message received,id={id},body={message}");
        }
    }
}
