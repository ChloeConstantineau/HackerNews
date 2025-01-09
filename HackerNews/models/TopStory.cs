using System.Collections.Concurrent;

namespace HackerNews.models
{
    public record class TopStory(string Title, ConcurrentDictionary<string, int> Comments);
}