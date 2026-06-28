# ArchiveFlow Studio

Node-based personal digital archive and metadata workflow prototype.

ArchiveFlow Studio is a learning-focused desktop application for exploring file review, metadata editing, and visual archive workflows. It is built with C#/.NET 10, Avalonia UI 12, SQLite, Dapper, FluentMigrator, and CommunityToolkit.Mvvm.

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20macOS%20%7C%20Linux-lightgrey.svg)
![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)

## Live Demo

- Browser Workspace Demo: https://justin21523.github.io/archive-flow-studio/
- Browser Demo notes: [docs/browser-demo.md](docs/browser-demo.md)
- Automation notes: [docs/automation.md](docs/automation.md)

The Browser Workspace Demo is an Avalonia WebAssembly static site for online portfolio review. It opens directly into a workspace-style node canvas with a node library, inspector, result table, pending changes, and mock export preview. It uses built-in sample data and simulated import/export behavior because GitHub Pages cannot run server-side .NET, native SQLite, or desktop file-system APIs.

The Desktop Full Version remains the primary application for local archive workflows.

## Current Status

This repository is in prototype/MVP development. The architecture and core demo workflow exist, but several larger product features are experimental or planned rather than complete.

### Implemented

- Clean Architecture solution layout with Domain, Application, Infrastructure, App, and test projects.
- Avalonia desktop shell with sidebar navigation, a Tool Center hub, Workspace, Library, Import, Export Center, Graph, and Metadata surfaces.
- SQLite database initialization through FluentMigrator.
- File and metadata repositories backed by SQLite and Dapper.
- Dynamic metadata editor window with field creation, field updates, metadata value editing, and completeness calculation.
- Node canvas prototype with node library, draggable nodes, Bezier connections, contextual inspector, result table, selected-file metadata preview, and pending changes.
- Workflow preview for common source, filter, search, sorting, limit, output passthrough, and selected metadata action nodes.
- Preview/apply flow for supported metadata changes such as tags, subject, project, reading status, importance, and file status.
- Workspace export apply for CSV, JSON, and Dublin Core XML, writing local files under `Data/Exports/` without overwriting existing exports.
- Reference-only Import Pipeline v1 with folder selection, recursive scan preview, new/duplicate/existing classification, and confirm import.
- Archive Library v2 with file browsing, search, extension/status filters, saved filters, checkbox selection, selected/visible export, metadata editor access, open, reveal, and copy path actions.
- Export job log table and Export Center tab for recent CSV/JSON/Dublin Core export attempts.
- Import job history table and Import tab history for scan/apply summaries.
- Export Center v1 with manual all-files export plus export job history; Library selected/visible export writes to the same job log.
- Browser Workspace Demo host using Avalonia WebAssembly, in-memory repositories, built-in sample data, a node canvas demo, contextual inspector, result table, pending metadata changes, mock import, mock export, and GitHub Pages deployment workflow.
- SQLite FTS5 index table, rebuild flow, query modes, Library FTS search mode, and Workspace full-text search node handler.
- Text extraction for text files, PDF text layers, and DOCX documents, wired into import and FTS rebuild.
- Relationship table, duplicate-safe repository operations, multi-hop Graph Explorer tab, relationship type filters, manual relationship creation, visible relationship list, edit, and two-step delete flow.
- Local JSON workflow library with save, load, sample workflows, overwrite confirmation, and delete confirmation.
- Explicit workspace node capability audit for implemented, preview-only, and planned nodes.
- Runtime guard for implemented workspace nodes so they cannot silently pass through without a handler.
- Mock archive seeder that generates local demo records and runtime mock files.

### Partial or Experimental

- Workflow execution supports a visual DAG, but multi-port UI and advanced set routing are still being productized.
- Export is available from Workspace output nodes, Library selected/visible rows, and Export Center; richer export templates are still planned.
- Full-text search indexes filename, path, and extracted text for supported text/PDF/DOCX files. OCR is still planned.
- Plugin interfaces, loader code, and a sample plugin exist, but plugin loading is not wired into the app startup path and the sample plugin is not part of the solution build baseline.
- Graph Explorer v2 supports manual relationship browsing and editing. Automatic relationship suggestions are still planned.
- Background job, preview, workflow storage, smart collection DSL, and database manager services exist as early scaffolding and are not complete end-to-end product surfaces.

### Planned

- Import rules configuration
- Archive Library saved filter rule DSL and richer bulk actions
- Export Center with richer configuration templates
- Automatic relationship suggestions
- OCR, PDF metadata extraction, and richer FTS result ranking
- Settings, backup/restore, release packaging, and installer workflow

