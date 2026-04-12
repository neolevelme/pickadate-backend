# pickadate.me — Backend

.NET 10 Web API for [pickadate.me](https://pickadate.me).

See [`SPECIFICATION.md`](./SPECIFICATION.md) for the full product spec and
[`TASKS.md`](./TASKS.md) for the implementation roadmap.

## Architecture

Clean Architecture with the following layers:

```
src/
├── BuildingBlocks/
│   ├── Pickadate.BuildingBlocks.Domain          # Entity, ValueObject, IDomainEvent, IBusinessRule
│   ├── Pickadate.BuildingBlocks.Application     # ICommand, IQuery, handlers
│   └── Pickadate.BuildingBlocks.Infrastructure  # IUnitOfWork
├── Domain/Pickadate.Domain                      # Aggregates (User, Invitation, ...)
├── Application/Pickadate.Application            # Use cases (MediatR + FluentValidation)
├── Infrastructure/Pickadate.Infrastructure      # EF Core, repos, services
└── API/Pickadate.API                            # Controllers, middleware, Program.cs
```

## Running locally

```bash
# 1. Start PostgreSQL
docker compose up -d

# 2. Restore & run
dotnet restore
dotnet run --project src/API/Pickadate.API
```

API will be available at `http://localhost:5001` (Swagger UI at `/swagger`).

## Config

Environment variables override `appsettings.json` (double-underscore for nested keys):

- `ConnectionStrings__PickadateDb`
- `Jwt__SecretKey` (**must be ≥32 chars in prod**)
- `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationHours`
- `Email__SmtpHost`, `Email__SmtpUser`, `Email__SmtpPassword`, ...
- `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, ...
