namespace ActivityRag.Models;

public class EmbeddedActivity
{
    public ActivityRecord Activity{get; set; } = new();

    public float[] Embedding {get; set; } = [];
    
}