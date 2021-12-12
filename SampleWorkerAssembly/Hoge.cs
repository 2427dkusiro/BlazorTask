namespace SampleWorkerAssembly
{
    public class Hoge
    {
        public static void Fuga(int n, DateTime time)
        {
            Console.WriteLine(n);
            TimeSpan delay = DateTime.Now - time;
            Console.WriteLine(delay.TotalMilliseconds + "ms");
        }
    }
}