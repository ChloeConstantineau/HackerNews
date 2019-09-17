using System.Collections.Generic;

namespace Models{

    class Commentator{
        public string id {get; set;}
        public Dictionary<string, int> CommentCOuntPerStory {get; set;}
        public int totalCommentCount {get; set;}
    }
}