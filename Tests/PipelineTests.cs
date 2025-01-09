using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;
using System.Collections.Concurrent;
using HackerNews.blocks;
using HackerNews;
using System;
using RichardSzalay.MockHttp;
using Newtonsoft.Json;
using HackerNews.models;

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
            // Setup Mock
            var mockHttp = new MockHttpMessageHandler();
            List<int> expectedResponse = [1, 2, 3, 4, 5];
            string uri = "https://hacker-news.firebaseio.com/v0/topstories.json";
            mockHttp.When(uri)
                    .Respond("application/json", JsonConvert.SerializeObject(expectedResponse));


            // Setup Test
            var client = new HackerNewsClient(mockHttp.ToHttpClient());
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            GetTopStories getTopStoriesBlock = new(client);
            BufferBlock<ConcurrentBag<int>> resultBlock = new();

            getTopStoriesBlock.Block.LinkTo(resultBlock, linkOptions);
            getTopStoriesBlock.Block.Post(uri);
            getTopStoriesBlock.Block.Complete();

            // Assert
            var expectedResult = new ConcurrentBag<int>(expectedResponse);
            var actualResult = resultBlock.ReceiveAsync().Result;
            CollectionAssert.AreEqual(expectedResult, actualResult);
        }


        [TestMethod]
        public void FilterTopStoriesBlock()
        {

            var mockHttp = new MockHttpMessageHandler();
            var baseUri = "https://hacker-news.firebaseio.com";
            void setupMockItem(Item item)
            {
                var uri = baseUri + "/v0/item/" + item.Id.ToString() + ".json";
                mockHttp.When(uri).Respond("application/json", JsonConvert.SerializeObject(item));
            }

            // Setup Mock
            Item item1 = new(Id: 1, Type: "story");
            Item item2 = new(Id: 2, Type: "comment");
            setupMockItem(item1);
            setupMockItem(item2);

            // Setup HttpClient
            var httpClient = mockHttp.ToHttpClient();
            httpClient.BaseAddress = new Uri(baseUri);
            var client = new HackerNewsClient(httpClient);
            
            // Setup Test
            FilterTopStories filterTopStoriesBlock = new(client);
            BufferBlock<ConcurrentBag<Item>> resultBlock = new();
            var linkOptions = new DataflowLinkOptions { PropagateCompletion = true };

            filterTopStoriesBlock.Block.LinkTo(resultBlock, linkOptions);
            filterTopStoriesBlock.Block.Post([item1.Id, item2.Id]);
            filterTopStoriesBlock.Block.Complete();
            filterTopStoriesBlock.Block.Completion.Wait();

            var actualResult = resultBlock.ReceiveAsync().Result;

            // Asserts
            Assert.AreEqual(1, actualResult.Count);
            actualResult.TryPeek(out Item actualItem);
            Assert.AreEqual(item1, actualItem);
        }

        // [TestMethod]
        // public void TraverseTopStoriesCommentsBlock()
        // {
        //     Item story = new(21021709)
        //     {
        //         Kids = new List<int>(21021916),
        //         Dead = false,
        //         Deleted = false
        //     };

        //     GetComments traverseTopStoriesComments = new();
        //     traverseTopStoriesComments.Block.Post(story);

        //     var result = traverseTopStoriesComments.BufferBlock.ReceiveAsync().Result;
        //     Assert.IsNotNull(result.Comments);
        // }

        // [TestMethod]
        // public void ComputeTopCommentsPerStoryBlock()
        // {
        //     ConcurrentDictionary<string, int> comments = new();
        //     comments.TryAdd("a", 1);
        //     comments.TryAdd("b", 2);
        //     comments.TryAdd("c", 3);
        //     comments.TryAdd("d", 4);
        //     comments.TryAdd("e", 5);
        //     comments.TryAdd("f", 6);
        //     comments.TryAdd("g", 7);
        //     comments.TryAdd("h", 8);
        //     comments.TryAdd("i", 9);
        //     comments.TryAdd("j", 10);
        //     comments.TryAdd("k", 11);
        //     comments.TryAdd("l", 12);

        //     TopStory topStory = new("US takes 'richest nation on earth' crown from Switzerland", comments, []);
        //     maybe traverseTopStoriesComments = new();
        //     traverseTopStoriesComments.Block.Post(topStory);

        //     var topStoryWithTopComments = traverseTopStoriesComments.BufferBlock.ReceiveAsync().Result;

        //     Assert.IsNotNull(topStoryWithTopComments.TopComments);
        //     Assert.AreEqual(Constants.NB_TOP_COMMENTERS, topStoryWithTopComments.TopComments.Count);
        // }
    }
}
