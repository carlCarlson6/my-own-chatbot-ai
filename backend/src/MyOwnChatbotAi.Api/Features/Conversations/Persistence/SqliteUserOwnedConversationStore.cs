using Microsoft.Data.Sqlite;

namespace MyOwnChatbotAi.Api.Features.Conversations.Persistence;

public sealed class SqliteUserOwnedConversationStore(
    SqliteConversationConnectionFactory connectionFactory) : IUserOwnedConversationStore
{
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS conversations (
                conversation_id TEXT PRIMARY KEY,
                owner_user_id TEXT NOT NULL,
                title TEXT NOT NULL,
                has_manual_title INTEGER NOT NULL,
                model TEXT NOT NULL,
                status TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL
            );

            CREATE INDEX IF NOT EXISTS ix_conversations_owner_updated
                ON conversations (owner_user_id, updated_at_utc DESC);

            CREATE TABLE IF NOT EXISTS conversation_messages (
                message_id TEXT PRIMARY KEY,
                conversation_id TEXT NOT NULL,
                sequence INTEGER NOT NULL,
                role TEXT NOT NULL,
                content TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                FOREIGN KEY(conversation_id) REFERENCES conversations(conversation_id) ON DELETE CASCADE
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ix_conversation_messages_conversation_sequence
                ON conversation_messages (conversation_id, sequence);

            CREATE INDEX IF NOT EXISTS ix_conversation_messages_conversation_created
                ON conversation_messages (conversation_id, created_at_utc);
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid conversationId, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT 1
            FROM conversations
            WHERE conversation_id = $conversationId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$conversationId", conversationId.ToString());

        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<UserOwnedConversationHistory?> GetHistoryAsync(
        Guid conversationId,
        string ownerUserId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var summaryCommand = connection.CreateCommand();
        summaryCommand.CommandText =
            """
            SELECT conversation_id, owner_user_id, title, has_manual_title, model, status, created_at_utc, updated_at_utc
            FROM conversations
            WHERE conversation_id = $conversationId
              AND owner_user_id = $ownerUserId
            LIMIT 1;
            """;
        summaryCommand.Parameters.AddWithValue("$conversationId", conversationId.ToString());
        summaryCommand.Parameters.AddWithValue("$ownerUserId", ownerUserId);

        await using var summaryReader = await summaryCommand.ExecuteReaderAsync(cancellationToken);
        if (!await summaryReader.ReadAsync(cancellationToken))
        {
            return null;
        }

        var summary = new UserOwnedConversationSummary(
            ParseGuid(summaryReader, 0),
            summaryReader.GetString(1),
            summaryReader.GetString(2),
            summaryReader.GetBoolean(3),
            summaryReader.GetString(4),
            ParseDateTime(summaryReader, 6),
            ParseDateTime(summaryReader, 7),
            summaryReader.GetString(5));

        await summaryReader.DisposeAsync();

        await using var messageCommand = connection.CreateCommand();
        messageCommand.CommandText =
            """
            SELECT message_id, role, content, created_at_utc, sequence
            FROM conversation_messages
            WHERE conversation_id = $conversationId
            ORDER BY sequence ASC;
            """;
        messageCommand.Parameters.AddWithValue("$conversationId", conversationId.ToString());

        var messages = new List<StoredConversationMessage>();
        await using var messageReader = await messageCommand.ExecuteReaderAsync(cancellationToken);
        while (await messageReader.ReadAsync(cancellationToken))
        {
            messages.Add(new StoredConversationMessage(
                ParseGuid(messageReader, 0),
                messageReader.GetString(1),
                messageReader.GetString(2),
                ParseDateTime(messageReader, 3),
                messageReader.GetInt32(4)));
        }

        return new UserOwnedConversationHistory(summary, messages);
    }

    public async Task CreateConversationAsync(
        UserOwnedConversationSummary conversation,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO conversations (
                conversation_id,
                owner_user_id,
                title,
                has_manual_title,
                model,
                status,
                created_at_utc,
                updated_at_utc
            ) VALUES (
                $conversationId,
                $ownerUserId,
                $title,
                $hasManualTitle,
                $model,
                $status,
                $createdAtUtc,
                $updatedAtUtc
            );
            """;

        command.Parameters.AddWithValue("$conversationId", conversation.ConversationId.ToString());
        command.Parameters.AddWithValue("$ownerUserId", conversation.OwnerUserId);
        command.Parameters.AddWithValue("$title", conversation.Title);
        command.Parameters.AddWithValue("$hasManualTitle", conversation.HasManualTitle);
        command.Parameters.AddWithValue("$model", conversation.Model);
        command.Parameters.AddWithValue("$status", conversation.Status);
        command.Parameters.AddWithValue("$createdAtUtc", FormatDateTime(conversation.CreatedAtUtc));
        command.Parameters.AddWithValue("$updatedAtUtc", FormatDateTime(conversation.UpdatedAtUtc));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AppendMessagesAsync(
        Guid conversationId,
        string ownerUserId,
        string title,
        bool hasManualTitle,
        DateTime updatedAtUtc,
        IReadOnlyList<StoredConversationMessage> messages,
        CancellationToken cancellationToken = default)
    {
        if (messages.Count == 0)
        {
            return;
        }

        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var nextSequence = await GetNextSequenceAsync(connection, transaction, conversationId, cancellationToken);

        await UpdateConversationAsync(
            connection,
            transaction,
            conversationId,
            ownerUserId,
            title,
            hasManualTitle,
            updatedAtUtc,
            cancellationToken);

        foreach (var message in messages)
        {
            await using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = (SqliteTransaction)transaction;
            insertCommand.CommandText =
                """
                INSERT INTO conversation_messages (
                    message_id,
                    conversation_id,
                    sequence,
                    role,
                    content,
                    created_at_utc
                ) VALUES (
                    $messageId,
                    $conversationId,
                    $sequence,
                    $role,
                    $content,
                    $createdAtUtc
                );
                """;

            insertCommand.Parameters.AddWithValue("$messageId", message.MessageId.ToString());
            insertCommand.Parameters.AddWithValue("$conversationId", conversationId.ToString());
            insertCommand.Parameters.AddWithValue("$sequence", nextSequence++);
            insertCommand.Parameters.AddWithValue("$role", message.Role);
            insertCommand.Parameters.AddWithValue("$content", message.Content);
            insertCommand.Parameters.AddWithValue("$createdAtUtc", FormatDateTime(message.CreatedAtUtc));

            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task<int> GetNextSequenceAsync(
        SqliteConnection connection,
        System.Data.Common.DbTransaction transaction,
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        await using var sequenceCommand = connection.CreateCommand();
        sequenceCommand.Transaction = (SqliteTransaction)transaction;
        sequenceCommand.CommandText =
            """
            SELECT COALESCE(MAX(sequence), 0)
            FROM conversation_messages
            WHERE conversation_id = $conversationId;
            """;
        sequenceCommand.Parameters.AddWithValue("$conversationId", conversationId.ToString());

        var currentMax = (long)(await sequenceCommand.ExecuteScalarAsync(cancellationToken) ?? 0L);
        return checked((int)currentMax + 1);
    }

    private static async Task UpdateConversationAsync(
        SqliteConnection connection,
        System.Data.Common.DbTransaction transaction,
        Guid conversationId,
        string ownerUserId,
        string title,
        bool hasManualTitle,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var updateCommand = connection.CreateCommand();
        updateCommand.Transaction = (SqliteTransaction)transaction;
        updateCommand.CommandText =
            """
            UPDATE conversations
            SET title = $title,
                has_manual_title = $hasManualTitle,
                updated_at_utc = $updatedAtUtc
            WHERE conversation_id = $conversationId
              AND owner_user_id = $ownerUserId;
            """;
        updateCommand.Parameters.AddWithValue("$conversationId", conversationId.ToString());
        updateCommand.Parameters.AddWithValue("$ownerUserId", ownerUserId);
        updateCommand.Parameters.AddWithValue("$title", title);
        updateCommand.Parameters.AddWithValue("$hasManualTitle", hasManualTitle);
        updateCommand.Parameters.AddWithValue("$updatedAtUtc", FormatDateTime(updatedAtUtc));

        var rowsUpdated = await updateCommand.ExecuteNonQueryAsync(cancellationToken);
        if (rowsUpdated == 0)
        {
            throw new InvalidOperationException(
                $"Conversation '{conversationId}' was not found for user '{ownerUserId}'.");
        }
    }

    private static Guid ParseGuid(SqliteDataReader reader, int ordinal) =>
        Guid.Parse(reader.GetString(ordinal));

    private static DateTime ParseDateTime(SqliteDataReader reader, int ordinal) =>
        DateTime.Parse(
            reader.GetString(ordinal),
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.RoundtripKind);

    private static string FormatDateTime(DateTime value) =>
        value.ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture);
}
