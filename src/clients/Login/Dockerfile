FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS dev

WORKDIR /repo

ENV DOTNET_USE_POLLING_FILE_WATCHER=1

# Copy csproj files for restore
COPY libs/components/blazor/*.csproj libs/components/blazor/
COPY src/clients/Login/*.csproj src/clients/Login/
RUN dotnet restore src/clients/Login/Login.csproj

# Copy source
COPY libs/components/blazor/ libs/components/blazor/
COPY src/clients/Login/ src/clients/Login/

WORKDIR /repo/src/clients/Login
CMD ["dotnet", "watch", "run", "--no-launch-profile"]

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build

WORKDIR /repo

# Copy csproj files for restore
COPY libs/components/blazor/*.csproj libs/components/blazor/
COPY src/clients/Login/*.csproj src/clients/Login/
RUN dotnet restore src/clients/Login/Login.csproj

# Copy source
COPY libs/components/blazor/ libs/components/blazor/
COPY src/clients/Login/ src/clients/Login/

RUN dotnet publish src/clients/Login/Login.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS prod

WORKDIR /app

COPY --from=build /out .

USER app

ENTRYPOINT ["dotnet", "Login.dll"]
