using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Models
{
    class TopStory
    {
        public TopStory(string title, ConcurrentDictionary<string, int> comments)
        {
            this.Title = title;
            this.Comments = comments;
        }

        public string Title { get; set; }
        public ConcurrentDictionary<string, int> Comments { get; set; }
        public List<KeyValuePair<string, int>> TopComments { get; set; }
    }
}