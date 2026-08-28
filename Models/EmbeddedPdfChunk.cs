namespace ActivityRag.Models;

public class EmbeddedPdfChunk
{
    public PdfChunk Chunk {get; set;} = new PdfChunk();

    public float[] Embedding {get; set;} = [];
}