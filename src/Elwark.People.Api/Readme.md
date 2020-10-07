# Migrations

* dotnet ef migrations add InitialOAuth -c OAuthContext -o Migrations/OauthContext -s src/Elwark.People.Api -p src/Elwark.People.Infrastructure
* dotnet ef migrations add InitialEventLog -c IntegrationEventLogContext -o Migrations/IntegrationEventLogContext -s src/Elwark.People.Api -p src/Elwark.People.Infrastructure