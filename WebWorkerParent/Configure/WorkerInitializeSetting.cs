using System.Diagnostics.CodeAnalysis;

namespace WebWorkerParent.Configure;

/// <summary>
/// Represents settings of worker's initialization of dotnet runtime.
/// </summary>
// If you edit this record class, you have to edit js typedef too. 
public record WorkerInitializeSetting
{
    private static WorkerInitializeSetting? defaultInstance;

    /// <summary>
    /// Get a singleton which represents default setting. Use with expression to build setting.
    /// </summary>
    public static WorkerInitializeSetting Default
    {
        get => defaultInstance ??= new WorkerInitializeSetting()
        {
            BasePath = "../..",
            FrameworkDirName = "_framework",
            AppBinDirName = "appBinDir",
            DotnetJsName = null,
            DotnetWasmName = "dotnet.wasm",
            ResourceDecoderPath = null,
            ResourceDecodeMathodName = null,
            ResourcePrefix = null,
            Assemblies = null,
        };
    }

    public string? BasePath { get; init; }

    public string? FrameworkDirName { get; init; }

    public string? AppBinDirName { get; init; }

    public string? DotnetJsName { get; init; }

    public string? DotnetWasmName { get; init; }

    public string? ResourceDecoderPath { get; init; }

    public string? ResourceDecodeMathodName { get; init; }

    public string? ResourcePrefix { get; init; }

    public string[]? Assemblies { get; init; }

    /// <summary>
    /// Verify if this instance is valid or not. In case of invalid, message will set.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public bool IsValid([NotNullWhen(false)] out string? message)
    {
        var mustNotEmpty = new string?[] { BasePath, FrameworkDirName, AppBinDirName, DotnetJsName, DotnetWasmName };
        if (mustNotEmpty.Any(str => string.IsNullOrEmpty(str)))
        {
            message = $"Some required properties are null or empty.";
            return false;
        }

        if (ResourceDecoderPath is null)
        {
            if (ResourceDecodeMathodName is not null || ResourcePrefix is not null)
            {
                message = $"When you don't use decoder, property '{nameof(ResourceDecodeMathodName)}' and '{nameof(ResourcePrefix)}' must be null";
                return false;
            }
        }
        else
        {
            if (string.IsNullOrEmpty(ResourceDecodeMathodName) || string.IsNullOrEmpty(ResourcePrefix))
            {
                message = $"When you use decoder, property '{nameof(ResourceDecodeMathodName)}' and '{nameof(ResourcePrefix)}' must be not null";
                return false;
            }
        }

        if (Assemblies is null || Assemblies.Length == 0)
        {
            message = $"You must specify assemblies which should be loaded.";
            return false;
        }
        message = null;
        return true;
    }
}