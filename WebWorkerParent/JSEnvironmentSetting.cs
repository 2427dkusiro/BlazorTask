using System.Reflection;

namespace WebWorkerParent
{
    /// <summary>
    /// Represents worker parent settings which depend on user's environment.
    /// </summary>
    public class JSEnvironmentSetting
    {
        /// <summary>
        /// Get singleton which represents default setting.
        /// </summary>
        public static JSEnvironmentSetting Default { get; } = CreateDefault();

        private static JSEnvironmentSetting CreateDefault()
        {
            var asmName = Assembly.GetExecutingAssembly().GetName().Name ?? throw new InvalidOperationException("failed to get executing assembly name.");
            return new JSEnvironmentSetting("WorkerScript.js", asmName, nameof(Messaging.MessageReceiver.ReceiveMessageFromJS), nameof(Messaging.MessageReceiver.NotifyWorkerInitialized));
        }

        /// <summary>
        /// Initialize a new instance of <see cref="JSEnvironmentSetting"/> class.
        /// </summary>
        /// <param name="workerScriptUrl"></param>
        /// <param name="assemblyName"></param>
        /// <param name="messageHandlerName"></param>
        /// <param name="initializedHandlerName"></param>
        public JSEnvironmentSetting(string workerScriptUrl, string assemblyName, string messageHandlerName, string initializedHandlerName)
        {
            WorkerScriptUrl = workerScriptUrl;
            AssemblyName = assemblyName;
            MessageHandlerName = messageHandlerName;
            InitializedHandlerName = initializedHandlerName;
        }

        /// <summary>
        /// Get a javascript url which will be pass to worker. 
        /// </summary>
        public string WorkerScriptUrl { get; }

        /// <summary>
        /// Get the name of this assembly.
        /// </summary>
        public string AssemblyName { get; }

        /// <summary>
        /// Get the name of general message handler.
        /// </summary>
        public string MessageHandlerName { get; }

        /// <summary>
        /// Get the name of worker initialized message handler.
        /// </summary>
        public string InitializedHandlerName { get; }
    }
}
