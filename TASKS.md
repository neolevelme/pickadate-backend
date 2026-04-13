# pickadate.me — TASKS

> Implementation roadmap. Check items off as they land.
> Keep this file in sync between the backend and frontend repositories.

## Phase 0 — Scaffolding

- [x] Save `SPECIFICATION.md` into both repositories
- [x] Save `TASKS.md` into both repositories
- [x] Backend: Clean Architecture skeleton (.NET 10)
  - [x] `Directory.Build.props`, `PickadateBackend.slnx`
  - [x] BuildingBlocks (Domain / Application / Infrastructure)
  - [x] Domain / Application / Infrastructure / API projects
  - [x] `Program.cs` with MediatR, FluentValidation, EF Core, Serilog, JWT, Swagger, CORS
  - [x] `appsettings.json` (placeholders only, no real secrets)
  - [x] `Dockerfile` (multi-stage, non-root)
  - [x] `docker-compose.yml` (Postgres 16 on port 5434)
  - [x] Exception handling middleware
  - [x] Health controller
- [x] Frontend: Astro skeleton
  - [x] `astro.config.mjs` with React, Tailwind v4, Sitemap, Node adapter
  - [x] `tsconfig.json` with `@/*` alias
  - [x] SEO Head component (OG, Twitter, structured data)
  - [x] BaseLayout / LandingLayout
  - [x] Magic Patterns landing integrated (Navigation, Hero, HowItWorks, Activities, Preview, FinalCTA, Footer)
  - [x] `robots.txt` route
  - [x] `src/lib/api/client.ts` (fetch wrapper)
  - [x] `src/lib/store/auth-store.ts` (Zustand)
  - [x] `Dockerfile` with build-arg env vars
  - [x] `.env.example`
- [x] Both: `Documentation/` folder and `CHANGELOG.md`
- [x] Both: `.github/workflows/build-deploy.yml` (GitLab registry + GitOps Helm update)
- [x] Initial commit + push for both repositories

## Phase 1 — Auth flow (email + 6-digit code)

- [x] Backend: `User` aggregate (id, email, name, country, vibePreference, profileImageUrl, role)
- [x] Backend: `VerificationCode` entity (email, code, expiresAt, usedAt)
- [x] Backend: `POST /api/auth/request-code` endpoint (generates code, sends email)
- [x] Backend: `POST /api/auth/verify-code` endpoint (returns JWT)
- [x] Backend: `EmailService` (MailKit, SMTP from config, console fallback for dev)
- [x] Backend: JWT claims + middleware
- [x] Backend: EF migration `001_InitialAuth`
- [x] Frontend: `/login` page (email input → code input → redirect)
- [x] Frontend: auto-fill for 6-digit code (`autocomplete="one-time-code"`, `inputmode="numeric"`)
- [x] Frontend: auth store persistence in `localStorage`
- [ ] Frontend: `AuthGuard` component for protected routes (deferred — 401 redirect is enough for now)

## Phase 2 — Invitation creation (5-step wizard)

- [x] Backend: `Invitation` aggregate (pending → viewed; accept/counter/decline in Phase 3)
- [x] Backend: `Place` value object (googlePlaceId, lat, lng, formattedAddress, name)
- [x] Backend: `CreateInvitationCommand` + handler + validator
- [x] Backend: `CreateAndPublish` factory on the aggregate (generates `xx-yyyy` slug, expiresAt = +72h)
- [x] Backend: `GET /api/invitations/{slug}` (public endpoint, lazy auth) + `RecordView`
- [x] Backend: EF migration `002_Invitations` (owned Place columns)
- [x] Frontend: `/create` 5-step wizard
  - [x] Step 1 — Vibe (Coffee / Drinks / Walk / Activity / Dinner / Custom)
  - [x] Step 2 — Place (manual inputs — Google Places autocomplete is Phase 2.5)
  - [x] Step 3 — Time (date + time picker)
  - [x] Step 4 — Message (textarea + media URL — Giphy picker is Phase 2.5)
  - [x] Step 5 — Preview + submit
- [x] Frontend: public `/i/[slug]` page (details + Google Maps link — accept/counter/decline are Phase 3)
- [ ] Frontend: Google Maps JS SDK integration (Phase 2.5)
- [ ] Frontend: Giphy API integration (Phase 2.5)
- [ ] Backend: weather integration in the preview (Phase 4)

## Phase 3 — Opening the invitation (recipient)

