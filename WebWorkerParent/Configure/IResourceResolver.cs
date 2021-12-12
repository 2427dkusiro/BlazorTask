namespace BlazorTask.Configure;

public interface IResourceResolver
{
    public IEnumerable<string> ResolveAssemblies();

    public string ResolveDotnetJS();
}
