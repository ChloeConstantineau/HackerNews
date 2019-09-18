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
    class TraverseTopStoriesCommentsBlock
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
            if (item.Kids != null)
            {
                var storyComment = await traverseCommentTree(item.Kids);
                var topStory = new TopStory(item.Title, storyComment);

                this.bufferBlock.Post(topStory);
            }
            else
            {
                var topStory = new TopStory(item.Title, new ConcurrentDictionary<string, int>());
                this.bufferBlock.Post(topStory);
            }
        }

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
                            this.commentsRegistry.AddOrUpdate(item.By, 1, (key, oldValue) => oldValue + 1);
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

        public ActionBlock<Item> block { get; set; }
        public BufferBlock<TopStory> bufferBlock { get; set; }
        public ConcurrentDictionary<string, int> commentsRegistry { get; set; }
    }
}