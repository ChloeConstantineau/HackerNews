using System;
using System.Threading.Tasks.Dataflow;
using Block;

public static class Constants
{
    public const int NBSTORIES = 30;
    public const int NBOFCOMMENTATORS = 10;
}

namespace HackerNews
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
                string uri = "https://hacker-news.firebaseio.com/v0/topstories.json";

                Pipeline hackerNewsPipeline = new Pipeline(uri, linkOptions);
                hackerNewsPipeline.Run(uri);

            }
            catch (AggregateException ex)
            {
                System.Console.WriteLine(ex.Flatten());
            }
        }
    }
}