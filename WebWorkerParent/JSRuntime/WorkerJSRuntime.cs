using System.Diagnostics.CodeAnalysis;

namespace BlazorTask.JSRuntime;

internal class WorkerJSRuntime : IJSRuntime, IJSInProcessRuntime, IJSUnmarshalledRuntime
{
    private static WorkerJSRuntime? _singleton;

    // Warn: Getting JSRuntime from static constructor causes runtime crash!!
    public static WorkerJSRuntime Singleton { get => _singleton ??= new WorkerJSRuntime(); }
    private readonly int self;

    public WorkerJSRuntime()
    {
        var globalThis = LowLevelJSRuntime.GetGlobalObject("globalThis", out _);
        self = LowLevelJSRuntime.GetJsHandle(globalThis);
    }

    public TResult Invoke<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TResult>(string identifier, params object?[]? args)
    {
        var result = LowLevelJSRuntime.InvokeJSWithArgs(self, identifier, args, out _);
        return (TResult)result;
    }

#pragma warning disable CS1998

    public async ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, object?[]? args)
    {
        var result = LowLevelJSRuntime.InvokeJSWithArgs(self, identifier, args, out _);
        return (TValue)result;
    }

    public async ValueTask<TValue> InvokeAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
    {
        var result = LowLevelJSRuntime.InvokeJSWithArgs(self, identifier, args, out _);
        return (TValue)result;
    }

#pragma warning restore

    public TResult InvokeUnmarshalled<TResult>(string identifier)
    {
        var result = LowLevelJSRuntime.InvokeJSWithArgs(self, identifier, null, out _);
        return (TResult)result;
    }

    public TResult InvokeUnmarshalled<T0, TResult>(string identifier, T0 arg0)
    {
        var result = LowLevelJSRuntime.InvokeJSWithArgs(self, identifier, new object?[] { arg0 }, out _);
        return (TResult)result;
    }

    public TResult InvokeUnmarshalled<T0, T1, TResult>(string identifier, T0 arg0, T1 arg1)
    {
        var result = LowLevelJSRuntime.InvokeJSWithArgs(self, identifier, new object?[] { arg0, arg1 }, out _);
        return (TResult)result;
    }

    public TResult InvokeUnmarshalled<T0, T1, T2, TResult>(string identifier, T0 arg0, T1 arg1, T2 arg2)
    {
        var result = LowLevelJSRuntime.InvokeJSWithArgs(self, identifier, new object?[] { arg0, arg1, arg2 }, out _);
        return (TResult)result;
    }
}
