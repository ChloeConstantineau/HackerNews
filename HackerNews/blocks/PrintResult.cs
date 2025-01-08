using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using HackerNews.models;

namespace HackerNews.blocks
{
    public class PrintResult
    {
        public ActionBlock<ConcurrentBag<TopStory>> Block = new(topStories =>
        {
            var commentsRegistry = GetCommentsRegistry(topStories);

            Console.WriteLine("Top Story Count: {0}", topStories.Count);

            Parallel.ForEach(topStories, topStory =>
            {
                StringBuilder sb = new();

                sb.AppendFormat("> {0}", topStory.Title);

                List<KeyValuePair<string, int>> sortedTopComments = [.. topStory.Comments.ToArray().OrderByDescending(c => c.Value)];

                for (int n = 0; n < Math.Min(sortedTopComments.Count, Constants.NB_TOP_COMMENTERS); n++)
                {
                    var username = sortedTopComments[n].Key;
                    sb.AppendFormat("\n | {0} ( {1} for story - {2} total)", username, sortedTopComments[n].Value, commentsRegistry[username]);
                }
                Console.WriteLine(sb.ToString());
            });
        });



        static private Dictionary<string, int> GetCommentsRegistry(ConcurrentBag<TopStory> topStories)
        {
            var mergedComments = new Dictionary<string, int>();
            var storyComments = topStories.Select(s => s.Comments);

            foreach (var comments in storyComments)
            {
                foreach (var kvp in comments)
                {
                    if (!mergedComments.ContainsKey(kvp.Key))
                    {
                        mergedComments[kvp.Key] = kvp.Value;
                    }
                }
            }
            return mergedComments;
        }
    }
}