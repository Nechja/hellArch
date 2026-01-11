FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build

WORKDIR /repo

# Copy project files for restore
COPY src/workers/realtime/*.csproj src/workers/realtime/
COPY libs/csharp/HellArch.RabbitMQ/*.csproj libs/csharp/HellArch.RabbitMQ/
COPY libs/cloud_events/csharp/*.csproj libs/cloud_events/csharp/

RUN dotnet restore src/workers/realtime/Realtime.csproj

# Copy source files
COPY src/workers/realtime/ src/workers/realtime/
COPY libs/csharp/HellArch.RabbitMQ/ libs/csharp/HellArch.RabbitMQ/
COPY libs/cloud_events/csharp/ libs/cloud_events/csharp/

RUN dotnet publish src/workers/realtime/Realtime.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS prod

WORKDIR /app

COPY --from=build /out .

USER app

ENTRYPOINT ["dotnet", "Realtime.dll"]
