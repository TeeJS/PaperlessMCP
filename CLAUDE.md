# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
dotnet build                # Build the solution
dotnet test                 # Run all tests
dotnet test --filter "FullyQualifiedName~DocumentToolsTests"  # Run specific test class
dotnet test --logger "console;verbosity=detailed"             # Verbose test output
```

**Running locally:**
```bash
# Set required environment variables
export PAPERLESS_BASE_URL=https://your-instance.com
export PAPERLESS_API_TOKEN=your-token

# stdio mode (for Claude Desktop)
dotnet run --project PaperlessMCP -- --stdio

# HTTP mode (runs at http://localhost:5000/mcp)
dotnet run --project PaperlessMCP
```

## Architecture

PaperlessMCP is an MCP (Model Context Protocol) server that bridges Paperless-ngx with AI assistants. It supports dual transport modes: stdio for Claude Desktop integration and HTTP for remote usage.

### Key Components

- **Program.cs** - Entry point with dual transport setup (stdio vs HTTP). Configures DI, HttpClient with Polly retry policies, and MCP server registration.

- **Client/PaperlessClient.cs** - Central API client for all Paperless-ngx operations. Uses snake_case JSON serialization to match API conventions.

- **Client/PaperlessAuthHandler.cs** - DelegatingHandler that injects API token authentication.

- **Tools/** - MCP tool implementations, one file per entity type:
  - `DocumentTools.cs` - Search, CRUD, bulk operations, upload (base64 and file path)
  - `TagTools.cs`, `CorrespondentTools.cs`, `DocumentTypeTools.cs`, `StoragePathTools.cs` - Entity management with matching algorithms
  - `CustomFieldTools.cs` - Custom field definitions and assignments
  - `HealthTools.cs` - Ping and capabilities

- **Models/** - Request/response DTOs organized by entity (Documents/, Tags/, etc.). Uses `PaginatedResponse<T>` for list endpoints.

### Configuration

Environment variables (take precedence over appsettings.json):
- `PAPERLESS_BASE_URL` / `PAPERLESS_URL` - Paperless-ngx instance URL
- `PAPERLESS_API_TOKEN` / `PAPERLESS_TOKEN` - API authentication token
- `MAX_PAGE_SIZE` - Max items per page (default: 100)
- `MCP_PORT` - HTTP server port (default: 5000)
- `HTTP_TIMEOUT_SECONDS` - Timeout for Paperless API requests (default: 30)

### Testing

Tests use xUnit with FluentAssertions for assertions, NSubstitute for mocking, and RichardSzalay.MockHttp for HTTP mocking. Test files mirror the main project structure under `PaperlessMCP.Tests/`.

## Conventions

- Use [Conventional Commits](https://www.conventionalcommits.org/) - version bumps are automatic based on commit type (`fix:` = patch, `feat:` = minor, `feat!:` = major)
- Trunk-based development: feature branches merge directly to `main`
- All destructive operations (delete, bulk operations) require explicit `confirm=true` and default to dry-run mode

## Versioning & Releases

> **This fork does not use upstream's release automation.** `.woodpecker/` (`pr.yml`,
> `release.yml`) is barryw's Woodpecker CI, inherited at fork time and **never run here** —
> it pushes to barryw's GHCR namespace and creates releases on barryw's repo. This fork
> releases through GitHub Actions in `.github/workflows/` only.

**Version scheme: `<upstream version>-teejs.<n>`** (e.g. `0.3.2-teejs.1`), read as "barryw's
0.3.2, plus our patch set, revision 1". Bump the suffix for our own changes; reset it on the
next upstream sync (upstream 0.4.0 → `0.4.0-teejs.1`). This is the downstream-distribution
convention (cf. Debian's `1.2.3-1ubuntu2`), and it exists because our code differs materially
from upstream at the same version number — restricted tool surface, name-only writes,
notes-stripping.

**Versions are hand-maintained. Nothing computes or bumps them.** To release:

1. Edit `version.json` and `<Version>` in `PaperlessMCP/PaperlessMCP.csproj` to match.
2. Merge to `main`.
3. Tag and push it **by name**:
   ```bash
   git tag -a v0.3.2-teejs.2 -m "Upstream base: barryw v0.3.2 / fork revision 2"
   git push origin v0.3.2-teejs.2
   ```

**Never `git push --tags`.** All of barryw's tags (`v0.1.0`–`v0.3.2`) live in the local clone,
inherited at fork time — this fork has cut exactly one of its own. `--tags` would push ~28 of
upstream's tags into this repo. Always push tags by name.

**What the workflows actually do:**

- `docker.yml` — a push to `main` publishes `:latest` + `:sha-<short>`; pushing a `v*` tag
  publishes the exact version (`:0.3.2-teejs.1`) + `:sha-<short>`; PRs build only, never push.
  On non-tag builds the `VERSION` build-arg comes from `version.json`; on tag builds it comes
  from the tag name. That build-arg reaches `dotnet publish /p:Version=`, so it sets the
  **compiled assembly's** version. (The OCI `org.opencontainers.image.*` labels do *not* come
  from the Dockerfile — docker/metadata-action's `--label` flags override them, so
  `image.version` reads `latest` on main and `pr-N` on PRs.)
- `test.yml` — runs `dotnet test` on pushes to `main`, `v*` tags, PRs targeting `main`, and
  `workflow_dispatch`. **A push to a plain feature branch does not run it** — open a PR to get
  the suite. `docker.yml` compiles only the main project, so a compile error in
  `PaperlessMCP.Tests/` is invisible to it.

**There is no floating `:0.3` tag, by design.** The `-teejs.N` suffix makes the version a semver
prerelease, and docker/metadata-action deliberately skips `{{major}}.{{minor}}` for prereleases.
Only the exact version is published. That's fine: `:latest` already covers "track the newest",
and a pinned tag exists precisely so it does *not* move.

**Do not use `cog` for versioning.** It is not installed on the dev machine and has never tagged
this fork. It also *cannot* drive this scheme: semver sorts `0.3.2-teejs.1` **below** plain
`0.3.2`, so `from_latest_tag = true` would resolve barryw's `v0.3.2` over our tags and never see
them. `cog.toml` is inherited from upstream.

**Conventional Commits are a convention here, not enforced.** No `commit-msg` hook is installed,
and there is no commitlint step in `.github/workflows/`. Follow the format anyway — it keeps
history readable and consistent with upstream's, which matters when reading commit messages to
resolve sync conflicts.

**Commit format:**
```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

Types: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`
