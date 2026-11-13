using System.Text.Json;
using FindYourFood.Discovery.Application.DTOs;
using FindYourFood.Discovery.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FindYourFood.Discovery.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/discovery/search")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class RestaurantSearchController(RestaurantSearch search) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(4096)]
    public async Task<ActionResult<SearchResults>> Search(SearchRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.City) || request.City.Length > 80 ||
            request.City.Any(char.IsControl) || request.Query?.Length > 100 || request.Tag?.Length > 40 ||
            request.Page is < 1 or > 500 ||
            !((request.Latitude is null && request.Longitude is null) ||
                (request.Latitude is >= -90 and <= 90 && request.Longitude is >= -180 and <= 180)))
        {
            return Problem(statusCode: 400, title: "Check your city, search terms and location.");
        }

        try { return Ok(await search.SearchAsync(request, ct)); }
        catch (Exception error) when (error is SearchUnavailableException or HttpRequestException or JsonException ||
            (error is OperationCanceledException && !ct.IsCancellationRequested))
        {
            return Problem(statusCode: 503, title: "Search is temporarily unavailable. Try again.");
        }
    }
}
