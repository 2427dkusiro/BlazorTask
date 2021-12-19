using System.Diagnostics.CodeAnalysis;

namespace BlazorTask.Configure;

/// <summary>
/// Represents settings of worker dotnet runtime initialization.
/// </summary>
// If you edit this record class, you have to edit js typedef too. 
public record WorkerInitializeSetting
{
    private static WorkerInitializeSetting? defaultInstance;

    private static readonly Type messageHandler = typeof(Messaging.MessageHandlerManager);

    /// <summary>
    /// Get a singleton which represents default setting. Use with expression to build setting.
    /// </summary>
    public static WorkerInitializeSetting Default
    {
        get => defaultInstance ??= new WorkerInitializeSetting()
        {
            JSExecutePath = "../..",
            BasePath = "/",
            FrameworkDirName = "_framework",
            AppBinDirName = "appBinDir",
            DotnetJsName = null,
            DotnetWasmName = "dotnet.wasm",
            ResourceDecoderPath = null,
            ResourceDecodeMathodName = null,
            ResourceSuffix = null,
            UseResourceCache = true,
            DotnetCulture = null,
            TimeZoneString = null,
            TimeZoneFileName = "dotnet.timezones.blat",
            MessageHandlerMethodFullName = $"[{messageHandler.Assembly.GetName().Name}]{messageHandler.FullName}:{nameof(Messaging.MessageHandlerManager.ReceiveMessage)}",
            CreateMessageReceiverMethodFullName = $"[{messageHandler.Assembly.GetName().Name}]{messageHandler.FullName}:{nameof(Messaging.MessageHandlerManager.CreateAtThisContext)}",
            Assemblies = null,
        };
    }

    /// <summary>
    /// Get the relative path to app root from javascript file.
    /// </summary>
    public string? JSExecutePath { get; init; }

    /// <summary>
    /// Get the base path of this app.
    /// </summary>
    public string? BasePath { get; init; }

    /// <summary>
    /// Get the name of _framework directory.
    /// </summary>
    public string? FrameworkDirName { get; init; }

    /// <summary>
    /// Get the directory name used in initializing runtime. This should be default unless there is a ploblem.
    /// </summary>
    public string? AppBinDirName { get; init; }

    /// <summary>
    /// Get the name of dotnet.***.js file.
    /// </summary>
    public string? DotnetJsName { get; init; }

    /// <summary>
    /// Get the name of dotnet.wasm file.
    /// </summary>
    public string? DotnetWasmName { get; init; }

    /// <summary>
    /// Get the path of resource decoder path such as brotli decoder path. You can be <see langword="null"/> if decode is not required.
    /// </summary>
    public string? ResourceDecoderPath { get; init; }

    /// <summary>
    /// Get the name of resource decoder's decode method name. Must be <see langword="null"/> when <see cref="ResourceDecoderPath"/> is <see langword="null"/> and must not be <see langword="null"/> when  <see cref="ResourceDecoderPath"/> is not <see langword="null"/>.
    /// </summary>
    public string? ResourceDecodeMathodName { get; init; }

    /// <summary>
    /// Get the suffix of resource name such as '.br'. Must be <see langword="null"/> when <see cref="ResourceDecoderPath"/> is <see langword="null"/> and must not be <see langword="null"/> when  <see cref="ResourceDecoderPath"/> is not <see langword="null"/>.
    /// </summary>
    public string? ResourceSuffix { get; init; }

    /// <summary>
    /// Get the value if fetch resources from cache API or not. This should be default unless there is a ploblem. 
    /// </summary>
    public bool UseResourceCache { get; init; } = true;

    /// <summary>
    /// Get worker runtime's culture. When be <see langword="null"/>, use browser provided culture.
    /// </summary>
    public string? DotnetCulture { get; init; }

    /// <summary>
    /// Get worker runtime's timezone. When be <see langword="null"/>, use browser provided timezone. 
    /// </summary>
    public string? TimeZoneString { get; init; }

    /// <summary>
    /// Get the name of timezone file.
    /// </summary>
    public string? TimeZoneFileName { get; init; }

    /// <summary>
    /// Get the name of worker's message handling method name.
    /// </summary>
    public string? MessageHandlerMethodFullName { get; init; }

    /// <summary>
    /// Get the name of set buffer method.
    /// </summary>
    public string? CreateMessageReceiverMethodFullName { get; init; }

    /// <summary>
    /// Get the names of assembly should be loaded in worker.
    /// </summary>
    public string[]? Assemblies { get; init; }

    /// <summary>
    /// Verify if this instance is valid or not. In case of invalid, message will set.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public bool IsValid([NotNullWhen(false)] out string? message)
    {
        var mustNotEmpty = new string?[] { JSExecutePath, BasePath, FrameworkDirName, AppBinDirName, DotnetJsName, DotnetWasmName, TimeZoneFileName, MessageHandlerMethodFullName, CreateMessageReceiverMethodFullName };
        if (mustNotEmpty.Any(str => string.IsNullOrEmpty(str)))
        {
            message = $"Some required properties are null or empty.";
            return false;
        }

        if (ResourceDecoderPath is null)
        {
            if (ResourceDecodeMathodName is not null || ResourceSuffix is not null)
            {
                message = $"When you don't use decoder, property '{nameof(ResourceDecodeMathodName)}' and '{nameof(ResourceSuffix)}' must be null";
                return false;
            }
        }
        else
        {
            if (string.IsNullOrEmpty(ResourceDecodeMathodName) || string.IsNullOrEmpty(ResourceSuffix))
            {
                message = $"When you use decoder, property '{nameof(ResourceDecodeMathodName)}' and '{nameof(ResourceSuffix)}' must be not null";
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