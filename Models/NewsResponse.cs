namespace LeadSpotting.Assignment.Models;

public class NewsResponse
{
    public required string Query { get; set; }
    public required List<ArticleDto> Articles { get; set; } = new();
}
