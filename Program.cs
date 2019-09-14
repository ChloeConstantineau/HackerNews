using System;
using System.Net.Http;
using System.Threading.Tasks.Dataflow;

namespace HackerNews
{
    class Program
    {

        static void Main(string[] args)
        {
            var downloadString = new TransformBlock<string, string>(async uri =>
            {
                Console.WriteLine("Downloading '{0}'...", uri);
                return await new HttpClient().GetStringAsync(uri);
            });

            var printGetResult = new ActionBlock<string>(result =>
            {
                Console.WriteLine(result);
            });

            string hackerNewsUri = "https://hacker-news.firebaseio.com/v0/topstories.json";
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            downloadString.LinkTo(printGetResult, linkOptions);

            downloadString.Post(hackerNewsUri);
            downloadString.Complete();
            printGetResult.Completion.Wait();

        }
    }
}
