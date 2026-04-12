# Changelog

All notable changes to the pickadate.me backend are documented here.

## 2026-04-12 — Faza 2: invitation create + public view

### Added
- `Place` value object (`GooglePlaceId`, `Name`, `FormattedAddress`, `Lat`, `Lng`) with domain validation
- `Invitation.CreateAndPublish` factory — enforces `MeetingMustBeInTheFuture`, `MessageLengthWithinLimit`, `CustomVibeRequiredWhenVibeIsCustom`; sets `Status=Pending`, `ExpiresAt=+72h`
- `Invitation.RecordView` transitions `Pending → Viewed` and stamps `FirstViewedAt`
- `ISlugGenerator` / `SlugGenerator` — crypto-RNG, `xx-yyyy` format using only unambiguous characters
- `ICurrentUser` / `CurrentUser` — reads the JWT sub claim via `IHttpContextAccessor`
- `InvitationRepository` (`GetById`, `GetBySlug`, `SlugExists`, `Add`)
- `CreateInvitationCommand` + validator + handler — auth-required, retries slug generation on collision, atomic publish
- `GetInvitationBySlugQuery` — public, returns null on miss, records the first view on hit
- `InvitationsController` — `POST /api/invitations` (auth) returning 201 Created, `GET /api/invitations/{slug}` (anonymous) returning 404 or DTO
- `ExceptionHandlingMiddleware` now maps `UnauthorizedAccessException` to 401
- EF migration `Invitations` — adds `place_*` owned columns, `Vibe`/`Status` enum conversions, `InitiatorId` index

### Changed
- Infrastructure project now has `FrameworkReference Microsoft.AspNetCore.App` so `CurrentUser` can consume `IHttpContextAccessor`

## 2026-04-12 — Faza 1: email + 6-digit code auth

### Added
- `VerificationCode` entity (10-min TTL) + `IVerificationCodeRepository`
- `IUserRepository.GetByEmailAsync` / `AddAsync`
- `RequestCodeCommand` — invalidates outstanding codes, issues a crypto-RNG 6-digit code, sends via `IEmailService`
- `VerifyCodeCommand` — constant-time check, lazy-registers on first success, returns `AuthResponse { token, expiresAt, user }`
- `ValidationBehavior` MediatR pipeline surfaces FluentValidation errors as RFC 7807 400s
- `InvalidCredentialsException` → 401
- `IJwtTokenService` / `JwtTokenService` (HS256, configurable expiry)
- `IEmailService` / `SmtpEmailService` (MailKit) with a dev fallback that logs the code to the console when `Email:SmtpHost` is empty
- `IVerificationCodeGenerator` / `VerificationCodeGenerator`
- `UserRepository`, `VerificationCodeRepository`, `UnitOfWork`
- `AuthController` — `POST /api/auth/request-code` (204), `POST /api/auth/verify-code` (200)
- EF migration `InitialAuth` — `users`, `invitations`, `verification_codes`

### Changed
- `IUnitOfWork` moved from `BuildingBlocks.Infrastructure` into `BuildingBlocks.Application` so the Application layer can consume it without leaking Infrastructure references

## 2026-04-12 — Initial scaffold

### Added
- Clean Architecture solution layout (.NET 10): BuildingBlocks (Domain/Application/Infrastructure) + Domain + Application + Infrastructure + API projects
- `Directory.Build.props` enforcing `net10.0`, `Nullable=enable`, `TreatWarningsAsErrors`
- `Dockerfile` (multi-stage, non-root) and `docker-compose.yml` (Postgres 16 on port 5435)
- `Program.cs` wired with MediatR, FluentValidation, EF Core (Npgsql), JWT Bearer auth, Serilog, Swagger, CORS
- Skeleton `User` and `Invitation` aggregates with EF mappings
- `ExceptionHandlingMiddleware` mapping domain and validation exceptions to RFC 7807 ProblemDetails
- `HealthController` (`GET /api/health`)
- `SPECIFICATION.md` and `TASKS.md`
- `.github/workflows/build-deploy.yml` for GitOps deployment via GitLab registry
