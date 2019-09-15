using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Models;

namespace TreeTraversal
{
    class BFSTraversal
    {
        async public Task TraverseBFS(int itemId)
        {
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(itemId);
            while (queue.Count != 0)
            {
                int id = queue.Dequeue();
                HttpClient client = new HttpClient();
                string reqUri = "https://hacker-news.firebaseio.com/v0/item/" + id.ToString() + ".json";
                string payload = await client.GetStringAsync(reqUri);
                Item item = JsonConvert.DeserializeObject<Item>(payload);
                if (item.Kids != null)
                {
                    item.Kids.ForEach(kid => queue.Enqueue(kid));
                }
            }
        }
    }
}