# Jiwa MCP Server

An [MCP (Model Context Protocol)](https://modelcontextprotocol.io/) server that exposes [Jiwa Financials](https://jiwa.com.au) business data to AI agents and MCP-compatible clients. Built with .NET 10 and ASP.NET Core, it uses the **streamable HTTP** transport.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Available Tools](#available-tools)
- [Configuration](#configuration)
- [Building and Running](#building-and-running)
- [Using with VS Code and GitHub Copilot](#using-with-vs-code-and-github-copilot)
- [Connecting an MCP Client](#connecting-an-mcp-client)
- [Deploying to a Server](#deploying-to-a-server)

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Access to a running Jiwa Financials API instance
- A valid Jiwa API key

---

## Project Structure

```
JiwaMcpServer/
├── JiwaMcpServer.csproj
├── Program.cs
├── Config.cs
├── appsettings.json
├── Tools/
│   ├── JiwaToolBase.cs
│   ├── CustomerTools.cs
│   ├── ProductTools.cs
│   ├── SalesOrderTools.cs
│   └── SchemaTools.cs
└── Services/
    └── JiwaApiClient.cs
```

---

## Available Tools

### CustomerTools
| Tool | Description |
|---|---|
| `SearchCustomers` | Search for customers (debtors/accounts) by field |
| `GetCustomer` | Get full details for a customer |
| `GetCustomerTransactions` | Retrieve outstanding transactions (invoices/credits) for a customer |

### ProductTools
| Tool | Description |
|---|---|
| `SearchProducts` | Search for products (inventory items) by field |
| `GetProduct` | Get full details for a product |
| `GetStockOnHand` | Get stock on hand quantities for a product |

### SalesOrderTools
| Tool | Description |
|---|---|
| `SearchSalesOrders` | Search for sales orders (invoices) by field |
| `GetSalesOrder` | Get full details for a sales order |
| `CreateSalesOrder` | Create a new sales order for a customer |
| `ModifySalesOrder` | Modify an existing sales order |
| `AddAProductToASalesOrder` | Add a product line to an existing sales order |

### SchemaTools
| Tool | Description |
|---|---|
| `GetDtoSchema` | Returns the DTO schema for any Jiwa DTO type by name (useful for discovering available fields before calling other tools) |

---

## Configuration

Configuration is stored in `appsettings.json` in the application directory.

```json
{
  "JiwaAPIURL": "https://your-jiwa-instance.example.com",
  "JiwaAPIKey": "your-api-key-here",
  "BearerTokens": [
    "replace-with-generated-token-for-user1",
    "replace-with-generated-token-for-user2"
  ]
}
```

| Setting | Description |
|---|---|
| `JiwaAPIURL` | The base URL of your Jiwa Financials REST API |
| `JiwaAPIKey` | A valid Jiwa API key. The associated user requires permissions for the data you intend to expose. |
| `BearerTokens` | A JSON array of bearer tokens. Every MCP client request must supply one of these via the `Authorization: Bearer <token>` header. |

> **Note:** All settings are required. The server will throw an error on startup if any are missing or blank.

### Bearer Token Authentication

Every request to the MCP server must include a valid bearer token in the `Authorization` header:

```
Authorization: Bearer <token>
```

Each user or client should have their own unique token. This allows you to revoke access for a single user by removing their token from the array and restarting the server, without affecting anyone else.

#### Generating a Token

Run the following in PowerShell to generate a cryptographically strong random token:

```powershell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
```

This produces a 256-bit base64-encoded token. Generate one per user and add it to the `BearerTokens` array in `appsettings.json`.

#### Revoking a Token

Remove the user's token from the `BearerTokens` array in `appsettings.json` and restart the server. All other users are unaffected.

### Changing the Port

By default the server listens on `http://localhost:5000`. To change this, set the `ASPNETCORE_URLS` environment variable or add a `urls` entry to `appsettings.json`:

```json
{
  "JiwaAPIURL": "https://your-jiwa-instance.example.com",
  "JiwaAPIKey": "your-api-key-here",
  "BearerTokens": [ "your-token-here" ],
  "urls": "http://0.0.0.0:8080"
}
```

---

## Building and Running

### Run from source

```powershell
# Clone the repository
git clone https://github.com/JiwaFinancials/JiwaMcpServer.git
cd JiwaMcpServer

# Edit appsettings.json with your Jiwa API URL, key, and bearer tokens
notepad appsettings.json

# Run
dotnet run
```

The server will start and listen on `http://localhost:5000` by default. The MCP endpoint is available at:

```
http://localhost:5000/mcp
```

### Publish a self-contained executable

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -o ./publish
```

Replace `win-x64` with your target runtime (e.g. `linux-x64`, `osx-x64`). Copy `appsettings.json` alongside the executable and edit it with your settings before running.

---

## Using with VS Code and GitHub Copilot

This section walks through configuring VS Code and GitHub Copilot to use the Jiwa MCP Server as a tool source in Copilot Chat (Agent mode).

### Prerequisites

- [Visual Studio Code](https://code.visualstudio.com/) with the [GitHub Copilot](https://marketplace.visualstudio.com/items?itemName=GitHub.copilot) extension (v1.99 or later)
- A GitHub Copilot subscription (Individual, Business, or Enterprise)
- The Jiwa MCP Server running locally or on a reachable host (see [Building and Running](#building-and-running))
- A bearer token generated for your user (see [Bearer Token Authentication](#bearer-token-authentication))

### Step 1 — Start the MCP Server

Start the server before configuring VS Code so you can verify connectivity later.

```powershell
cd C:\path\to\JiwaMcpServer
dotnet run
```

Confirm it is listening:

```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
```

### Step 2 — Register the Server in VS Code

VS Code looks for MCP server definitions in two places. Choose whichever suits your workflow:

#### Option A — Workspace config (per-project, do not commit if it contains tokens)

Create or edit `.vscode/mcp.json` in your workspace root:

```json
{
  "servers": {
    "Jiwa": {
      "type": "http",
      "url": "http://localhost:5000/mcp",
      "headers": {
        "Authorization": "Bearer <your-token>"
      }
    }
  }
}
```

> ⚠️ This file will contain your bearer token. Add it to `.gitignore` to avoid accidentally committing it:
> ```
> .vscode/mcp.json
> ```

#### Option B — User config (applies to all your VS Code workspaces)

Open the VS Code command palette (`Ctrl+Shift+P`) and run:

```
MCP: Open User Configuration
```

This opens your global `mcp.json`. Add the same entry:

```json
{
  "servers": {
    "Jiwa": {
      "type": "http",
      "url": "http://localhost:5000/mcp",
      "headers": {
        "Authorization": "Bearer <your-token>"
      }
    }
  }
}
```

> **Tip:** Replace `<your-token>` with the token generated for you by the server administrator. If you are connecting to a remote or production server, replace `http://localhost:5000` with your server's base URL, e.g. `https://mcp.yourcompany.com`.

### Step 3 — Enable the Server in Copilot Chat

1. Open the Copilot Chat panel (`Ctrl+Alt+I`).
2. Switch to **Agent mode** by clicking the mode selector at the top of the chat input and choosing **Agent**.
3. Click the **Tools** button (🔧) at the bottom of the chat input to open the tool picker.
4. Locate **jiwa** in the list and ensure it is checked/enabled.

You should see all available Jiwa tools listed (SearchCustomers, GetProduct, CreateSalesOrder, etc.).

### Step 4 — Verify the Connection

In Copilot Chat (Agent mode) type a test prompt such as:

```
List all available Jiwa MCP tools
```

or try a real query:

```
Search for customers with the name "Smith" using the Jiwa tools
```

Copilot will call the appropriate tool and return results from your Jiwa system.

### Step 5 — (Optional) Auto-start the Server

To avoid manually starting the server each time, you can configure a VS Code task that launches it automatically.

Create or edit `.vscode/tasks.json`:

```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "Start Jiwa MCP Server",
      "type": "shell",
      "command": "dotnet run --project ${workspaceFolder}/JiwaMcpServer.csproj",
      "isBackground": true,
      "problemMatcher": {
        "pattern": {
          "regexp": "^$"
        },
        "background": {
          "activeOnStart": true,
          "beginsPattern": "Building\\.\\.\\.",
          "endsPattern": "Now listening on:"
        }
      },
      "presentation": {
        "reveal": "silent",
        "panel": "dedicated"
      }
    }
  ]
}
```

Then reference it as a pre-launch dependency in `.vscode/launch.json` or trigger it manually via **Terminal → Run Task → Start Jiwa MCP Server**.

### Troubleshooting

| Problem | Solution |
|---|---|
| Tools not appearing in Copilot Chat | Ensure the server is running and accessible at the configured URL. Open a browser to `http://localhost:5000/mcp` — you should receive an MCP response, not a connection error. |
| `ERR_CONNECTION_REFUSED` | The server is not running or is listening on a different port. Check the console output and verify `appsettings.json`. |
| `401 Unauthorized` | Your bearer token is missing, incorrect, or has been revoked. Check that the `Authorization` header in your `mcp.json` matches a token in the server's `BearerTokens` array. |
| Copilot asks for confirmation before every tool call | This is expected behaviour in Agent mode. You can approve individual calls or select **Always Allow** for trusted tools. |
| Server returns 500 errors | Check that `JiwaAPIURL` and `JiwaAPIKey` in `appsettings.json` are correct and that your Jiwa instance is reachable from the machine running the MCP server. |

---

## Connecting an MCP Client

The server uses the **streamable HTTP** transport. The MCP endpoint is:

```
http://<host>:<port>/mcp
```

All requests must include the `Authorization` header with a valid bearer token:

```
Authorization: Bearer <your-token>
```

### GitHub Copilot (VS Code)

See the dedicated [Using with VS Code and GitHub Copilot](#using-with-vs-code-and-github-copilot) section above for full step-by-step instructions.

For a quick reference, add the following to `.vscode/mcp.json` in your workspace (or your user-level `mcp.json`):

```json
{
  "servers": {
    "Jiwa": {
      "type": "http",
      "url": "http://localhost:5000/mcp",
      "headers": {
        "Authorization": "Bearer <your-token>"
      }
    }
  }
}
```

### GitHub Copilot (Visual Studio)

Add the following to your `mcp.json` (typically at `%USERPROFILE%\.vs\mcp.json` or the solution's `.vs\mcp.json`):

```json
{
  "servers": {
    "Jiwa": {
      "type": "http",
      "url": "http://localhost:5000/mcp",
      "headers": {
        "Authorization": "Bearer <your-token>"
      }
    }
  }
}
```

### MCP Inspector (for testing)

You can test the server with the [MCP Inspector](https://github.com/modelcontextprotocol/inspector):

```powershell
npx -y @modelcontextprotocol/inspector http://localhost:5000/mcp
```

When prompted, add the `Authorization` header with your bearer token.

---

## Deploying to a Server

### Windows (as a Windows Service)

1. Publish the application:
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained true -o C:\Services\JiwaMcpServer
   ```

2. Copy and edit `appsettings.json` in the publish folder, including the `BearerTokens` array.

3. Register as a Windows Service:
   ```powershell
   sc.exe create JiwaMcpServer binPath="C:\Services\JiwaMcpServer\JiwaMcpServer.exe" start=auto
   sc.exe start JiwaMcpServer
   ```

### Linux (as a systemd service)

1. Publish the application:
   ```bash
   dotnet publish -c Release -r linux-x64 --self-contained true -o /opt/jiwamcpserver
   ```

2. Edit `/opt/jiwamcpserver/appsettings.json` with your settings, including the `BearerTokens` array.

3. Create a systemd unit file at `/etc/systemd/system/jiwamcpserver.service`:
   ```ini
   [Unit]
   Description=Jiwa MCP Server
   After=network.target

   [Service]
   WorkingDirectory=/opt/jiwamcpserver
   ExecStart=/opt/jiwamcpserver/JiwaMcpServer
   Restart=always
   RestartSec=10
   Environment=ASPNETCORE_ENVIRONMENT=Production
   Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

   [Install]
   WantedBy=multi-user.target
   ```

4. Enable and start the service:
   ```bash
   sudo systemctl daemon-reload
   sudo systemctl enable jiwamcpserver
   sudo systemctl start jiwamcpserver
   ```

### Behind a Reverse Proxy (nginx / IIS)

When hosting behind a reverse proxy, configure it to forward requests to the MCP endpoint. Example nginx snippet:

```nginx
location /mcp {
    proxy_pass         http://localhost:5000/mcp;
    proxy_http_version 1.1;
    proxy_set_header   Upgrade $http_upgrade;
    proxy_set_header   Connection keep-alive;
    proxy_set_header   Host $host;
    proxy_cache_bypass $http_upgrade;
    proxy_buffering    off;
}
```

> **Tip:** Disable proxy response buffering (`proxy_buffering off`) to ensure streaming responses are forwarded immediately to the client.