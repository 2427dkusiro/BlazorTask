namespace WebWorkerParent.Tasks
{
    public sealed class StartWorkerTask : WorkerTask
    {
        private readonly IJSUnmarshalledRuntime runtime;
        private readonly IJSUnmarshalledObjectReference module;
        private readonly WorkerInitializeSetting workerInitOption;
        private readonly Action<int> onIdAssigned;

        public StartWorkerTask(IJSUnmarshalledRuntime runtime, IJSUnmarshalledObjectReference module, WorkerInitializeSetting workerInitOption, Action<int> onIdAssigned)
        {
            this.runtime = runtime;
            this.module = module;
            this.workerInitOption = workerInitOption;
            this.onIdAssigned = onIdAssigned;
        }

        protected override void BeginAsyncInvoke(WorkerAwaiter workerAwaiter)
        {
            var id = module.InvokeUnmarshalledJson<int, WorkerInitializeSetting>("CreateWorker", workerInitOption);
            onIdAssigned.Invoke(id);
            Messaging.MessageReceiver.RegisterInitializedAwait(id, workerAwaiter);
        }

        protected override void BlockingInvoke()
        {
            throw new NotImplementedException();
        }
    }
}
