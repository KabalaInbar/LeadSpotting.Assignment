using LeadSpotting.Assignment.Controllers;
using LeadSpotting.Assignment.Models;
using LeadSpotting.Assignment.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LeadSpotting.Assignment.Tests;

public class NewsControllerTests
{
    private readonly Mock<INewsService> _newsServiceMock;
    private readonly Mock<ILogger<NewsController>> _loggerMock;
    private readonly NewsController _controller;

    public NewsControllerTests()
    {
        _newsServiceMock = new Mock<INewsService>();
        _loggerMock = new Mock<ILogger<NewsController>>();
        _controller = new NewsController(_newsServiceMock.Object, _loggerMock.Object);
    }

    // Verifies that a missing, empty, or whitespace-only query returns 400
    // and that the news service is not called.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task GetNews_WithInvalidQuery_ReturnsBadRequest(string? query)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act
        var result = await _controller.GetNews(query, cancellationToken);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        var errorResponse = Assert.IsType<NewsErrorResponse>(badRequestResult.Value);

        Assert.Equal("Query parameter is required and cannot be empty", errorResponse.Error);

        // Verify that the service was NEVER called
        _newsServiceMock.Verify(
            x => x.GetNewsByQueryAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // Verifies that a valid query returns 200 with the expected news response
    // and that the service is called exactly once.
    [Fact]
    public async Task GetNews_WithValidQuery_ReturnsOkWithExpectedResponse()
    {
        // Arrange
        const string query = "Microsoft";
        var cancellationToken = CancellationToken.None;
        var expectedResponse = new NewsResponse
        {
            Query = query,
            Articles = new List<ArticleDto>
            {
                new()
                {
                    Title = "Microsoft announces new product",
                    Description = "Details about the announcement",
                    Source = "TechNews",
                    PublishedAt = new DateTime(2024, 1, 1),
                    Url = "https://example.com/article1"
                }
            }
        };

        _newsServiceMock
            .Setup(x => x.GetNewsByQueryAsync(query, cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetNews(query, cancellationToken);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var newsResponse = Assert.IsType<NewsResponse>(okResult.Value);

        Assert.Equal(query, newsResponse.Query);
        Assert.Single(newsResponse.Articles);
        Assert.Equal(expectedResponse.Articles[0].Title, newsResponse.Articles[0].Title);

        _newsServiceMock.Verify(
            x => x.GetNewsByQueryAsync(query, cancellationToken),
            Times.Once);
    }

    // Verifies that an HTTP error from the news service is translated
    // into a 502 Bad Gateway response.
    [Fact]
    public async Task GetNews_WhenServiceThrowsHttpRequestException_ReturnsBadGateway()
    {
        // Arrange
        const string query = "Microsoft";
        var cancellationToken = CancellationToken.None;

        _newsServiceMock
            .Setup(x => x.GetNewsByQueryAsync(query, cancellationToken))
            .ThrowsAsync(new HttpRequestException("External API failure"));

        // Act
        var result = await _controller.GetNews(query, cancellationToken);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status502BadGateway, statusCodeResult.StatusCode);

        var errorResponse = Assert.IsType<NewsErrorResponse>(statusCodeResult.Value);
        Assert.Equal("External news service returned an error", errorResponse.Error);
    }

    // Verifies that a timeout/cancellation from the news service
    // is translated into a 504 Gateway Timeout response.
    [Fact]
    public async Task GetNews_WhenServiceThrowsOperationCanceledException_ReturnsGatewayTimeout()
    {
        // Arrange
        const string query = "Microsoft";
        var cancellationToken = CancellationToken.None;

        _newsServiceMock
            .Setup(x => x.GetNewsByQueryAsync(query, cancellationToken))
            .ThrowsAsync(new OperationCanceledException("Request timed out"));

        // Act
        var result = await _controller.GetNews(query, cancellationToken);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status504GatewayTimeout, statusCodeResult.StatusCode);

        var errorResponse = Assert.IsType<NewsErrorResponse>(statusCodeResult.Value);
        Assert.Equal("Request to external news service timed out", errorResponse.Error);
    }

    // Verifies that an unexpected exception from the news service
    // is translated into a 500 Internal Server Error response.
    [Fact]
    public async Task GetNews_WhenServiceThrowsUnexpectedException_ReturnsInternalServerError()
    {
        // Arrange
        const string query = "Microsoft";
        var cancellationToken = CancellationToken.None;

        _newsServiceMock
            .Setup(x => x.GetNewsByQueryAsync(query, cancellationToken))
            .ThrowsAsync(new InvalidOperationException("Something went wrong"));

        // Act
        var result = await _controller.GetNews(query, cancellationToken);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, statusCodeResult.StatusCode);

        var errorResponse = Assert.IsType<NewsErrorResponse>(statusCodeResult.Value);
        Assert.Equal("An unexpected error occurred", errorResponse.Error);
    }

    // Verifies that a valid query with no matching articles
    // returns 200 with an empty articles collection.
    [Fact]
    public async Task GetNews_WithValidQueryAndNoResults_ReturnsOkWithEmptyArticles()
    {
        // Arrange
        const string query = "ObscureTermWithNoResults";
        var cancellationToken = CancellationToken.None;
        var expectedResponse = new NewsResponse
        {
            Query = query,
            Articles = new List<ArticleDto>()
        };

        _newsServiceMock
            .Setup(x => x.GetNewsByQueryAsync(query, cancellationToken))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _controller.GetNews(query, cancellationToken);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var newsResponse = Assert.IsType<NewsResponse>(okResult.Value);

        Assert.Empty(newsResponse.Articles);
    }
}