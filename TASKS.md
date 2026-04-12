# pickadate.me — TASKS

> Roadmap za implementaciju. Označi redove kako se završavaju.
> Sinhronizuj između oba repozitorijuma (backend i frontend).

## Faza 0 — Scaffolding (trenutna faza)

- [x] Sačuvati `SPECIFICATION.md` u oba repozitorijuma
- [x] Sačuvati `TASKS.md` u oba repozitorijuma
- [x] Backend: Clean Architecture skeleton (.NET 10)
  - [x] `Directory.Build.props`, `PickadateBackend.slnx`
  - [x] BuildingBlocks (Domain / Application / Infrastructure)
  - [x] Domain / Application / Infrastructure / API projekti
  - [x] `Program.cs` sa MediatR, FluentValidation, EF Core, Serilog, JWT, Swagger, CORS
  - [x] `appsettings.json` (samo placeholderi, bez pravih secret-a)
  - [x] `Dockerfile` (multi-stage, non-root)
  - [x] `docker-compose.yml` (Postgres 16 na portu 5434)
  - [x] Exception handling middleware
  - [x] Health controller
- [x] Frontend: Astro skeleton
  - [x] `astro.config.mjs` sa React, Tailwind v4, Sitemap, Node adapter
  - [x] `tsconfig.json` sa `@/*` aliasom
  - [x] SEO Head komponenta (OG, Twitter, structured data)
  - [x] BaseLayout / LandingLayout
  - [x] Integrisan Magic Patterns landing (Navigation, Hero, HowItWorks, Activities, Preview, FinalCTA, Footer)
  - [x] `robots.txt` ruta
  - [x] `src/lib/api/client.ts` (fetch wrapper)
  - [x] `src/lib/store/auth-store.ts` (Zustand)
  - [x] `Dockerfile` sa build-arg env vars
  - [x] `.env.example`
- [x] Oba: `Documentation/` folder i `CHANGELOG.md`
- [x] Oba: `.github/workflows/build-deploy.yml` (GitLab registry + GitOps Helm update)
- [x] Prvi commit + push na oba repozitorijuma

## Faza 1 — Auth flow (email + 6-digit code)

- [x] Backend: `User` agregat (id, email, name, country, vibePreference, profileImageUrl, role)
- [x] Backend: `VerificationCode` entitet (email, code, expiresAt, usedAt)
- [x] Backend: `POST /api/auth/request-code` endpoint (generiše kod, šalje email)
- [x] Backend: `POST /api/auth/verify-code` endpoint (vraća JWT)
- [x] Backend: `EmailService` (MailKit, SMTP iz config-a, konzolni fallback za dev)
- [x] Backend: JWT claims + middleware
- [x] Backend: EF migracija `001_InitialAuth`
- [x] Frontend: `/login` stranica (email input → code input → redirect)
- [x] Frontend: auto-fill podrška za 6-digit kod (`autocomplete="one-time-code"`, `inputmode="numeric"`)
- [x] Frontend: auth store persistence u `localStorage`
- [ ] Frontend: `AuthGuard` komponenta za zaštićene stranice (Faza 2 — kad bude trebala)

## Faza 2 — Kreiranje poziva (5-step wizard)

- [x] Backend: `Invitation` agregat (pending → viewed; accept/counter/decline u Fazi 3)
- [x] Backend: `Place` value object (googlePlaceId, lat, lng, formattedAddress, name)
- [x] Backend: `CreateInvitationCommand` + handler + validator
- [x] Backend: `CreateAndPublish` u agregatu (generiše `xx-yyyy` slug, expiresAt = +72h)
- [x] Backend: `GET /api/invitations/{slug}` (javni endpoint, lazy auth) + `RecordView`
- [x] Backend: EF migracija `002_Invitations` (owned Place columns)
- [x] Frontend: `/create` wizard stranica sa 5 koraka:
  - [x] Korak 1 — Vibe (Kafa / Piće / Šetnja / Aktivnost / Večera / Custom)
  - [x] Korak 2 — Mjesto (manualni inputi — Google Places je Faza 2.5)
  - [x] Korak 3 — Vrijeme (date + time picker)
  - [x] Korak 4 — Poruka (textarea + URL za media — Giphy picker je Faza 2.5)
  - [x] Korak 5 — Pregled sa "Pošalji" dugmetom
