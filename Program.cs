// Chatgpt test data server program
// non-persistent storage of data

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);

// explicitly configure Kestrel endpoints
builder.WebHost.UseKestrel(options =>
{
	options.Listen(System.Net.IPAddress.Any, 5000);
});

// Register SQlite TestServerDbContext
builder.Services.AddDbContext<TestServerDbContext>(options =>
    options.UseSqlite("Data Source=TestServerData.db"));

builder.Services.AddControllers();

var app = builder.Build();

// automatically create the db file if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TestServerDbContext>();
    db.Database.EnsureCreated();
}

// handles exceptions globally
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { 
            error = "An internal server error occurred.",
            message = "Check service logs for details." 
        });
    });
});

// Health Check
app.MapGet("/health", () => Results.Content($"Service is running.{Environment.NewLine}"));

app.MapControllers();
app.Run();

// ===== Models =====
public class DataItem
{
    public string Key { get; set; } = "";
    public int Value { get; set; }
}

// ===== In-Memory Store =====
public static class DataStore
{
	public static ConcurrentDictionary<string, int> Data = new();
	public static int Count()	
	{
	    return Data.Count;
	}
}

// ===== Controller =====
[ApiController]
[Route("api/data")]
public class DataController : ControllerBase
{
    private readonly TestServerDbContext _context;
    private readonly ILogger<DataController> _logger;

    public DataController(TestServerDbContext context, ILogger<DataController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Upload(DataItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Key))
        {
            _logger.LogWarning("Upload rejected: empty key");
            return BadRequest("Key is required.");            
        }
        
        // Error out if key already exists
        if (await _context.DataItems.AnyAsync(d => d.Key == item.Key))
        {
            _logger.LogWarning(
                "Upload rejected: duplicate key. Key={Key}, From={IP}",
                item.Key,
                HttpContext.Connection.RemoteIpAddress?.ToString()
            );
            
            return Conflict(new
            {
                error = "Key already exists.",
                key = item.Key
            });
        }

        _context.DataItems.Add(item);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Data uploaded successfully: Key={Key}, Value={Value}, From={IP}",
            item.Key,
            item.Value,
            HttpContext.Connection.RemoteIpAddress?.ToString()
        );

        return Ok();
    }

    // Return value linked with specified key
    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        var item = await _context.DataItems.FindAsync(key);

        if (item == null)
        {
            _logger.LogWarning(
                "Data requested but not found: Key={Key}", 
                key
            );
            return NotFound();
        }

        return Ok(item);
    }
    
    // Return list of all keys and values
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var allData = await _context.DataItems.ToListAsync();

            if (allData.Count == 0)
            {
                return Conflict(new
                {                
                error = "No data has been stored."
                });
            }
            
            _logger.LogInformation("All data retrieved. Count: {Count}", allData.Count);
            return Ok(allData);         
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve all data.");
            return StatusCode(500, "Internal error while fetching data.");
        }
    }
}