using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using System.Threading.Tasks;
using Models;
using System.Collections.Concurrent;
using System.Linq;
using Block;

public static class Constants
{
    public const int NBSTORIES = 30;
    public const int NBOFCOMMENTATORS = 10;
}

namespace HackerNews
{
    class Program
    {
        static void Main(string[] args)
        {

            void printFinalResults(List<TopStory> results)
            {
                foreach (var result in results)
                {
                    System.Console.Write(result.Title);

                    foreach (var topComment in result.TopComments)
                    {
                        //System.Console.Write(" | " + topComment.Key + " (" + topComment.Value + " for story - " + commentsRegistry[topComment.Key] + " total)");

                    }
                    System.Console.WriteLine();
                }

            }

            try
            {

                var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };
                string uri = "https://hacker-news.firebaseio.com/v0/topstories.json";

                GetTopStoriesBlock getTopStories = new GetTopStoriesBlock();
                FilterTopStoriesBlock filterTopStories = new FilterTopStoriesBlock();
                TraverseTopStoriesCommentsBlock traverseTopStoriesComments = new TraverseTopStoriesCommentsBlock();
                ComputeTopCommentsPerStoryBlock computeTopCommentsPerStory = new ComputeTopCommentsPerStoryBlock();

                getTopStories.block.LinkTo(filterTopStories.block, linkOptions);
                filterTopStories.bufferBlock.LinkTo(traverseTopStoriesComments.block, linkOptions);
                traverseTopStoriesComments.bufferBlock.LinkTo(computeTopCommentsPerStory.block, linkOptions);
                getTopStories.block.Post(uri);
                getTopStories.block.Complete();

                List<TopStory> finalResults = new List<TopStory>();
                var receiveAllStories = Task.Run(() =>
                   {
                       for (int i = 0; i <= Constants.NBSTORIES; i++)
                       {
                           finalResults.Add(computeTopCommentsPerStory.bufferBlock.Receive());
                       }
                   });
                Task.WaitAll(receiveAllStories);
                computeTopCommentsPerStory.bufferBlock.Complete();
                printFinalResults(finalResults);

            }
            catch (AggregateException ex)
            {
                System.Console.WriteLine(ex.Flatten());
            }

        }
    }
}