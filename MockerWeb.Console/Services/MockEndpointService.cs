using MockerWeb.Console.Models;
using System.Text.Json;

namespace MockerWeb.Console.Services;

public class MockEndpointService
{
    public List<MockEndpoint> Endpoints { get; } = new();

    public MockEndpointService(IWebHostEnvironment env)
    {
        var configDir = Path.Combine(env.ContentRootPath, "Config");
        foreach (var file in Directory.GetFiles(configDir, "*.json"))
        {
            var json = File.ReadAllText(file);
            var endpoints = JsonSerializer.Deserialize<List<MockEndpoint>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (endpoints != null)
                Endpoints.AddRange(endpoints);
        }
    }

    public MockEndpoint? Match(HttpRequest request, JsonElement? bodyJson, out Dictionary<string, string> routeParams)
    {
        var method = request.Method;
        var pathTokens = request.Path.Value?.Trim('/').Split('/') ?? Array.Empty<string>();
        var query = request.Query;
        routeParams = new();

        foreach (var endpoint in Endpoints)
        {
            if (!endpoint.Method.Equals(method, StringComparison.OrdinalIgnoreCase))
                continue;

            var endpointTokens = endpoint.RouteTokens;
            if (endpointTokens.Length != pathTokens.Length)
                continue;

            bool matched = true;
            for (int i = 0; i < endpointTokens.Length; i++)
            {
                if (endpointTokens[i].StartsWith("{") && endpointTokens[i].EndsWith("}"))
                {
                    var key = endpointTokens[i].Trim('{', '}');
                    routeParams[key] = pathTokens[i];
                }
                else if (!endpointTokens[i].Equals(pathTokens[i], StringComparison.OrdinalIgnoreCase))
                {
                    matched = false;
                    break;
                }
            }

            if (!matched)
                continue;

            if (endpoint.Query != null && !endpoint.Query.All(q =>
                query.TryGetValue(q.Key, out var val) && val == q.Value))
                continue;

            if (endpoint.BodyMatch != null && (!bodyJson.HasValue || !endpoint.BodyMatch.All(m =>
                bodyJson.Value.TryGetProperty(m.Key, out var val) && val.ToString() == m.Value)))
                continue;

            return endpoint;
        }

        return null;
    }
}