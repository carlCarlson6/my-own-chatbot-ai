namespace MyOwnChatbotAi.Api.Features.Conversations.Persistence;

public sealed class ConversationPersistenceOptions
{
    public const string SectionName = "ConversationPersistence";

    public string DatabasePath { get; set; } = "App_Data/conversations.sqlite";
}
