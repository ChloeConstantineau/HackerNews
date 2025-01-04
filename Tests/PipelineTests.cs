using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;
using System.Collections.Concurrent;
using HackerNews.models;
using HackerNews.blocks;
using HackerNews;

//* This project tests all four blocks used in the pipeline solution *//
//* Future iteration should mock the api service                     *//

namespace Tests
{
    [TestClass]
    public class PipelineTests
    {
        [TestMethod]
        public void GetTopStoriesBlock()
        {
            string uri = "https://hacker-news.firebaseio.com/v0/topstories.json";
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            BufferBlock<Queue<string>> bufferBlock = new();
            GetTopStoriesBlock getTopStoriesBlock = new();
            getTopStoriesBlock.Block.LinkTo(bufferBlock, linkOptions);

            getTopStoriesBlock.Block.Post(uri);
            getTopStoriesBlock.Block.Complete();
            var result = bufferBlock.ReceiveAsync().Result;
            Assert.IsNotNull(result);
        }


        [TestMethod]
        public void FilterTopStoriesBlock()
        {
            Queue<string> ids = new();
            ids.Enqueue("21018236");
            ids.Enqueue("21020682");
            ids.Enqueue("21020682");

            FilterTopStoriesBlock filterTopStoriesBlock = new();
            filterTopStoriesBlock.Block.Post(ids);

            List<Item> results = [];

            for (int i = 0; i < ids.Count; i++)
            {
                var item = filterTopStoriesBlock.BufferBlock.ReceiveAsync().Result;
                results.Add(item);
            }

            Assert.AreEqual(results.Count, ids.Count);
        }

        [TestMethod]
        public void TraverseTopStoriesCommentsBlock()
        {
            Item story = new()
            {
                Id = 21021709,
                Kids = new List<int>(21021916),
                Dead = false,
                Deleted = false
            };

            TraverseTopStoriesCommentsBlock traverseTopStoriesComments = new();
            traverseTopStoriesComments.Block.Post(story);

            var result = traverseTopStoriesComments.BufferBlock.ReceiveAsync().Result;
            Assert.IsNotNull(result.Comments);
        }

        [TestMethod]
        public void ComputeTopCommentsPerStoryBlock()
        {
            ConcurrentDictionary<string, int> comments = new();
            comments.TryAdd("a", 1);
            comments.TryAdd("b", 2);
            comments.TryAdd("c", 3);
            comments.TryAdd("d", 4);
            comments.TryAdd("e", 5);
            comments.TryAdd("f", 6);
            comments.TryAdd("g", 7);
            comments.TryAdd("h", 8);
            comments.TryAdd("i", 9);
            comments.TryAdd("j", 10);
            comments.TryAdd("k", 11);
            comments.TryAdd("l", 12);

            TopStory topStory = new("US takes 'richest nation on earth' crown from Switzerland", comments);
            ComputeTopCommentsPerStoryBlock traverseTopStoriesComments = new();
            traverseTopStoriesComments.Block.Post(topStory);

            var topStoryWithTopComments = traverseTopStoriesComments.BufferBlock.ReceiveAsync().Result;

            Assert.IsNotNull(topStoryWithTopComments.TopComments);
            Assert.AreEqual(Constants.NBOFCOMMENTERS, topStoryWithTopComments.TopComments.Count);
        }
    }
}
