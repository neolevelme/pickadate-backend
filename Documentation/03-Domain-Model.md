# 03 — Domain Model

## Aggregates (current scaffold)

### `User`
- `Id` (Guid)
- `Email` (unique, lowercase)
- `Name` (nullable — set on first login)
- `Country`, `VibePreference`, `ProfileImageUrl` (all optional)
- `Role` (`User` / `Admin`)
- `CreatedAt`

### `Invitation`
- `Id` (Guid)
- `InitiatorId` (FK → User)
- `Slug` (unique, short, URL-safe — e.g. `jv-4k2p`)
- `Vibe` (enum: `Coffee`, `Drinks`, `Walk`, `Activity`, `Dinner`, `Custom`)
- `CustomVibe` (if `Vibe == Custom`)
- `PlaceName`, `PlaceGoogleId`, `PlaceLat`, `PlaceLng`, `PlaceFormattedAddress`
- `MeetingAt` (UTC)
- `Message` (≤140 chars), `MediaUrl` (GIF/sticker)
- `Status` (see state machine)
- `CounterRound` (0-3)
- `CreatedAt`, `ExpiresAt`

## Invitation state machine

```
DRAFT -> PENDING -> VIEWED -> { ACCEPTED | COUNTERED | DECLINED }
                                        |
                                        v
                                   COUNTERED (max 3 rounds) -> EXPIRED
```

Terminal states: `ACCEPTED`, `DECLINED`, `CANCELLED`, `COMPLETED`, `EXPIRED`.

## Coming in later phases

- `VerificationCode` — email + 6-digit code, 10-min expiry
- `CounterProposal` — one round of changes
- `Place` — value object (may be promoted to aggregate later)
- `SafetyCheck` — friend-accessible check-in
- `Anniversary` — user pair + first-successful-date timestamp
- `DeclineRecord` — IP-based rate limiting
- `PushSubscription` — Web Push VAPID subscription
