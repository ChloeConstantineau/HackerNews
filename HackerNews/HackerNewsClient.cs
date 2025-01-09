using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace HackerNews
{

    public class HackerNewsClient(HttpClient client)
    {
        public async Task<T?> GetFromJSON<T>(string uri)
        {
            var result = await client.GetStringAsync(uri);
            return JsonConvert.DeserializeObject<T>(result);
        }
    }
}