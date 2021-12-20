using BlazorTask.Configure;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop.WebAssembly;

namespace BlazorTask;

/// <summary>
/// Provides easy way to create <see cref="Worker"/>.
/// </summary>
public sealed class WorkerService
{
    private bool isInitializing;
    private bool isInitialized;

    private HttpClient httpClient;
    private WebAssemblyJSRuntime jSRuntime;

    private IJSUnmarshalledObjectReference module;

    private Func<WorkerServiceConfigHelper, WorkerServiceConfigHelper> configFunc;
    private WorkerServiceConfig config;

    private Messaging.MessageHandler messageHandler;
    private IntPtr bufferPtr;
    private const int bufferLength = 256;

    public WorkerService(Func<WorkerServiceConfigHelper, WorkerServiceConfigHelper> func)
    {
        this.configFunc = func ?? throw new ArgumentNullException(nameof(func));
    }

    public WorkerService(WorkerServiceConfig config)
    {
        this.config = config;
    }

    public async ValueTask InitializeAsync(HttpClient httpClient, WebAssemblyJSRuntime jSRuntime)
    {
        if (isInitialized || isInitializing)
        {
            return;
        }
        isInitializing = true;
        this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        this.jSRuntime = jSRuntime ?? throw new ArgumentNullException(nameof(jSRuntime));

        if (configFunc is not null)
        {
            var config = new WorkerServiceConfig(JSEnvironmentSetting.Default, WorkerInitializeSetting.Default);
            var helper = new WorkerServiceConfigHelper(httpClient, jSRuntime);
            var result = await configFunc(helper).ApplyAllAsync(config);
            this.config = result;
        }

        var module = await jSRuntime.InvokeAsync<IJSUnmarshalledObjectReference>("import", config.JSEnvironmentSetting.ParentScriptPath);
        var receiverId = Messaging.MessageHandlerManager.CreateAtWorkerModuleContext(module);
        var receiver = Messaging.MessageHandlerManager.GetHandler(receiverId);

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

        var buffer = module.InvokeUnmarshalledJson<IntPtr, JSEnvironmentSetting, int>("Configure", config.JSEnvironmentSetting, bufferLength);
        receiver.SetBuffer(buffer, bufferLength);

        this.module = module ?? throw new ArgumentNullException(nameof(module));
        this.messageHandler = receiver ?? throw new ArgumentNullException(nameof(messageHandler));
        this.bufferPtr = buffer;

        isInitialized = true;
    }

    /// <summary>
    /// Get new instance of <see cref="Worker"/>.
    /// </summary>
    /// <remarks>
    /// This method not await worker creation but initializing of this class.
    /// </remarks>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async ValueTask<Worker> CreateWorkerAsync()
    {
        if (!isInitialized)
        {
            if (isInitializing)
            {
                await WaitForInit();
            }
            else
            {
                throw new InvalidOperationException("Service must be initialized.");
            }
        }

        var worker = new Worker(jSRuntime, module, config.WorkerInitializeSetting, bufferPtr, bufferLength, messageHandler);
        return worker;
    }

    private async Task WaitForInit()
    {
        for (int i = 0; i < 600; i++) // 30sec
        {
            await Task.Delay(50);
            if (isInitialized)
            {
                return;
            }
        }
        throw new TimeoutException();
    }
}

public static class WorkerServiceExtension
{
    public static IServiceCollection AddWorkerService(this IServiceCollection services, Func<WorkerServiceConfigHelper, WorkerServiceConfigHelper> func)
    {
        return services.AddSingleton<WorkerService, WorkerService>(sp => new WorkerService(func));
    }

    public static IServiceCollection AddWorkerService(this IServiceCollection services, WorkerServiceConfig config)
    {
        return services.AddSingleton<WorkerService, WorkerService>(sp => new WorkerService(config));
    }

    public static async Task InitializeWorkerService(this WebAssemblyHost host)
    {
        using (var scope = host.Services.CreateScope())
        {
            var http = scope.ServiceProvider.GetRequiredService<HttpClient>() ?? throw new NotSupportedException();
            var js = scope.ServiceProvider.GetRequiredService<IJSRuntime>() ?? throw new NotSupportedException();
            var service = scope.ServiceProvider.GetRequiredService<WorkerService>() ?? throw new NotSupportedException();
            await service.InitializeAsync(http, js as WebAssemblyJSRuntime ?? throw new PlatformNotSupportedException());
        }
    }
}