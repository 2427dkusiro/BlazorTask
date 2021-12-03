namespace WebWorkerParent.Utility
{
    public interface IResourceResolver
    {
        public IEnumerable<string> ResolveAssemblies();

        public string ResolveDotnetJS();
    }
}
