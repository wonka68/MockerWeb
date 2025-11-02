using HandlebarsDotNet;
using Microsoft.AspNetCore.Mvc;
using MockerWeb.Console.Services;
using System.Text.Json;

namespace MockerWeb.Console.Controllers;

[ApiController]
[Route("{**catchAll}")]
public class DynamicMockController : ControllerBase
{
    private readonly MockEndpointService _service;

    public DynamicMockController(MockEndpointService service)
    {
        _service = service;
    }

    [HttpGet, HttpPost, HttpPut, HttpDelete, HttpPatch]
    public async Task<IActionResult> Handle()
    {
        JsonElement? bodyJson = null;

        if (Request.Path == "/")
            return NotFound();

        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        if (!string.IsNullOrWhiteSpace(body))
            bodyJson = JsonSerializer.Deserialize<JsonElement>(body);

        var match = _service.Match(Request, bodyJson, out var routeParams);
        if (match == null)
            return NotFound(new { error = "No mock configured for this route." });

        var template = Handlebars.Compile(match.Response.BodyTemplate);
        var data = new
        {
            route = routeParams,
            query = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString()),
            body = bodyJson?.Deserialize<Dictionary<string, object>>() ?? new Dictionary<string, object>()
        };

        var rendered = template(data);
        var jsonDoc = JsonDocument.Parse(rendered);
        return StatusCode(match.Response.Status, jsonDoc.RootElement);
    }
}
