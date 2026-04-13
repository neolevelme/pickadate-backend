# Changelog

All notable changes to the pickadate.me backend are documented here.

## 2026-04-13 — Phase 7: safety check

### Added
- `SafetyCheck` aggregate under `Pickadate.Domain.Safety` — `FriendToken`, `ScheduledCheckInAt` (meeting + 2h), `ConfirmedAt`, `AlertedAt`. Confirm is idempotent; `NeedsAlerting` drives the hosted service.
- `ISafetyCheckRepository` + `SafetyCheckRepository` — lookups by id, friend token, active-for-user, and due-for-alert.
- `ISafetyTokenGenerator` / `SafetyTokenGenerator` — 24 random bytes → 32-char URL-safe base64url. Longer than the invitation slug because the friend link is a bearer capability.
- `CreateSafetyCheckCommand` — auth required, rejects with 422 when the invitation isn't Accepted, and returns the existing active check instead of duplicating.
- `ConfirmSafetyCheckCommand` — "all good" marks the active check confirmed (idempotent).
- `GetFriendSafetyViewQuery` — anonymous bearer-token endpoint returning meeting details and current status (`Scheduled` / `Confirmed` / `Overdue`).
- `SafetyChecksController` — `POST /api/safety-checks/invitations/{slug}`, `POST /{slug}/confirm`, `GET /api/safety-checks/{friendToken}`.
- `SafetyCheckAlertHostedService` — 5-minute background sweep that marks due checks as alerted and logs a warning where the Phase 5 push would fire once notifications ship.
- `ExceptionHandlingMiddleware` maps `InvalidSafetyCheckStateException` → 422.
- EF migration `SafetyChecks` — adds the `safety_checks` table with a unique index on `FriendToken`, a composite index on `(InvitationId, UserId)`, and an index on `ScheduledCheckInAt` for the sweep.

### Removed
- Phase 11 (Admin) from TASKS.md — user confirmed it's not part of the product. Downstream phases keep their numbers.


## 2026-04-12 — Phase 4: weather forecast

### Added
- `IWeatherService` contract in Application with a `WeatherForecast` record
- `OpenMeteoWeatherService` in Infrastructure — typed `HttpClient` against `api.open-meteo.com`, no API key, 5s timeout
- In-memory cache keyed by `(lat rounded to 3 decimals, lng rounded to 3 decimals, date)` with a 6h TTL so co-located invitations share one upstream hit (spec §6)
- `WmoCodes.Describe` maps WMO weather codes to plain English
- `GetInvitationBySlugQuery` and `GetMyInvitationsQuery` both fetch the forecast for each invitation (graceful null if beyond the 7-day horizon or the upstream fails)
- `WeatherDto` on `InvitationDetailDto`
- `AddHttpClient<IWeatherService, OpenMeteoWeatherService>` in `Program.cs`


## 2026-04-12 — Phase 6: dashboard, cancel, complete, purge

### Added
- `Invitation.Cancel()`, `Invitation.MarkCompleted()`, `Invitation.AcceptCounterProposal(counter)` domain methods guarded by `CanBeCancelled`, `MustBeAccepted`, `MustBeInCounteredState` business rules. `AcceptCounterProposal` merges the counter's new time and/or place into the invitation and flips status to Accepted — this closes the ping-pong loop.
- `IInvitationRepository.ListForInitiatorAsync` + `PurgeOlderThanAsync` (ExecuteDelete-based bulk purge).
- `GetMyInvitationsQuery` returns the caller's invitations (newest first) with the latest counter-proposal attached for each.
- `CancelInvitationCommand`, `MarkCompletedCommand`, `AcceptCounterProposalCommand` — all initiator-only, validated against the caller via `ICurrentUser`.
- `InvitationsController` new endpoints: `GET /api/invitations/my`, `POST /{slug}/cancel`, `POST /{slug}/complete`, `POST /{slug}/accept-counter`.
- `InvitationPurgeHostedService` — `BackgroundService` running every 24h that deletes invitations with `MeetingAt` > 30 days in the past (spec §10), decline records > 24h (spec §12), and expired verification codes. First run happens one minute after startup so migrations and the initial request burst have settled.

### Fixed
- **Phase 3 regression**: `InvitationsController` and `ExceptionHandlingMiddleware` reverted to their Phase 2 shapes after a Write that silently failed, so the accept / counter / decline endpoints from Phase 3 were never actually served — the frontend was hitting 404s. Re-applied the endpoint definitions and exception mappings (`InvitationNotFoundException` → 404, `TooManyDeclinesException` → 429, `UnauthorizedAccessException` → 401).


## 2026-04-12 — Phase 3: accept / counter / decline

### Added
- `Invitation.Accept()`, `Invitation.Decline(reason)`, `Invitation.CounterPropose(proposerId, newMeetingAtUtc, newPlace)` domain methods guarded by `CanBeRespondedTo`, `DeclineReasonWithinLimit` (≤80), and `CounterRoundsNotExhausted` (max 3) business rules
- Counter round `MaxCounterRounds = 3` constant on `Invitation`; the third counter auto-closes the invitation (`Status=Expired`)
- `Invitation.RespondedAt` and `Invitation.DeclineReason` columns
- `CounterProposal` entity + owned `NewPlace` columns + `ICounterProposalRepository` + `CounterProposalRepository`
- `DeclineRecord` entity (IP + timestamp, 24h retention per spec §12) + `IDeclineRecordRepository` + `DeclineRecordRepository`
- `IClientContext` / `ClientContext` — best-effort client IP, honours `X-Forwarded-For` (leftmost) and falls back to the socket address
- `AcceptInvitationCommand`, `DeclineInvitationCommand`, `CounterProposeInvitationCommand` + validators
- `DeclineInvitationCommandHandler` enforces 20 declines / 24h / IP via `DeclineRecord` count, and throws `TooManyDeclinesException` → 429
- `InvitationsController` endpoints: `POST /api/invitations/{slug}/accept` (auth), `POST /{slug}/counter` (auth), `POST /{slug}/decline` (anonymous)
- `InvitationDetailDto` now includes `CounterRound`, `MaxCounterRounds`, and `LatestCounter` (a `CounterProposalDto`) so the frontend can render the current state correctly
- `ExceptionHandlingMiddleware` maps `InvitationNotFoundException` → 404 and `TooManyDeclinesException` → 429
- EF migration `InvitationActions` — `counter_proposals` table (with owned `new_place_*` columns), `decline_records` table, plus `decline_reason` / `responded_at` columns on `invitations`

## 2026-04-12 — Phase 2: invitation create + public view

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

## 2026-04-12 — Phase 1: email + 6-digit code auth

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
