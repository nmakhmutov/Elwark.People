FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /
COPY "src" "src/"
WORKDIR /src/People.Webhooks
RUN dotnet publish People.Webhooks.csproj -c Release -o /app

FROM build AS publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "People.Webhooks.dll"]
