using Microsoft.EntityFrameworkCore;
using UrlShortener._7.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("PostgreSQLConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/shorturl", async (UrlDto url, AppDbContext db, HttpContext ctx) =>
{
    // Validating input url
    if (!Uri.TryCreate(url.Url, UriKind.Absolute, out var inputUrl))
        return Results.BadRequest(error: "Invalid url has been provided");

    // Creating a short version of the provided url
    var random = new Random();
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789@az";
    var shortUrl = new string(Enumerable.Repeat(chars, 6)
        .Select(s => s[random.Next(s.Length)]).ToArray());

    // Mapping the short with the long url
    var urlShort = new UrlShort
    {
        Url = url.Url,
        ShortUrl = shortUrl
    };

    // Saving the mapping to the database
    await db.Urls.AddAsync(urlShort);
    await db.SaveChangesAsync();

    // returning the short url
    var result = $"{ctx.Request.Scheme}://{ctx.Request.Host}/{shortUrl}";

    return Results.Ok(new UrlShortResponseDto()
    {
        Url = result,
    });
});

app.Run();

class AppDbContext : DbContext
{
    public virtual DbSet<UrlShort> Urls { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {

    }
}