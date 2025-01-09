using HackerNews.models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HackerNews.blocks
{
    public class GetComments(HackerNewsClient httpClient)
    {
        static private async Task<ConcurrentDictionary<string, int>> TraverseCommentTree(List<int>? kids, HackerNewsClient httpClient)
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
                        var item = await httpClient.GetFromJSON<Item>("/v0/item/" + currentKidId.ToString() + ".json");

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

        public TransformBlock<ConcurrentBag<Item>, ConcurrentBag<TopStory>> Block = new(async items =>
        {
            ConcurrentBag<TopStory> topStories = [];

            var tasks = items.Select(async item =>
            {
                var comments = await TraverseCommentTree(item.Kids, httpClient);
                topStories.Add(new TopStory(item.Title ?? "<Empty Title>", comments));
            });
            await Task.WhenAll(tasks);

            return topStories;
        });
    }
}
