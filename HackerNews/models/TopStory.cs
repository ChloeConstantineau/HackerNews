using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Models
{
    public class TopStory
    {
        public TopStory(string title, ConcurrentDictionary<string, int> comments, List<KeyValuePair<string, int>> topComments = null)
        {
            this.Title = title;
            this.Comments = comments;
            this.TopComments = null;
        }

        public string Title { get; set; }
        public ConcurrentDictionary<string, int> Comments { get; set; }
        public List<KeyValuePair<string, int>> TopComments { get; set; }
    }
}
