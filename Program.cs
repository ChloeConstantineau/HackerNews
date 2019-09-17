using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Models;
using System.Collections.Concurrent;

public static class Constants
{
    public const int NBSTORIES = 5;
    public const int NBOFCOMMENTATORS = 10;
    public const int MAXNBTHREADS = 5;
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

            var filterTopStories = new TransformBlock<ConcurrentQueue<string>, ConcurrentBag<Item>>(async queueId =>
            {
                ConcurrentBag<Item> topStories = new ConcurrentBag<Item>();

                var tasks = new List<Task>();
                for (int n = 0; n < Constants.MAXNBTHREADS; n++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        while (queueId.TryDequeue(out string id) && topStories.Count < Constants.NBSTORIES)
                        {
                            string url = "https://hacker-news.firebaseio.com/v0/item/" + id.ToString() + ".json";
                            string payload = await new HttpClient().GetStringAsync(url);
                            Item item = JsonConvert.DeserializeObject<Item>(payload);
                            if (item.Type == "story" && topStories.Count < Constants.NBSTORIES)
                            {
                                topStories.Add(item);
                            }
                        }
                    }));
                }
                await Task.WhenAll(tasks);

                return topStories;
            });

            var prepTraverseCommentTrees = new TransformBlock<ConcurrentBag<Item>, Queue<Story>>(topStories =>
            {
                Queue<Story> stories = new Queue<Story>();

                while (!topStories.IsEmpty)
                {
                    topStories.TryTake(out Item item);
                    var kids = (item.Kids != null) ? item.Kids : null;
                    stories.Enqueue(new Story(item.Title, item.Id, kids));
                }
                return stories;
            });

            int count = 0;

            var traverseCommentTrees = new TransformBlock<Queue<Story>, Dictionary<string, ConcurrentDictionary<string, int>>>(
                stories =>
                {
                    Dictionary<string, ConcurrentDictionary<string, int>> storyAlmanac = new Dictionary<string, ConcurrentDictionary<string, int>>();
                    while (stories.Count != 0)
                    {
                        var currentStory = stories.Dequeue();
                        if (currentStory.ImmdiateKids == null)
                            storyAlmanac.Add(currentStory.Title, null);
                        else
                        {
                            var comments = traverseCommentTree(new ConcurrentQueue<int>(currentStory.ImmdiateKids)).Result;
                            storyAlmanac.Add(currentStory.Title, comments);
                        }
                    }
                    return storyAlmanac;
                }
            );

            async Task<ConcurrentDictionary<string, int>> traverseCommentTree(ConcurrentQueue<int> kids)
            {
                ConcurrentDictionary<string, int> comments = new ConcurrentDictionary<string, int>();
                var tasks = new List<Task>();
                for (int n = 0; n < 4; n++)
                {
                    tasks.Add(Task.Run(async () =>
                    {
                        while (kids.TryDequeue(out int id))
                        {
                            string reqUri = "https://hacker-news.firebaseio.com/v0/item/" + id.ToString() + ".json";
                            string payload = await new HttpClient().GetStringAsync(reqUri);
                            Item item = JsonConvert.DeserializeObject<Item>(payload);
                            if (!item.Deleted && !item.Dead)
                                comments.AddOrUpdate(item.By, 1, (key, oldValue) => oldValue + 1);

                            if (item.Kids != null)
                            {
                                item.Kids.ForEach(kid => kids.Enqueue(kid));
                            }
                        }
                    }));
                }
                await Task.WhenAll(tasks);
                System.Console.WriteLine(comments.Count);
                count++;
                System.Console.WriteLine("COUNT: " + count);
                return comments;
            };

            var printResults = new ActionBlock<Dictionary<string, ConcurrentDictionary<string, int>>>(storyAlmanac =>
           {
               foreach (var story in storyAlmanac)
               {
                   var topCommentators = getTopCommentators(story.Value);
                   System.Console.Write(story.Key);
                   for(int i=0; i < topCommentators.Length ; i++){
                       System.Console.Write(" | " + topCommentators[i].Key + " (" + topCommentators[i].Value + " for story)");
                   }
                   System.Console.WriteLine();
               }

           });

            KeyValuePair<string, int>[] getTopCommentators(ConcurrentDictionary<string, int> comments)
            {
                var commentArray = comments.ToArray();
                ConcurrentDictionary<string, int> topCommentators = new ConcurrentDictionary<string, int>();
                int lowestCommentCount = 100000000;
                string lowestCountCommentator = "";

                for (int i = 0; i < commentArray.Length; i++)
                {
                    var currentComment = commentArray[i];
                    if (topCommentators.Count < Constants.NBOFCOMMENTATORS)
                    {
                        topCommentators.TryAdd(currentComment.Key, currentComment.Value);
                        if (lowestCommentCount < currentComment.Value)
                        {
                            lowestCommentCount = currentComment.Value;
                            lowestCountCommentator = currentComment.Key;
                        }
                    }
                    else
                    {
                        if (lowestCommentCount < currentComment.Value)
                        {
                            topCommentators.Remove(lowestCountCommentator, out var value);
                            lowestCommentCount = currentComment.Value;
                            lowestCountCommentator = currentComment.Key;
                            topCommentators.TryAdd(currentComment.Key, currentComment.Value);
                        }

                    }
                }
                return topCommentators.ToArray();
            }

            void getTotalCommentCountPerUser(Dictionary<string, ConcurrentDictionary<string, int>> storyAlmanac){

            }

            try
            {
                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
                getTopStories.LinkTo(filterTopStories, linkOptions);
                filterTopStories.LinkTo(prepTraverseCommentTrees, linkOptions);
                prepTraverseCommentTrees.LinkTo(traverseCommentTrees, linkOptions);
                traverseCommentTrees.LinkTo(printResults, linkOptions);

                getTopStories.Post("https://hacker-news.firebaseio.com/v0/topstories.json");
                getTopStories.Complete();
                printResults.Completion.Wait();
            }
            catch (AggregateException ex)
            {
                System.Console.WriteLine(ex.Flatten());
            }
        }
    }
}
