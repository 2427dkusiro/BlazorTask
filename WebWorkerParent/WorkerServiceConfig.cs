using WebWorkerParent.Configure;

namespace WebWorkerParent;

public class WorkerServiceConfig
{
    internal JSEnvironmentSetting jSEnvironmentSetting;
    internal WorkerInitializeSetting workerInitializeSetting;

    public WorkerServiceConfig(JSEnvironmentSetting jSEnvironmentSetting, WorkerInitializeSetting workerInitializeSetting)
    {
        this.jSEnvironmentSetting = jSEnvironmentSetting;
        this.workerInitializeSetting = workerInitializeSetting;
    }
}

public static class ConfigureExtentionMethod
{
    public static async Task<WorkerServiceConfig> ResolveResoucesFromBootJson(this WorkerServiceConfig workerServiceConfig, HttpClient httpClient)
    {
        return await ResolveResourcesFromBootJson(workerServiceConfig, httpClient, DefaultSettings.DefaultBootJsonPath);
    }

    public static async Task<WorkerServiceConfig> ResolveResourcesFromBootJson(this WorkerServiceConfig workerServiceConfig, HttpClient httpClient, string path)
    {
        var resolver = await BootJsonResourceResolver.CreateInstanceAsync(httpClient, path);
        return ResolveResourcesFromResolver(workerServiceConfig, resolver);
    }

    public static WorkerServiceConfig ResolveResourcesFromResolver(this WorkerServiceConfig workerServiceConfig, IResourceResolver resourceResolver)
    {
        workerServiceConfig.workerInitializeSetting = workerServiceConfig.workerInitializeSetting with
        {
            Assemblies = resourceResolver.ResolveAssemblies().ToArray(),
            DotnetJsName = resourceResolver.ResolveDotnetJS(),
        };
        return workerServiceConfig;
    }

    public static WorkerServiceConfig FetchBrotliResources(this WorkerServiceConfig workerServiceConfig, string decoderJSPath)
    {
        workerServiceConfig.workerInitializeSetting = workerServiceConfig.workerInitializeSetting with
        {
            ResourceDecoderPath = decoderJSPath,
            ResourceDecodeMathodName = "BrotliDecode",
            ResourcePrefix = ".br",
        };
        return workerServiceConfig;
    }
}