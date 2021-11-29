using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace WebWorkerParent
{
    public class JSEnviromentSettings
    {
        public static JSEnviromentSettings Default { get; } = CreateDefault();

        private static JSEnviromentSettings CreateDefault()
        {
            var asmName = Assembly.GetExecutingAssembly().GetName().Name ?? throw new InvalidOperationException("failed to get executing assembly name.");
            return new JSEnviromentSettings("WorkerScript.js", asmName, nameof(Messaging.MessageReceiver.ReceiveMessageFromJS));
        }

        public JSEnviromentSettings(string workerScriptUrl, string assemblyName, string messageHandlerName)
        {
            WorkerScriptUrl = workerScriptUrl;
            AssemblyName = assemblyName;
            MessageHandlerName = messageHandlerName;
        }

        public string WorkerScriptUrl { get; }

        public string AssemblyName { get; }

        public string MessageHandlerName { get; }
    }
}
