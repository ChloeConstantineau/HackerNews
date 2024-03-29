using System.Collections.Generic;
using System;

namespace Models
{
    //* Api model of the ITEM object *//
    public class Item
    {
        public string By { get; set; }
        public int Descendants { get; set; }
        public int Id { get; set; }
        public bool Deleted { get; set; }
        public List<int> Kids { get; set; }
        public int Score { get; set; }
        public int Time { get; set; }
        public string Title { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }
        public string Text { get; set; }
        public bool Dead { get; set; }
        public int Parent { get; set; }
        public int Poll { get; set; }
        public List<int> Parts { get; set; }
    }
}
