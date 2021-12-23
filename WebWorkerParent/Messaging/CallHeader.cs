using System.Runtime.InteropServices;

namespace BlazorTask.Messaging;

[StructLayout(LayoutKind.Sequential)]
public readonly struct CallHeader
{
    public CallHeader(CallType callType)
    {
        callId = CallIdManager.CallId;
        this.callType = callType;
    }

    public readonly int payloadLength = 12;

    public readonly int callId;

    public readonly CallType callType;

    [Flags]
    public enum CallType : int
    {
        Static = 0,
        Instance = 1,
        Ctor = 2,

    }
}

internal static class CallIdManager
{
    private static int callId = 0;

    public static int CallId { get => callId++; }
}