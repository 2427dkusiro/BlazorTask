global using Microsoft.JSInterop;
namespace BlazorTask;

internal static class DefaultSettings
{
    public static string DefaultWorkerScriptPath => "./_content/WebResource/WorkerScript.js";

    public static string DefaultParentScriptPath => "./_content/WebResource/WorkerParent.js";

    public static string DefaultBootJsonPath => "./_framework/blazor.boot.json";
}