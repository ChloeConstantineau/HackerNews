using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace HackerNews
{
    public class HackerNewsClient
    {
        public async Task<T?> GetFromJSON<T>(string uri)
        {
            var result = await client.GetStringAsync(uri);
            return JsonConvert.DeserializeObject<T>(result);
        }

        private readonly HttpClient client = new()
        {
            BaseAddress = new Uri("https://hacker-news.firebaseio.com"),
        };
    }
}
