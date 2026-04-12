# pickadate.me — Specifikacija aplikacije

> Ovo je izvorna specifikacija proizvoda. Backend i frontend prate ove zahtjeve.
> Iste datoteke treba držati sinhronizovanim u oba repozitorijuma
> (`pickadate-backend` i `pickadate-frontend`) dok se ne uvede zajednički `docs` repo.

## Pregled

`pickadate.me` je web aplikacija za zakazivanje prvog izlaska između dvije osobe
koje su se upoznale na dating aplikacijama (Tinder, Hinge, Bumble) ili drugim
kanalima. Inicijator pravi konkretan poziv (vrijeme + mjesto + tip izlaska +
opciona poruka) i šalje deljivi link primaocu, koji ga otvara bez naloga i bira
jednu od tri opcije: prihvati, kontra-predloži, ili odbij.

**Cilj proizvoda:** zamijeniti *chat purgatory* ("we should grab coffee sometime")
jednim elegantnim potezom.

**Tech stack:**
- Frontend: Astro + React (islands) + Tailwind v4
- Backend: .NET 10 (Clean Architecture + MediatR + EF Core)
- Baza: PostgreSQL 16
- SEO optimizacija kao prioritet

---

## 1. Autentifikacija i role

**Prijava:** isključivo preko email adrese sa verifikacionim kodom (6 cifara)
koji stiže emailom. Bez lozinke, bez Google OAuth-a, bez SMS-a.

- Verifikacioni kod važi **10 minuta**.
- Mobilni OS-ovi (iOS/Android) automatski prepoznaju kod iz emaila i nude auto-fill.
- **Lazy authentication:** primalac poziva može da vidi pun poziv i da ga odbije
  bez ikakvog naloga. Login se traži tek kad korisnik radi akciju koja stvara
  odnos (prihvatanje ili kontra-predlog).

**Role:**
- **Admin** — vidi sistem, upravlja korisnicima, moderira flag-ovane sadržaje
- **Initiator** — kreator poziva, ima nalog, vidi svoju istoriju
- **Recipient** — gost (bez naloga, samo odbija) ili pun korisnik
  (pravi nalog kad prihvata ili kontra-predlaže)

## 2. Korisnički profil

Svaki registrovani korisnik ima:
- **ime** (uneseno ručno pri prvoj prijavi, može da se mijenja)
- **email** (ne može da se mijenja)
- **profilna slika** (opciono)
- **država** (opciono)
- **preferirani vibe** (opciono)

Bez bio polja. Bez javnog profila. Niko ne može da pretražuje korisnike —
`pickadate.me` nije dating aplikacija.

## 3. Kreiranje poziva (5 koraka)

### Korak 1 — Vibe izlaska
Jedan od pet ponuđenih tipova: **Kafa, Piće, Šetnja, Aktivnost, Večera**.
Plus opcija **"Drugo"** za custom vibe (npr. kuglanje, izložba, koncert, kuvanje).

### Korak 2 — Mjesto
Dva načina:
- Pretraga kroz **Google Places autocomplete**
- Ručno postavljanje pina na **Google mapi**

Aplikacija **ne predlaže** mjesta — sva mjesta dolaze iz Google Maps.
Filter prihvata samo javna mjesta (restorani, kafići, barovi, muzeji, parkovi
tokom radnog vremena), odbija privatne adrese/stambene objekte.

### Korak 3 — Vrijeme
Jedan tačan termin: dan i sat. Bilo koji budući termin je dozvoljen.

### Korak 4 — Poruka (opciono)
- Do **140 karaktera**, sa emojijima
- Može se priložiti **GIF ili sticker** (preko Giphy/Tenor integracije)
- Polje se može u potpunosti preskočiti

### Korak 5 — Pregled i link
Aplikacija prikazuje sažetak (vibe, mjesto, vrijeme, poruka, GIF/sticker) i
**vremensku prognozu** za dan sastanka ako je dostupna.

