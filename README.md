# TruthEye

**TruthEye** is an online exam proctoring platform for universities. It combines real-time browser monitoring, AI-assisted face and object detection, and a risk-scoring engine that helps exam supervisors review suspicious sessions. It also covers the academic side of a faculty: courses, doctors, students, proctors, and staff roles.

[![CI](https://github.com/ahmed0ty/TruthEye/actions/workflows/ci.yml/badge.svg)](https://github.com/ahmed0ty/TruthEye/actions/workflows/ci.yml)
[![CodeQL](https://github.com/ahmed0ty/TruthEye/actions/workflows/codeql.yml/badge.svg)](https://github.com/ahmed0ty/TruthEye/actions/workflows/codeql.yml)

---

## Features

- **Live exam monitoring:** students stay connected through heartbeats, and proctors follow sessions in real time over SignalR.
- **AI-assisted detection:** MediaPipe runs in the browser to detect faces, and a Python SBERT service supports answer grading.
- **Integrity events and risk scoring:** events such as tab switches, fullscreen exits, copy/paste, multiple faces, and duplicate logins are weighted into a per-session risk score (low, medium, or high).
- **Session review:** reviewers get a sorted, filterable list of sessions with the evidence attached to each one.
- **Role-based access:** SuperAdmin, Dean, Faculty Secretary, Technical Staff, Doctor, Student, Proctor, and Support, each with its own permissions.
- **Secure account setup:** new accounts receive a one-time setup link by email. Administrators never see or send passwords.
- **Bulk import:** students can be imported from Excel (`.xlsx`) or CSV files.
- **Reports:** PDF reports (QuestPDF) and Excel exports (ClosedXML), with protection against spreadsheet formula injection.
- **Messaging and notifications:** real-time chat and notifications over SignalR.

## Architecture

| Layer | Technology |
|-------|-----------|
| Backend API | ASP.NET Core 9, Entity Framework Core 9, SQL Server |
| Real-time | SignalR hubs (chat, notifications, exam monitoring) |
| Authentication | JWT bearer tokens, rotating refresh tokens in HttpOnly cookies, per-request session validation |
| Frontend | Next.js 16 (App Router), React, TanStack Query, react-hook-form, zod |
| Browser AI | MediaPipe (self-hosted models) |
| AI grading service | Python, Sentence-BERT (SBERT), Docker |
| Logging | Serilog |
| Tests | xUnit with an in-memory database |

## Repository layout

TruthEye/ ASP.NET Core API (controllers, services, EF Core, migrations)
TruthEye.Tests/ Backend tests
Front_End/truth-eye-web/ Next.js frontend
TruthEye/AI-BERT-Service/ Python SBERT grading service (Docker)
.github/ CI, CodeQL, and Dependabot configuration


## Getting started

### Prerequisites

- .NET 9 SDK
- Node.js 22 or later
- SQL Server (local instance or LocalDB)
- Docker (optional, only for the SBERT service)

### 1. Configure the backend

Secrets are never stored in the repository. Set them with user-secrets from the `TruthEye` folder:

```bash
cd TruthEye
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your SQL Server connection string>"
dotnet user-secrets set "Jwt:Key" "<a random key of at least 32 characters>"
dotnet user-secrets set "Smtp:Password" "<your SMTP app password>"
```

Other settings, such as the SMTP host and the frontend reset URL, are in `appsettings.json`.

### 2. Create the database

```bash
dotnet ef database update
```

### 3. Create the first administrator

Set `Setup:AllowSuperAdminRegistration` to `true` temporarily, register the SuperAdmin account, then set it back to `false`.

### 4. Run the API

```bash
dotnet run --project TruthEye
```

### 5. Run the frontend

Create `Front_End/truth-eye-web/.env.local`:

NEXT_PUBLIC_API_URL=http://localhost:5210


Then run:

```bash
cd Front_End/truth-eye-web
npm ci
npm run dev
```

### 6. Run the SBERT service (optional)

```bash
cd TruthEye/AI-BERT-Service
docker compose up --build
```

## Running the tests

```bash
cd TruthEye.Tests
dotnet test
```

## Security

- Secrets (connection strings, JWT keys, SMTP passwords) must be set with user-secrets or environment variables, and must never be committed.
- The API exposes a Content Security Policy report endpoint at `/api/csp-report`, and the frontend uses per-request nonces.
- Dependencies are monitored with Dependabot, CodeQL scans run weekly, and CI blocks packages with known vulnerabilities.
- To report a vulnerability, see [SECURITY.md](SECURITY.md).

## License

No license has been chosen yet. All rights are reserved by the author until one is added.