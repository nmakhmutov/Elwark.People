# Migrations

* dotnet ef migrations add InitialOAuth -c OAuthContext -o Migrations/OauthContext -s src/People/Elwark.People.Api -p src/People/Elwark.People.Infrastructure
* dotnet ef migrations add InitialEventLog -c IntegrationEventLogContext -o Migrations/IntegrationEventLogContext -s src/People/Elwark.People.Api -p src/People/Elwark.People.Infrastructure