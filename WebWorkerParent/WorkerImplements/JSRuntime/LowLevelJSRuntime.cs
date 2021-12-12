using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using static System.Reflection.Emit.OpCodes;

namespace BlazorTask.WorkerImplements.JSRuntime;

/// <summary>
/// Provides low-level JS invoke functions.
/// </summary>
public class LowLevelJSRuntime
{
    private static readonly Module module;
    private static readonly Type runtime;
    private static readonly Type jSObject;

    private static readonly string asmName = "System.Private.Runtime.InteropServices.JavaScript, Version=6.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";

    static LowLevelJSRuntime()
    {
        var asm = Assembly.Load(asmName);

        runtime = (asm.GetType("Interop") ?? throw new InvalidOperationException("Failed to find 'Interop' class."))
            .GetNestedType("Runtime", BindingFlags.NonPublic | BindingFlags.Static) ?? throw new InvalidOperationException("Failed to find 'Runtime' class.");
        module = runtime?.Module ?? throw new InvalidOperationException("Failed to find module.");
        jSObject = asm.GetType("System.Runtime.InteropServices.JavaScript.JSObject") ?? throw new InvalidCastException("Failed to find 'JSObject' class.");
    }

    private static Func<string, IntPtr, string>? _invokeJS;

    /// <summary>
    /// Invoke Javascript without argument.
    /// </summary>
    /// <param name="methodName">JS method Name to call.</param>
    /// <param name="exceptionalResult">Error code.</param>
    /// <returns></returns>
    public static unsafe string InvokeJS(string methodName, out int exceptionalResult)
    {
        exceptionalResult = 0;
        return (_invokeJS ??= BuildInvokeJS())(methodName, (IntPtr)Unsafe.AsPointer(ref exceptionalResult));
    }

    private static Func<string, IntPtr, string> BuildInvokeJS()
    {
        var invokeJS = runtime.GetMethod("InvokeJS", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Failed to find 'InvokeJS' Method.");

        DynamicMethod dynamicMethod = new("<>InvokeJS", typeof(string), new[] { typeof(string), typeof(IntPtr) }, module, true);
        var ilGen = dynamicMethod.GetILGenerator();
        ilGen.Emit(Ldarg_0);
        ilGen.Emit(Ldarg_1);
        ilGen.Emit(Call, invokeJS);
        ilGen.Emit(Ret);
        var del = dynamicMethod.CreateDelegate<Func<string, IntPtr, string>>();
        return del;
    }

    private static Func<int, string, object[], IntPtr, object>? _invokeJSWithArgs;

    /// <summary>
    /// Invoke JavaScript with arguments.
    /// </summary>
    /// <param name="jsObjHandle">Object handle of JS context. To invoke in global context, set to 0.</param>
    /// <param name="method">Method Name.</param>
    /// <param name="parms">Arguments to pass to JS.</param>
    /// <param name="exceptionalResult">Error code.</param>
    /// <returns></returns>
    public static unsafe object InvokeJSWithArgs(int jsObjHandle, string method, object[] parms, out int exceptionalResult)
    {
        exceptionalResult = 0;
        return (_invokeJSWithArgs ??= BuildInvokeJSWithArgs())(jsObjHandle, method, parms, (IntPtr)Unsafe.AsPointer(ref exceptionalResult));
    }

    private static Func<int, string, object[], IntPtr, object> BuildInvokeJSWithArgs()
    {
        var invokeJSWithArgs = runtime.GetMethod("InvokeJSWithArgs", BindingFlags.NonPublic | BindingFlags.Static)
             ?? throw new InvalidOperationException("Failed to find 'InvokeJSWithArgs' Method.");

        DynamicMethod dynamicMethod = new("<>InvokeJSWithArgs", typeof(object), new[] { typeof(int), typeof(string), typeof(object[]), typeof(IntPtr) }, module, true);
        var ilGen = dynamicMethod.GetILGenerator();
        ilGen.Emit(Ldarg_0);
        ilGen.Emit(Ldarg_1);
        ilGen.Emit(Ldarg_2);
        ilGen.Emit(Ldarg_3);
        ilGen.Emit(Call, invokeJSWithArgs);
        ilGen.Emit(Ret);
        var del = dynamicMethod.CreateDelegate<Func<int, string, object[], IntPtr, object>>();
        return del;
    }

    private static Func<string, IntPtr, object>? _getGlobalObject;

    /// <summary>
    /// Find JS own object in global context.
    /// </summary>
    /// <param name="globalName">Name to find.</param>
    /// <param name="exceptionalResult">Error code.</param>
    /// <returns></returns>
    public static unsafe object GetGlobalObject(string globalName, out int exceptionalResult)
    {
        exceptionalResult = 0;
        return (_getGlobalObject ??= BuildGetGlobalObject())(globalName, (IntPtr)Unsafe.AsPointer(ref exceptionalResult));
    }

    private static Func<string, IntPtr, object> BuildGetGlobalObject()
    {
        var getGlobalObject = runtime.GetMethod("GetGlobalObject", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Failed to find 'GetGlobalObject' Method.");

        DynamicMethod dynamicMethod = new("<>GetGlobalObject", typeof(object), new[] { typeof(string), typeof(IntPtr) }, module, true);
        var ilGen = dynamicMethod.GetILGenerator();
        ilGen.Emit(Ldarg_0);
        ilGen.Emit(Ldarg_1);
        ilGen.Emit(Call, getGlobalObject);
        ilGen.Emit(Ret);
        var del = dynamicMethod.CreateDelegate<Func<string, IntPtr, object>>();
        return del;
    }

    private static Func<object, int>? _getJsHandle;

    /// <summary>
    /// Get handle(id) of JS own object.
    /// </summary>
    /// <param name="jSObject"></param>
    /// <returns></returns>
    public static int GetJsHandle(object jSObject)
    {
        return (_getJsHandle ??= BuildGetJSHandle())(jSObject);
    }

    private static Func<object, int> BuildGetJSHandle()
    {
        var getJSHandle = jSObject.GetProperty("JSHandle")?.GetGetMethod()
            ?? throw new InvalidOperationException("Failed to find 'JSHandle' Property's get method.");

        DynamicMethod dynamicMethod = new("<>GetJSHandle", typeof(int), new[] { typeof(object) }, module, true);
        var ilGen = dynamicMethod.GetILGenerator();
        ilGen.Emit(Ldarg_0);
        ilGen.Emit(Call, getJSHandle);
        ilGen.Emit(Ret);
        var del = dynamicMethod.CreateDelegate<Func<object, int>>();
        return del;
    }
}