using System.Text.Json;

namespace WebWorkerParent.Utility
{
    public static class AssemblyResolver
    {
        // Since fetch is expencive, this should be cached.
        private static string[]? cacheAsms;
        private static string? cacheDotnetJS;

        public static async ValueTask<string[]> GetAssembliesFromBootJson(HttpClient httpClient, string path)
        {
            if (cacheAsms is null)
            {
                await FetchAssemlyNameFromJson(httpClient, path);
            }
            return cacheAsms!; // Not null
        }

        public static async ValueTask<string> GetDotnetJSFromBootJson(HttpClient httpClient, string path)
        {
            if(cacheDotnetJS is null)
            {
                await FetchAssemlyNameFromJson(httpClient, path);
            }
            return cacheDotnetJS!; // Not null
        }

        private static async Task FetchAssemlyNameFromJson(HttpClient httpClient, string path)
        {
            var json = await httpClient.GetByteArrayAsync(path);
            var data = JsonDocument.Parse(json);
            var resources = data.RootElement.GetProperty("resources");
            var asms = resources.GetProperty("assembly").EnumerateObject().Select(asm => asm.Name);
            cacheAsms = asms.ToArray();
            cacheDotnetJS = resources.GetProperty("runtime").EnumerateObject().First(runtime => runtime.Name.StartsWith("dotnet.") && runtime.Name.EndsWith(".js")).Name;
        }
    }
}
