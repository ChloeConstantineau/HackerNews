using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Block;
using Models;

namespace HackerNews
{
    class Pipeline
    {
        public Pipeline(string uri, DataflowLinkOptions linkOptions)
        {
            this.uri = uri;
            this.linkOptions = linkOptions;
            this.topStories = new List<TopStory>();
            this.commentsRegistry = new ConcurrentDictionary<string, int>();
        }

        public void Run(string uri)
        {
            GetTopStoriesBlock getTopStories = new GetTopStoriesBlock();
            FilterTopStoriesBlock filterTopStories = new FilterTopStoriesBlock();
            TraverseTopStoriesCommentsBlock traverseTopStoriesComments = new TraverseTopStoriesCommentsBlock();
            ComputeTopCommentsPerStoryBlock computeTopCommentsPerStory = new ComputeTopCommentsPerStoryBlock();

            getTopStories.block.LinkTo(filterTopStories.block, this.linkOptions);
            filterTopStories.bufferBlock.LinkTo(traverseTopStoriesComments.block, linkOptions);
            traverseTopStoriesComments.bufferBlock.LinkTo(computeTopCommentsPerStory.block, linkOptions);
            getTopStories.block.Post(uri);
            getTopStories.block.Complete();

            var receiveAllStories = Task.Run(() =>
               {
                   for (int i = 0; i <= Constants.NBSTORIES; i++)
                   {
                       this.topStories.Add(computeTopCommentsPerStory.bufferBlock.Receive());
                   }
               });

            Task.WaitAll(receiveAllStories);
            computeTopCommentsPerStory.bufferBlock.Complete();
            printResults(this.topStories, traverseTopStoriesComments.commentsRegistry);
        }

        void printResults(List<TopStory> results, ConcurrentDictionary<string, int> commentsRegistry)
        {
            foreach (var result in results)
            {
                System.Console.Write(result.Title);

                foreach (var topComment in result.TopComments)
                {
                    System.Console.Write(" | " + topComment.Key + " (" + topComment.Value + " for story - " + commentsRegistry[topComment.Key] + " total)");
                }
                System.Console.WriteLine();
            }

        }
        string uri { get; set; }
        DataflowLinkOptions linkOptions { get; set; }
        List<TopStory> topStories { get; set; }
        ConcurrentDictionary<string, int> commentsRegistry { get; set; }
    }
}