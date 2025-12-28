FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS dev

WORKDIR /app

# Development environment for proper Blazor static assets
ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=1

COPY *.csproj ./
RUN dotnet restore

COPY . .

# Use dotnet run in Development mode - handles Blazor static assets correctly
ENTRYPOINT ["dotnet", "run", "--no-launch-profile"]
