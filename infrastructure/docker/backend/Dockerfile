# ── Build stage ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project file first to leverage layer caching
COPY backend/src/MyOwnChatbotAi.sln ./
COPY backend/src/MyOwnChatbotAi.Api/MyOwnChatbotAi.Api.csproj MyOwnChatbotAi.Api/
RUN dotnet restore MyOwnChatbotAi.Api/MyOwnChatbotAi.Api.csproj

# Copy source and publish
COPY backend/src/MyOwnChatbotAi.Api/ MyOwnChatbotAi.Api/
RUN dotnet publish MyOwnChatbotAi.Api/MyOwnChatbotAi.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Runtime stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Create a non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser
USER appuser

COPY --from=build --chown=appuser:appgroup /app/publish .

ENV ASPNETCORE_URLS=http://+:5050
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5050

ENTRYPOINT ["dotnet", "MyOwnChatbotAi.Api.dll"]
