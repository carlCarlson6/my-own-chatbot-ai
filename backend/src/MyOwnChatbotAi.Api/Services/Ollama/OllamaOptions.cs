namespace MyOwnChatbotAi.Api.Services.Ollama;

public sealed class OllamaOptions
{
    public const string SectionName = "Ollama";
    public string BaseUrl { get; set; } = "http://127.0.0.1:11434";
    public string DefaultModel { get; set; } = "llama3.1";
    public IReadOnlyList<string> AllowedModels { get; set; } = ["llama3.1", "mistral"];
    public int TimeoutSeconds { get; set; } = 120;
}
