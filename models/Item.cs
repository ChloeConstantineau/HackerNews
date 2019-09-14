using System.Collections.Generic;
class Item {
    public string by { get; set; }
    public int descendants { get; set; }
    public int id { get; set; }
    public bool deleted { get; set; }
    public List<int> kids { get; set; }
    public int score { get; set; }
    public int time { get; set; }
    public string title { get; set; }
    public string type { get; set; }
    public string url { get; set; }
    public string text { get; set; }
    public bool dead { get; set; }
    public int parent { get; set; }
    public int poll { get; set; }
    public List<int> parts { get; set; }
}