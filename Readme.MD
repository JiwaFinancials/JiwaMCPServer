# Jiwa MCP Server

Jiwa MCP Server is a .NET 10 MCP server for Jiwa Financials. It exposes Jiwa business operations as MCP tools, adds a deterministic tool-routing pipeline for agent scenarios, supports document upload/download and export flows, and can be extended with runtime-loaded plugins.

## Projects

| Project | Purpose |
| --- | --- |
| `JiwaMcpServer` | Main ASP.NET Core MCP server and HTTP endpoints |
| `JiwaMcpServer.Tests` | xUnit test suite for routing, catalog, document, and tool behavior |
| `JiwaMcpServerSamplePlugin` | Example plugin assembly showing custom tools and routing overrides |

## Features

- MCP server hosted over HTTP transport
- Jiwa tool domains for customers, inventory/products, creditor purchases, and documents
- Deterministic routing pipeline for selecting the right tools from user prompts
- Conversation context update endpoint for follow-up routing
- Temporary file upload/download endpoints for document workflows
- Local filesystem access restricted to configured allow-listed roots
- Runtime plugin loading for additional tools and tool-selection overrides

## Requirements

- .NET SDK 10.0
- A reachable Jiwa API endpoint
- A Jiwa API key for authenticated requests
- Windows if you want to run it as a Windows Service

## Configuration

The main configuration lives in `JiwaMcpServer\appsettings.json`.

### Core settings

| Key | Description |
| --- | --- |
| `JiwaAPIURL` | Base URL for the Jiwa API |
| `JiwaAPIKey` | Default Jiwa API key used by the server |
| `PageSize` | Default page size for paged query tools |

### Plugin settings

| Key | Description |
| --- | --- |
| `Plugins:Enabled` | Enables or disables plugin loading |
| `Plugins:Folder` | Folder scanned for plugin `.dll` files |
| `Plugins:Assemblies` | Additional plugin assembly paths to load |

### Local file system settings

| Key | Description |
| --- | --- |
| `LocalFileSystem:AllowedRoots` | Absolute paths that document tools may read/write within |
| `LocalFileSystem:MaxReadBytes` | Maximum bytes returned from one local file read |
| `LocalFileSystem:MaxWriteBytes` | Maximum UTF-8 bytes written in one local file save |

### Document processing settings

| Key | Description |
| --- | --- |
| `DocumentProcessing:MaxUploadSizeMB` | Maximum upload size for temporary file storage |
| `DocumentProcessing:TempFolder` | Folder used for uploaded/generated temporary files |
| `DocumentProcessing:RetentionHours` | Retention window before cleanup removes temp files |

## Running locally

1. Update `JiwaMcpServer\appsettings.json` with your Jiwa API URL and any required local settings.
2. From the repository root, run:

```powershell
dotnet run --project .\JiwaMcpServer\JiwaMcpServer.csproj
```

The server exposes its MCP endpoint at `/mcp`.

## HTTP endpoints

| Endpoint | Method | Purpose |
| --- | --- | --- |
| `/mcp` | MCP over HTTP | Main MCP transport endpoint |
| `/agent/tools/route` | `POST` | Returns a shortlisted/ranked tool route for a prompt |
| `/mcp/route` | `POST` | Alias for tool routing |
| `/agent/tools/context` | `POST` | Updates routing context from tool execution results |
| `/mcp/context` | `POST` | Alias for context updates |
| `/files/upload` | `POST` | Uploads a temporary file and returns a `FileId` |
| `/files/{fileId}` | `GET` | Downloads a previously stored temporary file |

## Request notes

- The server accepts an `X-Jiwa-API-Key` request header and uses it for the current request.
- CORS is configured with an open policy by default.
- Uploaded files can be sent either as `multipart/form-data` or as a raw body with an `X-File-Name` header.

## Tool routing

The routing pipeline is registered through `AddJiwaAgentPipeline(...)` and includes:

- tool discovery and catalog construction
- intent extraction
- domain resolution
- context-aware shortlist generation
- schema resolution for selected tools
- optional selection overrides discovered from loaded assemblies

This is what powers endpoints such as `/agent/tools/route` and follow-up context handling via `/agent/tools/context`.

## Plugins

Plugins can contribute additional MCP tools and `IToolSelectionOverride` implementations. The sample plugin project demonstrates both:

- `SamplePluginTools` adds a diagnostic MCP tool
- `SampleToolSelectionOverride` shows how a plugin can influence shortlist ordering

To use a plugin, build it and place the resulting `.dll` in the configured plugin folder or list it under `Plugins:Assemblies`.

## Testing

Run the test suite from the repository root:

```powershell
dotnet test .\JiwaMcpServer.Tests\JiwaMcpServer.Tests.csproj
```

## Notes

- The main application is configured with `UseWindowsService()`, but it can still be run directly during development.
- If `LocalFileSystem:AllowedRoots` is empty, local file access features are disabled.
