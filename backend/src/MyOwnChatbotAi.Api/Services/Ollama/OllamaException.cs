namespace MyOwnChatbotAi.Api.Services.Ollama;

public sealed class OllamaException : Exception
{
    public OllamaException(string message) : base(message) { }
    public OllamaException(string message, Exception inner) : base(message, inner) { }
}
