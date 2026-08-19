using LeadSpotting.Assignment.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddHttpClient("NewsApi", client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["NewsApi:BaseUrl"]!);

        client.DefaultRequestHeaders.Add(
        "User-Agent",
        "LeadSpottingAssignment/1.0");

    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<INewsService, NewsService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
