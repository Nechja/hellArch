FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS dev

WORKDIR /app

ENV DOTNET_USE_POLLING_FILE_WATCHER=1

COPY *.csproj ./
RUN dotnet restore

COPY . .

CMD ["dotnet", "watch", "run", "--no-launch-profile"]

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build

WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . .

RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS prod

WORKDIR /app

COPY --from=build /out .

USER app

ENTRYPOINT ["dotnet", "Realtime.dll"]
