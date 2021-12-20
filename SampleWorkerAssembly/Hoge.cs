namespace SampleWorkerAssembly
{
    public class Hoge
    {
        public static void Empty()
        {

        }

        public static int Add(int a, int b)
        {
            return a + b;
        }

        public static void Exception()
        {
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

            while (true) yield return ((flip = !flip) ? -1 : 1) * (i += 2);
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