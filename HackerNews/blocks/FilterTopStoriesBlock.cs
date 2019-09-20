using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Models;
using Newtonsoft.Json;

namespace Block
{
    //* INPUT : Queue<string>, top story item ids                                 *//
    //* OUTPUT : Object Item of type 'story' outputed throught the block's buffer *//

    //* This block takes all the ids returned from the API and filters the items based on their type  *//
    //* The call send ids of stories and jobs. Since jobs have no comments they must be filtered out. *//
    //* As soon as an id has been identified as type 'story' it is sent to the next block             *//
    public class FilterTopStoriesBlock
    {
        public FilterTopStoriesBlock()
        {
            this.bufferBlock = new BufferBlock<Item>();

            this.block = new ActionBlock<Queue<string>>(async itemIds =>
            {
                await Run(itemIds);
            });
        }
        private async Task Run(Queue<string> itemIds)
        {
            int topStoryCounter = 0;
            HttpClient apiClient = new HttpClient();

            while (topStoryCounter <= Constants.NBSTORIES && itemIds.TryDequeue(out string id))
            {
                var currentItem = await getItemApi(id, apiClient);

                if (currentItem.Type == "story" && !currentItem.Dead && !currentItem.Deleted)
                {
                    topStoryCounter++;
                    this.bufferBlock.Post(currentItem); //Posting result to the next block as it is computed
                }
            }

            Console.WriteLine("Finished filtering the top stories");
        }

        // Utils function to call the HackerNews api route GET Item
        private async Task<Item> getItemApi(string id, HttpClient httpClient)
        {
            string url = "https://hacker-news.firebaseio.com/v0/item/" + id.ToString() + ".json";
            string payload = await httpClient.GetStringAsync(url);
            Item item = JsonConvert.DeserializeObject<Item>(payload);
            return item;
        }

        public BufferBlock<Item> bufferBlock { get; set; }
        public ActionBlock<Queue<string>> block { get; set; }
    }
}
