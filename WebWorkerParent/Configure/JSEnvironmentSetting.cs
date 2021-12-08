using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace WebWorkerParent.Configure;

/// <summary>
/// Represents worker parent's settings which depend on the environment.
/// </summary>
public record JSEnvironmentSetting
{
    private static JSEnvironmentSetting? defaultInstance;
    /// <summary>
    /// Get a singleton which represents default setting.
    /// </summary>
    public static JSEnvironmentSetting Default { get => defaultInstance ??= CreateDefault(); }

    private static JSEnvironmentSetting CreateDefault()
    {
        var asmName = Assembly.GetExecutingAssembly().GetName().Name ?? throw new InvalidOperationException("failed to get executing assembly name.");
        return new JSEnvironmentSetting()
        {
            ParentScriptPath = DefaultSettings.DefaultParentScriptPath,
            WorkerScriptPath = DefaultSettings.DefaultWorkerScriptPath,
            AssemblyName = asmName,
            MessageHandlerName = nameof(Messaging.MessageReceiver.ReceiveMessageFromJS),
            InitializedHandlerName = nameof(Messaging.MessageReceiver.NotifyWorkerInitialized),
        };
    }

    /// <summary>
    /// Get a worker parent javascript path. 
    /// </summary>
    public string? ParentScriptPath { get; init; }

    /// <summary>
    /// Get a javascript path which will be pass to worker. 
    /// </summary>
    public string? WorkerScriptPath { get; init; }

    /// <summary>
    /// Get the name of this assembly.
    /// </summary>
    public string? AssemblyName { get; init; }

    /// <summary>
    /// Get the name of general message handler.
    /// </summary>
    public string? MessageHandlerName { get; init; }

    /// <summary>
    /// Get the name of worker initialized message handler.
    /// </summary>
    public string? InitializedHandlerName { get; init; }

    /// <summary>
    /// Verify if this instance is valid or not. In case of invalid, message will set.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public bool IsValid([NotNullWhen(false)] out string? message)
    {
        var mustNotEmpty = new string?[] { ParentScriptPath, WorkerScriptPath, AssemblyName, MessageHandlerName, InitializedHandlerName };
        if (mustNotEmpty.Any(str => string.IsNullOrEmpty(str)))
        {
            message = $"Some required properties are null or empty.";
            return false;
        }
        message = null;
        return true;
    }
}
