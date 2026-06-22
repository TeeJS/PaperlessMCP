# PaperlessMCP

**Stop manually organizing your documents. Let AI do it.**

[![Build Status](https://ci.barrywalker.io/api/badges/3/status.svg)](https://ci.barrywalker.io/repos/3)
[![Latest Release](https://img.shields.io/github/v/release/barryw/PaperlessMCP)](https://github.com/barryw/PaperlessMCP/releases/latest)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

You've got a Paperless-ngx instance. You've got hundreds (thousands?) of documents. You *know* you should tag them, set correspondents, organize them properly. But who has time for that?

PaperlessMCP connects your Paperless-ngx to any MCP-compatible AI. Now instead of clicking through the UI, you just ask:

> "Find all my tax documents from 2023"
>
> "Tag these 50 invoices as 'Business Expense' and set the correspondent to 'Acme Corp'"
>
> "Upload this receipt and figure out what it is"
>
> "What documents am I missing from my insurance folder?"

It's Paperless-ngx on LLM steroids. An interface designed *specifically* for AI to manage your documents while you do literally anything else.

---

## What Can AI Do With Your Paperless?

Everything. Full CRUD on every entity type:

| You Say | AI Does |
|---------|---------|
| "Find receipts from Amazon over $100" | Searches documents with filters |
| "Tag all 2024 invoices as 'Tax Year 2024'" | Bulk updates dozens of docs at once |
| "Upload this PDF and file it appropriately" | Uploads, auto-tags, sets correspondent |
| "Delete all documents tagged 'Junk'" | Removes with confirmation (dry-run by default) |
| "Create a tag for medical records, make it red" | Creates tag with color |
| "Who sends me the most documents?" | Lists correspondents by document count |
| "Set up a storage path for legal documents" | Creates organized folder structure |

**43 tools** covering:
- **Documents** — search, upload, download, update, delete, bulk operations, OCR reprocessing
- **Tags** — full CRUD with colors and matching rules
- **Correspondents** — track who sends you stuff
- **Document Types** — classify invoices, receipts, contracts, whatever
- **Storage Paths** — organize files with smart templates
- **Custom Fields** — add your own metadata (dates, amounts, URLs, etc.)

All destructive operations require explicit confirmation and default to dry-run mode. AI can't nuke your archive by accident.

---

## Is PaperlessMCP Right For You?

**Yes, if:**

- You run Paperless-ngx (self-hosted or cloud)
- You use any AI assistant that speaks MCP (Claude, or anything else supporting the protocol)
- You have a backlog of untagged documents and feel guilty about it
- You'd rather say "organize this" than click 47 buttons
- You want to query your documents in plain English
- You think computers should work for you, not the other way around

**No, if:**

- You don't use Paperless-ngx (this isn't a general document tool)
- You enjoy manually tagging documents (weirdo, but respect)
- You don't trust AI with your files (fair, but you can dry-run everything first)

**The sweet spot:** You've got Paperless running, you've got an MCP-compatible AI, and you want them to be friends.

---

## Getting Started

### You'll Need

1. **A Paperless-ngx instance** with an API token
   *(Settings → Django Admin → Tokens → Create one for your user)*

2. **An MCP-compatible AI** (Claude Desktop, or anything speaking the protocol)

### Option 1: Docker (Recommended)

The fastest path from zero to talking to your documents.

[![Latest Release](https://img.shields.io/github/v/release/barryw/PaperlessMCP?label=latest)](https://github.com/barryw/PaperlessMCP/releases/latest)

```bash
docker run -d \
  --name paperless-mcp \
  --restart unless-stopped \
  -e PAPERLESS_BASE_URL=https://your-paperless.example.com \
  -e PAPERLESS_API_TOKEN=your-token-here \
  -p 5000:5000 \
  ghcr.io/barryw/paperlessmcp:vX.Y.Z
```

> **Grab the version from the badge above.** We don't use `latest` because [you deserve reproducible deployments](https://vsupalov.com/docker-latest-tag/).

Connect your MCP client to `http://localhost:5000/mcp` and start talking to your documents.

### Option 2: Claude Desktop

Add to your config file:

| OS | Path |
|----|------|
| macOS | `~/Library/Application Support/Claude/claude_desktop_config.json` |
| Windows | `%APPDATA%\Claude\claude_desktop_config.json` |

```json
{
  "mcpServers": {
    "paperless": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/PaperlessMCP/PaperlessMCP", "--", "--stdio"],
      "env": {
        "PAPERLESS_BASE_URL": "https://your-paperless.example.com",
        "PAPERLESS_API_TOKEN": "your-token-here"
      }
    }
  }
}
```

Restart Claude Desktop. Look for the tools icon — Paperless should be there.

### Option 3: Claude Code

One command if you're already running the server somewhere:

```bash
# Connect to a running Streamable HTTP server
claude mcp add --transport http paperless http://localhost:5000/mcp
```

Or run from source with stdio:

```bash
claude mcp add --transport stdio paperless \
  -e PAPERLESS_BASE_URL=https://your-paperless.example.com \
  -e PAPERLESS_API_TOKEN=your-token-here \
  -- dotnet run --project /path/to/PaperlessMCP/PaperlessMCP -- --stdio
```

Verify it's there:

```bash
claude mcp list
```

### Option 4: LiteLLM Proxy

LiteLLM 1.83.7 can register PaperlessMCP as a Streamable HTTP MCP server in `config.yaml`.

Start PaperlessMCP first using Docker, Kubernetes, or source, then add it to LiteLLM:

```yaml
mcp_servers:
  paperless:
    url: "http://paperless-mcp:5000/mcp"
    transport: "http"
    description: "Paperless-ngx document management"
```

Use a URL that the LiteLLM process can reach. In Docker Compose, set the host to the PaperlessMCP service name from that Compose file, such as `paperless-mcp`. If LiteLLM runs directly on the host and PaperlessMCP publishes port 5000, use `http://127.0.0.1:5000/mcp`.

Set `transport: "http"` explicitly for PaperlessMCP's `/mcp` endpoint. LiteLLM's MCP config defaults to `sse`, which is the wrong transport for this endpoint.

`PAPERLESS_API_TOKEN` belongs on the PaperlessMCP service; it is the token PaperlessMCP uses when calling Paperless-ngx. PaperlessMCP does not require an inbound token on `/mcp` unless you put a separate auth layer, such as a reverse proxy, in front of it.

For LiteLLM database-backed MCP storage, enable database storage in LiteLLM:

```yaml
general_settings:
  store_model_in_db: true
```

For static configuration, keep the server under the top-level `mcp_servers` key.

### Option 5: Kubernetes

For the homelabbers running k8s. We include ready-to-use manifests with Kustomize support.

```bash
# Clone and customize
git clone https://github.com/barryw/PaperlessMCP.git
cd PaperlessMCP/k8s

# Create your secrets
kubectl create secret generic paperless-mcp \
  --from-literal=PAPERLESS_BASE_URL=https://your-paperless.example.com

kubectl create secret generic paperless-token \
  --from-literal=token=your-api-token-here

# Deploy (edit the image tag first)
kubectl apply -k .
```

[See the manifests](k8s/)

Includes: Deployment, Service, Ingress, Kustomization. Tweak to taste.

### Option 6: From Source

For contributors and tinkerers:

```bash
git clone https://github.com/barryw/PaperlessMCP.git
cd PaperlessMCP
dotnet run --project PaperlessMCP             # Streamable HTTP on :5000
dotnet run --project PaperlessMCP -- --stdio  # stdio mode
```

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

---

## The Full Toolbox

43 tools, organized by what they touch. Every entity supports full CRUD.

<details>
<summary><strong>Documents</strong> — the main event</summary>

| Tool | What it does |
|------|--------------|
| `paperless.documents.search` | Find documents with full-text search and filters |
| `paperless.documents.get` | Get a document by ID with all metadata |
| `paperless.documents.upload` | Upload a document (base64) |
| `paperless.documents.upload_from_path` | Upload from a file path |
| `paperless.documents.update` | Update title, tags, correspondent, etc. |
| `paperless.documents.delete` | Delete a document (requires confirmation) |
| `paperless.documents.bulk_update` | Update multiple documents at once |
| `paperless.documents.download` | Get download URL for original file |
| `paperless.documents.preview` | Get preview URL |
| `paperless.documents.thumbnail` | Get thumbnail URL |
| `paperless.documents.reprocess` | Re-run OCR on a document |

</details>

<details>
<summary><strong>Tags</strong> — organize everything</summary>

| Tool | What it does |
|------|--------------|
| `paperless.tags.list` | List all tags |
| `paperless.tags.get` | Get a tag by ID |
| `paperless.tags.create` | Create a tag with optional color and matching rules |
| `paperless.tags.update` | Update a tag |
| `paperless.tags.delete` | Delete a tag |
| `paperless.tags.bulk_delete` | Delete multiple tags |

</details>

<details>
<summary><strong>Correspondents</strong> — who sends you stuff</summary>

| Tool | What it does |
|------|--------------|
| `paperless.correspondents.list` | List all correspondents |
| `paperless.correspondents.get` | Get a correspondent by ID |
| `paperless.correspondents.create` | Create with optional matching rules |
| `paperless.correspondents.update` | Update a correspondent |
| `paperless.correspondents.delete` | Delete a correspondent |
| `paperless.correspondents.bulk_delete` | Delete multiple correspondents |

</details>

<details>
<summary><strong>Document Types</strong> — invoices, receipts, contracts...</summary>

| Tool | What it does |
|------|--------------|
| `paperless.document_types.list` | List all document types |
| `paperless.document_types.get` | Get a document type by ID |
| `paperless.document_types.create` | Create with optional matching rules |
| `paperless.document_types.update` | Update a document type |
| `paperless.document_types.delete` | Delete a document type |
| `paperless.document_types.bulk_delete` | Delete multiple document types |

</details>

<details>
<summary><strong>Storage Paths</strong> — where things live</summary>

| Tool | What it does |
|------|--------------|
| `paperless.storage_paths.list` | List all storage paths |
| `paperless.storage_paths.get` | Get a storage path by ID |
| `paperless.storage_paths.create` | Create with path template |
| `paperless.storage_paths.update` | Update a storage path |
| `paperless.storage_paths.delete` | Delete a storage path |
| `paperless.storage_paths.bulk_delete` | Delete multiple storage paths |

</details>

<details>
<summary><strong>Custom Fields</strong> — your own metadata</summary>

| Tool | What it does |
|------|--------------|
| `paperless.custom_fields.list` | List all custom field definitions |
| `paperless.custom_fields.get` | Get a custom field by ID |
| `paperless.custom_fields.create` | Create a field (string, date, number, monetary, etc.) |
| `paperless.custom_fields.update` | Update a field definition |
| `paperless.custom_fields.delete` | Delete a field |
| `paperless.custom_fields.assign` | Assign a field value to a document |

</details>

<details>
<summary><strong>Health</strong> — is it alive?</summary>

| Tool | What it does |
|------|--------------|
| `paperless.ping` | Check connectivity and auth |
| `paperless.capabilities` | List supported features |

</details>

---

## Configuration

Environment variables. That's it. No config files to manage.

| Variable | Required | Default | Description |
|----------|:--------:|---------|-------------|
| `PAPERLESS_BASE_URL` | Yes | — | Your Paperless-ngx URL |
| `PAPERLESS_API_TOKEN` | Yes | — | API token for authentication |
| `MCP_PORT` | | `5000` | Port for Streamable HTTP mode |
| `MCP_RELAX_ACCEPT_HEADER` | | `false` | Normalize `/mcp` POST `Accept` headers for clients that cannot send both Streamable HTTP media types |
| `MAX_PAGE_SIZE` | | `100` | Max items per paginated request |
| `HTTP_TIMEOUT_SECONDS` | | `30` | Timeout for requests to Paperless-ngx. Raise it if large full-text searches time out |

Aliases supported: `PAPERLESS_URL` and `PAPERLESS_TOKEN` also work if that's your style.

### LocalAI Compatibility

Streamable HTTP clients are expected to send `Accept: application/json, text/event-stream` on `/mcp` POST requests. Some clients cannot configure that header. Set `MCP_RELAX_ACCEPT_HEADER=true` to have PaperlessMCP normalize missing or incomplete `Accept` headers before the MCP SDK handles the request.

---

## Support the Project

If PaperlessMCP saves you time, consider supporting development:

[![GitHub Sponsors](https://img.shields.io/github/sponsors/barryw?style=for-the-badge&logo=github&label=Sponsor)](https://github.com/sponsors/barryw)
[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support-ff5e5b?style=for-the-badge&logo=ko-fi)](https://ko-fi.com/barryw)

Every bit helps keep the lights on and the commits flowing.

---

## Contributing

Yes please. We use trunk-based development with conventional commits.

```bash
git clone https://github.com/barryw/PaperlessMCP.git
cd PaperlessMCP
dotnet build
dotnet test
```

**The rules:**
- Conventional commits (`feat:`, `fix:`, `docs:`, etc.) — versions bump automatically
- Tests pass or it doesn't merge
- Destructive operations need `confirm=true` and dry-run by default

See [CONTRIBUTING.md](CONTRIBUTING.md) for the full rundown.

---

## License

[MIT](LICENSE) — do whatever you want, just don't blame me.

---

## Acknowledgments

- [Paperless-ngx](https://github.com/paperless-ngx/paperless-ngx) — the document system that makes this worth building
- [Model Context Protocol](https://modelcontextprotocol.io/) — the glue between AI and everything else
- Everyone who's ever felt guilty about their untagged documents
