using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
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

    class Pipeline()
    {
        public static HttpClient sharedClient = new()
        {
            BaseAddress = new Uri("https://hacker-news.firebaseio.com"),
        };

        static public void Run()
        {
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            GetTopStories getTopStories = new(sharedClient);
            FilterTopStories filterTopStories = new(sharedClient);
            GetComments getComments = new(sharedClient);
            PrintResult printResult = new();

            // Links and interactions betwen each block
            getTopStories.Block.LinkTo(filterTopStories.Block, linkOptions);
            filterTopStories.Block.LinkTo(getComments.Block, linkOptions);
            getComments.Block.LinkTo(printResult.Block, linkOptions);

            // Triggering the start of the pipeline
            getTopStories.Block.Post("/v0/topstories.json");
            getTopStories.Block.Complete();
            printResult.Block.Completion.Wait();
        }
    }
}
