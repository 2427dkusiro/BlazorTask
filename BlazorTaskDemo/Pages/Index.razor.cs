using WebWorkerParent;

namespace BlazorTaskDemo.Pages
{
    public partial class Index
    {
        private Worker? worker;
        private string serviceBootTime = "";
        private string workerBootTime = "";

        protected async Task OnBootClick()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var runtime = JSRuntime as Microsoft.JSInterop.WebAssembly.WebAssemblyJSRuntime ?? throw new InvalidOperationException();

            WorkerService service = await WorkerService.ConfigureAsync(Http, runtime, config => config
                .ResolveResourcesFromBootJson(Http)
                .FetchBrotliResources("decode.min.js")
                .SetBasePath(Http.BaseAddress?.AbsolutePath ?? throw new InvalidOperationException())
            );
            watch.Stop();
            serviceBootTime = $"サービス起動時間：{watch.Elapsed.TotalMilliseconds.ToString("F1")}ms";
            watch.Restart();
            worker = service.CreateWorker();
            await worker.Start();
            watch.Stop();
            workerBootTime = $"ワーカー起動時間：{watch.Elapsed.TotalMilliseconds.ToString("F1")}ms";
        }

        protected async Task OnRunClicked()
        {
            if (worker is null)
            {
                return;
            }
            await worker._Call(nameof(SampleWorkerAssembly.Hoge.Fuga));
            await worker._Call(nameof(SampleWorkerAssembly.Hoge.Piyo));
        }
    }
}