- [x] Frontend: `/i/[slug]` public page (no login)
- [x] Frontend: full details + "Open in Maps" button (weather is Phase 4)
- [x] Frontend: "Accept" button → auth flow → status update (`?next=` honoured by login)
- [x] Frontend: "Suggest a change" → auth flow → inline counter-proposal form
- [x] Frontend: "Not this time" → optional comment (80 chars) → status update (no login)
- [x] Backend: `AcceptInvitationCommand` (auth required)
- [x] Backend: `CounterProposeInvitationCommand` (auth required, max 3 rounds, auto-close on the 3rd)
- [x] Backend: `DeclineInvitationCommand` (anonymous + optional comment ≤80)
- [x] Backend: IP-based decline rate limiter (20/day, `DeclineRecord` entity, HTTP 429)
- [x] Backend: `RecordView` in `GetInvitationBySlugQuery` (Pending → Viewed)
- [ ] Backend: "max 5 active invitations per browser" rate limit (deferred to a later abuse pass)
- [x] Backend: initiator-side response to a counter (landed in Phase 6 dashboard via `AcceptCounterProposalCommand`)

## Phase 4 — Weather

- [x] Backend: `IWeatherService` / `OpenMeteoWeatherService` (no API key needed)
- [x] Backend: in-memory cache keyed by (lat, lng, date) with 6h TTL
- [x] Backend: include weather in `GET /api/invitations/{slug}` and `GET /api/invitations/my`
- [x] Frontend: `WeatherCard` component (icon + temperature + short description), surfaced on `/i/[slug]` and the dashboard cards
- [ ] Surface weather in the Phase 5 24h reminder notification

## Phase 5 — Notifications

- [ ] Backend: Web Push (VAPID keys, `PushSubscription` entity)
- [ ] Backend: `NotificationService` (push + email fallback)
- [ ] Backend: hosted service for the `reminder-24h` and `reminder-2h` cron
- [ ] Frontend: prompt for push permissions after login
- [ ] Frontend: service worker for push

## Phase 6 — History, cancelation, auto-purge

- [x] Backend: `GET /api/invitations/my` (initiator sees their own history)
- [x] Backend: `CancelInvitationCommand` (initiator-only)
- [x] Backend: `MarkCompletedCommand` (initiator-only for now)
- [x] Backend: `AcceptCounterProposalCommand` (closes the ping-pong loop)
- [x] Backend: `InvitationPurgeHostedService` (daily cron: >30d invitations, >24h decline records, expired verification codes)
- [x] Frontend: `/dashboard` page with status badges, counter banner, and inline actions
- [ ] Frontend: `AuthGuard` component (current 401 redirect is enough for now)
- [ ] Notify the other party on cancel (depends on Phase 5 push notifications)

## Phase 7 — Safety check

- [x] Backend: `SafetyCheck` aggregate (invitationId, userId, friendToken, scheduledCheckInAt, confirmedAt, alertedAt)
- [x] Backend: `CreateSafetyCheckCommand` (auth, requires invitation to be Accepted, idempotent per invitation+user)
- [x] Backend: `ConfirmSafetyCheckCommand` ("all good" — idempotent)
- [x] Backend: `GetFriendSafetyViewQuery` (anonymous, returns meeting details + status for the bearer token)
- [x] Backend: `SafetyCheckAlertHostedService` (5-minute cron) — marks due checks as alerted and logs where the Phase 5 push would fire
- [x] Backend: `ISafetyTokenGenerator` (24-byte base64url), EF migration `SafetyChecks`
- [x] Frontend: `SafetyCheckPanel` on `InvitationView` when `Accepted`
- [x] Frontend: `/safety/[token]` public SSR page (`noindex`) showing meeting details and status
- [ ] Real push alert delivery — depends on Phase 5 Web Push infrastructure

## Phase 8 — QR code

- [x] Frontend: client-side QR code generation (`qrcode` npm package, SVG output)
- [x] Frontend: `QrCodeModal` component wired to a dashboard "Show QR" button

## Phase 9 — Anniversary mode

- [ ] Backend: `Anniversary` entity (user pair, firstDateAt)
- [ ] Backend: yearly cron → push + email
- [ ] Frontend: toggle in settings

## Phase 10 — SEO & performance

- [x] Frontend: `sitemap.xml` allow-list (landing + 7 public legal/marketing pages)
- [x] Frontend: `robots.txt` (blocks `/i/*`, `/dashboard`, `/create`, `/login`, `/safety/*`, `/admin`)
- [x] Frontend: default Open Graph image (`public/og-default.svg`, gradient hero)
- [x] Frontend: structured data (JSON-LD `WebApplication` injected on every page via `Head.astro`)
- [x] Frontend: `MarketingLayout` + stubs for `/about`, `/faq`, `/privacy`, `/terms`, `/contact`, `/disclaimer`, `/cookie-policy`
- [ ] Frontend: Lighthouse audit ≥ 95 on every public page (run during Phase 12 launch review)
- [ ] Frontend: hreflang for multi-language support (depends on Phase 12 localisation)

## Phase 12 — Launch

- [ ] Privacy policy, Terms, Cookie policy, Disclaimer pages
- [ ] GDPR cookie banner
- [ ] "Delete my account" flow
- [ ] Load test (k6 or Artillery)
- [ ] Deploy to production
