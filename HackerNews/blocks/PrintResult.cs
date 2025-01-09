using HackerNews.models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace HackerNews.blocks
{
    public class PrintResult
    {
        public ActionBlock<ConcurrentBag<TopStory>> Block = new(topStories =>
        {
            var commentsRegistry = GetCommentsRegistry(topStories);

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

        static private ConcurrentDictionary<string, int> GetCommentsRegistry(ConcurrentBag<TopStory> topStories)
        {
            var mergedComments = new ConcurrentDictionary<string, int>();
            var itComments = topStories.Select(s => s.Comments);

            foreach (var comments in itComments)
            {
                foreach (var kvp in comments)
                {
                    mergedComments.AddOrUpdate(kvp.Key, kvp.Value, (_, value) => kvp.Value + value);
                }
            }
            return mergedComments;
        }
    }
}