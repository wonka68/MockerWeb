namespace MockerWeb.Console.Models;

public class MockEndpoint
{
    public string Method { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public Dictionary<string, string>? Query { get; set; }
    public Dictionary<string, string>? BodyMatch { get; set; }
    public MockResponse Response { get; set; }

    // Derived tokens for matching
    public string[] RouteTokens => Route.Trim('/').Split('/');

}