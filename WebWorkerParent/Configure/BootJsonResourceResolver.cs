using System.Text.Json;

namespace BlazorTask.Configure;

/// <summary>
/// Represent a resolver which resolve resource by fetching boot.json.
/// </summary>
public sealed class BootJsonResourceResolver : IResourceResolver
{
    private BootJsonResourceResolver() { }

    /// <summary>
    /// Create a new instance of <see cref="BootJsonResourceResolver"/>.
    /// </summary>
    /// <param name="httpClient"></param>
    /// <param name="path"></param>
    /// <returns></returns>
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
        JsonElement resources = data.RootElement.GetProperty("resources");
        IEnumerable<string>? asms = resources.GetProperty("assembly").EnumerateObject().Select(asm => asm.Name);
        assemblies = asms.ToArray();
        dotnetJS = resources.GetProperty("runtime").EnumerateObject().First(runtime => runtime.Name.StartsWith("dotnet.") && runtime.Name.EndsWith(".js")).Name;
    }

    /// <inheritdoc />
    public IEnumerable<string> ResolveAssemblies()
    {
        return assemblies;
    }

    /// <inheritdoc />
    public string ResolveDotnetJS()
    {
        return dotnetJS;
    }
}