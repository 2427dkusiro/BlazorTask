global using Microsoft.JSInterop;
namespace WebWorkerParent;

internal static class DefaultSettings
{
    public static string DefaultWorkerScriptPath { get => "./_content/WebResource/WorkerScript.js"; }

    public static string DefaultParentScriptPath { get => "./_content/WebResource/WorkerParent.js"; }

    public static string DefaultBootJsonPath { get => "./_framework/blazor.boot.json"; }
}