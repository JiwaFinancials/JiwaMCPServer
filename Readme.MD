# Jiwa MCP Server

An [MCP (Model Context Protocol)](https://modelcontextprotocol.io/) server that exposes [Jiwa Financials](https://jiwa.com.au) business data to AI agents and MCP-compatible clients. Built with .NET 10 and ASP.NET Core, it uses the **streamable HTTP** transport.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Available Tools](#available-tools)
- [Large Result Set Confirmation](#large-result-set-confirmation)
- [Warehouse Context Synchronization](#warehouse-context-synchronization)
- [Configuration](#configuration)
  - [Per-Request API Key](#per-request-api-key)
- [Building and Running](#building-and-running)
- [Testing](#testing)
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
│   ├── FormTools.cs
│   ├── ProductTools.cs
│   ├── SalesOrderTools.cs
│   ├── PurchaseOrderTools.cs
│   ├── SupplierTools.cs
│   ├── WarehouseTools.cs
│   └── SchemaTools.cs
├── Services/
│   └── JiwaApiClient.cs
└── JiwaMcpServer.Tests/
    └── JiwaMcpServer.Tests.csproj
```

---

## Available Tools

### CustomerTools
| Tool | Description |
|---|---|
| `SearchCustomers` | Search for customers (debtors/accounts) by field. |
| `GetCustomer` | Get full details for a customer. |
| `GetCustomerTransactions` | Retrieve outstanding customer transactions. |
| `SearchCustomerClassifications` | Retrieve available customer/debtor classifications. |
| `SearchCustomerCategories` | Retrieve available customer/debtor categories. |

### ProductTools
| Tool | Description |
|---|---|
| `SearchProducts` | Search for products (inventory items) by field. |
| `GetProduct` | Get full details for a product. |
| `GetStockOnHand` | Get stock-on-hand quantities for products. |
| `SearchProductClassifications` | Retrieve available product classifications. |
| `SearchProductCategories` | Retrieve available product categories. |
| `GetProductPicture` | Retrieve a product image by `InventoryID` or `PartNo`. |

### FormTools
| Tool | Description |
|---|---|
| `SearchForms` | Search forms and plugins by field. |
| `OpenAForm` | Open a Jiwa form or plugin, optionally with a specific record loaded via `DrillDownID`. |

### SalesOrderTools
| Tool | Description |
|---|---|
| `GetSalesOrder` | Get full details for a sales order (sales invoice). |
| `CreateSalesOrder` | Create a sales order. |
| `ModifySalesOrder` | Modify an existing sales order. |
| `AddAProductToASalesOrder` | Add a product line to an existing sales order. |
| `SearchSalesInformation` | Search detailed sales information (including sold part numbers). |
| `SearchSalesOrders` | Search sales order headers. |

### PurchaseOrderTools
| Tool | Description |
|---|---|
| `GetPurchaseOrder` | Get full details for a purchase order. |
| `CreatePurchaseOrder` | Create a purchase order. |
| `ModifyPurchaseOrder` | Modify an existing purchase order. |
| `AddAProductToAPurchaseOrder` | Add a product line to an existing purchase order. |
| `SearchPurchaseInformation` | Search current and historical purchase detail, including purchased part numbers; use `GetPurchaseOrder` to retrieve full details for a specific purchase order. |
| `SearchPurchaseOrders` | Search current and historical purchase order header data. |

`SearchPurchaseInformation` and `SearchPurchaseOrders` both support pagination via `skip` and `take`, and may require large-result confirmation before returning the full result set.

### SupplierTools
| Tool | Description |
|---|---|
| `SearchSuppliers` | Search suppliers (creditors) by field. |
| `GetSupplier` | Get full details for a supplier. |
| `SearchCreditorClassifications` | Retrieve available supplier/creditor classifications. |

### WarehouseTools
| Tool | Description |
|---|---|
| `SearchWarehouses` | Search logical warehouses by field. |
| `GetCurrentLogicalWarehouse` | Get the current logical warehouse for the session. |
| `ChangeCurrentLogicalWarehouse` | Change the current logical warehouse for the session. |

### SchemaTools
| Tool | Description |
|---|---|
| `GetDtoSchema` | Return the DTO schema for any Jiwa DTO type by name. |

---

## Large Result Set Confirmation

Most search/list tools support safe handling of large datasets.

When a query exceeds the warning threshold, call flow is:

1. Call tool with `confirmLargeResultSet=false` (default).
2. If result is large, the tool returns a warning and a short-lived `confirmationToken`.
3. Call the same tool again with `confirmLargeResultSet=true` and that token.

This avoids unintentionally returning very large payloads in a single tool call.

This is especially relevant for `SearchPurchaseInformation` and `SearchPurchaseOrders`, which can span current and historical purchasing data.

---

## Warehouse Context Synchronization

The Jiwa system maintains warehouse context at **three** distinct levels:

1. **API Server Context** (Jiwa REST API): Warehouse state for API requests
2. **MCP Server Context** (JiwaMcpServer): Stateless per-request API calls
3. **Jiwa Client Context** (AI Chat): `Manager.CurrentLogicalWarehouse` in the Jiwa client application

### The Synchronization Challenge

When you use `ChangeCurrentLogicalWarehouse`:

| Step | What Happens | State |
|------|--------------|-------|
| 1. Call tool | MCP Server updates Jiwa API | API warehouse ✓ |
| 2. Tool returns | Response includes new warehouse ID | Response data ready |
| 3. **REQUIRED ACTION** | **AI Chat must update** `Manager.CurrentLogicalWarehouse` | AI Chat warehouse needs update |
| 4. Call GetCurrentLogicalWarehouse | Verify and get full details | Confirmation |

**The Critical Issue:** The MCP Server cannot directly update `Manager.CurrentLogicalWarehouse` because it's in a different process (the Jiwa client application). The AI Chat must be instructed to do this.

### How ChangeCurrentLogicalWarehouse Response Works

The tool returns a structured response with the new warehouse context:

```json
{
  "success": true,
  "newWarehouseId": "WAREHOUSE_ID_12345",
  "newWarehouseDetails": { /* warehouse data */ }
}
```

### Required Workflow for AI Chat

1. **Call** `ChangeCurrentLogicalWarehouse(newWarehouseId)` → **API updated** ✓
2. **Receive response** with `newWarehouseId`
3. **Update Jiwa Client** `Manager.CurrentLogicalWarehouse = newWarehouseId` ← **Critical Step**
4. **Call** `GetCurrentLogicalWarehouse` → **Verify and get details** ✓
5. **Inform user** "Warehouse changed to [warehouse name]"

### Why Both Updates Are Necessary

- **API Context Update**: Ensures future API calls use the correct warehouse
- **Manager Update**: Ensures the Jiwa AI Chat client displays and uses the correct warehouse

If either is missed, the contexts diverge and operations use the wrong warehouse.

---

## Configuration

Configuration is stored in `appsettings.json` in the application directory.

```json
{
  "JiwaAPIURL": "https://your-jiwa-instance.example.com",
  "JiwaAPIKey": "",
  "PageSize": 100
}
```

| Setting | Description |
|---|---|
| `JiwaAPIURL` | The base URL of your Jiwa Financials REST API |
| `JiwaAPIKey` | *(Optional)* A server-side fallback Jiwa API key. When set, it is used for any request that does not supply its own key via the `X-Jiwa-API-Key` header. Can be left blank if all clients supply their own key. |
| `PageSize` | *(Optional)* Default page size used by paginated search/list tools. Default is `100`. |

> **Note:** `JiwaAPIURL` is required. `JiwaAPIKey` and `PageSize` are optional — see [Per-Request API Key](#per-request-api-key) below.

### Per-Request API Key

In addition to (or instead of) a server-side `JiwaAPIKey` in `appsettings.json`, clients can supply a Jiwa API key on a per-request basis using the `X-Jiwa-API-Key` HTTP header:

```
X-Jiwa-API-Key: <your-jiwa-api-key>
```

This allows each MCP client (or each user) to authenticate to Jiwa with their own API key, without sharing a single server-level credential. The priority is:

1. **`X-Jiwa-API-Key` header** — used if present and non-empty.
2. **`JiwaAPIKey` in `appsettings.json`** — used as a fallback if no header is supplied.

If neither is set, calls to the Jiwa API will be made without a bearer token and will fail unless the Jiwa endpoint allows anonymous access.

### Changing the Port

By default the server listens on `http://localhost:5000`. To change this, set the `ASPNETCORE_URLS` environment variable or add a `urls` entry to `appsettings.json`:

```json
{
  "JiwaAPIURL": "https://your-jiwa-instance.example.com",
  "JiwaAPIKey": "your-api-key-here",
  "PageSize": 100,
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

# Edit appsettings.json with your Jiwa API URL and key
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

## Testing

A dedicated test project is included at `JiwaMcpServer.Tests/` with coverage for tool behavior, pagination, and error handling.

Run all tests:

```powershell
dotnet test JiwaMcpServer.Tests/JiwaMcpServer.Tests.csproj
```

---

## Using with VS Code and GitHub Copilot

This section walks through configuring VS Code and GitHub Copilot to use the Jiwa MCP Server as a tool source in Copilot Chat (Agent mode).

### Prerequisites

- [Visual Studio Code](https://code.visualstudio.com/) with the [GitHub Copilot](https://marketplace.visualstudio.com/items?itemName=GitHub.copilot) extension (v1.99 or later)
- A GitHub Copilot subscription (Individual, Business, or Enterprise)
- The Jiwa MCP Server running locally or on a reachable host (see [Building and Running](#building-and-running))
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
        "X-Jiwa-API-Key": "<your-jiwa-api-key>"
      }
    }
  }
}
```

> ⚠️ This file will contain your Jiwa API key. Add it to `.gitignore` to avoid accidentally committing it:
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
        "X-Jiwa-API-Key": "<your-jiwa-api-key>"
      }
    }
  }
}
```

> **Tip:** Replace `<your-jiwa-api-key>` with your personal Jiwa API key.

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
| Jiwa API returns 401 / Unauthorized |
| Copilot asks for confirmation before every tool call | This is expected behaviour in Agent mode. You can approve individual calls or select **Always Allow** for trusted tools. |
| Server returns 500 errors | Check that `JiwaAPIURL` in `appsettings.json` is correct, that a Jiwa API key is being supplied (see above), and that your Jiwa instance is reachable from the machine running the MCP server. |

---

## Connecting an MCP Client

The server uses the **streamable HTTP** transport. The MCP endpoint is:

```
http://<host>:<port>/mcp
```

Requests may optionally include the `X-Jiwa-API-Key` header with a Jiwa API key:

```
X-Jiwa-API-Key: <your-jiwa-api-key>
```

The `X-Jiwa-API-Key` header is required if no server-side fallback key is configured in `appsettings.json`. See [Per-Request API Key](#per-request-api-key) for details.

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
        "X-Jiwa-API-Key": "<your-jiwa-api-key>"
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
        "X-Jiwa-API-Key": "<your-jiwa-api-key>"
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

---

## Deploying to a Server

### Windows (as a Windows Service)

1. Publish the application:
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained true -o C:\Services\JiwaMcpServer
   ```

2. Copy and edit `appsettings.json` in the publish folder with your settings.

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

2. Edit `/opt/jiwamcpserver/appsettings.json` with your settings.

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