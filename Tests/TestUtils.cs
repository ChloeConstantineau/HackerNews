using HackerNews;
using HackerNews.models;
using Newtonsoft.Json;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;

namespace Tests
{
    public class TestUtils()
    {
        static public HackerNewsClient SetupClient<T>(List<(string, T)> expectedResponses)
        {
            var mockHttp = new MockHttpMessageHandler();
            var baseUri = "https://hacker-news.firebaseio.com";
            expectedResponses.ForEach(r =>
            {
                var uri = baseUri + r.Item1;
                mockHttp.When(uri).Respond("application/json", JsonConvert.SerializeObject(r.Item2));
            });

            var httpClient = mockHttp.ToHttpClient();
            httpClient.BaseAddress = new Uri(baseUri);
            return new HackerNewsClient(httpClient);
        }

        static public string ItemPath(Item item) => "/v0/item/" + item.Id.ToString() + ".json";
    }
}