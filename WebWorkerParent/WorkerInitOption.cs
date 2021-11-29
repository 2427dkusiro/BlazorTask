namespace WebWorkerParent
{
    // 修正時、JS型定義も修正すること。
    public class WorkerInitOption
    {
        public static WorkerInitOption Default
        {
            get => new WorkerInitOption()
            {
                BasePath = ".",
                FrameworkDirName = "_framework",
                AppBinDirName = "appBinDir",
                DotnetJsName = "dotnet.6.0.0.tj42mwroj7.js",
                DotnetWasmName = "dotnet.wasm",
            };
        }

        public string? BasePath { get; set; }

        public string? FrameworkDirName { get; set; }

        public string? AppBinDirName { get; set; }

        public string? DotnetJsName { get; set; }

        public string? DotnetWasmName { get; set; }

        public List<string> Assemblies { get; } = new();
    }
}

