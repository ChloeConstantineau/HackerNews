using System.Collections.Generic;

namespace HackerNews.models
{
    public record class Item(int Id, string? By = null, int? Descendants = null, bool? Deleted = null, List<int>? Kids = null, int? Score = null, int? Time = null, string? Title = null, string? Type = null, string? Url = null, string? Text = null, bool? Dead = null, int? Parent = null, int? Poll = null, List<int>? Parts = null);
}