## Screenshot

![ArchiveFlow Studio Main Canvas](data/screenshot.png)

## Technology Stack

- .NET 10
- Avalonia UI 12
- SQLite
- Dapper
- FluentMigrator
- CommunityToolkit.Mvvm
- xUnit

## Project Structure

```text
src/
  ArchiveFlow.Domain
  ArchiveFlow.Application
  ArchiveFlow.Infrastructure
  ArchiveFlow.App
  ArchiveFlow.Browser

tests/
  ArchiveFlow.Domain.Tests
  ArchiveFlow.Application.Tests
  ArchiveFlow.Infrastructure.Tests

plugins/
  ArchiveFlow.SamplePlugin
```

Runtime data is generated under `Data/` and should not be committed, except for checked-in documentation assets such as `data/screenshot.png`.

## Prerequisites

- .NET 10 SDK
- `wasm-tools` workload for the Browser Demo

Verify the installed SDK:

```bash
dotnet --info
```

Install the WebAssembly workload:

```bash
dotnet workload install wasm-tools
```

## Build and Test

```bash
dotnet restore ArchiveFlow.sln
dotnet build ArchiveFlow.sln
dotnet test ArchiveFlow.sln
```

Run the full automated local check:

```bash
./scripts/browser-demo-check.sh
```

Run the desktop app:

```bash
./scripts/run-desktop.sh
```

Run the browser demo locally:

```bash
./scripts/run-browser.sh
```

Publish the browser demo:

```bash
./scripts/publish-browser.sh
```

Deployable static output:

```text
src/ArchiveFlow.Browser/bin/Release/net10.0-browser/publish/wwwroot
```

## Quick Start

1. Launch the app.
2. Open the Workspace tab.
3. Click `Reset + Generate Mock Data`.
4. Execute the starter workflow `All Files -> Result Table`.
5. Add filters or metadata action nodes, execute preview, then apply pending changes when ready.

## Known Limitations

- Some nodes are listed for roadmap visibility but do not yet have real handlers.
- Browser Demo storage is in-memory and resets on reload.
- Browser Demo import/export is simulated and does not scan or write arbitrary local paths.
- GitHub Pages deploys only the Avalonia WebAssembly `wwwroot`; it does not run the desktop app or server-side .NET.
- Physical file actions are intentionally blocked from apply in the current prototype.
- Import is reference-only and does not copy files into a managed archive folder.
- Export nodes write local files only after the pending-change confirmation flow; empty workflow results are blocked.
- Workflow library is local JSON storage under `Data/Workflows/`; workflow package import/export and cloud sync are still planned.
- FTS extraction is text-layer only for PDFs; scanned documents still require future OCR support.
- Tests now cover domain entities, workflow set operations, import/export services, SQLite repositories, FTS, relationships, and saved filters.
- NuGet currently reports a transitive `SQLitePCLRaw.lib.e_sqlite3` advisory through SQLite dependencies.

## Development Notes

- The app resolves the local database path by walking up to `ArchiveFlow.sln` and then using `Data/archiveflow.db`.
- GitHub Pages deployment is handled by `.github/workflows/deploy-pages.yml`.
- The project page URL base href is prepared by `scripts/prepare-pages-artifact.sh` so `_framework` and `.wasm` assets resolve correctly.
- GitHub Pages can be switched to GitHub Actions deployment mode with `./scripts/setup-github-pages.sh` after `gh auth login`.
- `Data/mock-files/` is generated demo data and should remain ignored.
- The solution build does not currently include the sample plugin project.

## Browser Demo Script

1. Open the Browser Workspace Demo.
2. Review the default `All Files -> Extension Filter -> Keyword Search -> Add Tag Preview -> Result Table` workflow.
3. Drag a node, middle-drag to pan, or use the mouse wheel to zoom the canvas.
4. Select different nodes and confirm the inspector changes with node-specific purpose, parameters, and output.
5. Search or filter the built-in files, then run the workflow.
6. Apply pending metadata changes or export the filtered result and inspect the generated preview.

## License

This project is licensed under the MIT License.

<!-- portfolio-release-notes:start -->
## Portfolio Release Notes

## Overview
ArchiveFlow Studio is a cross-platform desktop prototype built with C#, .NET 10, Avalonia, SQLite, Dapper, and FluentMigrator. Its strongest implemented surface is the Workspace node canvas for previewing file and metadata workflows. Plugin, FTS, graph, export center, and background-job capabilities exist only as partial scaffolding or roadmap work unless noted in the main README status table.

