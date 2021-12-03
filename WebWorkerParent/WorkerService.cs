using Microsoft.JSInterop.WebAssembly;

using WebWorkerParent.Utility;

namespace WebWorkerParent
{
    /// <summary>
    /// Provides easy way to create <see cref="Worker"/>.
    /// </summary>
    /// <remarks>
    /// Add instance of <see cref="WorkerService"/> to DI system at your app's program class.
    /// </remarks>
    public class WorkerService
    {
        protected readonly HttpClient httpClient;
        protected readonly WebAssemblyJSRuntime jSRuntime;

        protected IJSUnmarshalledObjectReference? jSModule;
        protected IResourceResolver? resourceResolver;

        protected const string defaultJSPath = "./WorkerParent.js";
        protected const string bootJsonPath = "./_framework/blazor.boot.json";

        protected WorkerService(HttpClient httpClient, WebAssemblyJSRuntime jSRuntime)
        {
            this.httpClient = httpClient;
            this.jSRuntime = jSRuntime;
        }

        protected async Task InitializeAsync(string jsPath, IResourceResolver resourceResolver)
        {
            jSModule = await jSRuntime.InvokeAsync<IJSUnmarshalledObjectReference>("import", jsPath);
            var config = JSEnvironmentSetting.Default;
            this.resourceResolver = resourceResolver;

            jSModule.InvokeVoidUnmarshalledJson("Configure", config);
        }

        /// <summary>
        /// Create new instance of <see cref="WorkerService"/>.
        /// </summary>
        /// <remarks>
        /// In Blazor WebAssembly app, injected <see cref="IJSRuntime"/> can be casted to <see cref="WebAssemblyJSRuntime"/>.
        /// </remarks>
        /// <param name="httpClient">available <see cref="HttpClient"></see> instance.</param>
        /// <param name="jSRuntime">available <see cref="WebAssemblyJSRuntime"/> instance.</param>
        /// <returns></returns>
        public static async Task<WorkerService> CreateInstanceAsync(HttpClient httpClient, WebAssemblyJSRuntime jSRuntime, IResourceResolver? resolver = null)
        {
            var instance = new WorkerService(httpClient, jSRuntime);
            await instance.InitializeAsync(defaultJSPath, resolver ?? await BootJsonResourceResolver.CreateInstanceAsync(httpClient, bootJsonPath));
            return instance;
        }

        /// <summary>
        /// Create new instance of <see cref="WorkerService"/>.
        /// </summary>
        /// <remarks>
        /// In Blazor WebAssembly app, injected <see cref="IJSRuntime"/> can be casted to <see cref="WebAssemblyJSRuntime"/>.
        /// </remarks>
        /// <param name="httpClient">available <see cref="HttpClient"></see> instance.</param>
        /// <param name="jSRuntime">available <see cref="WebAssemblyJSRuntime"/> instance.</param>
        /// <param name="jsPath">custom path to worker parent js module.</param>
        /// <returns></returns>
        public static async Task<WorkerService> CreateInstanceAsync(HttpClient httpClient, WebAssemblyJSRuntime jSRuntime, string jsPath, IResourceResolver? resolver = null)
        {
            var instance = new WorkerService(httpClient, jSRuntime);
            await instance.InitializeAsync(jsPath, resolver ?? await BootJsonResourceResolver.CreateInstanceAsync(httpClient, bootJsonPath));
            return instance;
        }

        /// <summary>
        /// Get new instance of <see cref="Worker"/>.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Worker CreateWorker()
        {
            if (jSModule is null)
            {
                throw new InvalidOperationException($"Service was not initialized. You have to call '{nameof(InitializeAsync)}' first.");
            }

            var worker = new Worker(resourceResolver!, jSRuntime, jSModule);
            return worker;
        }
    }
}