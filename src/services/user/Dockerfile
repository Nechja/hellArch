FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS dev

WORKDIR /app

# Hot reload
ENV DOTNET_USE_POLLING_FILE_WATCHER=1

COPY User/*.fsproj ./User/
RUN dotnet restore User/User.fsproj

COPY . .

WORKDIR /app/User
ENTRYPOINT ["dotnet", "watch", "run", "--no-launch-profile"]
