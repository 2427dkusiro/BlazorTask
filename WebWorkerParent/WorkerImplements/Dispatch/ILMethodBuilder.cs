using BlazorTask.Messaging;

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

using static System.Reflection.Emit.OpCodes;

namespace BlazorTask.WorkerImplements.Dispatch;

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

    /// <summary>
    /// Build a delegate to call method from serialized argument.
    /// </summary>
    /// <param name="methodInfo">Method to call.</param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public static Action<object[], long> BuildSerialized(MethodInfo methodInfo)
    {
        var paramTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
        DynamicMethod dynamicMethod = new DynamicMethod($"<>{methodId++}", null, new[] { typeof(object[]), typeof(long) });
        var generator = dynamicMethod.GetILGenerator();

        var deserializeMethod = _deserializeMethod ??= typeof(System.Text.Json.JsonSerializer).GetMethod(nameof(System.Text.Json.JsonSerializer.Deserialize), new[] { typeof(System.Text.Json.JsonElement), typeof(System.Text.Json.JsonSerializerOptions) });
        if (deserializeMethod is null)
        {
            throw new InvalidOperationException("Failed to find 'Deserialize' method");
        }

        var local1 = generator.DeclareLocal(typeof(object[]));
        if (methodInfo.ReturnType != typeof(void))
        {
            var local2 = generator.DeclareLocal(methodInfo.ReturnType);
        }
        var lable1 = generator.DefineLabel();
        var lable2 = generator.DefineLabel();

        generator.Emit(Ldarg_0);
        generator.Emit(Dup);
        generator.Emit(Ldlen);
        Ldc(paramTypes.Length, generator);
        generator.Emit(Beq, lable1); // check argument length
        generator.Emit(Ldstr, $"invalid argument length. Excepted {paramTypes.Length}.");
        generator.Emit(Newobj, _argumentExcpString ??= (typeof(ArgumentException).GetConstructor(new[] { typeof(string) })
            ?? throw new InvalidOperationException("Failed to find 'ArgumentException..ctor'.")));
        generator.Emit(Throw);
        generator.Emit(Br, lable2);

        generator.MarkLabel(lable1);
        generator.Emit(Stloc_0);

        for (var i = 0; i < paramTypes.Length; i++)
        {
            generator.Emit(Ldloc_0);
            Ldc(i, generator);
            generator.Emit(Ldelem_Ref);
            generator.Emit(Unbox_Any, typeof(System.Text.Json.JsonElement));
            generator.Emit(Ldnull);
            var method = deserializeMethod.MakeGenericMethod(paramTypes[i]);
            generator.Emit(Call, method);
        }
        generator.Emit(Call, methodInfo);

        if (methodInfo.ReturnType != typeof(void))
        {
            generator.Emit(Stloc_1);
        }
        generator.Emit(Leave_S, lable2);
        // throw ex;
        generator.Emit(Pop);
        // result
        generator.MarkLabel(lable2);
        if (methodInfo.ReturnType == typeof(void))
        {
            generator.Emit(Ldarg_1);

            var method = _returnVoid ??= typeof(StaticMessageHandler).GetMethod(nameof(StaticMessageHandler.ReturnResultVoid))
                ?? throw new InvalidOperationException($"Failed to find '{nameof(MessageHandler.ReturnResultVoid)}' method.");
            generator.Emit(Call, method);
        }
        else
        {
            generator.Emit(Ldloc_1);
            generator.Emit(Ldarg_1);

            var method = _returnSerialized ??= typeof(StaticMessageHandler).GetMethod(nameof(StaticMessageHandler.ReturnResultSerialized))
               ?? throw new InvalidOperationException($"Failed to find '{nameof(StaticMessageHandler.ReturnResultSerialized)}' method.");
            generator.Emit(Call, method.MakeGenericMethod(methodInfo.ReturnType));
        }
        generator.Emit(Ret);

        return dynamicMethod.CreateDelegate<Action<object[], long>>();
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
