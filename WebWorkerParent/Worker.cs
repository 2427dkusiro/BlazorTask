using Microsoft.JSInterop;

namespace WebWorkerParent
{
    /// <summary>
    /// Web Worker APIによるワーカーを表現します。
    /// </summary>
    public class Worker
    {
        private readonly HttpClient httpClient;
        private readonly IJSRuntime jSRuntime;
        private readonly IJSObjectReference jSModule;

        private readonly string bootJsonPath = "./_framework/blazor.boot.json";

        public Worker(HttpClient httpClient, IJSRuntime jSRuntime, IJSObjectReference jSModule)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.jSRuntime = jSRuntime ?? throw new ArgumentNullException(nameof(jSRuntime));
            this.jSModule = jSModule ?? throw new ArgumentNullException(nameof(jSModule));
        }

        private int workerId = -1;

        public async Task Start()
        {
            WorkerInitOption workerInitOption = WorkerInitOption.Default;
            var asm = await Utility.AssemblyResolver.GetAssembliesFromBootJson(httpClient, bootJsonPath);
            workerInitOption.Assemblies.AddRange(asm);

            await Start(workerInitOption);
        }

        public async Task Start(WorkerInitOption workerInitOption)
        {
            workerId = await StartInternal(workerInitOption);
        }

        private async Task<int> StartInternal(WorkerInitOption workerInitOption)
        {
            if (workerId >= 0)
            {
                throw new InvalidOperationException("既にワーカーは起動しています。");
            }
            return await WorkerBuilder.CreateNew(jSModule, workerInitOption);
        }

        public Task Terminate()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// デバッグ用仮実装。
        /// このメソッドのシグネチャは大きく変更される可能性があります。
        /// </summary>
        /// <returns></returns>
        public async Task _Call(string name)
        {
            await jSModule.InvokeVoidAsync("_Call", workerId, "[SampleWorkerAssembly]SampleWorkerAssembly.Hoge:" + name);
        }
    }
}
