using CardsServer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Get Railway DATABASE_URL
var dbUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (string.IsNullOrWhiteSpace(dbUrl))
    throw new Exception("DATABASE_URL env variable missing");

// Convert URL to Npgsql format
string connectionString = ConvertDatabaseUrl(dbUrl);

builder.Services.AddDbContext<AppDb>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Auto-create tables
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.EnsureCreated();
}

// ============ ENTERENCE ENDPOINTS ============

// POST /enter — increase counter
app.MapPost("/enter", async (AppDb db) =>
{
    var record = await db.Enterence.FirstOrDefaultAsync();

    if (record == null)
    {
        record = new Enterence
        {
            Counter = 1,
            LastEnetered = DateTime.UtcNow,
            Text = "Site entries counter"
        };
        db.Enterence.Add(record);
    }
    else
    {
        record.Counter++;
        record.LastEnetered = DateTime.UtcNow;
    }

    await db.SaveChangesAsync();

    return Results.Ok(new
    {
        counter = record.Counter,
        last = record.LastEnetered
    });
});

// GET /enter — return counter info
app.MapGet("/enter", async (AppDb db) =>
{
    var record = await db.Enterence.FirstOrDefaultAsync();

    if (record == null)
        return Results.Ok(new { counter = 0, last = (DateTime?)null });

    return Results.Ok(new
    {
        counter = record.Counter,
        last = record.LastEnetered
    });
});


// ============ REVIEWS ENDPOINTS ============

// POST /reviews — add a review
app.MapPost("/reviews", async (ReviewDto dto, AppDb db) =>
{
    var review = new Review
    {
        Phone = dto.Phone,
        Text = dto.Text,
        CreatedAt = DateTime.UtcNow
    };

    db.Reviews.Add(review);
    await db.SaveChangesAsync();

    return Results.Ok(new { success = true });
});

// GET /reviews — get all reviews
app.MapGet("/reviews", async (AppDb db) =>
{
    var reviews = await db.Reviews
        .OrderByDescending(r => r.CreatedAt)
        .ToListAsync();

    return Results.Ok(reviews);
});

app.Run();


// ===================
// Convert FUNCTION
// ===================
static string ConvertDatabaseUrl(string url)
{
    var uri = new Uri(url);
    var userInfo = uri.UserInfo.Split(':');

    return $"Host={uri.Host};Port={uri.Port};Username={userInfo[0]};Password={userInfo[1]};" +
           $"Database={uri.AbsolutePath.TrimStart('/')};SSL Mode=Require;Trust Server Certificate=true;";
}

