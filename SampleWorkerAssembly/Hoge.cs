using System.Text.Json;

namespace SampleWorkerAssembly
{
    public class Hoge
    {
        public static void Huga() { }

        public static void Piyo() { }

        public static unsafe void HogeFuga(int a, int b, int c, int d)
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var str = new string((char*)a, 0, b / sizeof(char));
            var json = new Span<byte>((void*)c, d);
            var arg = JsonSerializer.Deserialize<object[]>(json);
            stopwatch.Stop();
            Console.WriteLine($"Called:{str}");
            Console.WriteLine($"Accept arguments:{stopwatch.Elapsed.TotalMilliseconds}ms");

            int arg0 = ((JsonElement)arg[0]).Deserialize<int>();
            Console.WriteLine(arg0);

            DateTime arg1 = ((JsonElement)arg[1]).Deserialize<DateTime>();
            TimeSpan diff = DateTime.Now - arg1;
            Console.WriteLine($"Call Delay={diff.TotalMilliseconds}ms");
        }
    }
}