# LeadSpotting News API

## Overview

A lightweight ASP.NET Core Web API that fetches news data from NewsAPI.org, parses and filters relevant fields, and returns a clean, structured response to clients.

This is a technical home assignment for a Senior Full Stack Developer position, demonstrating clean architecture, proper separation of concerns, and production-minded code practices using **.NET 10**.

## Architecture & Implementation

### Design Principles

- **Separation of Concerns**: Controllers handle HTTP concerns and validation, while `NewsService` handles business logic and external communication.
- **Dependency Injection**: All services, including `IHttpClientFactory` and `IConfiguration`, are managed via the built-in DI container.
- **DTO Separation**: Internal contracts (`NewsResponse`, `ArticleDto`) are strictly decoupled from the external API model (`ExternalNewsResponse`).
- **Modern C# Patterns**: Extensive use of `async/await`, `CancellationToken` support for all I/O, and Nullable Reference Types.
- **Efficient Processing**: External JSON is processed using stream-based deserialization (`ReadFromJsonAsync`) to minimize memory allocations.
- **Structured Logging**: All key operations and error states are logged using standard .NET logging abstractions.

### Project Structure

```
LeadSpotting.Assignment.slnx        # Solution file (references both projects below)
LeadSpotting.Assignment.csproj      # Main project file
README.md                           # This file
requests.http                       # Manual/Postman-style HTTP requests

Controllers/
└── NewsController.cs              # GET /api/news endpoint and request validation
Services/
├── INewsService.cs                # Service interface contract
└── NewsService.cs                 # External API integration via IHttpClientFactory
Models/
├── ArticleDto.cs                  # Article DTO for response
├── NewsResponse.cs                # API response contract
├── NewsErrorResponse.cs           # Unified error response contract
└── ExternalNewsResponse.cs        # NewsAPI.org specific response models
Program.cs                          # Application bootstrap and DI registration
appsettings.json                    # Configuration settings (BaseUrl only; API key is not stored here)

LeadSpotting.Assignment.Tests/
├── LeadSpotting.Assignment.Tests.csproj
└── NewsControllerTests.cs          # xUnit + Moq tests for NewsController

```

## Configuration & API Key

