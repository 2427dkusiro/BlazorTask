using BlazorTask.Messaging;

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

using static System.Reflection.Emit.OpCodes;

namespace BlazorTask.Dispatch;

/// <summary>
/// Build method call delegate using IL.
/// </summary>
internal static class ILMethodBuilder
{
    private static int methodId = 0;

    private static ConstructorInfo? _argumentExcpString;
    private static MethodInfo? _deserializeMethod;

    private static MethodInfo? _returnVoid;
    private static MethodInfo? _returnSerialized;
    private static MethodInfo? _returnException;

    /// <summary>
    /// Build a delegate to call method from serialized argument.
    /// </summary>
    /// <param name="methodInfo">Method to call.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static Action<object[], long> BuildSerialized(MethodInfo methodInfo)
    {
        Type[]? paramTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
        var dynamicMethod = new DynamicMethod($"<>{methodId++}", null, new[] { typeof(object[]), typeof(long) });
        ILGenerator? generator = dynamicMethod.GetILGenerator();

        MethodInfo? deserializeMethod = _deserializeMethod ??= typeof(System.Text.Json.JsonSerializer).GetMethod(nameof(System.Text.Json.JsonSerializer.Deserialize), new[] { typeof(System.Text.Json.JsonElement), typeof(System.Text.Json.JsonSerializerOptions) });
        if (deserializeMethod is null)
        {
            throw new InvalidOperationException("Failed to find 'Deserialize' method");
        }

        LocalBuilder? local1 = generator.DeclareLocal(typeof(object[]));
        if (methodInfo.ReturnType != typeof(void))
        {
            LocalBuilder? local2 = generator.DeclareLocal(methodInfo.ReturnType);
        }
        Label lable1 = generator.DefineLabel();
        Label label3 = generator.DefineLabel();

        generator.Emit(Ldarg_0);
        generator.Emit(Dup);
        generator.Emit(Ldlen);
        Ldc(paramTypes.Length, generator);
        generator.Emit(Beq, lable1); // check argument length
        generator.Emit(Ldstr, $"invalid argument length. Excepted {paramTypes.Length}.");
        generator.Emit(Newobj, _argumentExcpString ??= (typeof(ArgumentException).GetConstructor(new[] { typeof(string) })
            ?? throw new InvalidOperationException("Failed to find 'ArgumentException..ctor'.")));
        generator.Emit(Throw);
        generator.Emit(Br, label3);

        generator.MarkLabel(lable1);
        generator.Emit(Stloc_0);

        for (var i = 0; i < paramTypes.Length; i++)
        {
            generator.Emit(Ldloc_0);
            Ldc(i, generator);
            generator.Emit(Ldelem_Ref);
            generator.Emit(Unbox_Any, typeof(System.Text.Json.JsonElement));
            generator.Emit(Ldnull);
            MethodInfo? method = deserializeMethod.MakeGenericMethod(paramTypes[i]);
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
        MethodInfo? exMethod = _returnException ??= typeof(MessageHandlerManager).GetMethod(nameof(MessageHandlerManager.ReturnException))
            ?? throw new InvalidOperationException($"Failed to find '{nameof(MessageHandlerManager.ReturnException)}' method");
        generator.Emit(Call, exMethod);
        generator.Emit(Leave_S, label3);

        // not exception result
        generator.EndExceptionBlock();

        if (methodInfo.ReturnType == typeof(void))
        {
            generator.Emit(Ldarg_1);

            MethodInfo? method = _returnVoid ??= typeof(MessageHandlerManager).GetMethod(nameof(MessageHandlerManager.ReturnResultVoid))
                ?? throw new InvalidOperationException($"Failed to find '{nameof(MessageHandlerManager.ReturnResultVoid)}' method.");
            generator.Emit(Call, method);
        }
        else if (methodInfo.ReturnType == typeof(Task))
        {
            generator.Emit(Ldloc_1);
            generator.Emit(Ldarg_1);
            MethodInfo method = returnAsyncVoid ??= typeof(ILMethodBuilder).GetMethods().First(x => x.Name == nameof(ReturnAsyncValueHelper) && !x.IsGenericMethod);
            generator.Emit(Call, method);
        }
        else if (methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            generator.Emit(Ldloc_1);
            generator.Emit(Ldarg_1);
            Type[]? genericParam = methodInfo.ReturnType.GetGenericArguments();
            MethodInfo method = returnAsyncValue ??= typeof(ILMethodBuilder).GetMethods().First(x => x.Name == nameof(ReturnAsyncValueHelper) && x.IsGenericMethod)
                .MakeGenericMethod(methodInfo.ReturnType.GetGenericArguments())
                ?? throw new InvalidOperationException($"Failed to find '{nameof(ReturnAsyncValueHelper)}' method");
            generator.Emit(Call, method);
        }
        else
        {
            generator.Emit(Ldloc_1);
            generator.Emit(Ldarg_1);

            MethodInfo? method = _returnSerialized ??= typeof(MessageHandlerManager).GetMethod(nameof(MessageHandlerManager.ReturnResultSerialized))
               ?? throw new InvalidOperationException($"Failed to find '{nameof(MessageHandlerManager.ReturnResultSerialized)}' method.");
            generator.Emit(Call, method.MakeGenericMethod(methodInfo.ReturnType));
        }
        generator.MarkLabel(label3);
        generator.Emit(Ret);

        return dynamicMethod.CreateDelegate<Action<object[], long>>();
    }

    private static MethodInfo? returnAsyncVoid;
    public static void ReturnAsyncValueHelper(Task value, long id)
    {
        System.Runtime.CompilerServices.TaskAwaiter awaiter = value.GetAwaiter();
        awaiter.OnCompleted(() =>
        {
            MessageHandlerManager.ReturnResultVoid(id);
        });
    }

    private static MethodInfo? returnAsyncValue;
    public static void ReturnAsyncValueHelper<T>(Task<T> value, long id)
    {
        System.Runtime.CompilerServices.TaskAwaiter<T> awaiter = value.GetAwaiter();
        awaiter.OnCompleted(() =>
        {
            MessageHandlerManager.ReturnResultSerialized(awaiter.GetResult(), id);
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