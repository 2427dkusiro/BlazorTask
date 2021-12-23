using BlazorTask;

using SampleWorkerAssembly;

namespace Net6WorkerTest.Pages
{
    public partial class Index
    {
        private Worker? worker;
        private string serviceBootTime = "";
        private string workerBootTime = "";

        private string methodCallTime = "";

        private string inputNum1;
        private string inputNum2;
        private string addResult = "";
        private string addTime = "";

        private string reverseCallString = "";

        private string exceptionString = "";

        protected async Task OnBootClick()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            worker = await workerService.CreateWorkerAsync();
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
            System.Reflection.MethodInfo? method = typeof(Hoge).GetMethod(nameof(Hoge.Empty));
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
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
            System.Reflection.MethodInfo? method = typeof(Hoge).GetMethod(nameof(Hoge.Add));
            if (int.TryParse(inputNum1, out var a) && int.TryParse(inputNum2, out var b))
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                var answer = await worker.Call<int>(method, a, b);
                stopwatch.Stop();
                addResult = answer.ToString();
                addTime = $"{stopwatch.Elapsed.TotalMilliseconds.ToString("F1")}ms";
            };
        }

        protected async Task OnReverseCallClicked()
        {
            if (worker is null)
            {
                return;
            }
            System.Reflection.MethodInfo? method = typeof(Hoge).GetMethod(nameof(Hoge.ReverseCall));
            Hoge.WorkerCallback = (string str) =>
            {
                reverseCallString += $"worker writen:{str}{Environment.NewLine}";
            };
            if (int.TryParse(inputNum1, out var a) && int.TryParse(inputNum2, out var b))
            {
                var res = await worker.Call<int>(method, a, b);
                reverseCallString += $"worker returns:{res.ToString()}{Environment.NewLine}";
            }
        }

        protected async Task OnExceptionClicked()
        {
            if (worker is null)
            {
                return;
            }
            try
            {
                await worker.Call(typeof(Hoge).GetMethod(nameof(Hoge.Exception))!);
            }
            catch (Exception ex)
            {
                exceptionString = ex.ToString();
            }
        }
    }
}