Generiše se deljivi link u formatu `pickadate.me/jv-4k2p`.

**Dijeljenje:**
- Web Share API (jedan tap u Tinder, WhatsApp, Instagram DM, iMessage, Telegram...)
- "Kopiraj link" kao backup
- **QR kod** se automatski generiše (za dijeljenje uživo)

**Link ističe za 72 sata** ako primalac ne otvori.

## 4. Otvaranje poziva (primalac)

Primalac otvara link **bez logina** i vidi:
- Ime inicijatora
- Vibe, datum, vrijeme
- Mjesto sa adresom (klik → Google Maps navigacija)
- Ličnu poruku (ako postoji, uključujući GIF/sticker)
- Vremensku prognozu za dan sastanka (ako je dostupna)

Tri opcije:

### Opcija 1 — Prihvatam
- Traži login (email + 6-cifreni kod)
- Status poziva → `ACCEPTED`
- Inicijator dobija push notifikaciju
- Oboje vide ekran potvrde sa: detaljima, navigacijom, safety check opcijom, prognozom

### Opcija 2 — Mogu, ali hajde da promijenimo
- Traži login
- Primalac bira šta mijenja: samo vrijeme / samo mjesto / oboje
- Kontra-predlog se šalje inicijatoru
- **Maksimalno 3 runde ping-ponga** (predlog → kontra → kontra-kontra → finalna odluka)
- Nakon 3 runde bez dogovora, poziv se automatski zatvara

### Opcija 3 — Odbij
- **Bez logina**
- Opciono polje za komentar (do **80 karaktera**)
- Inicijator dobija mirnu push notifikaciju "Tvoj poziv nije prihvaćen"
  + komentar ako je ostavljen
- **Komunikacija je jednosmjerna** — inicijator ne može da odgovori

## 5. Identifikacija "ko je ko"

- Svaki link sadrži ID poziva
- Inicijatorov browser pamti **tajni ključ** kroz cookie/localStorage
- Primalac postaje identifikovan tek kad se uloguje
- Registrovani korisnici imaju pun pristup istoriji sa bilo kog uređaja

## 6. Vremenska prognoza

Prikazuje se na više mjesta:
- U pregledu prije slanja poziva (inicijator vidi prognozu)
- U primljenom pozivu (primalac vidi)
- Na ekranu potvrde sastanka
- U **podsetniku 24h prije sastanka** (sa upozorenjem ako se prognoza
  dramatično promijenila — npr. kiša najavljena za šetnju)

- Dostupna **do 7 dana unaprijed**
- Besplatan API: **Open-Meteo** ili sličan

## 7. Safety check

Opcioni feature na ekranu potvrde sastanka (vidljiv **tek nakon prihvatanja**).

Korisnik kreira poseban link za prijatelja koji sadrži:
- Detalje sastanka (gdje, kada)
- **Automatski check-in** 2h posle početka

Prijatelj otvara link u svom pretraživaču bez instalacije aplikacije. Korisnik
može da pritisne "sve je u redu" ranije, što briše alarm. Ako ne pritisne,
prijatelj dobija push notifikaciju sa poslednjom poznatom lokacijom (samo ako
je korisnik uključio location sharing).

## 8. Anniversary mode

Kad je par označio jedan ili više sastanaka kao "uspješne", sistem pamti datum
prvog sastanka. Na godišnjicu oba korisnika dobijaju mirnu notifikaciju
*"Danas je godina dana od vašeg prvog sastanka"* sa opcijom da pošalju nov poziv
direktno iz notifikacije.

Opcioni feature — može se isključiti u podešavanjima.

## 9. Notifikacije (Inicijator)

Push + email fallback za:
- Primalac je otvorio link
- Primalac je prihvatio (sa detaljima)
- Primalac je poslao kontra-predlog
- Primalac je odbio (mirna formulacija)
- Podsetnik 24h prije sastanka (sa ažurnom prognozom)
- Podsetnik 2h prije sastanka
- Anniversary podsetnici (ako je uključeno)

