using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks.Dataflow;
using HackerNews.models;

namespace HackerNews.blocks
{
    //* INPUT : Object TopStory without TopComments property                             *//
    //* OUTPUT : Object TopStory with Top Comments property, throught th block's buffer  *//

    //* This block takes all the comments from a story and computes the top commentors (Default 10) *//
    //* The block sends a response as soon as the topCommentors for a story have been computed      *//

    public class ComputeTopCommentsPerStoryBlock
    {
        public ComputeTopCommentsPerStoryBlock()
        {
            BufferBlock = new BufferBlock<TopStory>();

            Block = new ActionBlock<TopStory>(Run);
        }
        private void Run(TopStory topStory)
        {
            var topComments = topStory.Comments.Count >= Constants.NBOFCOMMENTERS ? GetTopCommentators(topStory.Comments) : new List<KeyValuePair<string, int>>(topStory.Comments.ToArray());
            topStory.TopComments = topComments;
            System.Console.WriteLine("Finished processing story : '{0}'", topStory.Title);
            BufferBlock.Post(topStory);
        }

        List<KeyValuePair<string, int>> GetTopCommentators(ConcurrentDictionary<string, int> comments)
        {
            List<KeyValuePair<string, int>> topComments = [.. comments.ToArray()];
            List<KeyValuePair<string, int>> SortedTopComments = [.. topComments.OrderByDescending(c => c.Value)];

            return SortedTopComments.GetRange(0, Constants.NBOFCOMMENTERS);
        }

        public ActionBlock<TopStory> Block { get; set; }
        public BufferBlock<TopStory> BufferBlock { get; set; }
    }
}
