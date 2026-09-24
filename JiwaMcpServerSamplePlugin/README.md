# JiwaMcpServerSamplePlugin

A minimal sample plugin project for `JiwaMcpServer`.

## What it provides

- Tool class: `SamplePluginTools`
- Tool method: `GetSamplePluginInfo`
- Tool-selection hook: `SampleToolSelectionOverride`

The sample override promotes `GetSamplePluginInfo` whenever the prompt mentions `sample plugin` or `plugin diagnostic`. That gives plugins a safe way to step in and steer a bad prompt toward a known recovery tool.

## Build

```powershell
dotnet build JiwaMcpServerSamplePlugin\JiwaMcpServerSamplePlugin.csproj
```

## Deploy to JiwaMcpServer

1. Copy `JiwaMcpServerSamplePlugin.dll` to the server plugin folder (default: `Plugins`).
2. Ensure any dependency DLLs are copied with it.
3. Restart JiwaMcpServer.

After restart, the MCP tool `GetSamplePluginInfo` should be available, and the routing endpoints will automatically load `SampleToolSelectionOverride` from the same plugin assembly.
