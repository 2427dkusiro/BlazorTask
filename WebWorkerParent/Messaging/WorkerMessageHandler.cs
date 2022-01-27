namespace BlazorTask.Messaging
{
    public class WorkerMessageHandler : MessageHandler
    {
        private JSRuntime.WorkerJSRuntime workerJSRuntime => JSRuntime.WorkerJSRuntime.Singleton;

        public WorkerMessageHandler(HandlerId id)
        {
            Id = id;
        }

        protected override void InvokeJSVoid(string name)
        {
            _ = workerJSRuntime.InvokeUnmarshalled<object?>(name);
        }

        protected override void InvokeJSVoid(string name, int arg0)
        {
            _ = workerJSRuntime.InvokeUnmarshalled<int, object?>(name, arg0);
        }
    }
}