using BlazorTask;

using SampleWorkerAssembly;

using System.Diagnostics;
using System.Reflection;

namespace Net6WorkerTest.Pages;

public static class Benchmarks
{
    public static async ValueTask<Worker> BootWorker(this Tester tester, WorkerService workerService)
    {
        var result = tester.CreateNewCondition("Default");
        Stopwatch stopwatch = Stopwatch.StartNew();

        var worker = await workerService.CreateWorkerAsync();
        await worker.Start();
        stopwatch.Stop();
        result["Primary"] = stopwatch.Elapsed;

        return worker;
    }

    private static MethodInfo method;
    public static async Task RunPITest(this Tester tester, int n, Worker worker)
    {
        var result = tester.CreateNewCondition($"N={n}");
        Stopwatch stopwatch = Stopwatch.StartNew();

        var answer = await worker.Call<double>(method ??= typeof(MathsService).GetMethod(nameof(MathsService.EstimatePI)), n);

        stopwatch.Stop();
        Console.WriteLine(answer);
        result["Primary"] = stopwatch.Elapsed;
    }

    private static MethodInfo method2;
    public static async Task RunAddTest(this Tester tester, int n, Worker worker)
    {
        Random random = new Random();
        var testData = Enumerable.Range(0, n).Select(i => (random.Next(), random.Next())).ToArray();

        var result = tester.CreateNewCondition($"N={n}");
        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < n; i++)
        {
            var (a, b) = testData[i];
            var answer = await worker.Call<int>(method2 ??= typeof(MathsService).GetMethod(nameof(MathsService.Add)), a, b);
        }
        stopwatch.Stop();
        result["Primary"] = stopwatch.Elapsed;
    }

    public static async Task RunMTTest(this Tester tester, int n, WorkerService workerService)
    {
        var result = tester.CreateNewCondition($"N={n}");

        result[$"4 Threads"] = await RunMTTestInternal(tester, n, 4, workerService);
        // result[$"8 Threads"] = await RunMTTestInternal(tester, n, 8, workerService);
    }

    private static MethodInfo method3;

    private static async Task<TimeSpan> RunMTTestInternal(Tester tester, int n, int workerCount, WorkerService workerService)
    {
        Worker[] workers = new Worker[workerCount];

        var sliceSize = (int)Math.Floor((decimal)n / workerCount);

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < workerCount; i++)
        {
            var worker = await workerService.CreateWorkerAsync();
            await worker.Start();
            workers[i] = worker;
        }

        var start = 0;
        var allTasks = new List<Task<double>>();
        foreach (var worker in workers)
        {
            var end = start + sliceSize;
            var task = worker.Call<double>(method3 ??= typeof(MathsService).GetMethod(nameof(MathsService.EstimatePISlice)), start, sliceSize);
            allTasks.Add(task.AsTask());

            start = end;
        }


        var _result = await Task.WhenAll(allTasks.ToArray()).ContinueWith(t =>
        {
            return 4 * t.Result.Sum();
        });
        Console.WriteLine(_result);

        sw.Stop();

        foreach(var worker in workers)
        {
            worker.Dispose();
        }

        return sw.Elapsed;
    }
}