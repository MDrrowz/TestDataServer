// Chatgpt test data server program
// non-persistent storage of data

using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();

var app = builder.Build();

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
}

// ===== Controller =====
[ApiController]
[Route("api/data")]
public class DataController : ControllerBase
{
    private readonly ILogger<DataController> _logger;

    public DataController(ILogger<DataController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public IActionResult Upload(DataItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Key))
        {
            _logger.LogWarning("Upload rejected: empty key");
            return BadRequest("Key is required.");            
        }
        
        // Error out if key already exists
        if (DataStore.Data.ContainsKey(item.Key))
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

        DataStore.Data[item.Key] = item.Value;

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
    public IActionResult Get(string key)
    {
        if (!DataStore.Data.TryGetValue(key, out int value))
        {
            _logger.LogWarning(
                "Data requested but not found: Key={Key}", 
                key
            );
            return NotFound();
        }

        return Ok(new DataItem { Key = key, Value = value });
    }
    
    // Return list of all keys and values
    [HttpGet]
    public IActionResult GetAll()
    {
        try
        {
            if (DataStore.Count == 0)
            {
                return Conflict(new
                {                
                error = "No data has been stored."
                });
            }
            
            var allData = DataStore.Data.Select(kvp => new DataItem
            {
                Key = kvp.Key,
                Value = kvp.Value
            }).ToList();
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
