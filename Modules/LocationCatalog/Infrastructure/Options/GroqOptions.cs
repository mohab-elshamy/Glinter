namespace Glinter.Modules.LocationCatalog.Infrastructure.Options;

public sealed class GroqOptions
{
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1/";

    public string Model { get; set; } = "llama-3.3-70b-versatile";

    public string? ApiKey { get; set; }

    public double Temperature { get; set; } = 0.1;

    public int MaxTokens { get; set; } = 500;
}