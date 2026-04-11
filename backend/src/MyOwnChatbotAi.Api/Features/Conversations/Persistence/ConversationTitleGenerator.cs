namespace MyOwnChatbotAi.Api.Features.Conversations.Persistence;

public static class ConversationTitleGenerator
{
    private const int MaxGeneratedTitleLength = 100;
    public const string UntitledConversation = "New conversation";

    public static (string Title, bool HasManualTitle) CreateInitialTitle(string? requestedTitle)
    {
        if (string.IsNullOrWhiteSpace(requestedTitle))
        {
            return (UntitledConversation, false);
        }

        return (requestedTitle.Trim(), true);
    }

    public static string CreateDefaultTitleFromFirstMessage(string messageContent)
    {
        var normalized = messageContent.Trim();
        if (normalized.Length <= MaxGeneratedTitleLength)
        {
            return normalized;
        }

        return $"{normalized[..MaxGeneratedTitleLength]}...";
    }
}
