using Microsoft.Extensions.Options;

namespace MyOwnChatbotAi.Api.Features.Conversations.Persistence;

public static class ConversationPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddConversationPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ConversationPersistenceOptions>(
            configuration.GetSection(ConversationPersistenceOptions.SectionName));
        services.AddSingleton<SqliteConversationConnectionFactory>();
        services.AddSingleton<IUserOwnedConversationStore, SqliteUserOwnedConversationStore>();
        services.AddHostedService<ConversationPersistenceInitializationService>();

        return services;
    }
}

internal sealed class ConversationPersistenceInitializationService(
    IUserOwnedConversationStore conversationStore,
    ILogger<ConversationPersistenceInitializationService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await conversationStore.InitializeAsync(cancellationToken);
        logger.LogInformation("Initialized SQLite conversation persistence store");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class SqliteConversationConnectionFactory
{
    private readonly string _connectionString;

    public SqliteConversationConnectionFactory(
        IOptions<ConversationPersistenceOptions> options,
        IHostEnvironment hostEnvironment)
    {
        var configuredPath = options.Value.DatabasePath;
        var resolvedPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(hostEnvironment.ContentRootPath, configuredPath);

        var directory = Path.GetDirectoryName(resolvedPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder
        {
            DataSource = resolvedPath,
            Mode = Microsoft.Data.Sqlite.SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    public async Task<Microsoft.Data.Sqlite.SqliteConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = "PRAGMA foreign_keys = ON;";
        await pragmaCommand.ExecuteNonQueryAsync(cancellationToken);

        return connection;
    }
}
