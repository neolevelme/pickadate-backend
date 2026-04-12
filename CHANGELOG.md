# Changelog

All notable changes to the pickadate.me backend are documented here.

## 2026-04-12 — Initial scaffold

### Added
- Clean Architecture solution layout (.NET 10) with BuildingBlocks / Domain / Application / Infrastructure / API projects
- `Directory.Build.props` enforcing `net10.0`, `Nullable=enable`, `TreatWarningsAsErrors`
- `Dockerfile` (multi-stage, non-root) and `docker-compose.yml` (Postgres 16 on port 5435)
- `Program.cs` wired with MediatR, FluentValidation, EF Core (Npgsql), JWT Bearer auth, Serilog, Swagger, CORS
- Skeleton `User` and `Invitation` aggregates with EF mappings
- `ExceptionHandlingMiddleware` mapping domain and validation exceptions to RFC 7807 ProblemDetails
- `HealthController` (`GET /api/health`)
- `SPECIFICATION.md` and `TASKS.md`
- `.github/workflows/build-deploy.yml` for GitOps deployment via GitLab registry
