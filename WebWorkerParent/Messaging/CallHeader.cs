using System.Runtime.InteropServices;

namespace BlazorTask.Messaging;

/// <summary>
/// Represent the header to call worker.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct CallHeader
{
    /// <summary>
    /// Create new instance of <see cref="CallHeader"/>.
    /// </summary>
    /// <param name="callType"></param>
    public CallHeader(CallType callType)
    {
        callId = CallIdManager.CallId;
        this.callType = callType;
    }

    /// <summary>
    // Size of this struct in bytes.
    /// </summary>
    public readonly int payloadLength = 12;

    /// <summary>
    /// Unique ID of this method call.
    /// </summary>
    public readonly int callId;

    /// <summary>
    /// Metadata of this method call.
    /// </summary>
    public readonly CallType callType;

    /// <summary>
    /// Represent call metadata.
    /// </summary>
    [Flags]
    public enum CallType : int
    {
        /// <summary>
        /// Call static method
        /// </summary>
        Static = 0,

        /// <summary>
        /// Instance method call.
        /// </summary>
        Instance = 1,

        /// <summary>
        /// Call constructor.
        /// </summary>
        Ctor = 2,

    }
}

/// <summary>
/// Provide management of call ID.
/// </summary>
internal static class CallIdManager
{
    private static int callId = 0;

    /// <summary>
    /// Get a unique call ID.
    /// </summary>
    public static int CallId { get => callId++; }
}