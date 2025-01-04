using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using HackerNews.blocks;
using HackerNews.models;

namespace HackerNews
{
    //* INPUT: string HackerNews uri , DataflowLinkOptions*//
    //* OUTPUT: Story Title, top commentors (comment count, total comment count) for X number of Top Stories *//

    //* This is the heart of the program *//
    //* A pipeline created of DataFlow block each connected to one another. *//
    //* The pipeline starts as soon as the GetTopStoriesBlock is triggered *//
    //* Depending on the block, it can either wait for all the data to arrive, compute and send a result, *//
    //  or it can send a result for each input receive immediately after it's computation is done. *//
    class Pipeline(string uri, DataflowLinkOptions linkOptions)
    {
        public void Run()
        {
            // Declaration of all the pipeline blocks
            GetTopStoriesBlock getTopStories = new();
            FilterTopStoriesBlock filterTopStories = new();
            TraverseTopStoriesCommentsBlock traverseTopStoriesComments = new();
            ComputeTopCommentsPerStoryBlock computeTopCommentsPerStory = new();

            // Links and interactions betwen each block
            getTopStories.Block.LinkTo(filterTopStories.Block, LinkOptions);
            filterTopStories.BufferBlock.LinkTo(traverseTopStoriesComments.Block, LinkOptions);
            traverseTopStoriesComments.BufferBlock.LinkTo(computeTopCommentsPerStory.Block, LinkOptions);

            // Triggering the start of the pipeline
            getTopStories.Block.Post(Uri);
            getTopStories.Block.Complete();

            // Buffer at the end of the pipeline
            var receiveAllStories = Task.Run(() =>
               {
                   for (int i = 0; i < Constants.NBSTORIES; i++)
                   {
                       TopStories.Add(computeTopCommentsPerStory.BufferBlock.Receive()); // Wait for all the Top Story objects to arrive
                   }
               });

            Task.WaitAny(receiveAllStories);
            computeTopCommentsPerStory.BufferBlock.Complete(); //End of the pipeline

            CommentsRegistry = traverseTopStoriesComments.CommentsRegistry;
            PrintResults();
        }

        void PrintResults()
        {
            foreach (var topStory in TopStories)
            {
                Console.Write(topStory.Title);

                foreach (var topComment in topStory.TopComments)
                {
                    Console.Write(" | {0} ( {1} for story - {2} total)", topComment.Key, topComment.Value, CommentsRegistry[topComment.Key]);
                }

                Console.WriteLine();
            }
        }

        private string Uri { get; set; } = uri;
        DataflowLinkOptions LinkOptions { get; set; } = linkOptions;
        List<TopStory> TopStories { get; set; } = [];
        ConcurrentDictionary<string, int> CommentsRegistry { get; set; } = new ConcurrentDictionary<string, int>();
    }
}
