namespace SampleWorkerAssembly
{
    public class Hoge
    {
        public static void Fuga()
        {
            Console.WriteLine($"called:{nameof(Fuga)}()");
        }

        public static void Piyo(object obj)
        {
            Console.WriteLine($"called:{nameof(Piyo)}(\"{obj.ToString()}\":{obj.GetType().Name})");
        }
    }
}