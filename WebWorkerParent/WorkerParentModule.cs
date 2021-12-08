using Microsoft.JSInterop.WebAssembly;

using WebWorkerParent.Configure;

namespace WebWorkerParent;

public class WorkerParentModule
{
    protected internal IJSUnmarshalledObjectReference InternalModule { get; private set; }

    protected WorkerParentModule() { }

    /// <summary>
    /// Create a new instance of <see cref="WorkerParentModule"/>.
    /// </summary>
    /// <param name="jSRuntime"></param>
    /// <param name="jsPath"></param>
    /// <param name="jSEnvironmentSetting"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static async Task<WorkerParentModule> CreateInstanceAsync(WebAssemblyJSRuntime jSRuntime, JSEnvironmentSetting jSEnvironmentSetting)
    {
        var instance = new WorkerParentModule();
        await instance.InitializeAsync(jSRuntime, jSEnvironmentSetting);
        return instance;
    }

    protected async Task InitializeAsync(WebAssemblyJSRuntime jSRuntime, JSEnvironmentSetting jSEnvironmentSetting)
    {
        InternalModule = await jSRuntime.InvokeAsync<IJSUnmarshalledObjectReference>("import", jSEnvironmentSetting.ParentScriptPath);
        InternalModule.InvokeVoidUnmarshalledJson("Configure", jSEnvironmentSetting);
    }
}