## Demo
- Live Demo: /projects/ArchiveFlow#demo-guide
- Portfolio Case Study: /projects/ArchiveFlow
- GitHub Repository: https://github.com/Justin21523/archive-flow-studio
- Demo Video: /projects/ArchiveFlow#demo-video
- README: https://github.com/Justin21523/archive-flow-studio#readme

## Features
- Builds a custom node workflow canvas with drag, zoom, selection, and Bezier connections.
- Uses an EAV metadata model for flexible local metadata fields.
- Uses SQLite, Dapper, and FluentMigrator for local persistence.
- Includes mock data generation for repeatable desktop demos.

## Tech Stack
- C#
- .NET 10
- Avalonia 12
- SQLite
- Dapper
- FluentMigrator
- MVVM
- Clean Architecture

## Architecture
The app is organized into Domain, Application, Infrastructure, and Avalonia App projects. Several future-facing services are present in source, but the portfolio narrative should treat them as experimental until they are wired into app startup, UI, migrations, and tests.

## Project Structure
```text
ArchiveFlow/
  README.md
  src/
  tests/
  plugins/
  Data/                 # local runtime data, ignored except deliberate assets
```

## Getting Started
- Install: .NET 10 SDK
- Restore: `dotnet restore ArchiveFlow.sln`
- Build: `dotnet build ArchiveFlow.sln`
- Test: `dotnet test ArchiveFlow.sln`
- Run: `dotnet run --project src/ArchiveFlow.App`

## Screenshots
Screenshots are packaged in the portfolio under `public/projects/ArchiveFlow/screenshots/`. Replace placeholders with real captures when available.

## Demo Script
See `docs/demo-scripts/ArchiveFlow.md` in the portfolio release pack.

## Key Implementation Details
- Implemented technical signals: C#, .NET 10, Avalonia, SQLite, Dapper, FluentMigrator, MVVM, Clean Architecture.
- Main demo path: import or generate data, browse Library, preview a node workflow, inspect results, edit/apply metadata, export, then review Export Log.
- Experimental signals should be presented as roadmap or scaffolding, not shipped functionality.

## Challenges & Decisions
- The canvas coordinates interaction state, data flow, parameters, and visual connections.
- Archive metadata is flexible by nature, so the schema must stay adaptable while remaining queryable.
- Potentially destructive actions use preview/apply semantics so users can inspect changes before writing.

## Future Improvements
- Complete Export Center templates, relationship editing, content extraction/OCR, import rules, and workflow save/load polish.
- Add architecture diagrams, data-flow notes, and key technical decisions.
- Expand regression tests and CI coverage.
<!-- portfolio-release-notes:end -->

<!-- portfolio-quality-notes:start -->
## Portfolio Quality Notes

This section records the latest portfolio packaging check. It is intentionally factual: incomplete deployment, video, or build work is listed as follow-up instead of being presented as finished.

### Verification Status
- Build: not applicable
- Run: no verified run command detected
- Test: no test script detected
- Lint: no lint script detected
- Screenshots: real screenshots present
- Demo Video: real video present

### Portfolio Follow-up
- Deploy an external live demo if this project should be showcased as runnable.
<!-- portfolio-quality-notes:end -->

<!-- portfolio-readme:begin -->

## Portfolio Documentation

### Project Overview

**ArchiveFlow Studio** is maintained as part of the Justin21523 GitHub portfolio. TODO: Replace this placeholder with a concise project description verified against the implementation.

### Features

- TODO: Document the primary user-facing capabilities after source review.

### Tech Stack

- C#
- C#/.NET

### Installation

```bash
dotnet restore
dotnet build
```

### Usage

- Run `dotnet restore`
- Run `dotnet build`

### Project Structure

```text
archive-flow-studio/
  .github/
  .gitignore
  ArchiveFlow.sln
  Data/
  README.md
  data/
  plugins/
  src/
  tests/
```

### Environment Variables

- No required environment variables were detected. TODO: Confirm whether runtime secrets or API keys are needed.

### Deployment

- Demo / GitHub Pages: https://justin21523.github.io/archive-flow-studio/


### Demo

- Live demo: https://justin21523.github.io/archive-flow-studio/
- Source: https://github.com/Justin21523/archive-flow-studio

### Screenshots

- TODO: Add screenshots or describe the current visual/output evidence.

### License

- TODO: Add or confirm the repository license.

### Maintainer

- Justin21523 - https://github.com/Justin21523

<!-- portfolio-readme:end -->
