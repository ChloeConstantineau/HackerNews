using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using HackerNews.models;
using Newtonsoft.Json;

namespace HackerNews.blocks
{
    public class GetComments(HttpClient httpClient)
    {
        static private async Task<ConcurrentDictionary<string, int>> TraverseCommentTree(List<int>? kids, HttpClient httpClient)
        {
            if (kids == null || kids.Count == 0)
                return [];

            ConcurrentQueue<int> kidsToVisit = new(kids);
            ConcurrentDictionary<string, int> storyComments = new();

            var tasks = new List<Task>();
            var processCount = kidsToVisit.Count;

            for (int n = 0; n < processCount; n++) // Start processCount number of threads to traverse the comment tree
            {
                tasks.Add(Task.Run(async () =>
                {
                    while (!kidsToVisit.IsEmpty)
                    {
                        kidsToVisit.TryDequeue(out int currentKidId); // Breath first search (BFS) tree traversal with Queue
                        var item = await GetItemApi(currentKidId, httpClient);

                        if (item != null && item.By != null && item.Dead != true && item.Deleted != true)
                        {
                            storyComments.AddOrUpdate(item.By, 1, (key, oldValue) => oldValue + 1);
                            item.Kids?.ForEach(k => kidsToVisit.Enqueue(k));
                        }
                    }
                }));
            }
            await Task.WhenAll(tasks);

            return storyComments;
        }

        // Utils function to call the HackerNews api route GET Item
        static private async Task<Item?> GetItemApi(int id, HttpClient httpClient) // TODO refactor to have an http service
        {
            string url = "/v0/item/" + id.ToString() + ".json";
            string payload = await httpClient.GetStringAsync(url);
            return JsonConvert.DeserializeObject<Item>(payload);
        }

        public TransformBlock<ConcurrentBag<Item>, ConcurrentBag<TopStory>> Block = new(async items =>
        {
            ConcurrentBag<TopStory> topStories = [];
            var tasks = items.Select(async item =>
            {
                // some pre stuff
                var comments = await TraverseCommentTree(item.Kids, httpClient);
                topStories.Add(new TopStory(item.Title ?? "<Empty Title>", comments));
                // some post stuff
            });
            await Task.WhenAll(tasks);

            Console.WriteLine("Finished computing comments.");
            return topStories;
        });
    }
}
