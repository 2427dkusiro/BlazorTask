using Microsoft.JSInterop;
using Microsoft.JSInterop.WebAssembly;

using System.Text.Json;

namespace WebWorkerParent
{
    // DI用Worker作成用クラス。
    public class WorkerService
    {
        private readonly HttpClient httpClient;
        private readonly WebAssemblyJSRuntime jSRuntime;

        private IJSUnmarshalledObjectReference? jSModule;
        private readonly string jsPath;
        private readonly string defaultJSPath = "./WorkerParent.js";

        protected WorkerService(HttpClient httpClient, WebAssemblyJSRuntime jSRuntime, string? jsPath = null)
        {
            this.httpClient = httpClient;
            this.jSRuntime = jSRuntime;
            this.jsPath = jsPath ?? defaultJSPath;
        }

        protected async Task InitializeAsync()
        {
            jSModule = await jSRuntime.InvokeAsync<IJSUnmarshalledObjectReference>("import", jsPath);
            var config = JSEnviromentSettings.Default;

            jSModule.InvokeVoidUnmarshalledJson("Configure", config);
        }

        public static async Task<WorkerService> CreateInstance(HttpClient httpClient, WebAssemblyJSRuntime jSRuntime, string? jsPath = null)
        {
            var instance = new WorkerService(httpClient, jSRuntime);
            await instance.InitializeAsync();
            return instance;
        }

        public Worker CreateWorker()
        {
            if (jSModule is null)
            {
                throw new InvalidOperationException($"Service was not initialized. You have to call '{nameof(InitializeAsync)}' first.");
            }

            var worker = new Worker(httpClient, jSRuntime, jSModule);
            return worker;
        }
    }
}