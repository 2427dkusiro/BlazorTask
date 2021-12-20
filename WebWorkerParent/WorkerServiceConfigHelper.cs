using BlazorTask.Configure;

using Microsoft.JSInterop.WebAssembly;

namespace BlazorTask;

public class WorkerServiceConfigHelper
{
    public HttpClient HttpClient { get; }
    public WebAssemblyJSRuntime JSRuntime { get; }

    public WorkerServiceConfigHelper(HttpClient httpClient, WebAssemblyJSRuntime jSRuntime)
    {
        HttpClient = httpClient;
        JSRuntime = jSRuntime;
    }

    private readonly Queue<Func<WorkerServiceConfig, WorkerServiceConfig>> syncFunc = new();

    private readonly Queue<Func<WorkerServiceConfig, Task<WorkerServiceConfig>>> asyncFunc = new();

    private readonly Queue<bool> isAsync = new();

    internal void EnqueueSync(Func<WorkerServiceConfig, WorkerServiceConfig> func)
    {
        syncFunc.Enqueue(func);
        isAsync.Enqueue(false);
    }

    internal void EnqueueAsync(Func<WorkerServiceConfig, Task<WorkerServiceConfig>> func)
    {
        asyncFunc.Enqueue(func);
        isAsync.Enqueue(true);
    }

    internal async Task<WorkerServiceConfig> ApplyAllAsync(WorkerServiceConfig first)
    {
        WorkerServiceConfig current = first;
        var count = isAsync.Count;
        for (int i = 0; i < count; i++)
        {
            var _isAsync = isAsync.Dequeue();
            if (_isAsync)
            {
                current = await asyncFunc.Dequeue()(current);
            }
            else
            {
                current = syncFunc.Dequeue()(current);
            }
        }
        return current;
    }
}

public record struct WorkerServiceConfig(JSEnvironmentSetting JSEnvironmentSetting, WorkerInitializeSetting WorkerInitializeSetting);

public static class ConfigureExtensionMethod
{
    public static WorkerServiceConfigHelper ResolveResourcesFromBootJson(this WorkerServiceConfigHelper helper, HttpClient httpClient)
    {
        return ResolveResourcesFromBootJson(helper, httpClient, DefaultSettings.DefaultBootJsonPath);
    }

    public static WorkerServiceConfigHelper ResolveResourcesFromBootJson(this WorkerServiceConfigHelper helper, HttpClient httpClient, string path)
    {
        helper.EnqueueAsync(async config =>
        {
            var resolver = await BootJsonResourceResolver.CreateInstanceAsync(httpClient, path);
            return FromResolver(config, resolver);
        });
        return helper;
    }

    public static WorkerServiceConfigHelper ResolveResourcesFromResolver(this WorkerServiceConfigHelper helper, IResourceResolver resourceResolver)
    {
        helper.EnqueueSync(config => FromResolver(config, resourceResolver));
        return helper;
    }

    private static WorkerServiceConfig FromResolver(WorkerServiceConfig config, IResourceResolver resourceResolver)
    {
        return config with
        {
            WorkerInitializeSetting = config.WorkerInitializeSetting with
            {
                Assemblies = resourceResolver.ResolveAssemblies().ToArray(),
                DotnetJsName = resourceResolver.ResolveDotnetJS(),
            }
        };
    }

    public static WorkerServiceConfigHelper FetchBrotliResources(this WorkerServiceConfigHelper helper, string decoderJSPath)
    {
        helper.EnqueueSync(config => config with
        {
            WorkerInitializeSetting = config.WorkerInitializeSetting with
            {
                ResourceDecoderPath = decoderJSPath,
                ResourceDecodeMathodName = "BrotliDecode",
                ResourceSuffix = ".br",
            }
        });
        return helper;
    }

    public static WorkerServiceConfigHelper SetBasePath(this WorkerServiceConfigHelper helper, string basePath)
    {
        helper.EnqueueSync(config => config with
        {
            WorkerInitializeSetting = config.WorkerInitializeSetting with
            {
                BasePath = basePath,
            }
        });
        return helper;
    }

    public static WorkerServiceConfigHelper DisableCache(this WorkerServiceConfigHelper helper)
    {
        helper.EnqueueSync(config => config with
        {
            WorkerInitializeSetting = config.WorkerInitializeSetting with
            {
                UseResourceCache = false,
            }
        });
        return helper;
    }
}