using System.Net;

namespace ActivityRag.Models;

public class ActivityRecord
{
    public string Id { get; set; } = "";
    public string Title{ get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime RecordedAt { get; set; }
    public string Source{get; set;} = "";
}