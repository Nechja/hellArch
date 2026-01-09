# Build stage - compile migration runner
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy project files
COPY src/schema/Schema/Schema.csproj src/schema/Schema/
COPY src/schema/MigrationRunner/MigrationRunner.csproj src/schema/MigrationRunner/
COPY libs/data_types/csharp/ libs/data_types/csharp/

# Restore
RUN dotnet restore src/schema/MigrationRunner/MigrationRunner.csproj

# Copy source and build
COPY src/schema/Schema/ src/schema/Schema/
COPY src/schema/MigrationRunner/ src/schema/MigrationRunner/

RUN dotnet publish src/schema/MigrationRunner/MigrationRunner.csproj \
    -c Release \
    -r linux-x64 \
    -o /app/publish

# Runtime stage - just postgres + migration binary
FROM postgres:17-bookworm

# Copy migration runner binary
COPY --from=build /app/publish/MigrationRunner /usr/local/bin/migrate

# Copy entrypoint wrapper
COPY infra/dbs/identity-db/entrypoint.sh /usr/local/bin/entrypoint-wrapper.sh
RUN chmod +x /usr/local/bin/entrypoint-wrapper.sh /usr/local/bin/migrate

ENTRYPOINT ["/usr/local/bin/entrypoint-wrapper.sh"]
CMD ["postgres"]
