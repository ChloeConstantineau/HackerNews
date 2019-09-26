using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Models;
using Newtonsoft.Json;

namespace Block
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
            this.commentsRegistry = new ConcurrentDictionary<string, int>();
            this.bufferBlock = new BufferBlock<TopStory>();

            this.block = new ActionBlock<Item>(async item =>
            {
                await Run(item);
            });
        }

        private async Task Run(Item item)
        {
            var storyComment = (item.Kids != null) ? await traverseCommentTree(item.Kids) : new ConcurrentDictionary<string, int>();
            var topStory = new TopStory(item.Title, storyComment);
            this.bufferBlock.Post(topStory);
        }

        private async Task<ConcurrentDictionary<string, int>> traverseCommentTree(List<int> kids)
        {
            ConcurrentQueue<int> kidsToVisit = new ConcurrentQueue<int>(kids); //ConcurrentQueue for comments that still need to be fetched from the API
            ConcurrentDictionary<string, int> storyComments = new ConcurrentDictionary<string, int>(); //Almanach of the count of comments per user posted for all the stories. Ordered by user id.

            var tasks = new List<Task>();
            var processCount = 15; // Arbitrary value, optimized after tests

            for (int n = 0; n < processCount; n++) // Start processCount number of threads to traverse the comment tree
            {
                HttpClient apiCaller = new HttpClient();

                tasks.Add(Task.Run(async () =>
                {
                    while (kidsToVisit.Count != 0)
                    {
                        kidsToVisit.TryDequeue(out int currentKidId); // Breath first search (BFS) tree traversal with Queue
                        Item item = await getItemApi(currentKidId, apiCaller);

                        if (item != null && !item.Dead && !item.Deleted && item.By != null)
                        {
                            storyComments.AddOrUpdate(item.By, 1, (key, oldValue) => oldValue + 1); // The comment count per user for that story
                            this.commentsRegistry.AddOrUpdate(item.By, 1, (key, oldValue) => oldValue + 1); // The comment count per user for all stories

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
        private async Task<Item> getItemApi(int id, HttpClient httpClient)
        {
            string url = "https://hacker-news.firebaseio.com/v0/item/" + id.ToString() + ".json";
            string payload = await httpClient.GetStringAsync(url);
            Item item = JsonConvert.DeserializeObject<Item>(payload);
            return item;
        }

        public ActionBlock<Item> block { get; set; }
        public BufferBlock<TopStory> bufferBlock { get; set; }
        public ConcurrentDictionary<string, int> commentsRegistry { get; set; }
    }
}
