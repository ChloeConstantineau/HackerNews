using System;
using System.Net.Http;
using System.Threading.Tasks;
using Models;
using Newtonsoft.Json;

namespace TreeTraversal
{
    class DFSTraversal
    {
        async public Task TraverseDFS(int itemId)
        {
            HttpClient client = new HttpClient();
            string reqUri = "https://hacker-news.firebaseio.com/v0/item/" + itemId.ToString() + ".json";
            string payload = await client.GetStringAsync(reqUri);
            Item item = JsonConvert.DeserializeObject<Item>(payload);

            if (item.Type == "story" && item.Descendants == 0)
                return;

            try
            {
                if (item.Kids == null)
                {
                    return;
                }

                foreach (var kidId in item.Kids)
                {
                    TraverseDFS(kidId).Wait();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

}