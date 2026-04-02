FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /
COPY "Directory.Build.props" "Directory.Packages.props" "./"
COPY "src/People.Api/People.Api.csproj" "src/People.Api/"
COPY "src/People.Infrastructure/People.Infrastructure.csproj" "src/People.Infrastructure/"
COPY "src/People.Domain/People.Domain.csproj" "src/People.Domain/"

RUN dotnet restore "src/People.Api/People.Api.csproj"

COPY "src/" "src/"

RUN dotnet publish "src/People.Api/People.Api.csproj" -c Release -o /app --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "People.Api.dll"]
