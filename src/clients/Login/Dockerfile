FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS dev

WORKDIR /app

ENV DOTNET_USE_POLLING_FILE_WATCHER=1

COPY Login/*.csproj ./Login/
RUN dotnet restore Login/Login.csproj

COPY . .

WORKDIR /app/Login
ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile"]
