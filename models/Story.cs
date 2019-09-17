
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Models
{
    class Story
    {
        public Story(string title, int id, List<int> immediateKids)
        {
            this.Title = title;
            this.Id = id;
            this.Comments = null;
            this.ImmdiateKids = immediateKids;
        }
        public string Title { get; set; }
        public int Id { get; set; }
        public List<int> ImmdiateKids {get; set;}
        public ConcurrentDictionary<string, int> Comments { get; set; }
    }
}