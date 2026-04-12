# 01 — Overview

`pickadate.me` is a date-scheduling web app designed to replace *chat purgatory*
("we should grab coffee sometime") with a single elegant shareable link.
An initiator creates a concrete invitation (vibe + place + time + optional message)
and shares a link with the recipient, who opens it **without needing an account**
and chooses: **Accept**, **Counter-propose**, or **Decline**.

See [`SPECIFICATION.md`](../SPECIFICATION.md) for the full product specification.

## Tech stack

- **Runtime:** .NET 10
- **Web framework:** ASP.NET Core Web API (controllers)
- **ORM:** Entity Framework Core 10 (code-first migrations)
- **Database:** PostgreSQL 16
- **Mediator:** MediatR 12 (CQRS)
- **Validation:** FluentValidation 11
- **Auth:** JWT Bearer (email + 6-digit verification code flow)
- **Password hashing:** BCrypt (only used where hashed secrets are needed)
- **Email:** MailKit SMTP
- **Logging:** Serilog (console + file)
- **API docs:** Swashbuckle / Swagger (Development only)

## Key principles

1. **Lazy authentication** — the recipient can view and decline an invitation
   without creating an account. Auth is only required when creating a
   relationship (accept or counter-propose).
2. **Privacy by default** — no chat, no public profiles, no search. Invitations
   auto-delete 30 days after the planned meeting date.
3. **Clean Architecture** — Domain → Application → Infrastructure → API.
   Domain has zero external dependencies.

## Running locally

```bash
docker compose up -d              # start Postgres on :5435
dotnet restore
dotnet run --project src/API/Pickadate.API
```
