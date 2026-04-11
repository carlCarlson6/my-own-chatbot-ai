using MyOwnChatbotAi.Api.Contracts;
using MyOwnChatbotAi.Api.Features.Conversations.Persistence;

namespace MyOwnChatbotAi.Api.Features.Conversations;

internal static class ConversationContractMappings
{
    public static ConversationSummary ToContract(this UserOwnedConversationSummary summary) =>
        new(
            summary.ConversationId,
            summary.Title,
            summary.HasManualTitle,
            summary.Model,
            summary.CreatedAtUtc,
            summary.UpdatedAtUtc,
            summary.Status);
}
