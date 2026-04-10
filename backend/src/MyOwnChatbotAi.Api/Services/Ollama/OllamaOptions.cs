namespace MyOwnChatbotAi.Api.Services.Ollama;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";
    public string BaseUrl { get; set; } = "http://127.0.0.1:11434";
    public string DefaultModel { get; set; } = "mistral";
    public int TimeoutSeconds { get; set; } = 120;
}
