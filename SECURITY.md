# Security Policy

## Supported versions

Only the `main` branch receives security fixes.

## Reporting a vulnerability

Please do not open a public issue for security problems.

Use GitHub's **Report a vulnerability** button under the repository's *Security* tab. This opens a private advisory visible only to the maintainers.

Include:

- A description of the issue and its impact.
- Steps to reproduce, or a proof of concept.
- The affected file or endpoint, if known.

We aim to acknowledge reports within a few working days.

## Secrets

Never commit credentials, connection strings, JWT keys, or SMTP passwords. Use `dotnet user-secrets` for the backend and `.env.local` for the frontend. Both are excluded by `.gitignore`.
