using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using Models;

namespace Block
{
    class ComputeTopCommentsPerStoryBlock
    {
        public ComputeTopCommentsPerStoryBlock()
        {
            this.bufferBlock = new BufferBlock<TopStory>();

            this.block = new ActionBlock<TopStory>(topStory =>
            {
                Run(topStory);

            });
        }
        private void Run(TopStory topStory)
        {
            var topComments = (topStory.Comments.Count >= Constants.NBOFCOMMENTATORS) ? getTopCommentators(topStory.Comments) : new List<KeyValuePair<string, int>>(topStory.Comments.ToArray());
            topStory.TopComments = topComments;
            System.Console.WriteLine("Posting story: " + topStory.Title);
            this.bufferBlock.Post(topStory);

        }

        List<KeyValuePair<string, int>> getTopCommentators(ConcurrentDictionary<string, int> comments)
        {
            List<KeyValuePair<string, int>> topComments = new List<KeyValuePair<string, int>>(comments.ToArray());
            List<KeyValuePair<string, int>> SortedTopComments = topComments.OrderByDescending(c => c.Value).ToList();

            return SortedTopComments.GetRange(0, 9);
        }

        public ActionBlock<TopStory> block { get; set; }
        public BufferBlock<TopStory> bufferBlock { get; set; }

    }
}