# Maintaining and publishing

Notes for template maintainers: how to change, test and publish the template. This file isn't part of the NuGet
package.

## Repository layout

```text
dotnet-mcp-server-template/
├── README.md                     public readme (also shown on nuget.org)
├── CHANGELOG.md                  template version history
├── nuget-publish.md              this guide
├── LICENSE
├── TemplatePack.csproj           packs the template into a NuGet package
├── assets/                       README screenshots (loaded from GitHub, not packed)
├── .github/workflows/
│   ├── template-ci.yml           creates a sample project, builds, tests, packs; checks UI bundles (push, PR)
│   └── publish.yml               publishes to nuget.org (version tags or manual run)
└── templates/
    └── DotnetMcpTemplate/        the template itself (everything a new project gets)
        ├── .template.config/template.json
        ├── DotnetMcpTemplate.sln
        └── src/ tests/ ui/ docker/ docs/ .github/ ...
```

The `.github/workflows` inside `templates/DotnetMcpTemplate` belong to generated projects. They don't run in this
repository.

## Rules

- Keep `DotnetMcpTemplate` as the placeholder name everywhere (`sourceName`). Never rename it.
- Keep `dotnetmcptemplate-slug` where a lowercase, dash-only name is needed (Docker image, Compose, `ui/package.json`).
- Set NuGet package versions only in `templates/DotnetMcpTemplate/Directory.Packages.props`.
- After changing anything in `templates/DotnetMcpTemplate/ui/`, run `pnpm run build` there and commit the updated
  bundles in `src/DotnetMcpTemplate/ui_dist/`. CI fails when they are stale.
- Never commit or pack `.env`. It's git-ignored and excluded in `TemplatePack.csproj`, and CI and `publish` fail if a
  `.env` ends up in the package. `.env.example` is the committed one.
- Never create projects inside this repository.

## Install from source

```bash
dotnet new install ./templates/DotnetMcpTemplate            # first time
dotnet new install ./templates/DotnetMcpTemplate --force    # after changes
dotnet new uninstall ./templates/DotnetMcpTemplate          # remove
dotnet new list code4mk-mcp                                 # check
```

## Workflow for a change

1. Create a branch and edit files under `templates/DotnetMcpTemplate`.
2. Test the template in place (it builds and runs as a normal solution):

   ```bash
   cd templates/DotnetMcpTemplate
   dotnet build && dotnet test
   cd ui && pnpm install && pnpm run build     # when ui/ changed
   ```

3. Test a generated project, outside the repository:

   ```bash
   dotnet new install ./templates/DotnetMcpTemplate --force
   cd ..
   dotnet new code4mk-mcp -n Test.Mcp -o test-mcp
   cd test-mcp && dotnet build && dotnet test
   grep -rIi --exclude-dir=bin --exclude-dir=obj --exclude-dir=node_modules dotnetmcptemplate . || echo "no placeholders left"
   cd .. && rm -rf test-mcp
   ```

4. Add an entry to `CHANGELOG.md` under a new version.
5. Open a pull request. `template-ci` runs automatically.
6. After merging, publish the new version.

## Publishing to nuget.org

Packages are published under the nuget.org account **Code4mk** as `Code4mk.McpServer.Template`.

Before every publish:

1. `main` is pushed. The README's screenshots and CI badge load from GitHub, so they break on nuget.org otherwise.
2. `CHANGELOG.md` has the new version and date.
3. `template-ci` is green on `main`.

### From your machine

```bash
dotnet pack TemplatePack.csproj -c Release -o artifacts -p:PackageVersion=1.0.0

# check the contents: only .env.example, no .env, bin/, obj/ or node_modules/
unzip -l artifacts/Code4mk.McpServer.Template.1.0.0.nupkg | grep -E "\.env|/bin/|/obj/|node_modules"

# macOS / Linux
export NUGET_API_KEY="your-key"
dotnet nuget push artifacts/Code4mk.McpServer.Template.1.0.0.nupkg \
  --source https://api.nuget.org/v3/index.json --api-key $NUGET_API_KEY

# Windows PowerShell
$env:NUGET_API_KEY = "your-key"
dotnet nuget push artifacts\Code4mk.McpServer.Template.1.0.0.nupkg `
  --source https://api.nuget.org/v3/index.json --api-key $env:NUGET_API_KEY
```

Then tag the release so the repository matches the package: `git tag v1.0.0 && git push origin v1.0.0`.
If `NUGET_API_KEY` is also set as a repository secret, that tag starts `publish` too; it skips the version that
already exists (`--skip-duplicate`), so this is harmless.

### From GitHub Actions

1. Add the nuget.org API key as the repository secret `NUGET_API_KEY` (Settings → Secrets and variables → Actions).
2. Either push a version tag: `git tag v1.1.0 && git push origin v1.1.0`,
   or open Actions → `publish` → **Run workflow** and enter the version (e.g. `1.1.0`).

The workflow validates the version, packs the template with it, checks the package for `.env` files and pushes it.

### Notes

- Create the API key on nuget.org (username → API Keys) with the **Push** scope and glob pattern `Code4mk.*`.
  Give it an expiry and don't paste it into files or commits.
- New versions appear on nuget.org after validation and indexing, usually within 15 to 30 minutes.
- Every publish needs a new version number. A broken version can be unlisted but never deleted or replaced.
- Pre-releases use a suffix (`1.1.0-preview.1`); `dotnet new install` only picks them when asked for explicitly.
- `README.md` is packed into the package and shown on nuget.org: keep its links and images absolute (GitHub URLs,
  images from `raw.githubusercontent.com` or `img.shields.io`).
- Users update with `dotnet new install Code4mk.McpServer.Template` (installs the latest) or
  `dotnet new update`.

## Troubleshooting

| Problem | Fix |
| --- | --- |
| `No templates found matching: 'code4mk-mcp'` | Install the template and check `dotnet new list code4mk-mcp` |
| Namespaces like `my_app.Mcp` | Dashes were used in `-n`. Use PascalCase in `-n` and dashes only in `-o` |
| New project contains another project | `dotnet new` ran inside the template repo. Delete it and run from another folder |
| `template-ci` fails on "Committed bundles match the source" | Run `pnpm run build` in `templates/DotnetMcpTemplate/ui` and commit `ui_dist/` |
| `publish` fails on "Package contains no local-only files" | A `.env` got into the package: check the excludes in `TemplatePack.csproj` |
| README images missing on nuget.org | Push `main` (images load from GitHub); nuget.org caches the README per version |
| `409 Conflict` on push | That version already exists on nuget.org: bump the version |
| `403 Forbidden` on push | The API key expired, lacks the Push scope, or its glob doesn't match `Code4mk.*` |
