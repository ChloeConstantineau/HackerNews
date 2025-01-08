using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks.Dataflow;

namespace HackerNews.blocks
{
    public class GetTopStories(HttpClient httpClient)
    {
        public TransformBlock<string, ConcurrentBag<int>> Block = new(async uri =>
            {
                List<int> topIds = await httpClient.GetFromJsonAsync<List<int>>(uri) ?? [];
                Console.WriteLine("Finished loading the top stories");
                return [.. topIds];
            });

    }
}
