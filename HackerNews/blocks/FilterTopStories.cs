using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using HackerNews.models;
using Newtonsoft.Json;

namespace HackerNews.blocks
{
    public class FilterTopStories(HttpClient httpClient)
    {
        static private async Task<Item?> GetItemApi(int id, HttpClient httpClient)
        {
            string url = "/v0/item/" + id.ToString() + ".json";
            string payload = await httpClient.GetStringAsync(url);
            return JsonConvert.DeserializeObject<Item>(payload);
        }

        static private bool IsValidStory(Item item)
        {
            return item.Dead != true && item.Deleted != true && item.Type == "story";
        }

        public TransformBlock<ConcurrentBag<int>, ConcurrentBag<Item>> Block = new(async ids =>
            {
                ConcurrentBag<Item> topStories = [];
                while (topStories.Count < Constants.NB_TOP_STORIES && ids.TryTake(out int id))
                {
                    var currentItem = await GetItemApi(id, httpClient);

                    if (currentItem != null && IsValidStory(currentItem))
                    {
                        topStories.Add(currentItem);
                    }
                }
                    ;
                Console.WriteLine("Finished filtering the top stories");
                return topStories;
            });

    }
}
