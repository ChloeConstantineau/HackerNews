using System;
using System.Net.Http;

namespace HackerNews
{
    class Program
    {
        static void Main()
        {
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Pipeline.Run();
                watch.Stop();
                Console.WriteLine("\nTime Elapsed: {0} milliseconds", watch.ElapsedMilliseconds);

            }
            catch (AggregateException ex)
            {
                Console.WriteLine(ex.Flatten());
            }
        }
    }
}
