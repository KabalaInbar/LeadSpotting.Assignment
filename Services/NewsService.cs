using System.Net;
using System.Text.Json;
using System.Web;
using LeadSpotting.Assignment.Models;


namespace LeadSpotting.Assignment.Services;

public class NewsService : INewsService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NewsService> _logger;
    private readonly string _apiKey;

    public NewsService(
        IHttpClientFactory httpClientFactory,
        ILogger<NewsService> logger,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKey = configuration["NewsApi:ApiKey"] ?? throw new InvalidOperationException("NewsApi:ApiKey configuration is missing");
    }


        public async Task<NewsResponse> GetNewsByQueryAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        try
        {
            var client = _httpClientFactory.CreateClient("NewsApi");
            var requestUri = BuildRequestUri(query);
            
            _logger.LogInformation("Fetching news for query: {Query}", query);
            
            var response = await client.GetAsync(requestUri, cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("External API returned {StatusCode}: {Error}", response.StatusCode, errorContent);
                
                if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    throw new HttpRequestException("Invalid or missing API key", null, response.StatusCode);
                }
                
                throw new HttpRequestException($"External API error: {response.StatusCode}", null, response.StatusCode);
            }

            var externalResponse = await response.Content.ReadFromJsonAsync<ExternalNewsResponse>(cancellationToken: cancellationToken);
            
            if (externalResponse == null || externalResponse.Status != "ok")
            {
                _logger.LogError("External API returned error status or empty response: {Status}, {Message}", 
                    externalResponse?.Status, externalResponse?.Message);
                throw new HttpRequestException(externalResponse?.Message ?? "Failed to fetch news from external provider");
            }

            return MapToNewsResponse(externalResponse, query);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching news for query: {Query}", query);
            throw;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout while fetching news for query: {Query}", query);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching news for query: {Query}", query);
            throw;
        }
    }

        private string BuildRequestUri(string query)
    {
        var queryParams = HttpUtility.ParseQueryString(string.Empty);
        
        queryParams["q"] = query;
        queryParams["apiKey"] = _apiKey;
        
        return $"?{queryParams}";
    }


    private NewsResponse MapToNewsResponse(ExternalNewsResponse externalResponse, string query)
    {
        return new NewsResponse
        {
            Query = query,
            Articles = externalResponse.Articles.Select(a => new ArticleDto
            {
                Title = a.Title,
                Description = a.Description,
                Source = a.Source.Name,
                PublishedAt = a.PublishedAt,
                Url = a.Url
            }).ToList()
        };
    }
}

