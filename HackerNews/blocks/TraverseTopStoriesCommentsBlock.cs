using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using HackerNews.models;
using Newtonsoft.Json;

namespace HackerNews.blocks
{
    //* INPUT : Object Item of Type story*//
    //* OUTPUT : Object TopStory outputed throught the block's buffer*//

    //* This block takes one top story at a time and traverses the comments posted on such story *//
    //* While doing so, it collects                     *//
    //* 1) The comment count per user for that story    *//
    //* 2) The comment count per user for all stories   *//
    public class TraverseTopStoriesCommentsBlock
    {
        public TraverseTopStoriesCommentsBlock()
        {
            CommentsRegistry = new ConcurrentDictionary<string, int>();
            BufferBlock = new BufferBlock<TopStory>();

            Block = new ActionBlock<Item>(Run);
        }

        private async Task Run(Item item)
        {
            ConcurrentDictionary<string, int> storyComment = item.Kids != null ? await TraverseCommentTree(item.Kids) : new();
            TopStory topStory = new(item.Title, storyComment);
            BufferBlock.Post(topStory);
        }

        private async Task<ConcurrentDictionary<string, int>> TraverseCommentTree(List<int> kids)
        {
            ConcurrentQueue<int> kidsToVisit = new(kids); //ConcurrentQueue for comments that still need to be fetched from the API
            ConcurrentDictionary<string, int> storyComments = new(); //Almanach of the count of comments per user posted for all the stories. Ordered by user id.

            var tasks = new List<Task>();
            var processCount = 15; // Arbitrary value, optimized after tests

            for (int n = 0; n < processCount; n++) // Start processCount number of threads to traverse the comment tree
            {
                HttpClient apiCaller = new();

                tasks.Add(Task.Run(async () =>
                {
                    while (!kidsToVisit.IsEmpty)
                    {
                        kidsToVisit.TryDequeue(out int currentKidId); // Breath first search (BFS) tree traversal with Queue
                        Item item = await GetItemApi(currentKidId, apiCaller);

                        if (item != null && !item.Dead && !item.Deleted && item.By != null)
                        {
                            storyComments.AddOrUpdate(item.By, 1, (key, oldValue) => oldValue + 1); // The comment count per user for that story
                            CommentsRegistry.AddOrUpdate(item.By, 1, (key, oldValue) => oldValue + 1); // The comment count per user for all stories

                            if (item.Kids != null)
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

        // Utils function to call the HackerNews api route GET Item
        private async Task<Item> GetItemApi(int id, HttpClient httpClient)
        {
            string url = "https://hacker-news.firebaseio.com/v0/item/" + id.ToString() + ".json";
            string payload = await httpClient.GetStringAsync(url);
            Item item = JsonConvert.DeserializeObject<Item>(payload);
            return item;
        }

        public ActionBlock<Item> Block { get; set; }
        public BufferBlock<TopStory> BufferBlock { get; set; }
        public ConcurrentDictionary<string, int> CommentsRegistry { get; set; }
    }
}
