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

    private readonly WorkerParentModule module;
    private readonly WorkerInitializeSetting workerInitializeSetting;

    private WorkerService(HttpClient httpClient, WebAssemblyJSRuntime jSRuntime, WorkerParentModule module, WorkerInitializeSetting workerInitializeSetting)
    {
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.jSRuntime = jSRuntime ?? throw new ArgumentNullException(nameof(jSRuntime));
        this.module = module;
        this.workerInitializeSetting = workerInitializeSetting;
    }

    /// <summary>
    /// Create and configure a new instance of <see cref="WorkerService"/>.
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="jSRuntime"></param>
    /// <param name="func">Function to configure.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static Task<WorkerService> ConfigureAsync(HttpClient httpClient, WebAssemblyJSRuntime jSRuntime, Func<WorkerServiceConfig, WorkerServiceConfig> func)
    {
        var config = new WorkerServiceConfig(JSEnvironmentSetting.Default, WorkerInitializeSetting.Default);
        config = func(config);
        return ConfigureAsync(httpClient, jSRuntime, config);
    }

    /// <summary>
    /// Create and configure a new instance of <see cref="WorkerService"/>.
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="jSRuntime"></param>
    /// <param name="func">Function to configure.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static async Task<WorkerService> ConfigureAsync(HttpClient httpClient, WebAssemblyJSRuntime jSRuntime, Func<WorkerServiceConfig, Task<WorkerServiceConfig>> func)
    {
        var config = new WorkerServiceConfig(JSEnvironmentSetting.Default, WorkerInitializeSetting.Default);
        config = await func(config);
        return await ConfigureAsync(httpClient, jSRuntime, config);
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
        if (config is null || config.jSEnvironmentSetting is null || config.workerInitializeSetting is null)
        {
            throw new InvalidOperationException("configs cannot be null.");
        }
        if (!config.jSEnvironmentSetting.IsValid(out var message1))
        {
            throw new InvalidOperationException($"{nameof(JSEnvironmentSetting)} is invalid. {message1}");
        }
        if (!config.workerInitializeSetting.IsValid(out var message2))
        {
            throw new InvalidOperationException($"{nameof(WorkerInitializeSetting)} is invalid. {message2}");
        }

        var module = await WorkerParentModule.CreateInstanceAsync(jSRuntime, config.jSEnvironmentSetting);
        var workerInitializeSetting = config.workerInitializeSetting;

        WorkerService workerService = new(httpClient, jSRuntime, module, workerInitializeSetting);
        return workerService;
    }

    /// <summary>
    /// Get new instance of <see cref="Worker"/>.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public Worker CreateWorker()
    {
        var worker = new Worker(jSRuntime, module, workerInitializeSetting);
        return worker;
    }
}