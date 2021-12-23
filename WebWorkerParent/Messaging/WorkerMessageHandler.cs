namespace BlazorTask.Messaging
{
    public class WorkerMessageHandler : MessageHandler
    {
        private JSRuntime.WorkerJSRuntime workerJSRuntime { get => JSRuntime.WorkerJSRuntime.Singleton; }

        public WorkerMessageHandler(HandlerId id)
        {
            Id = id;
        }

        protected override void JSInvokeVoid(string name, int arg0)
        {
            _ = workerJSRuntime.InvokeUnmarshalled<int, object?>(name, arg0);
        }
    }
}