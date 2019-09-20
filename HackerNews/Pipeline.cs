using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Block;
using Models;

namespace HackerNews
{
    //* INPUT: string HackerNews uri , DataflowLinkOptions*//
    //* OUTPUT: Story Title, top commentors (comment count, total comment count) for X number of Top Stories *//

    //* This is the heart of the program *//
    //* A pipeline created of DataFlow block each connected to one another. *//
    //* The pipeline starts as soon as the GetTopStoriesBlock is triggered *//
    //* Depending on the block, it can either wait for all the data to arrive, compute and send a result, *//
    //  or it can send a result for each input receive immediately after it's computation is done. *//
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
            // Declaration of all the pipeline blocks
            GetTopStoriesBlock getTopStories = new GetTopStoriesBlock();
            FilterTopStoriesBlock filterTopStories = new FilterTopStoriesBlock();
            TraverseTopStoriesCommentsBlock traverseTopStoriesComments = new TraverseTopStoriesCommentsBlock();
            ComputeTopCommentsPerStoryBlock computeTopCommentsPerStory = new ComputeTopCommentsPerStoryBlock();

            // Links and interactions betwen each block
            getTopStories.block.LinkTo(filterTopStories.block, this.linkOptions);
            filterTopStories.bufferBlock.LinkTo(traverseTopStoriesComments.block, linkOptions);
            traverseTopStoriesComments.bufferBlock.LinkTo(computeTopCommentsPerStory.block, linkOptions);

            // Triggering the start of the pipeline
            getTopStories.block.Post(uri);
            getTopStories.block.Complete();

            // Buffer at the end of the pipeline
            var receiveAllStories = Task.Run(() =>
               {
                   for (int i = 0; i <= Constants.NBSTORIES; i++)
                   {
                       this.topStories.Add(computeTopCommentsPerStory.bufferBlock.Receive()); // Wait for all the Top Story objects to arrive
                   }
               });

            Task.WaitAll(receiveAllStories);
            computeTopCommentsPerStory.bufferBlock.Complete(); //End of the pipeline

            commentsRegistry = traverseTopStoriesComments.commentsRegistry;
            printResults();
        }

        void printResults()
        {
            foreach (var topStory in topStories)
            {
                System.Console.Write(topStory.Title);

                foreach (var topComment in topStory.TopComments)
                {
                    System.Console.Write(" | {0} ( {1} for story - {2} total)", topComment.Key, topComment.Value, commentsRegistry[topComment.Key]);
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
