using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Models;
using System.Collections.Concurrent;
using System.Threading;

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
            var getTopStories = new TransformBlock<string, ConcurrentQueue<string>>(async uri =>
             {
                 Console.WriteLine("Downloading '{0}'...", uri);
                 string payload = await new HttpClient().GetStringAsync(uri);
                 IEnumerable<string> topIds = JsonConvert.DeserializeObject<IEnumerable<string>>(payload);
                 System.Console.WriteLine("Finished loading the top stories");
                 return new ConcurrentQueue<string>(topIds);
             });

            var filterStories = new ActionBlock<ConcurrentQueue<string>>(async queueId =>
            {
                var maxThreads = 6;
                int COUNTER = 0;

                var tasks = new List<Task>();
                for (int n = 0; n < maxThreads; n++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        while (queueId.TryDequeue(out string id) && COUNTER < 30)
                        {
                            string url = "https://hacker-news.firebaseio.com/v0/item/" + id.ToString() + ".json";
                            string payload = await new HttpClient().GetStringAsync(url);
                            Item item = JsonConvert.DeserializeObject<Item>(payload);
                            if (item.Type == "story")
                            {
                                Interlocked.Increment(ref COUNTER);
                            }
                            System.Console.WriteLine(COUNTER);
                        }
                    }));
                }
                await Task.WhenAll(tasks);

            });

            try
            {
                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
                getTopStories.LinkTo(filterStories, linkOptions);

                getTopStories.Post("https://hacker-news.firebaseio.com/v0/topstories.json");
                getTopStories.Complete();
                filterStories.Completion.Wait();

            }
            catch (AggregateException ex)
            {
                System.Console.WriteLine(ex.Flatten());
            }

        }
    }
}
