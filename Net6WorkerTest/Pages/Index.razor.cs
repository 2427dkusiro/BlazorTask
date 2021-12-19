using BlazorTask;

namespace Net6WorkerTest.Pages
{
    public partial class Index
    {
        private WorkerService workerService;
        private Worker? worker;
        private string serviceBootTime = "";
        private string workerBootTime = "";

        private string methodCallTime = "";

        private string inputNum1;
        private string inputNum2;
        private string addResult = "";
        private string addTime = "";

        protected async Task OnBootClick()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            var runtime = JSRuntime as Microsoft.JSInterop.WebAssembly.WebAssemblyJSRuntime ?? throw new InvalidOperationException();

            if (workerService is null)
            {
                workerService = await WorkerService.ConfigureAsync(Http, runtime, config => config
                    .ResolveResourcesFromBootJson(Http)
                    .FetchBrotliResources("decode.min.js")
                );
                watch.Stop();
                serviceBootTime = $"Create WorkerService：{watch.Elapsed.TotalMilliseconds.ToString("F1")}ms";
                watch.Restart();
            }
            worker = workerService.CreateWorker();
            await worker.Start();
            watch.Stop();
            workerBootTime = $"Create Worker：{watch.Elapsed.TotalMilliseconds.ToString("F1")}ms";
        }

        protected async Task OnRunClicked()
        {
            if (worker is null)
            {
                return;
            }
            var method = typeof(SampleWorkerAssembly.Hoge).GetMethod(nameof(SampleWorkerAssembly.Hoge.Empty));
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            await worker.Call(method);
            stopwatch.Stop();
            methodCallTime = $"{stopwatch.Elapsed.TotalMilliseconds.ToString("F1")}ms";
        }

        protected async Task OnAddClicked()
        {
            if (worker is null)
            {
                return;
            }
            var method = typeof(SampleWorkerAssembly.Hoge).GetMethod(nameof(SampleWorkerAssembly.Hoge.Add));
            if (int.TryParse(inputNum1, out var a) && int.TryParse(inputNum2, out var b))
            {
                System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var answer = await worker.Call<int>(method, a, b);
                stopwatch.Stop();
                addResult = answer.ToString();
                addTime = $"{stopwatch.Elapsed.TotalMilliseconds.ToString("F1")}ms";
            };
        }

        protected async Task OnExceptionClicked()
        {
            if (worker is null)
            {
                return;
            }
            var method = typeof(SampleWorkerAssembly.Hoge).GetMethod(nameof(SampleWorkerAssembly.Hoge.Exception));
            await worker.Call(method);
        }
    }
}