namespace WebWorkerParent
{
    public static class JSHelper
    {
        public static unsafe TResult InvokeUnmarshalledJson<TResult, TArg>(this IJSUnmarshalledObjectReference reference, string methodName, TArg arg)
        {
            var bin = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(arg);
            fixed (void* ptr = bin)
            {
                return reference.InvokeUnmarshalled<IntPtr, int, object?, TResult>(methodName, (IntPtr)ptr, bin.Length, null);
            }
        }

        public static unsafe void InvokeVoidUnmarshalledJson<TArg>(this IJSUnmarshalledObjectReference reference, string methodName, TArg arg)
        {
            var bin = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(arg);
            fixed (void* ptr = bin)
            {
                _ = reference.InvokeUnmarshalled<IntPtr, int, object?, object?>(methodName, (IntPtr)ptr, bin.Length, null);
            }
        }
    }
}