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

            var filterStories = new TransformBlock<ConcurrentQueue<string>, Item[]>(async queueId =>
            {
                var processCount = Environment.ProcessorCount; //Number of threads equals number of core
                int topStoryCounter = 0;
                ConcurrentBag<Item> topStories = new ConcurrentBag<Item>();

                var tasks = new List<Task>();
                for (int n = 0; n < processCount; n++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        while (queueId.TryDequeue(out string id) && topStoryCounter < Constants.NBSTORIES)
                        {
                            string url = "https://hacker-news.firebaseio.com/v0/item/" + id.ToString() + ".json";
                            string payload = await new HttpClient().GetStringAsync(url);
                            Item item = JsonConvert.DeserializeObject<Item>(payload);
                            if (item.Type == "story" && topStoryCounter < Constants.NBSTORIES)
                            {
                                Interlocked.Increment(ref topStoryCounter);
                                topStories.Add(item);
                            }
                        }
                    }));
                }
                await Task.WhenAll(tasks);
                return topStories.ToArray();
            });

            var sortTopStories = new TransformBlock<Item[], Item[]>(topStories =>
            {
                Array.Sort(topStories, (Item a, Item b) =>
                {
                    if (a.Kids == null)
                        a.Kids = new List<int>();

                    if (b.Kids == null)
                        b.Kids = new List<int>();

                    if (a.Kids.Count > b.Kids.Count)
                        return 1;

                    if (a.Kids.Count < b.Kids.Count)
                        return -1;

                    else
                        return 0;
                });

                return topStories;
            });

            try
            {
                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
                getTopStories.LinkTo(filterStories, linkOptions);
                filterStories.LinkTo(sortTopStories, linkOptions);

                getTopStories.Post("https://hacker-news.firebaseio.com/v0/topstories.json");
                getTopStories.Complete();
                sortTopStories.Completion.Wait();

            }
            catch (AggregateException ex)
            {
                System.Console.WriteLine(ex.Flatten());
            }

        }
    }
}
