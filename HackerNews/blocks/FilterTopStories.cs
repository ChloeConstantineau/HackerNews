using HackerNews.models;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;

namespace HackerNews.blocks
{
    public class FilterTopStories(HackerNewsClient httpClient)
    {
        static private bool IsValidStory(Item item)
        {
            return item.Dead != true && item.Deleted != true && item.Type == "story";
        }

        public TransformBlock<ConcurrentBag<int>, ConcurrentBag<Item>> Block = new(async ids =>
            {
                ConcurrentBag<Item> topStories = [];
                while (topStories.Count < Constants.NB_TOP_STORIES && ids.TryTake(out int id))
                {
                    var currentItem = await httpClient.GetFromJSON<Item>("/v0/item/" + id.ToString() + ".json");

                    if (currentItem != null && IsValidStory(currentItem))
                    {
                        topStories.Add(currentItem);
                    }
                }
                    ;
                return topStories;
            });

    }
}