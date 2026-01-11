FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS dev

WORKDIR /repo

ENV DOTNET_USE_POLLING_FILE_WATCHER=1

# Copy fsproj files for restore
COPY libs/data_types/fsharp/*.fsproj libs/data_types/fsharp/
COPY libs/fsharp/httputil/*.fsproj libs/fsharp/httputil/
COPY libs/cloud_events/fsharp/*.fsproj libs/cloud_events/fsharp/
COPY libs/protos/fsharp/*.fsproj libs/protos/fsharp/
COPY src/services/user/User/*.fsproj src/services/user/User/
RUN dotnet restore src/services/user/User/User.fsproj

# Copy source
COPY libs/data_types/fsharp/ libs/data_types/fsharp/
COPY libs/fsharp/httputil/ libs/fsharp/httputil/
COPY libs/cloud_events/fsharp/ libs/cloud_events/fsharp/
COPY libs/protos/ libs/protos/
COPY src/services/user/ src/services/user/

WORKDIR /repo/src/services/user/User
CMD ["dotnet", "watch", "run", "--no-launch-profile"]

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build

WORKDIR /repo

# Copy fsproj files for restore
COPY libs/data_types/fsharp/*.fsproj libs/data_types/fsharp/
COPY libs/fsharp/httputil/*.fsproj libs/fsharp/httputil/
COPY libs/cloud_events/fsharp/*.fsproj libs/cloud_events/fsharp/
COPY libs/protos/fsharp/*.fsproj libs/protos/fsharp/
COPY src/services/user/User/*.fsproj src/services/user/User/
RUN dotnet restore src/services/user/User/User.fsproj

# Copy source
COPY libs/data_types/fsharp/ libs/data_types/fsharp/
COPY libs/fsharp/httputil/ libs/fsharp/httputil/
COPY libs/cloud_events/fsharp/ libs/cloud_events/fsharp/
COPY libs/protos/ libs/protos/
COPY src/services/user/ src/services/user/

RUN dotnet publish src/services/user/User/User.fsproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS prod

WORKDIR /app

COPY --from=build /out .

USER app

ENTRYPOINT ["dotnet", "User.dll"]
