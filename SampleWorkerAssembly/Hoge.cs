namespace SampleWorkerAssembly
{
    public static class Hoge
    {
        public static void Empty()
        {

        }

        public static int Add(int a, int b)
        {
            return a + b;
        }

        public static void WriteAnswer(int ans)
        {
            WorkerCallback?.Invoke(ans.ToString());
        }

        public static void WriteAnswerSync(int ans)
        {
            WorkerSyncCallback?.Invoke(ans.ToString());
        }

        public static Action<string>? WorkerCallback { get; set; }

        public static Action<string>? WorkerSyncCallback { get; set; }

        public static async Task<int> ReverseCall(int a, int b)
        {
            var answer = a + b;
            await BlazorTask.WorkerContext.Parent.Call(typeof(Hoge).GetMethod(nameof(Hoge.WriteAnswer))!, answer);
            return answer;
        }

        public static int SyncReverseCall(int a, int b)
        {
            var answer = a + b;
            BlazorTask.WorkerContext.Parent.Call(typeof(Hoge).GetMethod(nameof(Hoge.WriteAnswerSync))!, answer).Wait();
            return answer;
        }

        public static void Exception()
        {
            throw new NotImplementedException();
        }

        public static async Task<int> AsyncAdd(int a, int b)
        {
            await Task.Yield();
            return a + b;
        }

        public static async Task AsyncException()
        {
            await Task.Yield();
            throw new NotImplementedException();
        }
    }

    public static class MathsService
    {
        private static IEnumerable<int> AlternatingSequence(int start = 0)
        {
            int i;
            bool flip;
            if (start == 0)
            {
                yield return 1;
                i = 1;
                flip = false;
            }
            else
            {
                i = (start * 2) - 1;
                flip = start % 2 == 0;
            }

            while (true)
            {
                yield return ((flip = !flip) ? -1 : 1) * (i += 2);
            }
        }

        public static double EstimatePI(int sumLength)
        {
            return 4 * AlternatingSequence().Take(sumLength)
                .Sum(x => 1.0 / x);
        }

        public static double EstimatePISlice(int sumStart, int sumLength)
        {
            var array = AlternatingSequence(sumStart)
                .Take(sumLength).Select(x => 1d / x)
                .Sum();

            return array;
        }

        public static int Add(int a, int b)
        {
            return a + b;
        }
    }
}