using System.Text.Json;

namespace WebWorkerParent.Configure;

public sealed class BootJsonResourceResolver : IResourceResolver
{
    private BootJsonResourceResolver() { }

    public static async Task<BootJsonResourceResolver> CreateInstanceAsync(HttpClient httpClient, string path)
    {
        var instance = new BootJsonResourceResolver();
        await instance.FetchAssemlyNameFromJson(httpClient, path);
        return instance;
    }

    private string[] assemblies;
    private string dotnetJS;

    private async Task FetchAssemlyNameFromJson(HttpClient httpClient, string path)
    {
        var json = await httpClient.GetByteArrayAsync(path);
        var data = JsonDocument.Parse(json);
        var resources = data.RootElement.GetProperty("resources");
        var asms = resources.GetProperty("assembly").EnumerateObject().Select(asm => asm.Name);
        assemblies = asms.ToArray();
        dotnetJS = resources.GetProperty("runtime").EnumerateObject().First(runtime => runtime.Name.StartsWith("dotnet.") && runtime.Name.EndsWith(".js")).Name;
    }

    public IEnumerable<string> ResolveAssemblies()
    {
        return assemblies;
    }

    public string ResolveDotnetJS()
    {
        return dotnetJS;
    }
}