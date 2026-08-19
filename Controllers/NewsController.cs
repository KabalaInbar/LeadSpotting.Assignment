using LeadSpotting.Assignment.Models;
using LeadSpotting.Assignment.Services;
using Microsoft.AspNetCore.Mvc;

namespace LeadSpotting.Assignment.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NewsController : ControllerBase
{
    private readonly INewsService _newsService;
    private readonly ILogger<NewsController> _logger;

    public NewsController(INewsService newsService, ILogger<NewsController> logger)
    {
        _newsService = newsService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<NewsResponse>> GetNews(
        [FromQuery] string? query,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogWarning("GetNews called with empty query parameter");
            return BadRequest(new NewsErrorResponse
            {
                Error = "Query parameter is required and cannot be empty"
            });
        }

        try
        {
            var result = await _newsService.GetNewsByQueryAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "External API returned an error");
            return StatusCode(
                StatusCodes.Status502BadGateway,
                new NewsErrorResponse { Error = "External news service returned an error" });
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Request timeout");
            return StatusCode(
                StatusCodes.Status504GatewayTimeout,
                new NewsErrorResponse { Error = "Request to external news service timed out" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing news query");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                new NewsErrorResponse { Error = "An unexpected error occurred" });
        }
    }
}
