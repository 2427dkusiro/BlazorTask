namespace BlazorTask
{
    public static class JSHelper
    {
        public static unsafe TResult InvokeUnmarshalledJson<TResult, TArg>(this IJSUnmarshalledObjectReference reference, string methodName, TArg arg)
        {
            return InvokeUnmarshalledJson<TResult, TArg, object?>(reference, methodName, arg, null);
        }

        public static unsafe void InvokeVoidUnmarshalledJson<TArg>(this IJSUnmarshalledObjectReference reference, string methodName, TArg arg)
        {
            InvokeVoidUnmarshalledJson<TArg, object?>(reference, methodName, arg, null);
        }

        public static unsafe TResult InvokeUnmarshalledJson<TResult, TJsonArg, TArg>(this IJSUnmarshalledObjectReference reference, string methodName, TJsonArg jArg, TArg arg)
        {
            var bin = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(jArg);
            fixed (void* ptr = bin)
            {
                return reference.InvokeUnmarshalled<IntPtr, int, TArg, TResult>(methodName, (IntPtr)ptr, bin.Length, arg);
            }
        }

        public static unsafe void InvokeVoidUnmarshalledJson<TJsonArg, TArg>(this IJSUnmarshalledObjectReference reference, string methodName, TJsonArg jArg, TArg arg)
        {
            var bin = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(jArg);
            fixed (void* ptr = bin)
            {
                _ = reference.InvokeUnmarshalled<IntPtr, int, TArg, object?>(methodName, (IntPtr)ptr, bin.Length, arg);
            }
        }
    }
}