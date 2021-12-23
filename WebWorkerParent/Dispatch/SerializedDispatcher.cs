using BlazorTask.Messaging;

using System.Text.Json;

namespace BlazorTask.Dispatch;

/// <summary>
/// Provides method dispatching from json serialized arguments.
/// </summary>
public class SerializedDispatcher
{
    private static readonly SpanStringDictionary<Action<object[], long>> functionCache = new();

    /// <summary>
    /// Calls a <see langword="static"/> method.
    /// </summary>
    /// <param name="methodName">Name of method to call.</param>
    /// <param name="jsonArg">Json serialized arguments.</param>
    /// <param name="id">Call id.</param>
    /// <exception cref="ArgumentException"></exception>
    public static void CallStatic(ref CallHeader header, Span<char> methodName, Span<byte> jsonArg, long id)
    {
        var method = MethodNameBuilder.ToMethodInfo(methodName);
        var args = JsonSerializer.Deserialize<object[]>(jsonArg);
        if (args is null)
        {
            throw new ArgumentException("Failed to desirialize json");
        }
        if (!functionCache.TryGetValue(methodName, out Action<object[], long>? value))
        {
            value = ILMethodBuilder.BuildSerialized(method);
        }
        value(args, id);
    }
}
