using System;
using System.Net;
using System.Threading.Tasks.Dataflow;

namespace HackerNews
{
    class Program
    {
        static void Main(string[] args)
        {
            // Changed these default values for better performance time
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.MaxServicePointIdleTime = 9000;

            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
                string uri = "https://hacker-news.firebaseio.com/v0/topstories.json";

                Pipeline hackerNewsPipeline = new(uri, linkOptions);
                hackerNewsPipeline.Run();

                watch.Stop();
                Console.WriteLine("Time Elapsed: {0} seconds or {1} milliseconds", watch.ElapsedMilliseconds / 1000, watch.ElapsedMilliseconds);

            }
            //* If an error occurs in the pipeline, it will be propagated to the end of the pipeline. *//
            //* As such, only one try/catch is necessary                                                *//
            catch (AggregateException ex)
            {
                Console.WriteLine(ex.Flatten());
            }
        }
    }
}
