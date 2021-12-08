using Microsoft.JSInterop.WebAssembly;

using WebWorkerParent.Configure;
using WebWorkerParent.Tasks;

namespace WebWorkerParent
{
    /// <summary>
    /// Web Worker APIによるワーカーを表現します。
    /// </summary>
    public class Worker : IAsyncDisposable
    {
        private readonly WebAssemblyJSRuntime jSRuntime;
        private readonly WorkerParentModule module;
        private readonly WorkerInitializeSetting workerInitializeSetting;

        public Worker(WebAssemblyJSRuntime jSRuntime, WorkerParentModule module, WorkerInitializeSetting workerInitializeSetting)
        {
            this.jSRuntime = jSRuntime;
            this.module = module;
            this.workerInitializeSetting = workerInitializeSetting;
        }

        private int workerId = -1;

        public WorkerTask Start()
        {
            if (workerId >= 0)
            {
                throw new InvalidOperationException("Worker is already started.");
            }
            StartWorkerTask task = new(jSRuntime, module.InternalModule, workerInitializeSetting, id => workerId = id);
            return task;
        }

        public Task Terminate()
        {
            throw new NotImplementedException();
        }

        public ValueTask DisposeAsync()
        {
            //TODO: terminate worker and release related js resource.
            throw new NotImplementedException();
        }

        /// <summary>
        /// デバッグ用仮実装。
        /// このメソッドのシグネチャは大きく変更される可能性があります。
        /// </summary>
        /// <returns></returns>
        public async Task _Call(string name)
        {
            await module.InternalModule.InvokeVoidAsync("_Call", workerId, "[SampleWorkerAssembly]SampleWorkerAssembly.Hoge:" + name);
        }
    }
}
