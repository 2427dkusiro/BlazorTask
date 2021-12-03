namespace WebWorkerParent
{
    /// <summary>
    /// Represents settings of worker's initialization of dotnet runtime.
    /// </summary>
    public record WorkerInitializeSetting
    {
        /// <summary>
        /// Create a new instance of <see cref="WorkerInitializeSetting"/> class.
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="frameworkDirName"></param>
        /// <param name="appBinDirName"></param>
        /// <param name="dotnetJsName"></param>
        /// <param name="dotnetWasmName"></param>
        /// <param name="assemblies"></param>
        public WorkerInitializeSetting(string? basePath, string? frameworkDirName, string? appBinDirName, string? dotnetJsName, string? dotnetWasmName, string[]? assemblies)
        {
            BasePath = basePath;
            FrameworkDirName = frameworkDirName;
            AppBinDirName = appBinDirName;
            DotnetJsName = dotnetJsName;
            DotnetWasmName = dotnetWasmName;
            Assemblies = assemblies;
        }

        private static WorkerInitializeSetting? defaultInstance;

        /// <summary>
        /// Get a singleton which represents default setting. Use with expression to build setting.
        /// </summary>
        public static WorkerInitializeSetting Default
        {
            get => defaultInstance ??= new("../..", "_framework", "appBinDir", null, "dotnet.wasm", null);
        }

        public string? BasePath { get; init; }

        public string? FrameworkDirName { get; init; }

        public string? AppBinDirName { get; init; }

        public string? DotnetJsName { get; init; }

        public string? DotnetWasmName { get; init; }

        public string[]? Assemblies { get; init; }
    }
}

