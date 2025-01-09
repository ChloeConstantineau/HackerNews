using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

namespace HackerNews.blocks
{
    public class GetTopStories(HackerNewsClient httpClient)
    {
        public TransformBlock<string, ConcurrentBag<int>> Block = new(async uri =>
            {
                List<int> topIds = await httpClient.GetFromJSON<List<int>>(uri) ?? [];
                return [.. topIds];
            });
    }
}