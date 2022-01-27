using BlazorTask.Messaging;

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static System.Reflection.Emit.OpCodes;

namespace BlazorTask.Dispatch;

/// <summary>
/// Build method call delegate using IL.
/// </summary>
internal static class ILMethodBuilder
{
    private static int methodId = 0;

    private static ConstructorInfo? _argumentException_String;
    private static ConstructorInfo ArgumentException_String => _argumentException_String ??= typeof(ArgumentException).GetConstructor(new[] { typeof(string) })
            ?? throw new InvalidOperationException($"Failed to find '{nameof(ArgumentException)}..ctor'.");

    private static MethodInfo? _deserializeMethod;
    private static MethodInfo DeserializeMethod => _deserializeMethod ??= typeof(System.Text.Json.JsonSerializer).FindMethod(nameof(System.Text.Json.JsonSerializer.Deserialize), new[] { typeof(System.Text.Json.JsonElement), typeof(System.Text.Json.JsonSerializerOptions) });

    private static MethodInfo? _returnVoid;
    private static MethodInfo ReturnVoid => _returnVoid ??= typeof(MessageHandlerManager).FindMethod(nameof(MessageHandlerManager.ReturnResultVoid));

    private static MethodInfo? _returnSerialized;
    private static MethodInfo ReturnSerialized => _returnSerialized ??= typeof(MessageHandlerManager).FindMethod(nameof(MessageHandlerManager.ReturnResultSerialized));

    private static MethodInfo? _returnException;
    private static MethodInfo ReturnException => _returnException ??= typeof(MessageHandlerManager).FindMethod(nameof(MessageHandlerManager.ReturnException));

    private static MethodInfo? _returnAsyncVoid;
    private static MethodInfo ReturnAsyncVoid => _returnAsyncVoid ??= typeof(ILMethodBuilder).FindMethod(nameof(ReturnAsyncVoidHelper));

    private static MethodInfo? _returnAsyncValue;
    private static MethodInfo ReturnAsyncValue => _returnAsyncValue ??= typeof(ILMethodBuilder).FindMethod(nameof(ReturnAsyncValueHelper));

    private static MethodInfo FindMethod(this Type type, string methodName)
    {
        MethodInfo? methodInfo = type.GetMethod(methodName);
        if (methodInfo is null)
        {
            throw new InvalidOperationException($"Failed to find '{methodName}' method");
        }
        else
        {
            return methodInfo;
        }
    }

    private static MethodInfo FindMethod(this Type type, string methodName, params Type[] args)
    {
        MethodInfo? methodInfo = type.GetMethod(methodName, args);
        if (methodInfo is null)
        {
            throw new InvalidOperationException($"Failed to find '{methodName}' method");
        }
        else
        {
            return methodInfo;
        }
    }

    /// <summary>
    /// Build a delegate to call method from serialized argument.
    /// </summary>
    /// <param name="methodInfo">Method to call.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static Action<object[], long> BuildSerialized(MethodInfo methodInfo)
    {
        DebugHelper.Debugger.CheckPoint();
        DebugHelper.Debugger.Assert(methodInfo is not null);

        Type[]? paramTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
        var dynamicMethod = new DynamicMethod($"<>{methodId++}", null, new[] { typeof(object[]), typeof(long) });
        ILGenerator? generator = dynamicMethod.GetILGenerator();

        LocalBuilder? argumentsArray = generator.DeclareLocal(typeof(object[]));
        if (methodInfo.ReturnType != typeof(void))
        {
            LocalBuilder? returnValue = generator.DeclareLocal(methodInfo.ReturnType);
        }
        Label argumentOk = generator.DefineLabel();
        Label end = generator.DefineLabel();

        generator.Emit(Ldarg_0);
        generator.Emit(Dup);
        generator.Emit(Ldlen);
        Ldc(paramTypes.Length, generator);
        generator.Emit(Beq, argumentOk); // check argument length
        generator.Emit(Ldstr, $"invalid argument length. Excepted {paramTypes.Length}.");
        generator.Emit(Newobj, ArgumentException_String);
        generator.Emit(Throw);
        generator.Emit(Br, end);

        generator.MarkLabel(argumentOk);
        generator.Emit(Stloc_0);

        for (var i = 0; i < paramTypes.Length; i++)
        {
            generator.Emit(Ldloc_0);
            Ldc(i, generator);
            generator.Emit(Ldelem_Ref);
            generator.Emit(Unbox_Any, typeof(System.Text.Json.JsonElement));
            generator.Emit(Ldnull);
            MethodInfo? method = DeserializeMethod.MakeGenericMethod(paramTypes[i]);
            generator.Emit(Call, method);
        }

        generator.BeginExceptionBlock();
        generator.Emit(Call, methodInfo);

        if (methodInfo.ReturnType != typeof(void))
        {
            generator.Emit(Stloc_1);
        }

        // handle exception
        generator.BeginCatchBlock(typeof(Exception));
        generator.Emit(Ldarg_1);
        generator.Emit(Call, ReturnException);
        generator.Emit(Leave_S, end);

        // not exception result
        generator.EndExceptionBlock();

        if (methodInfo.ReturnType == typeof(void))
        {
            generator.Emit(Ldarg_1);
            generator.Emit(Call, ReturnVoid);
        }
        else if (methodInfo.ReturnType == typeof(Task))
        {
            generator.Emit(Ldloc_1);
            generator.Emit(Ldarg_1);
            generator.Emit(Call, ReturnAsyncVoid);
        }
        else if (methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            generator.Emit(Ldloc_1);
            generator.Emit(Ldarg_1);
            Type[]? genericParam = methodInfo.ReturnType.GetGenericArguments();
            if (genericParam.Length != 1)
            {
                throw new InvalidOperationException("Task<T> mush have 1 generic parameter.");
            }
            generator.Emit(Call, ReturnAsyncValue.MakeGenericMethod(genericParam[0]));
        }
        else
        {
            generator.Emit(Ldloc_1);
            generator.Emit(Ldarg_1);
            generator.Emit(Call, ReturnSerialized.MakeGenericMethod(methodInfo.ReturnType));
        }
        generator.MarkLabel(end);
        generator.Emit(Ret);

        return dynamicMethod.CreateDelegate<Action<object[], long>>();
    }

