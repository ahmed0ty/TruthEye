# TruthEye

Online examination platform with browser-based proctoring, live monitoring, and AI-assisted grading for university faculties.

![CI](https://github.com/ahmed0ty/TruthEye/actions/workflows/ci.yml/badge.svg)

## Features

- **Role-based access** for a university president (super admin), deans, faculty secretaries, technical staff, doctors, students, proctors, and support staff.
- **Exams with proctoring**: face and phone detection run in the student's browser (MediaPipe), so no video leaves the device. Suspicious events are logged with evidence.
- **Live monitoring**: proctors watch running exams and receive alerts in real time over SignalR.
- **Session review**: a risk score built from logged events, with a decision workflow for proctors and deans.
- **AI-assisted grading** of open-ended answers through a Sentence-BERT service.
- **Reports** exported as PDF (QuestPDF) and Excel (ClosedXML).
- **Account setup by email link**: administrators never see or send user passwords. New users and password resets receive a one-time link.

## Tech stack

| Area | Technology |
| --- | --- |
| Backend | ASP.NET Core 9, Entity Framework Core 9, SQL Server, SignalR, Serilog |
| Frontend | Next.js 16, React, TypeScript, TanStack Query, React Hook Form + Zod |
| Proctoring | MediaPipe (self-hosted models) |
| AI service | Python, Flask, sentence-transformers (Docker) |
| Tests | xUnit (in-memory database) |
| CI | GitHub Actions, CodeQL, Dependabot |

## Repository layout

```
TruthEye.sln                 Backend solution
TruthEye/                    ASP.NET Core API
TruthEye.Tests/              Backend tests
TruthEye/AI-BERT-Service/    Sentence-BERT grading service (Docker)
Front_End/truth-eye-web/     Next.js frontend
```

## Getting started

### Prerequisites

- .NET SDK 9.0
- Node.js 22 or newer
- SQL Server (or a compatible connection string)
- Docker (only for the AI grading service)

### Backend

Secrets are never stored in the repository. Set them with user-secrets:

```bash
cd TruthEye
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<your connection string>"
dotnet user-secrets set "Jwt:Key" "<at least 32 random characters>"
dotnet user-secrets set "Smtp:Password" "<your SMTP app password>"
dotnet ef database update
dotnet run
```

Super admin registration is disabled by default. Enable it only for the first setup by setting `Setup:AllowSuperAdminRegistration` to `true`, then turn it back off.

### Frontend

Create `Front_End/truth-eye-web/.env.local` (this file is ignored by git):

```bash
NEXT_PUBLIC_API_URL=http://localhost:5210
```

Then:

```bash
cd Front_End/truth-eye-web
npm ci
npm run dev
```

### AI grading service

```bash
cd TruthEye/AI-BERT-Service
docker compose up --build
```

The service binds to `127.0.0.1` by default.

## Running tests

```bash
dotnet test TruthEye.Tests/TruthEye.Tests.csproj
```

## Security

Please report vulnerabilities privately. See [SECURITY.md](SECURITY.md).

## License

No license has been chosen yet. All rights are reserved by the authors until a license is added.
