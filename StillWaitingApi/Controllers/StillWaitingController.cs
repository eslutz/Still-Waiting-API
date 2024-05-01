using System.Net;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using StillWaitingApi.Models;

namespace StillWaitingApi.Controllers;

[ApiController]
[Route("api")]
public class StillWaitingController(
    ILogger<StillWaitingController> logger,
    IConfiguration configuration) : ControllerBase
{
    private readonly ILogger<StillWaitingController> _logger = logger;
    private readonly IConfiguration _configuration = configuration;

    [HttpGet("healthcheck", Name = "StillWaiting.HealthCheck")]
    [Produces("text/plain")]
    [ProducesResponseType(200)]
    public string HealthCheck()
    {
        _logger.LogInformation("Health Check endpoint called.");
        return "Still Waiting API is up and running!";
    }

    [HttpPost("itemstatus", Name = "StillWaiting.ItemStatus")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(200, Type = typeof(Item))]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<Item>> ItemStatus([FromBody] ItemStatusRequest request)
    {
        _logger.LogInformation("Item Status endpoint called with ID: {ID}", request.Id);
        if (!string.IsNullOrWhiteSpace(request.Id))
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
                response = await container.ReadItemAsync<Item>(request.Id, new PartitionKey(request.Id));
            }
            catch(CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                _logger.LogError("Item not found for ID: {ID} | exception {Exception}", request.Id, ex);
                return NotFound($"Item not found: {request.Id}");
            }
            catch(CosmosException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                _logger.LogError("Unauthorized request: {Request} | exception {Exception}", request.Id, ex);
                return Unauthorized();
            }
            catch(CosmosException ex)
            {
                _logger.LogError("Error with request: {Request} | exception {Exception}", request.Id, ex);
                return StatusCode(500, $"Error finding item {request.Id}: {ex.Message}");
            }
            if (response.StatusCode == HttpStatusCode.OK)
            {
                _logger.LogInformation("Item found: {ID}", request.Id);
                var item = response.Resource;
                _logger.LogDebug("Item found: {Item}", item);
                return item;
            }
            else
            {
                _logger.LogError("CosmosDB return complete, but request unsuccessful: id: {ID} | status code: {StatusCode} | response: {Response}", request.Id, response.StatusCode, response.Resource);
                return NotFound("Item not found");
            }
        }
        else
        {
            _logger.LogError("Bad request: id: {ID}", request.Id);
            return BadRequest("Item ID is required");
        }
    }

    [HttpPut("updateitem", Name = "StillWaiting.UpdateItem")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(200, Type = typeof(Item))]
    [ProducesResponseType(401)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<ActionResult<Item>> UpdateItem([FromBody] Item request)
    {
        var databaseUrl = _configuration.GetValue<string>("DatabaseUrl");
        var databaseName = _configuration.GetValue<string>("Database");
        var containerName = _configuration.GetValue<string>("Container");

        using var dbClient = new CosmosClient(databaseUrl, new DefaultAzureCredential());
        var db = dbClient.GetDatabase(databaseName);
        var container = db.GetContainer(containerName);

        _logger.LogInformation("Item Update endpoint called with ID: {ID}", request.Id);
        ItemResponse<Item> response;
        try
        {
            response = await container.ReplaceItemAsync(request, request.Id, new PartitionKey(request.Id));
        }
        catch(CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogError("Item not found for ID: {ID} | exception {Exception}", request.Id, ex);
            return NotFound("Item not found");
        }
        catch(CosmosException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError("Unauthorized request: {Request} | exception {Exception}", request.Id, ex);
            return Unauthorized();
        }
        catch(CosmosException ex)
        {
            _logger.LogError("Error with request: {Request} | exception {Exception}", request.Id, ex);
            return StatusCode(500, $"Error updating item {request.Id}: {ex.Message}");
        }

        if (response.StatusCode == HttpStatusCode.OK)
        {
            _logger.LogInformation("Item found: {ID}", request.Id);
            var item = response.Resource;
            _logger.LogDebug("Item found: {Item}", item);
            return Ok(item);
        }
        else
        {
            _logger.LogError("Unknown error updating item: {Request}", JsonConvert.SerializeObject(request));
            return StatusCode(500, "Error updating item");
        }
    }
}
