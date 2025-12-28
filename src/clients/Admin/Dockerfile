FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS dev

WORKDIR /app

# Hot reload
ENV DOTNET_USE_POLLING_FILE_WATCHER=1

COPY *.csproj ./
RUN dotnet restore

COPY . .

ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile"]
