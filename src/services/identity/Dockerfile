FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS dev

WORKDIR /app

ENV DOTNET_USE_POLLING_FILE_WATCHER=1

COPY Identity/*.fsproj ./Identity/
RUN dotnet restore Identity/Identity.fsproj

COPY . .

WORKDIR /app/Identity
CMD ["dotnet", "watch", "run", "--no-launch-profile"]

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build

WORKDIR /app

COPY Identity/*.fsproj ./Identity/
RUN dotnet restore Identity/Identity.fsproj

COPY . .

WORKDIR /app/Identity
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS prod

WORKDIR /app

COPY --from=build /out .

USER app

ENTRYPOINT ["dotnet", "Identity.dll"]
