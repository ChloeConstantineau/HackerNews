using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HackerNews.models
{
    public record class TopStory(string Title, ConcurrentDictionary<string, int> Comments);
}
