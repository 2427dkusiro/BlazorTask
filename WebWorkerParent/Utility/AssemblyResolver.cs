using System.Text.Json;

namespace WebWorkerParent.Utility
{
    public static class AssemblyResolver
    {
        public static async Task<string[]> GetAssembliesFromBootJson(HttpClient httpClient, string path)
        {
            var json = await httpClient.GetByteArrayAsync(path);
            var data = JsonDocument.Parse(json);
            var asms = data.RootElement.GetProperty("resources").GetProperty("assembly").EnumerateObject().Select(asm => asm.Name);
            return asms.ToArray();
        }
    }
}
