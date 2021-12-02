namespace WebWorkerParent
{
    // 修正時、JS型定義も修正すること。
    public record WorkerInitOption
    {
        public WorkerInitOption(string? basePath, string? frameworkDirName, string? appBinDirName, string? dotnetJsName, string? dotnetWasmName, string[]? assemblies)
        {
            BasePath = basePath;
            FrameworkDirName = frameworkDirName;
            AppBinDirName = appBinDirName;
            DotnetJsName = dotnetJsName;
            DotnetWasmName = dotnetWasmName;
            Assemblies = assemblies;
        }

        private static WorkerInitOption? defaultInstance;
        public static WorkerInitOption Default
        {
            get => defaultInstance ??= new(".", "_framework", "appBinDir", null, "dotnet.wasm", null);
        }

        public string? BasePath { get; init; }

        public string? FrameworkDirName { get; init; }

        public string? AppBinDirName { get; init; }

        public string? DotnetJsName { get; init; }

        public string? DotnetWasmName { get; init; }

        public string[]? Assemblies { get; init; }
    }
}

