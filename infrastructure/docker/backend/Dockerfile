# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project file first to leverage layer caching
COPY backend/src/MyOwnChatbotAi.sln ./
COPY backend/src/MyOwnChatbotAi.Api/MyOwnChatbotAi.Api.csproj MyOwnChatbotAi.Api/
RUN dotnet restore MyOwnChatbotAi.sln

# Copy source and publish
COPY backend/src/MyOwnChatbotAi.Api/ MyOwnChatbotAi.Api/
RUN dotnet publish MyOwnChatbotAi.Api/MyOwnChatbotAi.Api.csproj \
    -c Release \
    -o /app/publish

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create a stable non-root user and writable app data directory for SQLite
RUN addgroup --system --gid 10001 appgroup \
    && adduser --system --uid 10001 --ingroup appgroup appuser \
    && mkdir -p /app/App_Data \
    && chown -R appuser:appgroup /app

COPY --from=build --chown=appuser:appgroup /app/publish .

USER appuser

ENV ASPNETCORE_URLS=http://+:5050
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5050

ENTRYPOINT ["dotnet", "MyOwnChatbotAi.Api.dll"]
