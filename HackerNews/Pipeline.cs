using HackerNews.blocks;
using System;
using System.Threading.Tasks.Dataflow;

namespace HackerNews
{
    class Pipeline()
    {
        static public void Run()
        {
            var sharedClient = new HackerNewsClient(new() { BaseAddress = new Uri("https://hacker-news.firebaseio.com") });
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
