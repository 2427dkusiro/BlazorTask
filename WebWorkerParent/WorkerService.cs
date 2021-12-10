using Microsoft.JSInterop.WebAssembly;

using WebWorkerParent.Configure;

namespace WebWorkerParent;

/// <summary>
/// Provides easy way to create <see cref="Worker"/>.
/// </summary>
/// <remarks>
/// Add instance of <see cref="WorkerService"/> to DI system at your app's program class.
/// </remarks>
public sealed class WorkerService
{
    private readonly HttpClient httpClient;
    private readonly WebAssemblyJSRuntime jSRuntime;

    private readonly IJSUnmarshalledObjectReference module;
    private readonly WorkerServiceConfig config;

    private WorkerService(HttpClient httpClient, WebAssemblyJSRuntime jSRuntime, IJSUnmarshalledObjectReference module, WorkerServiceConfig config)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.jSRuntime = jSRuntime ?? throw new ArgumentNullException(nameof(jSRuntime));
        this.module = module;
        this.config = config;
    }

    /// <summary>
    /// Create and configure a new instance of <see cref="WorkerService"/>.
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="jSRuntime"></param>
    /// <param name="func">Function to configure.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static async Task<WorkerService> ConfigureAsync(HttpClient httpClient, WebAssemblyJSRuntime jSRuntime, Func<WorkerServiceConfigHelper, WorkerServiceConfigHelper> func)
    {
        var config = new WorkerServiceConfig(JSEnvironmentSetting.Default, WorkerInitializeSetting.Default);
        var helper = new WorkerServiceConfigHelper();
        var result = await func(helper).ApplyAllAsync(config);
        return await ConfigureAsync(httpClient, jSRuntime, result);
    }

    /// <summary>
    /// Create and configure a new instance of <see cref="WorkerService"/>.
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="jSRuntime"></param>
    /// <param name="func">Function to configure.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static async Task<WorkerService> ConfigureAsync(HttpClient httpClient, WebAssemblyJSRuntime jSRuntime, WorkerServiceConfig config)
    {
        if (config.JSEnvironmentSetting is null || config.WorkerInitializeSetting is null)
        {
            throw new InvalidOperationException("configs cannot be null.");
        }
        if (!config.JSEnvironmentSetting.IsValid(out var message1))
        {
            throw new InvalidOperationException($"{nameof(JSEnvironmentSetting)} is invalid. {message1}");
        }
        if (!config.WorkerInitializeSetting.IsValid(out var message2))
        {
            throw new InvalidOperationException($"{nameof(WorkerInitializeSetting)} is invalid. {message2}");
        }

        var module = await jSRuntime.InvokeAsync<IJSUnmarshalledObjectReference>("import", config.JSEnvironmentSetting.ParentScriptPath);
        module.InvokeVoidUnmarshalledJson("Configure", config.JSEnvironmentSetting);

        WorkerService workerService = new(httpClient, jSRuntime, module, config);
        return workerService;
    }

    /// <summary>
    /// Get new instance of <see cref="Worker"/>.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Worker CreateWorker()
    {
        var worker = new Worker(jSRuntime, module, config.WorkerInitializeSetting);
        return worker;
    }
}