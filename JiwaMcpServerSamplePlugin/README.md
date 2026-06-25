# JiwaMcpServerSamplePlugin

A minimal sample plugin project for `JiwaMcpServer`.

## What it provides

- Tool class: `SamplePluginTools`
- Tool method: `GetSamplePluginInfo`

## Build

```powershell
dotnet build JiwaMcpServerSamplePlugin/JiwaMcpServerSamplePlugin.csproj
```

## Deploy to JiwaMcpServer

1. Copy `JiwaMcpServerSamplePlugin.dll` to the server plugin folder (default: `Plugins`).
2. Ensure any dependency DLLs are copied with it.
3. Restart JiwaMcpServer.

After restart, the MCP tool `GetSamplePluginInfo` should be available.