    /// <summary>
    /// <see langword="await"/> <see cref="Task"/> and then return void.
    /// </summary>
    /// <param name="value">Task to wait.</param>
    /// <param name="id"></param>
    public static void ReturnAsyncVoidHelper(Task value, long id)
    {
        DebugHelper.Debugger.CheckPoint();
        var hasException = true;
        TaskAwaiter awaiter = value.GetAwaiter();
        awaiter.OnCompleted(() =>
        {
            try
            {
                awaiter.GetResult();
                hasException = false;
            }
            catch (Exception ex)
            {
                MessageHandlerManager.ReturnException(ex, id);
                return;
            }
            DebugHelper.Debugger.Assert(!hasException);
            MessageHandlerManager.ReturnResultVoid(id);
        });
    }

    /// <summary>
    /// <see langword="await"/> <see cref="Task{T}"/> and then return its result.
    /// </summary>
    /// <typeparam name="T">Return type</typeparam>
    /// <param name="value">Task to wait.</param>
    /// <param name="id"></param>
    public static void ReturnAsyncValueHelper<T>(Task<T> value, long id)
    {
        DebugHelper.Debugger.CheckPoint();
        var hasException = true;
        TaskAwaiter<T> awaiter = value.GetAwaiter();
        awaiter.OnCompleted(() =>
        {
            T result;
            try
            {
                result = awaiter.GetResult();
                hasException = false;
            }
            catch (Exception ex)
            {
                MessageHandlerManager.ReturnException(ex, id);
                return;
            }
            DebugHelper.Debugger.Assert(!hasException);
            MessageHandlerManager.ReturnResultSerialized(result, id);
            return;
        });
    }

    /// <summary>
    /// Emits ldc command.
    /// </summary>
    /// <param name="n"></param>
    /// <param name="iLGenerator"></param>
    private static void Ldc(int n, ILGenerator iLGenerator)
    {
        OpCode? op = n switch
        {
            0 => Ldc_I4_0,
            1 => Ldc_I4_1,
            2 => Ldc_I4_2,
            3 => Ldc_I4_3,
            4 => Ldc_I4_4,
            5 => Ldc_I4_5,
            6 => Ldc_I4_6,
            7 => Ldc_I4_7,
            8 => Ldc_I4_8,
            -1 => Ldc_I4_M1,
            _ => null,
        };
        if (op is OpCode _op)
        {
            iLGenerator.Emit(_op);
        }
        else
        {
            if (0 <= n && n <= byte.MaxValue)
            {
                iLGenerator.Emit(Ldc_I4_S, n);
            }
            else
            {
                iLGenerator.Emit(Ldc_I4, n);
            }
        }
    }

    /// <summary>
    /// Emits ldemem command.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="iLGenerator"></param>
    private static void LdElem(Type type, ILGenerator iLGenerator)
    {
        if (type.IsValueType)
        {
            var size = Marshal.SizeOf(type);
            switch (size)
            {
                case 1:
                    iLGenerator.Emit(Ldelem_I1);
                    return;
                case 2:
                    iLGenerator.Emit(Ldelem_I2);
                    return;
                case 4:
                    iLGenerator.Emit(Ldelem_I4);
                    return;
                case 8:
                    iLGenerator.Emit(Ldelem_I8);
                    return;
                default:
                    iLGenerator.Emit(Ldelem, type);
                    return;
            }
        }
        iLGenerator.Emit(Ldelem_Ref);
    }
}