using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;

namespace HackerNews.blocks
{
    //* INPUT : String uri - GET topStories HackerNews API route*//
    //* OUTPUT : Queue<string>, Queue of ids*//

    //* This block fetches all the top stories from the API Hacker News *//
    // *through the route /v0/topstories and return the payload in the form of a Queue<string> *//
    public class GetTopStoriesBlock
    {
        public GetTopStoriesBlock()
        {
            Block = new TransformBlock<string, Queue<string>>(async uri =>
            {
                Queue<string> result = await Run(uri);
                return result;

            });
        }
        private async Task<Queue<string>> Run(string uri)
        {
            string payload = await new HttpClient().GetStringAsync(uri);
            IEnumerable<string> topIds = JsonConvert.DeserializeObject<IEnumerable<string>>(payload);

            Console.WriteLine("Finished loading the top stories from '{0}'", uri);

            return new Queue<string>(topIds);
        }
        public TransformBlock<string, Queue<string>> Block { get; set; }

    }
}
