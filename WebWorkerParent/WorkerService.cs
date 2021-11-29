using System.Text.Json;

using Microsoft.JSInterop;

namespace WebWorkerParent
{
    // DI用 軽量な(あまり中身のない)worker作成用クラス。
    public class WorkerService
    {
        private readonly HttpClient httpClient;
        private readonly IJSRuntime jSRuntime;

        private IJSObjectReference jSModule;
        private readonly string jsPath;
        private readonly string defaultJSPath = "./WorkerParent.js";

        protected WorkerService(HttpClient httpClient, IJSRuntime jSRuntime, string? jsPath = null)
        {
            this.httpClient = httpClient;
            this.jSRuntime = jSRuntime;
            this.jsPath = jsPath ?? defaultJSPath;
        }

        protected async Task InitializeAsync()
        {
            jSModule = await jSRuntime.InvokeAsync<IJSObjectReference>("import", jsPath);
            var config = JSEnviromentSettings.Default;
            await jSModule.InvokeVoidAsync("Configure", JsonSerializer.Serialize(config));
        }

        public static async Task<WorkerService> CreateInstance(HttpClient httpClient, IJSRuntime jSRuntime, string? jsPath = null)
        {
            var instance = new WorkerService(httpClient, jSRuntime);
            await instance.InitializeAsync();
            return instance;
        }

        public Worker CreateWorker()
        {
            var worker = new Worker(httpClient, jSRuntime, jSModule);
            return worker;
        }
    }
}

