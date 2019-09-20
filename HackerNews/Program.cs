using System;
using System.Threading.Tasks.Dataflow;

public static class Constants
{
    public const int NBSTORIES = 30;
    public const int NBOFCOMMENTERS = 10;
}

namespace HackerNews
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
                string uri = "https://hacker-news.firebaseio.com/v0/topstories.json";

                Pipeline hackerNewsPipeline = new Pipeline(uri, linkOptions);
                hackerNewsPipeline.Run(uri);

                watch.Stop();
                System.Console.WriteLine("Time Elapsed: {0} seconds or {1} milliseconds", watch.ElapsedMilliseconds / 1000, watch.ElapsedMilliseconds); ;

            }
            //* If an error occurs in the pipeline, it will be propagated to the end of the pipeline. *//
            //* As such, only one try/catch is necessary                                                *//
            catch (AggregateException ex)
            {
                System.Console.WriteLine(ex.Flatten());
            }
        }
    }
}
