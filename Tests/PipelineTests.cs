using HackerNews.blocks;
using HackerNews.models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;

namespace Tests
{

    [TestClass]
    public class PipelineTests
    {
        public DataflowLinkOptions LinkOptions = new() { PropagateCompletion = true };

        [TestMethod]
        public void Fetch_Top_Stories()
        {
            // Data
            var path = "/v0/topstories.json";
            List<int> expectedResponse = [1, 2, 3, 4, 5];

            // Setup Mock Client
            var client = TestUtils.SetupClient([(path, expectedResponse)]);

            // Setup Test
            GetTopStories getTopStoriesBlock = new(client);
            BufferBlock<ConcurrentBag<int>> resultBlock = new();

            getTopStoriesBlock.Block.LinkTo(resultBlock, LinkOptions);
            getTopStoriesBlock.Block.Post(path);
            getTopStoriesBlock.Block.Complete();
            getTopStoriesBlock.Block.Completion.Wait();

            // Assert
            var expectedResult = new ConcurrentBag<int>(expectedResponse);
            var actualResult = resultBlock.ReceiveAsync().Result;
            CollectionAssert.AreEqual(expectedResult, actualResult);
        }


        [TestMethod]
        public void Filter_Top_Stories()
        {
            // Data
            Item item1 = new(Id: 1, Type: "story");
            Item item2 = new(Id: 2, Type: "comment");

            // Setup Mock Client
            var client = TestUtils.SetupClient([(TestUtils.ItemPath(item1), item1), (TestUtils.ItemPath(item2), item2)]);

            // Setup Test
            FilterTopStories filterTopStoriesBlock = new(client);
            BufferBlock<ConcurrentBag<Item>> resultBlock = new();

            filterTopStoriesBlock.Block.LinkTo(resultBlock, LinkOptions);
            filterTopStoriesBlock.Block.Post([item1.Id, item2.Id]);
            filterTopStoriesBlock.Block.Complete();
            filterTopStoriesBlock.Block.Completion.Wait();

            var actualResult = resultBlock.ReceiveAsync().Result;

            // Asserts
            Assert.AreEqual(1, actualResult.Count);
            actualResult.TryPeek(out Item actualItem);
            Assert.AreEqual(item1, actualItem);
        }

        [TestMethod]
        public void Compute_Comments_Per_Top_Story()
        {
            // Data
            Item item1 = new(Id: 1, Type: "story", Kids: [2]);
            Item item2 = new(Id: 2, Type: "comment", By: "Some Author", Kids: [3, 4]);
            Item item3 = new(Id: 3, Type: "comment", By: "Some Other Author");
            Item item4 = new(Id: 4, Type: "comment", By: "Another Author");

            // Setup Mock Client
            var client = TestUtils.SetupClient([(TestUtils.ItemPath(item1), item1), (TestUtils.ItemPath(item2), item2), (TestUtils.ItemPath(item3), item3), (TestUtils.ItemPath(item4), item4)]);

            // Setup Test
            GetComments computeComments = new(client);
            BufferBlock<ConcurrentBag<TopStory>> resultBlock = new();
            computeComments.Block.LinkTo(resultBlock, LinkOptions);
            computeComments.Block.Post([item1]);

            var actualResult = resultBlock.ReceiveAsync().Result;

            // Asserts
            Assert.AreEqual(1, actualResult.Count);
            actualResult.TryPeek(out TopStory actualStory);
            Assert.AreEqual(3, actualStory.Comments.Count);
        }
    }
}