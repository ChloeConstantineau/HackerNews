using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Models;
using Newtonsoft.Json;

namespace Block
{
    class FilterTopStoriesBlock
    {
        public FilterTopStoriesBlock()
        {
            this.bufferBlock = new BufferBlock<Item>();

            this.block = new ActionBlock<Queue<string>>(async queueId =>
            {
                await Run(queueId);

            });
        }
        private async Task Run(Queue<string> queueId)
        {
            int topStoryCounter = 0;

            while (topStoryCounter <= Constants.NBSTORIES && queueId.TryDequeue(out string id))
            {
                string url = "https://hacker-news.firebaseio.com/v0/item/" + id.ToString() + ".json";
                string payload = await new HttpClient().GetStringAsync(url);
                Item currentItem = JsonConvert.DeserializeObject<Item>(payload);
                if (topStoryCounter <= Constants.NBSTORIES && currentItem.Type == "story" && !currentItem.Dead && !currentItem.Deleted)
                {
                    topStoryCounter++;
                    this.bufferBlock.Post(currentItem); //Posting result to the next block as it is computed
                }
            }
            Console.WriteLine("Finished Filtering Stories");

        }

        public ActionBlock<Queue<string>> block { get; set; }
        public BufferBlock<Item> bufferBlock { get; set; }

    }
}