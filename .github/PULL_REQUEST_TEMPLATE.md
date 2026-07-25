## What does this change?

<!-- One or two sentences. What, and why. -->

## Which service(s)?

- [ ] `api/` (.NET)
- [ ] `ai-runtime/` (Python)
- [ ] `frontend/` (Next.js)
- [ ] `docs/`
- [ ] Other (specify):

## Related issue

<!-- Closes #, or "N/A" for a small fix opened without a prior issue -->

## How was this tested?

- [ ] `dotnet build && dotnet test` (if `api/` changed)
- [ ] `pytest` (if `ai-runtime/` changed)
- [ ] `npx tsc --noEmit && npm run lint` (if `frontend/` changed)
- [ ] Manually ran the affected page/flow in the browser (required for any UI change — there is no
      frontend test suite yet, see `docs/ROADMAP.md`)
- [ ] Added/updated tests for the change

## Checklist

- [ ] I read `CONTRIBUTING.md`
- [ ] This follows the existing patterns for its service (CQRS folder structure for `api/`,
      declarative agent subclass for `ai-runtime/`, hooks-only data fetching for `frontend/`)
- [ ] I did not add authentication/authorization scope creep to an unrelated PR — that's a
      tracked, separate effort (see `docs/ROADMAP.md`)
- [ ] Docs updated if this changes an endpoint, event shape, or architectural boundary

## Screenshots (for UI changes)

<!-- Before/after, or a screenshot of the new state -->
