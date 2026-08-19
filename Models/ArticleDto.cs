namespace LeadSpotting.Assignment.Models;

public class ArticleDto
{
    public required string Title { get; set; }
    public string? Description { get; set; }
    public required string Source { get; set; }
    public required DateTime PublishedAt { get; set; }
    public required string Url { get; set; }
}
