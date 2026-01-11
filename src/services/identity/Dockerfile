FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build

WORKDIR /repo

# Copy fsproj files for restore
COPY libs/data_types/fsharp/*.fsproj libs/data_types/fsharp/
COPY libs/fsharp/httputil/*.fsproj libs/fsharp/httputil/
COPY src/services/identity/Identity/*.fsproj src/services/identity/Identity/
RUN dotnet restore src/services/identity/Identity/Identity.fsproj

# Copy source
COPY libs/data_types/fsharp/ libs/data_types/fsharp/
COPY libs/fsharp/httputil/ libs/fsharp/httputil/
COPY src/services/identity/ src/services/identity/

RUN dotnet publish src/services/identity/Identity/Identity.fsproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS prod

WORKDIR /app

COPY --from=build /out .

USER app

ENTRYPOINT ["dotnet", "Identity.dll"]
