using BlazorTask;

namespace Net6WorkerTest.Pages
{
    public partial class Benchmark
    {
        private List<TestUI> TestUIs = new();

        private static string Display(TimeSpan ts)
        {
            return $"{ts.TotalMilliseconds.ToString("F1")}ms";
        }

        protected override void OnInitialized()
        {
            TestUIs.Add(new TestUI("Boot worker", async tester =>
            {
                Worker? worker = await tester.BootWorker(workerService);
                this.worker = worker;
            }));

            TestUIs.Add(new TestUI("Add number", async tester =>
            {
                await tester.RunAddTest(1, worker);
                await tester.RunAddTest(100, worker);
                await tester.RunAddTest(256, worker);
            }));

            TestUIs.Add(new TestUI("Calculate PI", async tester =>
            {
                await tester.RunPITest(piIterations_small, worker);
                await tester.RunPITest(piIterations_mid, worker);
                await tester.RunPITest(piIterations_large, worker);
            }));

            TestUIs.Add(new TestUI("Calculate PI MT(¦–¢‰ðŒˆƒoƒO‚É‚æ‚è•Àsˆ—–³Œø‰»’†)", async tester =>
            {
                await tester.RunMTTest(piIterations_small, workerService);
                await tester.RunMTTest(piIterations_mid, workerService);
                await tester.RunMTTest(piIterations_large, workerService);
            }));
        }

        private const int piIterations_small = 50_000;
        private const int piIterations_mid = 500_000;
        private const int piIterations_large = 5_000_000;

        private Worker worker;
    }
}
