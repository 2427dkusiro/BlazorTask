using Microsoft.JSInterop;
using Microsoft.JSInterop.WebAssembly;

using System.Text.Json;

namespace WebWorkerParent
{
    /// <summary>
    /// Web Worker APIによるワーカーを表現します。
    /// </summary>
    public class Worker
    {
        private readonly HttpClient httpClient;
        private readonly WebAssemblyJSRuntime jSRuntime;
        private readonly IJSUnmarshalledObjectReference jSModule;

        private readonly string bootJsonPath = "./_framework/blazor.boot.json";

        public Worker(HttpClient httpClient, WebAssemblyJSRuntime jSRuntime, IJSUnmarshalledObjectReference jSModule)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.jSRuntime = jSRuntime ?? throw new ArgumentNullException(nameof(jSRuntime));
            this.jSModule = jSModule ?? throw new ArgumentNullException(nameof(jSModule));
        }

        private int workerId = -1;

        public async ValueTask Start()
        {
            var asm = await Utility.AssemblyResolver.GetAssembliesFromBootJson(httpClient, bootJsonPath);
            var dotnetJS = await Utility.AssemblyResolver.GetDotnetJSFromBootJson(httpClient, bootJsonPath);
            var workerInitOption = WorkerInitOption.Default with { DotnetJsName = dotnetJS, Assemblies = asm };

            workerId = StartInternal(workerInitOption);
        }

        public async ValueTask Start(WorkerInitOption workerInitOption)
        {
            workerId = StartInternal(workerInitOption);
        }

        private int StartInternal(WorkerInitOption workerInitOption)
        {
            if (workerId >= 0)
            {
                throw new InvalidOperationException("既にワーカーは起動しています。");
            }
            return jSModule.InvokeUnmarshalledJson<int, WorkerInitOption>("CreateWorker", workerInitOption);
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
