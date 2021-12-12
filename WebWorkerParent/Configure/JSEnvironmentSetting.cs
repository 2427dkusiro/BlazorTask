using System.Diagnostics.CodeAnalysis;

namespace BlazorTask.Configure;

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
        var type = typeof(Messaging.StaticMessageHandler);
        return new JSEnvironmentSetting()
        {
            ParentScriptPath = DefaultSettings.DefaultParentScriptPath,
            WorkerScriptPath = DefaultSettings.DefaultWorkerScriptPath,
            MessageReceiverFullName = $"[{type.Assembly.GetName().Name}]{type.FullName}:{nameof(Messaging.StaticMessageHandler.ReceiveMessage)}",
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

    public int MessageReceiverId { get; init; } = -1;

    public string? MessageReceiverFullName { get; init; }

    /// <summary>
    /// Verify if this instance is valid or not. In case of invalid, message will set.
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public bool IsValid([NotNullWhen(false)] out string? message)
    {
        var mustNotEmpty = new string?[] { ParentScriptPath, WorkerScriptPath, MessageReceiverFullName };
        if (mustNotEmpty.Any(str => string.IsNullOrEmpty(str)))
        {
            message = $"Some required properties are null or empty.";
            return false;
        }
        if (MessageReceiverId == -1)
        {
            message = "Must set message receiver id.";
            return false;
        }

        message = null;
        return true;
    }
}
