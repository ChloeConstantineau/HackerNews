using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Models;
using System.Collections.Concurrent;
using System.Linq;

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

            var filterStoriesBufferBlock = new BufferBlock<Item>();
            var traverseTopStoriesBufferBlock = new BufferBlock<TopStory>();
            var getTopCommentsBufferBlock = new BufferBlock<TopStory>();

            ConcurrentDictionary<string, int> commentsRegistry = new ConcurrentDictionary<string, int>();

            async Task<Item> ProcessItemAsync(string id)
            {
                string url = "https://hacker-news.firebaseio.com/v0/item/" + id.ToString() + ".json";
                string payload = await new HttpClient().GetStringAsync(url);
                Item item = JsonConvert.DeserializeObject<Item>(payload);
                return item;
            }


            var getTopStories = new TransformBlock<string, Queue<string>>(async uri =>
             {
                 Console.WriteLine("Downloading '{0}'...", uri);
                 string payload = await new HttpClient().GetStringAsync(uri);
                 IEnumerable<string> topIds = JsonConvert.DeserializeObject<IEnumerable<string>>(payload);
                 System.Console.WriteLine("Finished loading the top stories");
                 return new Queue<string>(topIds);
             });


            var filterStories = new ActionBlock<Queue<string>>(async queueId =>
            {
                int topStoryCounter = 0;

                while (topStoryCounter <= Constants.NBSTORIES && queueId.TryDequeue(out string id))
                {
                    Item currentItem = await ProcessItemAsync(id);
                    if (topStoryCounter <= Constants.NBSTORIES && currentItem.Type == "story" && !currentItem.Dead && !currentItem.Deleted)
                    {
                        topStoryCounter++;
                        filterStoriesBufferBlock.Post(currentItem); //Posting result to the next block as it is computed
                    }
                }
                Console.WriteLine("Finished Filtering Stories");
            });

            var traverseTopStories = new ActionBlock<Item>(async item =>
           {
               if (item.Kids != null)
               {
                   var storyComment = await traverseCommentTree(item.Kids);
                   var topStory = new TopStory(item.Title, storyComment);

                   traverseTopStoriesBufferBlock.Post(topStory);
               }
               else
               {
                   var topStory = new TopStory(item.Title, new ConcurrentDictionary<string, int>());
                   traverseTopStoriesBufferBlock.Post(topStory);
               }
           });

            async Task<ConcurrentDictionary<string, int>> traverseCommentTree(List<int> kids)
            {
                ConcurrentQueue<int> kidsToVisit = new ConcurrentQueue<int>(kids);
                ConcurrentDictionary<string, int> storyComments = new ConcurrentDictionary<string, int>();

                var tasks = new List<Task>();
                var processCount = Environment.ProcessorCount; //Number of threads equals number of core

                for (int n = 0; n < processCount; n++)
                {
                    HttpClient apiCaller = new HttpClient();
                    tasks.Add(Task.Run(async () =>
                    {
                        while (kidsToVisit.Count != 0)
                        {
                            kidsToVisit.TryDequeue(out int currentKidId);
                            string url = "https://hacker-news.firebaseio.com/v0/item/" + currentKidId.ToString() + ".json";
                            string payload = await apiCaller.GetStringAsync(url);
                            Item item = JsonConvert.DeserializeObject<Item>(payload);
                            if (item != null && !item.Dead && !item.Deleted && item.By != null)
                            {
                                storyComments.AddOrUpdate(item.By, 1, (key, oldValue) => oldValue + 1);
                                commentsRegistry.AddOrUpdate(item.By, 1, (key, oldValue) => oldValue + 1);
                                if (item != null && item.Kids != null && !item.Dead && !item.Deleted)
                                {
                                    foreach (var kidId in item.Kids)
                                    {
                                        kidsToVisit.Enqueue(kidId);
                                    }
                                }
                            }
                        }
                    }));
                }
                await Task.WhenAll(tasks);

                return storyComments;
            }

            var getTopComments = new ActionBlock<TopStory>(topStory =>
            {
                var topComments = (topStory.Comments.Count >= Constants.NBOFCOMMENTATORS) ? getTopCommentators(topStory.Comments) : new List<KeyValuePair<string, int>>(topStory.Comments.ToArray());
                topStory.TopComments = topComments;
                System.Console.WriteLine("Posting story: " + topStory.Title);
                getTopCommentsBufferBlock.Post(topStory);
            });

            List<KeyValuePair<string, int>> getTopCommentators(ConcurrentDictionary<string, int> comments)
            {
                List<KeyValuePair<string, int>> topComments = new List<KeyValuePair<string, int>>(comments.ToArray());
                List<KeyValuePair<string, int>> SortedTopComments = topComments.OrderByDescending(c => c.Value).ToList();

                return SortedTopComments.GetRange(0, 9);
            }

            void printFinalResults(List<TopStory> results)
            {
                foreach (var result in results)
                {
                    System.Console.Write(result.Title);

                    foreach (var topComment in result.TopComments)
                    {
                        System.Console.Write(" | " + topComment.Key + " (" + topComment.Value + " for story - " + commentsRegistry[topComment.Key] + " total)");

                    }
                    System.Console.WriteLine();
                }

            }

            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
                getTopStories.LinkTo(filterStories, linkOptions);
                filterStoriesBufferBlock.LinkTo(traverseTopStories, linkOptions);
                traverseTopStoriesBufferBlock.LinkTo(getTopComments, linkOptions);

                getTopStories.Post("https://hacker-news.firebaseio.com/v0/topstories.json");
                getTopStories.Complete();

                List<TopStory> finalResults = new List<TopStory>();
                var receiveAllStories = Task.Run(() =>
                   {
                       for (int i = 0; i <= Constants.NBSTORIES; i++)
                       {
                           finalResults.Add(getTopCommentsBufferBlock.Receive());
                       }
                   });
                Task.WaitAll(receiveAllStories);
                getTopCommentsBufferBlock.Complete();
                printFinalResults(finalResults);
                watch.Stop();
                System.Console.WriteLine(watch.ElapsedMilliseconds);
            }
            catch (AggregateException ex)
            {
                System.Console.WriteLine(ex.Flatten());
            }

        }
    }
}