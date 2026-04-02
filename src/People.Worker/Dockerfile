FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props Directory.Packages.props ./
COPY src/People.Domain/*.csproj src/People.Domain/
COPY src/People.Application/*.csproj src/People.Application/
COPY src/People.Infrastructure/*.csproj src/People.Infrastructure/
COPY src/People.Worker/*.csproj src/People.Worker/
RUN dotnet restore src/People.Worker/People.Worker.csproj
COPY src/ src/
RUN dotnet publish src/People.Worker/People.Worker.csproj -c Release -o /app --no-restore

FROM base AS final
COPY --from=build /app .
ENTRYPOINT ["dotnet", "People.Worker.dll"]
