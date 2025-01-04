using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HackerNews.models
{
    public class TopStory(string title, ConcurrentDictionary<string, int> comments, List<KeyValuePair<string, int>> topComments = null)
    {
        public string Title { get; set; } = title;
        public ConcurrentDictionary<string, int> Comments { get; set; } = comments;
        public List<KeyValuePair<string, int>> TopComments { get; set; } = topComments;
    }
}
