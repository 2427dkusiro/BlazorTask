using System.Reflection;
using System.Text;

namespace BlazorTask.Dispatch;

internal static class MethodNameBuilder
{
    public static string ToIdentifier(this MethodInfo methodInfo)
    {
        if (methodInfo.IsGenericMethod || methodInfo.DeclaringType.IsGenericType || methodInfo.GetParameters().Any(x => x.ParameterType.IsGenericType))
        {
            throw new NotImplementedException();
        }

        StringBuilder stringBuilder = new();

        Assembly? asm = methodInfo.DeclaringType.Assembly;
        Type? type = methodInfo.DeclaringType;
        var name = methodInfo.Name;
        BuildMethod(stringBuilder, asm, type, name);

        stringBuilder.Append('(');

        ParameterInfo[] array = methodInfo.GetParameters();
        for (var i = 0; i < array.Length; i++)
        {
            ParameterInfo? parameter = array[i];
            Assembly? pAsm = parameter.ParameterType.Assembly;
            Type? pType = parameter.ParameterType;
            Build(stringBuilder, pAsm, pType);

            if (i != array.Length - 1)
            {
                stringBuilder.Append(',');
            }
        }
        stringBuilder.Append(')');

        return stringBuilder.ToString();
    }

    private static void Build(StringBuilder stringBuilder, Assembly assembly, Type type)
    {
        stringBuilder.Append('[');
        stringBuilder.Append(assembly.GetName().Name);
        stringBuilder.Append(']');
        stringBuilder.Append(type.FullName);
    }

    private static void BuildMethod(StringBuilder stringBuilder, Assembly assembly, Type type, string methodName)
    {
        stringBuilder.Append('[');
        stringBuilder.Append(assembly.GetName().Name);
        stringBuilder.Append(']');
        stringBuilder.Append(type.FullName);
        stringBuilder.Append(':');
        stringBuilder.Append(methodName);
    }

    private static readonly SpanStringDictionary<Assembly> assemblyCache = new();
    private static readonly SpanStringDictionary<Type> typeCache = new();

    /// <summary>
    /// Get <see cref="MethodInfo"/> from runtime format <see cref="ReadOnlySpan{T}"/>.
    /// </summary>
    /// <remarks>
    /// Method name should be '[{Assembly}]{NameSpace}.{Class}:{Method}'.
    /// </remarks>
    /// <param name="fullName"></param>
    /// <returns></returns>
    /// <exception cref="FormatException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    public static MethodInfo ToMethodInfo(Span<char> span)
    {
        return TypeStringReader.Find(span);
    }

    private static class TypeStringReader
    {
        private static MethodInfo Build(Type declearingType, string methodName, Type[] arguments)
        {
            MethodInfo? methodInfo = declearingType.GetMethod(methodName, arguments);
            if (methodInfo is null)
            {
                throw new FormatException("No such a method.");
            }
            return methodInfo;
        }

        private static MethodInfo Build(Type declearingType, string methodName)
        {
            MethodInfo? methodInfo = declearingType.GetMethod(methodName);
            if (methodInfo is null)
            {
                throw new FormatException("No such a method.");
            }
            return methodInfo;
        }

        private static string ReadAsMethodName(Span<char> span, int i, ref int startIndex)
        {
            var name = new string(span.Slice(startIndex, i - startIndex));
            startIndex = i + 1;
            return name;
        }

        private static Type ReadAsType(Span<char> _span, int i, ref int startIndex)
        {
            Span<char> main = _span.Slice(startIndex, i - startIndex);
            Type? type = GetType(main);
            startIndex = i + 1;
            return type;
        }

        public static MethodInfo Find(Span<char> span)
        {
            Type? declearingType = null;
            string? methodName = null;
            var argumentIndex = 0;
            var startIndex = 0;

            var arguments = new Type[Count(span, ',') + 1];

            for (var i = 0; i < span.Length; i++)
            {
                if (char.IsWhiteSpace(span[i]))
                {
                    continue;
                }

                switch (span[i])
                {
                    case ':':
                        declearingType = ReadAsType(span, i, ref startIndex);
                        continue;

                    case '(':
                        methodName = ReadAsMethodName(span, i, ref startIndex);
                        continue;

                    case ')':
                        Span<char> main = span.Slice(startIndex, i - startIndex);
                        var iswhite = true;
                        for (var j = 0; j < main.Length; j++)
                        {
                            if (!char.IsWhiteSpace(main[j]))
                            {
                                iswhite = false;
                                break;
                            }
                        }
                        if (iswhite)
                        {
                            return Build(declearingType, methodName);
                        }
                        else
                        {
                            arguments[argumentIndex++] = GetType(main);
                            return Build(declearingType, methodName, arguments);
                        }

                    case ',':
                        Type? t2 = ReadAsType(span, i, ref startIndex);
                        arguments[argumentIndex++] = (t2);
                        continue;
                }
            }
            throw new FormatException("No method argument end.");
        }

        private static Type GetType(Span<char> fullName)
        {
            var asmIndexStart = fullName.IndexOf('[');
            var asmIndexEnd = fullName.IndexOf(']');
            if (asmIndexStart == -1 || asmIndexEnd == -1)
            {
                throw new FormatException("Faild to find assembly name from passed argument.");
            }
            ReadOnlySpan<char> asmName = fullName.Slice(asmIndexStart + 1, asmIndexEnd - asmIndexStart - 1);

            if (!assemblyCache.TryGetValue(asmName, out Assembly? asm))
            {
                var asmString = new string(asmName);
                asm = Assembly.Load(asmString);
                if (asm is null)
                {
                    throw new InvalidOperationException($"Failed to load assembly '{asmString}'.");
                }
                assemblyCache.Add(asmName, asm);
            }

            ReadOnlySpan<char> typeName = fullName.Slice(asmIndexEnd + 1, fullName.Length - asmIndexEnd - 1);
            if (!typeCache.TryGetValue(typeName, out Type? type))
            {
                var typeNameString = new string(typeName);
                type = asm.GetType(typeNameString);
                if (type is null)
                {
                    throw new InvalidOperationException($"Failed to find type '{typeNameString}'.");
                }
            }
            return type;
        }

        private static int Count(Span<char> span, char c)
        {
            var count = 0;
            for (var i = 0; i < span.Length; i++)
            {
                if (span[i] == c)
                {
                    count++;
                }
            }
            return count;
        }
    }
}