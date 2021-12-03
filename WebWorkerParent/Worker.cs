using Microsoft.JSInterop.WebAssembly;

using WebWorkerParent.Tasks;
using WebWorkerParent.Utility;

namespace WebWorkerParent
{
    /// <summary>
    /// Web Worker APIによるワーカーを表現します。
    /// </summary>
    public class Worker
    {
        private readonly IResourceResolver resourceResolver;
        private readonly WebAssemblyJSRuntime jSRuntime;
        private readonly IJSUnmarshalledObjectReference jSModule;

        // TODO:外挿させる
        private readonly string decoderPath = "decode.min.js";

        public Worker(IResourceResolver resourceResolver, WebAssemblyJSRuntime jSRuntime, IJSUnmarshalledObjectReference jSModule)
        {
            this.resourceResolver = resourceResolver ?? throw new ArgumentNullException(nameof(resourceResolver));
            this.jSRuntime = jSRuntime ?? throw new ArgumentNullException(nameof(jSRuntime));
            this.jSModule = jSModule ?? throw new ArgumentNullException(nameof(jSModule));
        }

        private int workerId = -1;

        public WorkerTask Start()
        {
            var asm = resourceResolver.ResolveAssemblies();
            var dotnetJS = resourceResolver.ResolveDotnetJS();
            var workerInitOption = WorkerInitializeSetting.Default with { DotnetJsName = dotnetJS, Assemblies = asm.ToArray(), BrotliDecoderPath = decoderPath };

            return StartInternal(workerInitOption);
        }

        public WorkerTask Start(WorkerInitializeSetting workerInitOption)
        {
            return StartInternal(workerInitOption);
        }

        private StartWorkerTask StartInternal(WorkerInitializeSetting workerInitOption)
        {
            if (workerId >= 0)
            {
                throw new InvalidOperationException("既にワーカーは起動しています。");
            }
            StartWorkerTask task = new StartWorkerTask(jSRuntime, jSModule, workerInitOption, id => workerId = id);
            return task;
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
