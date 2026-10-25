# Unfiltered (MVP)

An independent platform for honest reviews that Amazon, Flipkart and other shopping or service sites reject, hide or never allow. This MVP covers the core loop from the PRD: a buyer posts a review with proof, a moderator checks it, and everyone can find and read it.

- **Backend:** ASP.NET Core 9 Web API, EF Core 9, SQL Server, cookie authentication
- **Frontend:** React 18, React Router 7, Vite 6

## Prerequisites

- .NET 9 SDK
- Node.js 18+
- SQL Server (Express, Developer or LocalDB)

## Run it locally

**1. Backend** (http://localhost:5080)

```bash
cd backend/Unfiltered.Api
dotnet run
```

On first start it applies the EF Core migrations, creating the `Unfiltered` database, and in Development adds demo data. The connection string is in `appsettings.Development.json` and points at `.\MSSQLSERVER2018` with Windows authentication. Change it to match your SQL Server, for example `Server=(localdb)\\MSSQLLocalDB;...`.

**2. Frontend** (http://localhost:5173)

```bash
cd frontend
npm install
npm run dev
```

Vite proxies `/api` to the backend, so the auth cookie works without CORS. Open http://localhost:5173.

### Demo accounts

| Role | Email | Password |
| --- | --- | --- |
| Moderator | mod@unfiltered.local | moderator123 |
| Buyer | priya@unfiltered.local | buyer12345 |

To make any account a moderator (they must log out and back in):

```bash
cd backend/Unfiltered.Api
dotnet run -- make-mod someone@example.com
```

## Production build

```bash
cd frontend && npm run build      # outputs into backend/Unfiltered.Api/wwwroot
cd ../backend && dotnet publish Unfiltered.Api -c Release
```

The API then serves the React app and the API from one origin.

## What's in the MVP

| PRD feature | Status |
| --- | --- |
| F1 Accounts | Email + password, public nickname, private email, login rate limit |
| F2 Listing catalog | Products, sellers, service companies; Amazon/Flipkart links normalized so the same product isn't added twice |
| F3 Write review | Rating, title, body, topic tags, where and when bought |
| F4 Proof of purchase | Private upload (type and file signature checked), moderator-only access, Verified badge, files auto-deleted 90 days after review |
| F5 Rejected elsewhere | Screenshot of the original rejection earns a badge; counted on listing pages |
| F6 Search and browse | Search by name or pasted link; listing page with weighted score, rating breakdown, top topics, sort and verified filter |
| F7 Moderation | Rule-based pre-screen (redacts emails and phone numbers, flags strong allegations, links, abuse) then a human queue |
| F8 Report and flag | Users report reviews; moderators keep or remove them with a public reason |
| Transparency | Every moderation decision is shown on the review page; removed reviews stay as a tombstone |
| Abuse limits | 5 reviews per account per day, 10 login/register attempts per IP per minute |

## Not in the MVP yet

- Phone OTP and Google/Apple login (F1)
- OCR of invoices and image redaction (F4)
- AI moderation model instead of rules (F7)
- Business profiles and replies (F9), helpful votes (F10), browser extension (F11), notifications (F12)
- Automated tests

## Before going public

- Legal review of the content policy (`frontend/src/pages/Policy.jsx`) and a takedown process
- Run behind HTTPS (the auth cookie is HTTPS-only outside Development)
- Move uploads to private, encrypted blob storage (set `Uploads:Path` or swap `FileStore`)
- Share ASP.NET Core Data Protection keys across servers if you run more than one instance

## API overview

| Method | Path | Who |
| --- | --- | --- |
| POST | `/api/auth/register`, `/api/auth/login`, `/api/auth/logout` | anyone / signed in |
| GET | `/api/auth/me` | anyone |
| GET | `/api/reviews/home` | anyone |
| GET | `/api/listings?q=` | anyone |
| GET | `/api/listings/{id}?sort=recent\|lowest\|highest&verifiedOnly=` | anyone |
| GET | `/api/reviews/{id}` | anyone (pending/rejected: owner or moderator) |
| POST | `/api/reviews` (multipart) | signed in |
| GET | `/api/reviews/mine` | signed in |
| POST | `/api/reviews/{id}/flags` | signed in |
| DELETE | `/api/reviews/{id}` | owner |
| GET | `/api/moderation/queue` | moderator |
| GET | `/api/moderation/files/{name}` | moderator |
| POST | `/api/moderation/reviews/{id}/approve`, `.../reject` | moderator |
| POST | `/api/moderation/flags/{id}/dismiss`, `.../uphold` | moderator |

## Project layout

```
backend/
  Unfiltered.sln
  Unfiltered.Api/
    Controllers/     Auth, Listings, Reviews, Moderation
    Data/            DbContext, migrations, demo seeder
    Dtos/            request and response records
    Models/          EF Core entities
    Services/        content screener, URL normalizer, file store, proof cleanup
frontend/
  src/
    pages/           one component per route
    components/      layout, review card, loading/error
    api.js           fetch wrapper and useApi hook
    auth.jsx         auth context and route guard
```
