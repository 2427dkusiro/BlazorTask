using System.Reflection;
using System.Text.Json;

namespace BlazorTask.Dispatch;

/// <summary>
/// Provides method dispatching from json serialized arguments.
/// </summary>
public class SerializedDispatcher
{
    /// <summary>
    /// Calls a <see langword="static"/> method.
    /// </summary>
    /// <param name="methodName">Name of method to call.</param>
    /// <param name="jsonArg">Json serialized arguments.</param>
    /// <param name="id">Call id.</param>
    /// <exception cref="ArgumentException"></exception>
    public static void CallStatic(Span<char> methodName, Span<byte> jsonArg, long id)
    {
        /*
        var args = JsonSerializer.Deserialize<object[]>(jsonArg);
        var arg0 = ((JsonElement)args[0]).Deserialize<int>();
        var arg1 = ((JsonElement)args[1]).Deserialize<int>();
        var ans = MathsService.EstimatePISlice(arg0, arg1);
        Console.WriteLine(ans);
        Messaging.MessageHandlerManager.ReturnResultSerialized(arg0 + arg1, id);
        */
        var method = GetMethodInfo(methodName);
        var args = JsonSerializer.Deserialize<object[]>(jsonArg);
        if (args is null)
        {
            throw new ArgumentException("Failed to desirialize json");
        }
        if (!functionCache.TryGetValue(methodName, out var value))
        {
            value = ILMethodBuilder.BuildSerialized(method);
        }
        value(args, id);
    }

    private static readonly SpanStringDictionary<Action<object[], long>> functionCache = new();
    private static readonly SpanStringDictionary<Assembly> assemblyCache = new();
    private static readonly SpanStringDictionary<Type> typeCache = new();

    /// <summary>
    /// Get <see cref="MethodInfo"/> from runtime-acceptable format <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <remarks>
    /// Method name should be '[{Assembly}]{NameSpace}.{Class}:{Method}'.
    /// </remarks>
    /// <param name="fullName"></param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    private static MethodInfo GetMethodInfo(ReadOnlySpan<char> fullName)
    {
        var asmIndexStart = fullName.IndexOf('[');
        var asmIndexEnd = fullName.IndexOf(']');
        if (asmIndexStart == -1 || asmIndexEnd == -1)
        {
            throw new FormatException("Faild to find assembly name from passed argument.");
        }
        var asmName = fullName.Slice(asmIndexStart + 1, asmIndexEnd - asmIndexStart - 1);

        if (!assemblyCache.TryGetValue(asmName, out var asm))
        {
            var asmString = new string(asmName);
            asm = Assembly.Load(asmString);
            if (asm is null)
            {
                throw new InvalidOperationException($"Failed to load assembly '{asmString}'.");
            }
            assemblyCache.Add(asmName, asm);
        }

        var typeNameEnd = fullName.IndexOf(':');
        var typeName = fullName.Slice(asmIndexEnd + 1, typeNameEnd - asmIndexEnd - 1);
        if (!typeCache.TryGetValue(typeName, out var type))
        {
            var typeNameString = new string(typeName);
            type = asm.GetType(typeNameString);
            if (type is null)
            {
                throw new InvalidOperationException($"Failed to find type '{typeNameString}'.");
            }
        }

        // MethodInfo should not be cached because result delegate will be cached.
        var methodName = fullName.Slice(typeNameEnd + 1);
        var methodNameString = new string(methodName);
        var method = type.GetMethod(methodNameString);
        if (method is null)
        {
            throw new InvalidOperationException($"Failed to find method '{methodNameString}'.");
        }
        return method;
    }
}