## 10. Istorija i upravljanje pozivima

Registrovani korisnik može za svaki poziv:
- Otvoriti detalje (vrijeme, mjesto, status, prognoza)
- Kliknuti na lokaciju (Google Maps navigacija)
- Otkazati sastanak (šalje obavještenje)
- Označiti sastanak kao završen

**Pozivi se automatski brišu 30 dana** posle planiranog datuma
(data minimization), osim onih povezanih sa Anniversary mode-om.

## 11. Globalna dostupnost

- Dostupna iz cijelog svijeta od prvog dana
- Interfejs: engleski + jezik prvog tržišta
- Radi u svakom gradu na svijetu odmah (Google Maps integracija)

## 12. Privatnost i čuvanje podataka

- Bez chat funkcije unutar aplikacije
- Bez javnog profila / pretrage korisnika
- Bez praćenja lokacije u realnom vremenu (osim opciono za safety check)
- Pozivi se brišu nakon 30 dana (osim Anniversary podataka)
- Korisnik može da obriše nalog jednim klikom (briše sve podatke)
- Anonimni primaoci koji odbijaju: samo IP za rate limiting, briše se za 24h
- **GDPR** usklađenost kao osnovni princip dizajna

## 13. Anti-zloupotreba

- Rate limiting po IP: **max 20 odbijanja dnevno, max 5 aktivnih poziva po browseru**
- **Max 3 runde** kontra-predloga po pozivu
- Automatska detekcija script-ovane aktivnosti

## 14. Scope protection — šta NE radimo

- Nema chat funkcije
- Nema browse/discover funkcije
- Nema swipe mehanike
- Nema gamification, streakova, badge-ova
- Nema reklama
- Nema "report this person" u prvoj verziji
- Nema mogućnosti da inicijator odgovori na komentar primaoca
- Nema istorije odbijanja koju inicijator može da pregleda
- Nema "rate the person" funkcionalnosti
- Nema kuratorske baze mjesta — sva mjesta iz Google Maps

---

## Invitation State Machine (tehnički model)

```
          ┌────────────┐
          │  DRAFT     │  (inicijator pravi poziv, nije još poslat)
          └─────┬──────┘
                │ publish()
                ▼
          ┌────────────┐
          │  PENDING   │  (link generisan, primalac nije otvorio)
          └─────┬──────┘
                │ open()
                ▼
          ┌────────────┐
          │  VIEWED    │  (primalac otvorio link)
          └─┬─────┬──┬─┘
            │     │  │
     accept │     │  │ decline
            ▼     │  ▼
     ┌──────────┐ │  ┌──────────┐
     │ ACCEPTED │ │  │ DECLINED │
     └──────────┘ │  └──────────┘
                  │ counter()
                  ▼
            ┌──────────────┐
            │ COUNTERED    │  (runde: max 3)
            └──────┬───────┘
                   │ runda > 3
                   ▼
            ┌──────────────┐
            │ EXPIRED      │
            └──────────────┘
```

**Ostali final state-ovi:**
- `EXPIRED_UNOPENED` — primalac nije otvorio u 72h
- `CANCELLED` — inicijator otkazao
- `COMPLETED` — sastanak prošao, bilo ko označio kao završen

## Domain entiteti (visoki nivo)

- **User** — registrovani korisnik (Admin / Initiator)
- **Invitation** — jedan poziv sa svojim state-om i counter-proposal istorijom
- **Place** — Google Places referenca (place_id, lat, lng, formatted address, name)
- **CounterProposal** — jedna runda izmjene (nove vrijednosti za place/time, autor)
- **VerificationCode** — 6-cifreni kod vezan za email, važi 10 min
- **SafetyCheck** — link za prijatelja sa check-in vremenom
- **Anniversary** — par + datum prvog uspješnog sastanka
- **DeclineRecord** — za rate limiting (IP + timestamp, briše se za 24h)
