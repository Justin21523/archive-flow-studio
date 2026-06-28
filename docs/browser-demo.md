# ArchiveFlow Studio Browser Demo

## Purpose

The Browser Demo lets reviewers open ArchiveFlow Studio from GitHub Pages without installing a desktop app. It opens directly into a workspace-style node canvas so reviewers can see the product's core interaction first. It is an online product tour, not a replacement for the full desktop version.

Live Demo URL:

```text
https://justin21523.github.io/archive-flow-studio/
```

## Desktop Full Version vs Browser Demo Version

Desktop Full Version:

- Runs as an Avalonia desktop app on Windows, Linux, and macOS.
- Uses local SQLite storage.
- Scans local folders through native file/folder picker APIs.
- Writes exports to local folders.
- Opens and reveals local files through OS shell integration.

Browser Demo Version:

- Runs as a static WebAssembly app published from `ArchiveFlow.Browser`.
- Uses in-memory demo storage.
- Ships with built-in sample archive records.
- Opens with a browser-safe node canvas, contextual inspector, result table, pending changes, and export preview.
- Simulates import scanning with deterministic sample files.
- Generates export content in memory and records export jobs.
- Does not read arbitrary local paths, write local files, or use native SQLite.

## Supported Demo Features

- Reset demo data.
- Load demo scenarios.
- Review the default workspace workflow: `All Files -> Extension Filter -> Keyword Search -> Add Tag Preview -> Result Table`.
- Drag nodes, middle-drag to pan, and use the mouse wheel to zoom the canvas.
- Select nodes and inspect node-specific purpose, parameters, and current output.
- Browse built-in files.
- Search filename, path, metadata, and preview text.
- Filter by extension.
- Execute the demo workflow and preview pending metadata changes.
- Apply pending metadata changes to in-memory demo records.
- Preview and confirm a mock import.
- Create source-to-target file relationships.
- View relationship records.
- Export filtered results as CSV, JSON, or Dublin Core XML content.
- View import and export job logs.

## Simulated Features

- Folder import is simulated and does not access the user's local file system.
- Export is generated in memory for review instead of writing to a selected local folder.
- Storage resets when the page reloads.
- Metadata and relationships are demo records only.

## Known Limitations

- GitHub Pages is a static host. It cannot run server-side .NET or a desktop executable.
- Browser sandbox rules block unrestricted local folder scanning and direct local export paths.
- The demo does not connect to the desktop SQLite database.
- The demo uses a browser-safe workspace canvas, not the full desktop `NodeCanvasViewModel` runtime or OS shell integrations.
- Large production archives should be reviewed in the Desktop Full Version.

## Local Browser Commands

Install the WebAssembly workload once:

```bash
dotnet workload install wasm-tools
```

Run locally:

```bash
dotnet run --project src/ArchiveFlow.Browser/ArchiveFlow.Browser.csproj
```

Publish static files:

```bash
dotnet publish src/ArchiveFlow.Browser/ArchiveFlow.Browser.csproj -c Release
```

Deployable output:

```text
src/ArchiveFlow.Browser/bin/Release/net10.0-browser/publish/wwwroot
```

## GitHub Pages Deployment

The `deploy-pages.yml` workflow publishes only the Browser project and uploads the published `wwwroot` directory.

For this repository project page, the workflow rewrites:

```html
<base href="/" />
```

to:

```html
<base href="/archive-flow-studio/" />
```

This prevents `_framework`, `.wasm`, `main.js`, and CSS assets from resolving at the wrong URL.

## FAQ

### Can the Browser Demo scan my local folders?

No. The browser version uses mock import data because GitHub Pages cannot grant unrestricted local file-system access.

### Does the Browser Demo persist data?

No. It uses in-memory demo storage. Reloading the page resets runtime changes.

### Can the Browser Demo replace the desktop app?

No. It is designed for portfolio review and quick evaluation. Use the Desktop Full Version for local archive workflows.
