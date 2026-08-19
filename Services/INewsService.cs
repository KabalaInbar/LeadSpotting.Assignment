using LeadSpotting.Assignment.Models;

namespace LeadSpotting.Assignment.Services;

public interface INewsService
{
    Task<NewsResponse> GetNewsByQueryAsync(string query, CancellationToken cancellationToken = default);
}
