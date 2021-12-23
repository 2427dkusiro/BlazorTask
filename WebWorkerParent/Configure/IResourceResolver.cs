namespace BlazorTask.Configure;

/// <summary>
/// Exposes resource resolver which provide path to dotnet runtime resource.
/// </summary>
public interface IResourceResolver
{
    /// <summary>
    /// Get the path of assemblies.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> ResolveAssemblies();

    /// <summary>
    /// Get the dotnet.js path.
    /// </summary>
    /// <returns></returns>
    public string ResolveDotnetJS();
}
