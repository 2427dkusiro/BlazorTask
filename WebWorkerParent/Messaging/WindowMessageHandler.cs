namespace BlazorTask.Messaging
{
    public class WindowMessageHandler : MessageHandler
    {
        private readonly IJSUnmarshalledObjectReference module;

        public WindowMessageHandler(HandlerId id, IJSUnmarshalledObjectReference jSUnmarshalledObjectReference)
        {
            module = jSUnmarshalledObjectReference;
            Id = id;
        }

        protected override void InvokeJSVoid(string name)
        {
            _ = module.InvokeUnmarshalled<object?>(name);
        }

        protected override void InvokeJSVoid(string name, int arg0)
        {
            _ = module.InvokeUnmarshalled<int, object?>(name, arg0);
        }
    }
}