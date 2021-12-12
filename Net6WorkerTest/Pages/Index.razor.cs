using BlazorTask;

namespace Net6WorkerTest.Pages
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
            var method = typeof(SampleWorkerAssembly.Hoge).GetMethod("Fuga");
            var task = worker.Call(method, 123, DateTime.Now);
            await task;
        }
    }
}