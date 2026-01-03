FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS dev

WORKDIR /repo

ENV ASPNETCORE_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=1

# Copy csproj files for restore
COPY libs/components/blazor/*.csproj libs/components/blazor/
COPY libs/cloud_events/csharp/*.csproj libs/cloud_events/csharp/
COPY src/clients/Admin/*.csproj src/clients/Admin/
RUN dotnet restore src/clients/Admin/Admin.csproj

# Copy source
COPY libs/components/blazor/ libs/components/blazor/
COPY libs/cloud_events/csharp/ libs/cloud_events/csharp/
COPY src/clients/Admin/ src/clients/Admin/

WORKDIR /repo/src/clients/Admin
CMD ["dotnet", "run", "--no-launch-profile"]

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build

WORKDIR /repo

# Copy csproj files for restore
COPY libs/components/blazor/*.csproj libs/components/blazor/
COPY libs/cloud_events/csharp/*.csproj libs/cloud_events/csharp/
COPY src/clients/Admin/*.csproj src/clients/Admin/
RUN dotnet restore src/clients/Admin/Admin.csproj

# Copy source
COPY libs/components/blazor/ libs/components/blazor/
COPY libs/cloud_events/csharp/ libs/cloud_events/csharp/
COPY src/clients/Admin/ src/clients/Admin/

RUN dotnet publish src/clients/Admin/Admin.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS prod

WORKDIR /app

COPY --from=build /out .

USER app

ENTRYPOINT ["dotnet", "Admin.dll"]
