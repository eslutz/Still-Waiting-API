using System.Net;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using StillWaitingApi.Models;

namespace StillWaitingApi.Controllers;

[ApiController]
[Route("[controller]")]
public class StillWaitingController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public StillWaitingController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

   [HttpGet("HealthCheck", Name = "StillWaiting.HealthCheck")]
   [ProducesResponseType(200)]
   [Produces("text/plain")]
    public string HealthCheck()
    {
        return "Still Waiting API is up and running!";
    }

    [HttpPost("ItemStatus", Name = "StillWaiting.ItemStatus")]
    [ProducesResponseType(200, Type = typeof(Item))]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    [Produces("application/json")]
    public async Task<ActionResult<Item>> ItemStatus([FromBody] string id)
    {
        if (!string.IsNullOrWhiteSpace(id))
        {
            var databaseUrl = _configuration.GetValue<string>("DatabaseUrl");
            var databaseName = _configuration.GetValue<string>("Database");
            var containerName = _configuration.GetValue<string>("Container");

            using var dbClient = new CosmosClient(databaseUrl, new DefaultAzureCredential());
            var db = dbClient.GetDatabase(databaseName);
            var container = db.GetContainer(containerName);

            ItemResponse<Item> response;
            try
            {
                response = await container.ReadItemAsync<Item>(id, new PartitionKey(id));
            }
            catch(CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound("Item not found");
            }
            catch(CosmosException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                return Unauthorized();
            }
            catch(CosmosException ex)
            {
                return StatusCode(500, $"Error updating item: {ex.Message}");
            }
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var item = response.Resource;
                return item;
            }
            else
            {
                return NotFound("Item not found");
            }
        }
        else
        {
            return BadRequest("Item ID is required");
        }
    }

    [HttpPut("UpdateItem", Name = "StillWaiting.UpdateItem")]
    [ProducesResponseType(200, Type = typeof(Item))]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    [Produces("application/json")]
    public async Task<ActionResult<Item>> UpdateItem([FromBody] Item item)
    {
        var databaseUrl = _configuration.GetValue<string>("DatabaseUrl");
        var databaseName = _configuration.GetValue<string>("Database");
        var containerName = _configuration.GetValue<string>("Container");

        using var dbClient = new CosmosClient(databaseUrl, new DefaultAzureCredential());
        var db = dbClient.GetDatabase(databaseName);
        var container = db.GetContainer(containerName);

        ItemResponse<Item> response;
        try
        {
            item.ReleaseDate = DateTime.UtcNow;
            response = await container.ReplaceItemAsync(item, item.Id, new PartitionKey(item.Id));
        }
        catch(CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return NotFound("Item not found");
        }
        catch(CosmosException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            return Unauthorized();
        }
        catch(CosmosException ex)
        {
            return StatusCode(500, $"Error updating item: {ex.Message}");
        }

        if (response.StatusCode == HttpStatusCode.OK)
        {
            item = response.Resource;
            return Ok(item);
        }
        else
        {
            return StatusCode(500, "Error updating item");
        }
    }

    // public async Task<OmnipodItunesSearchResponse> GetItunesSearchResults()
    // {
    //     using var client = new HttpClient();
    //     client.DefaultRequestHeaders.Accept.Clear();
    //     client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

    //     var response = await client.GetAsync(searchUrl);
    //     if (response.IsSuccessStatusCode)
    //     {
    //         var searchResults = await response.Content.ReadFromJsonAsync<Item>();
    //         return searchResults ?? new Item();
    //     }
    //     else
    //     {
    //         throw new Exception("Network response was not ok");
    //     }
    // }
}
