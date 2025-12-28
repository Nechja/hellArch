FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS dev

WORKDIR /app

ENV DOTNET_USE_POLLING_FILE_WATCHER=1

COPY User/*.fsproj ./User/
RUN dotnet restore User/User.fsproj

COPY . .

WORKDIR /app/User
CMD ["dotnet", "watch", "run", "--no-launch-profile"]

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build

WORKDIR /app

COPY User/*.fsproj ./User/
RUN dotnet restore User/User.fsproj

COPY . .

WORKDIR /app/User
RUN dotnet publish -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS prod

WORKDIR /app

COPY --from=build /out .

USER app

ENTRYPOINT ["dotnet", "User.dll"]
