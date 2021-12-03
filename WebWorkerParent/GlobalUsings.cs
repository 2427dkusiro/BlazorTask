global using Microsoft.JSInterop;

internal static class Settings
{
    public static string WorkerScriptPath { get => "./_content/WebResource/WorkerScript.js"; }

    public static string WorkerParentScriptPath { get => "./_content/WebResource/WorkerParent.js"; }

    public static string BootJsonPath { get => "./_framework/blazor.boot.json"; }
}