The application requires a valid API key from [NewsAPI.org](https://newsapi.org/) to function.

### Security Practice
The real API key is **NOT** committed to the repository. This is an intentional security practice to prevent sensitive credentials from being exposed. Every developer or reviewer needs to configure their own API key locally before running the application.

### How to Configure

The `NewsService` reads the configuration using the key: `NewsApi:ApiKey`.

`appsettings.json` only contains the public `BaseUrl` setting:
```json
"NewsApi": {
  "BaseUrl": "https://newsapi.org/v2/everything"
}
```
No API key placeholder is stored in `appsettings.json`. This is intentional so that no key value—real or placeholder—ever exists in source control.

**Recommended: .NET User Secrets**
Use **.NET User Secrets** to store your key locally (the project already has a `UserSecretsId` configured):
```bash
dotnet user-secrets set "NewsApi:ApiKey" "your_actual_api_key_here"
```
User Secrets are stored outside the repository (in your user profile), so the key is never committed to git.

> Note: Using `appsettings.Development.json` for the API key is **not recommended**, since that file is currently **not** listed in `.gitignore` and could be committed accidentally. Stick to User Secrets for local development.

## How to Run

### Prerequisites
- .NET 10 SDK

### Steps

All commands below should be executed from the repository root, which already contains
`LeadSpotting.Assignment.csproj`, `LeadSpotting.Assignment.slnx`, and `requests.http`.

1. **Restore dependencies:**
   ```bash
   dotnet restore
   ```

2. **Configure the API Key**:
   Follow the steps in the [Configuration & API Key](#configuration--api-key) section above.

3. **Run the application:**
   ```bash
   dotnet run --project LeadSpotting.Assignment.csproj
   ```

4. **The API will be available at:**
   ```
   http://localhost:5002 (the port configured in Properties/launchSettings.json)
   ```

5. **Run the automated tests:**
   ```bash
   dotnet test LeadSpotting.Assignment.slnx
   ```

6. **Manual testing:**
   The `requests.http` file, located in the repository root, can be used for manual/Postman-style testing (see the [Manual / Postman Testing](#manual--postman-testing) section below).

## API Endpoints

### GET /

Returns a simple service status response to confirm the API is running.

Example response:
```json
{
  "status": "ok",
  "service": "LeadSpotting News API"
}
```

### GET /api/news

Fetches news articles for a given search query.

**Query Parameters:**
- `query` (string, required): The search term for news articles.

**Example Request:**
```
GET http://localhost:5002/api/news?query=technology
```

**Example Response (200 OK):**
```json
{
  "query": "technology",
  "articles": [
    {
      "title": "Example Article Title",
      "description": "A brief description of the article content",
      "source": "Example News Source",
      "publishedAt": "2026-08-17T10:30:00Z",
      "url": "https://example.com/article"
    }
  ]
}
```

## Error Handling

The API returns structured error responses using the following format:
```json
{
  "error": "Error message description",
  "details": "Optional additional details"
}
```

- **400 Bad Request**: Missing or empty `query` parameter.
- **502 Bad Gateway**: External API returned an error (e.g., Invalid API key, rate limit exceeded).
- **504 Gateway Timeout**: Request to NewsAPI.org timed out.
- **500 Internal Server Error**: Unexpected application error.

## HTTP Client Configuration

The `NewsApi` named `HttpClient` is registered in `Program.cs` via `IHttpClientFactory` with the following settings:

- **Client name**: `NewsApi`
- **BaseAddress**: configured from `NewsApi:BaseUrl` in configuration
- **User-Agent**: `LeadSpottingAssignment/1.0`
- **Timeout**: 10 seconds

## Design Decisions

1.  **No Extra NuGet Packages (Production Project)**: The production project (`LeadSpotting.Assignment.csproj`) relies exclusively on built-in .NET and ASP.NET Core libraries, avoiding unnecessary third-party dependencies to keep the solution lightweight and secure. The separate test project (`LeadSpotting.Assignment.Tests`) legitimately depends on `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`, and `Moq`, which are standard, expected testing dependencies and do not affect the production project.
2.  **Mapping Isolation**: Mapping logic is encapsulated within the service, ensuring that changes in the external API schema only affect one class.
3.  **Resilience Ready**: Used `IHttpClientFactory` to ensure the application is ready for future integration of resilience policies (like Polly).

## Trade-offs & Future Improvements

### Trade-offs
- **Coupling**: The internal models are currently mapped directly from NewsAPI.org. A more complex application might use a provider-agnostic abstraction if multiple news sources were required.
- **No Caching**: News results are fetched fresh for every request. This keeps the logic simple for the assignment but would be a performance bottleneck in production.

### Future Improvements
1.  **Caching**: Implement `IMemoryCache` or Redis to reduce API calls and improve latency.
2.  **Resilience**: Add retry policies and circuit breakers using Polly.
3.  **Testing**: Add direct unit tests for `NewsService`'s mapping logic and external API error handling (currently only exercised indirectly via mocked `INewsService` in the controller tests), and consider integration tests against the real HTTP pipeline.
4.  **Validation**: Integrate `FluentValidation` for more robust input handling.
5.  **Documentation**: Add Swagger/OpenAPI UI for interactive testing.

## Testing

The solution includes a dedicated test project: **`LeadSpotting.Assignment.Tests`**, using **xUnit** as the test framework and **Moq** for mocking dependencies.

- **Test project**: `LeadSpotting.Assignment.Tests/LeadSpotting.Assignment.Tests.csproj`
- **Test file**: `NewsControllerTests.cs`
- **Current status**: 8 tests, all passing
- **Run tests**:
  ```bash
  dotnet test LeadSpotting.Assignment.slnx
  ```

### Scenarios Covered

The tests isolate `NewsController` by mocking `INewsService`, and cover:

- **Invalid query** (`null`, empty string, whitespace) → `400 Bad Request`, and verifies the service is never called for invalid input.
- **Valid query** → `200 OK` with the expected `NewsResponse` (query and articles), and verifies `INewsService.GetNewsByQueryAsync` was called exactly once with the correct query and cancellation token.
- **Valid query with no results** → `200 OK` with an empty `Articles` collection.
- **`HttpRequestException` from the service** → `502 Bad Gateway` with a `NewsErrorResponse`.
- **`OperationCanceledException` from the service** → `504 Gateway Timeout` with a `NewsErrorResponse`.
- **Unexpected exception from the service** → `500 Internal Server Error` with a `NewsErrorResponse`.

### Known Testing Gap (Honest Disclosure)

The tests above validate `NewsController`'s behavior in isolation, using a mocked `INewsService`. The actual HTTP call and JSON-mapping logic inside `NewsService.cs` (building the request URI, classifying external API status codes such as 401/403 into `HttpRequestException`, and mapping `ExternalNewsResponse` to `NewsResponse`) is **not yet covered by direct unit tests**. This is a known gap, listed under Future Improvements.

## Manual / Postman Testing

In addition to the automated xUnit tests, the API can be exercised manually. A `requests.http` file is included in the repository root for quick manual testing (e.g., via the VS Code REST Client extension or Visual Studio's HTTP file support).

Recommended manual test scenarios:

| Request | Expected Result |
|---------|------------------|
| `GET /api/news?query=Microsoft` | `200 OK` with a `NewsResponse` containing matching articles |
| `GET /api/news?query=` | `400 Bad Request` |
| `GET /api/news` (no query parameter) | `400 Bad Request` |
| `GET /api/news?query=<unlikely search term>` | `200 OK` with an empty `Articles` array |

**Why both?** The xUnit tests isolate the controller's logic using mocks (fast, deterministic, no external dependencies). Manual/Postman testing exercises the real HTTP pipeline end-to-end — routing, model binding, the real `NewsService` implementation, an actual call to NewsAPI.org, and real serialization — confirming the system works correctly when fully wired together.

## AI Usage

AI assistance was used throughout this project for code generation, code review, debugging, and test development (including the xUnit/Moq test suite in `LeadSpotting.Assignment.Tests`). All AI-generated code was manually reviewed, tested, and validated against the assignment requirements before being included in the final solution.

