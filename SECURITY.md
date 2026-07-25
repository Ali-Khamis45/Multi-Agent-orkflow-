# Security Policy

## Scope and current posture

Read [docs/reviews/SECURITY_REVIEW.md](docs/reviews/SECURITY_REVIEW.md) before reporting — it's an
honest, detailed account of this project's current security posture, including the fact that
**there is no authentication or authorization anywhere in the system today**. That is a documented,
intentional scope boundary for this release (a local/demo, single-operator deployment), not a
secret vulnerability — please don't report it as one. Everything else in that document is real and
current as of the last review.

## Reporting a vulnerability

If you find a security issue that **isn't** already documented in
[docs/reviews/SECURITY_REVIEW.md](docs/reviews/SECURITY_REVIEW.md):

1. **Do not open a public GitHub issue.**
2. Use GitHub's [private security advisory](../../security/advisories/new) feature on this
   repository, or contact the maintainer directly.
3. Include: what you found, how to reproduce it, and what you think the impact is.

You should expect an initial response within a few days. This is a small project maintained
part-time — please be patient, and thank you for reporting responsibly.

## What's in scope

- The ASP.NET Core API (`api/`)
- The Python AI runtime (`ai-runtime/`)
- The Next.js frontend (`frontend/`)
- The Docker Compose deployment configuration

## What's explicitly out of scope

- The absence of authentication/authorization itself (documented, tracked in
  [docs/ROADMAP.md](docs/ROADMAP.md))
- Vulnerabilities requiring physical access to a machine already running this stack
- Findings from automated scanners run against dependencies already listed in
  [docs/reviews/SECURITY_REVIEW.md § Dependency vulnerabilities](docs/reviews/SECURITY_REVIEW.md#6-dependency-vulnerabilities)
  — those are already tracked; a PR bumping the dependency is more useful than a duplicate report.

## Supported versions

This project has not yet had a stable release before v1.0.0. Once tagged, security fixes will
target the latest `v1.x` release; there is no long-term-support branch policy yet.
