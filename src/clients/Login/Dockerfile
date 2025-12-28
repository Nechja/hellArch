FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS dev

WORKDIR /app

ENV DOTNET_USE_POLLING_FILE_WATCHER=1

COPY Login/*.csproj ./Login/
RUN dotnet restore Login/Login.csproj

COPY . .

WORKDIR /app/Login
CMD ["dotnet", "watch", "run", "--no-launch-profile"]

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build

WORKDIR /app

COPY Login/*.csproj ./Login/
RUN dotnet restore Login/Login.csproj

COPY . .

WORKDIR /app/Login
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS prod

WORKDIR /app

COPY --from=build /out .

USER app

ENTRYPOINT ["dotnet", "Login.dll"]
