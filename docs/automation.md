# Automation

This repository includes scripts for local verification, Browser Demo publishing, static artifact checks, and GitHub Pages setup.

## One-command Local Verification

```bash
./scripts/browser-demo-check.sh
```

This runs:

- WebAssembly workload check
- `dotnet restore`
- Release build
- Release tests
- Browser publish
- Static `wwwroot` smoke test for `/`, `/main.js`, and `/_framework/dotnet.js`

## Run Apps

Desktop:

```bash
./scripts/run-desktop.sh
```

Browser:

```bash
./scripts/run-browser.sh
```

## Publish Browser Demo

```bash
./scripts/publish-browser.sh
```

Deployable output:

```text
src/ArchiveFlow.Browser/bin/Release/net10.0-browser/publish/wwwroot
```

## Verify Static Browser Artifact

```bash
./scripts/verify-browser-static.sh
```

## Prepare GitHub Pages Artifact

```bash
./scripts/prepare-pages-artifact.sh
```

The script sets `.nojekyll` and rewrites `index.html` base href. In GitHub Actions it derives the base path from `GITHUB_REPOSITORY`; locally it defaults to `/archive-flow-studio/`.

Override manually:

```bash
BASE_PATH=/archive-flow-studio/ ./scripts/prepare-pages-artifact.sh
```

## Configure GitHub Pages Source

After authenticating with GitHub CLI:

```bash
gh auth login
./scripts/setup-github-pages.sh
```

This switches GitHub Pages to GitHub Actions deployment mode. It does not push code or modify git history.