- [x] Frontend: javna `/i/[slug]` stranica (detalji + Google Maps link, accept/counter/decline su Faza 3)
- [ ] Frontend: Google Maps JS SDK integracija (Faza 2.5)
- [ ] Frontend: Giphy API integracija (Faza 2.5)
- [ ] Backend: weather integracija u pregledu (Faza 4)

## Faza 3 — Otvaranje poziva (primalac)

- [x] Frontend: `/i/[slug]` javna stranica (bez logina)
- [x] Frontend: prikaz svih detalja + "Otvori u mapi" dugme (prognoza: Faza 4)
- [x] Frontend: "Prihvatam" dugme → auth flow → status update (`?next=` podržan u login-u)
- [x] Frontend: "Mogu, ali hajde da promijenimo" → auth flow → inline counter-proposal forma
- [x] Frontend: "Odbij" → opcioni komentar (80 char) → status update (bez logina)
- [x] Backend: `AcceptInvitationCommand` (zahtjeva auth)
- [x] Backend: `CounterProposeInvitationCommand` (zahtjeva auth, max 3 runde, auto-close na 3.)
- [x] Backend: `DeclineInvitationCommand` (anonimno + opcioni komentar ≤80)
- [x] Backend: IP-based rate limiter za decline (20/dan, `DeclineRecord` entitet, 429 status)
- [x] Backend: `RecordView` u `GetInvitationBySlugQuery` (Pending → Viewed)
- [ ] Backend: "5 aktivnih poziva po browseru" rate limit (vraća se kad bude Faza 13 admin/abuse)
- [ ] Backend: Initiator-side odgovor na counter (treba Faza 6 dashboard)

## Faza 4 — Vremenska prognoza

- [ ] Backend: `WeatherService` (Open-Meteo client)
- [ ] Backend: caching (per lat/lng/date, TTL 6h)
- [ ] Backend: uključiti prognozu u invitation detail response
- [ ] Frontend: `WeatherCard` komponenta (ikona + temp + opis)

## Faza 5 — Notifikacije

- [ ] Backend: Web Push (VAPID keys, `PushSubscription` entitet)
- [ ] Backend: `NotificationService` (push + email fallback)
- [ ] Backend: hosted service za `reminder-24h` i `reminder-2h` cron
- [ ] Frontend: prompt za push permissions nakon logina
- [ ] Frontend: service worker za push

## Faza 6 — Istorija, otkazivanje, auto-brisanje

- [ ] Backend: `GET /api/invitations/my` (inicijator vidi svoju istoriju)
- [ ] Backend: `CancelInvitationCommand` (šalje notifikaciju drugoj strani)
- [ ] Backend: `MarkCompletedCommand`
- [ ] Backend: `InvitationPurgeHostedService` (briše > 30 dana, izuzima anniversary)
- [ ] Frontend: `/dashboard` stranica (lista aktivnih + prošlih)

## Faza 7 — Safety check

- [ ] Backend: `SafetyCheck` entitet + `CreateSafetyCheckCommand`
- [ ] Backend: hosted service za slanje alarma ako nije potvrđen
- [ ] Frontend: safety check wizard na ekranu potvrde
- [ ] Frontend: `/safety/[token]` javna ruta za prijatelja

## Faza 8 — QR kod

- [ ] Frontend: QR kod generisanje klijentski (npr. `qrcode.react`)
- [ ] Frontend: modal za prikaz QR koda na stranici potvrde

## Faza 9 — Anniversary mode

- [ ] Backend: `Anniversary` entitet (userPair, firstDateAt)
- [ ] Backend: cron za godišnjice → push + email
- [ ] Frontend: toggle u podešavanjima

## Faza 10 — SEO & performance

- [ ] Frontend: `sitemap.xml` whitelist (landing, about, faq, privacy, terms, contact)
- [ ] Frontend: `robots.txt` (blokira sve `/i/*`, `/dashboard`, `/create`)
- [ ] Frontend: Open Graph slike za landing
- [ ] Frontend: structured data (JSON-LD WebApplication)
- [ ] Frontend: Lighthouse audit ≥ 95 na svim javnim stranicama
- [ ] Frontend: hreflang za multi-jezik

## Faza 11 — Admin

- [ ] Backend: admin endpoints (`/api/admin/users`, `/api/admin/flags`)
- [ ] Frontend: `/admin` stranica (jednostavna)

## Faza 12 — Launch

- [ ] Privacy policy, Terms, Cookie policy, Disclaimer stranice
- [ ] GDPR cookie banner
- [ ] "Obriši nalog" flow
- [ ] Load test (k6 ili Artillery)
- [ ] Deploy u prod
