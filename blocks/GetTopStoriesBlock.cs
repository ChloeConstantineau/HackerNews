using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json;

namespace Block
{
    class GetTopStoriesBlock
    {
        public GetTopStoriesBlock()
        {
            this.block = new TransformBlock<string, Queue<string>>(async uri =>
            {
                Queue<string> result = await Run(uri);
                return result;

            });
        }
        public TransformBlock<string, Queue<string>> block { get; set; }

        private async Task<Queue<string>> Run(string uri)
        {
            Console.WriteLine("Downloading '{0}'...", uri);
            string payload = await new HttpClient().GetStringAsync(uri);
            IEnumerable<string> topIds = JsonConvert.DeserializeObject<IEnumerable<string>>(payload);
            System.Console.WriteLine("Finished loading the top stories");

            return new Queue<string>(topIds);
        }
    }
}