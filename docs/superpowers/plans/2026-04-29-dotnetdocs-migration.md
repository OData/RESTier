# DotNetDocs Migration Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace `docs/msdocs/` (docfx) on `feature/vnext` with the DotNetDocs-based `src/Microsoft.Restier.Docs/` project ported from `main`, converting feature/vnext content into Mintlify-styled `.mdx`.

**Architecture:** Three logical groups of work — (1) scaffold the dotnetdocs project from `main@a040d26d` with feature/vnext-correct dependencies and assembly list; (2) verify the SDK restores and the project builds; (3) port the 21 markdown files from `docs/msdocs/` into the new project, converting prose `.md` to `.mdx` with Mintlify components per the design spec, then delete the legacy tree.

**Tech Stack:** .NET 8/9/10 MSBuild projects, `DotNetDocs.Sdk/1.2.0`, Mintlify-flavored MDX (frontmatter + JSX-style components like `<Info>`, `<Steps>`, `<CodeGroup>`, `<Tabs>`, `<CardGroup>`).

**Spec:** [`docs/superpowers/specs/2026-04-29-dotnetdocs-migration-design.md`](../specs/2026-04-29-dotnetdocs-migration-design.md). The body-transforms table in the spec is the per-file conversion contract; do not duplicate it here, follow it.

**Branch:** Work directly on `feature/vnext`. No worktree required (additive scaffolding + clean delete of `docs/msdocs/`).

---

## Phase 1 — Scaffold import

### Task 1: Import scaffold files from main

**Files:**
- Create: `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`
- Create: `src/Microsoft.Restier.Docs/docs.json`
- Create: `src/Microsoft.Restier.Docs/style.css`
- Create: `src/Microsoft.Restier.Docs/assembly-list.txt` (will be rewritten in Task 2)
- Create: `src/Microsoft.Restier.Docs/index.mdx`
- Create: `src/Microsoft.Restier.Docs/quickstart.mdx`
- Create: `src/Microsoft.Restier.Docs/contribution-guidelines.mdx`
- Create: `src/Microsoft.Restier.Docs/license.md`
- Create: `src/Microsoft.Restier.Docs/guides/index.mdx`
- Create: `src/Microsoft.Restier.Docs/guides/clients/dot-net.mdx`
- Create: `src/Microsoft.Restier.Docs/guides/clients/dot-net-standard.mdx`
- Create: `src/Microsoft.Restier.Docs/guides/clients/typescript.mdx`

- [ ] **Step 1: Verify target directory does not yet exist (Bash)**

```bash
test ! -e src/Microsoft.Restier.Docs && echo "OK: target dir clean"
```

Expected: `OK: target dir clean`

- [ ] **Step 2: Create the project directory structure**

```bash
mkdir -p src/Microsoft.Restier.Docs/guides/clients
```

- [ ] **Step 3: Check out files from main@a040d26d into their target paths**

Run each command from the repo root. Use `git show ... > target` (not `git checkout`) so the files land in the working tree without staging or affecting other paths.

```bash
git show a040d26d:src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj > src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
git show a040d26d:src/Microsoft.Restier.Docs/docs.json > src/Microsoft.Restier.Docs/docs.json
git show a040d26d:src/Microsoft.Restier.Docs/style.css > src/Microsoft.Restier.Docs/style.css
git show a040d26d:src/Microsoft.Restier.Docs/assembly-list.txt > src/Microsoft.Restier.Docs/assembly-list.txt
git show a040d26d:src/Microsoft.Restier.Docs/index.mdx > src/Microsoft.Restier.Docs/index.mdx
git show a040d26d:src/Microsoft.Restier.Docs/quickstart.mdx > src/Microsoft.Restier.Docs/quickstart.mdx
git show a040d26d:src/Microsoft.Restier.Docs/contribution-guidelines.mdx > src/Microsoft.Restier.Docs/contribution-guidelines.mdx
git show a040d26d:src/Microsoft.Restier.Docs/license.md > src/Microsoft.Restier.Docs/license.md
git show a040d26d:src/Microsoft.Restier.Docs/guides/index.mdx > src/Microsoft.Restier.Docs/guides/index.mdx
git show a040d26d:src/Microsoft.Restier.Docs/guides/clients/dot-net.mdx > src/Microsoft.Restier.Docs/guides/clients/dot-net.mdx
git show a040d26d:src/Microsoft.Restier.Docs/guides/clients/dot-net-standard.mdx > src/Microsoft.Restier.Docs/guides/clients/dot-net-standard.mdx
git show a040d26d:src/Microsoft.Restier.Docs/guides/clients/typescript.mdx > src/Microsoft.Restier.Docs/guides/clients/typescript.mdx
```

- [ ] **Step 4: Verify all 12 files exist**

```bash
ls -la src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj src/Microsoft.Restier.Docs/docs.json src/Microsoft.Restier.Docs/style.css src/Microsoft.Restier.Docs/assembly-list.txt src/Microsoft.Restier.Docs/index.mdx src/Microsoft.Restier.Docs/quickstart.mdx src/Microsoft.Restier.Docs/contribution-guidelines.mdx src/Microsoft.Restier.Docs/license.md src/Microsoft.Restier.Docs/guides/index.mdx src/Microsoft.Restier.Docs/guides/clients/*.mdx
```

Expected: 12 files listed, no `No such file or directory` errors.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/
git commit -m "$(cat <<'EOF'
docs: import DotNetDocs project scaffold from main@a040d26d

Brings the .docsproj, supporting files, and main's hand-written content
(index, quickstart, contribution-guidelines, license, guides/index, and
the three clients/ stubs) into feature/vnext. assembly-list.txt and
ProjectReferences will be rewritten in subsequent commits.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 2: Replace `assembly-list.txt` with feature/vnext source set

**Files:**
- Modify: `src/Microsoft.Restier.Docs/assembly-list.txt`

The imported `assembly-list.txt` from main has hardcoded Windows paths and references `Microsoft.Restier.AspNet`, which is not a project on `feature/vnext`. Replace it with the six current source projects at TFM `net9.0` using paths relative to the docsproj.

- [ ] **Step 1: Inspect the current contents (so the diff is clear in review)**

```bash
cat src/Microsoft.Restier.Docs/assembly-list.txt
```

Expected: 7 lines of `D:\GitHub\RESTier\src\…\bin\Debug\…\…\.dll` paths.

- [ ] **Step 2: Overwrite the file with the corrected contents**

```bash
cat > src/Microsoft.Restier.Docs/assembly-list.txt <<'EOF'
../Microsoft.Restier.Core/bin/Debug/net9.0/Microsoft.Restier.Core.dll
../Microsoft.Restier.AspNetCore/bin/Debug/net9.0/Microsoft.Restier.AspNetCore.dll
../Microsoft.Restier.AspNetCore.Swagger/bin/Debug/net9.0/Microsoft.Restier.AspNetCore.Swagger.dll
../Microsoft.Restier.Breakdance/bin/Debug/net9.0/Microsoft.Restier.Breakdance.dll
../Microsoft.Restier.EntityFramework/bin/Debug/net9.0/Microsoft.Restier.EntityFramework.dll
../Microsoft.Restier.EntityFrameworkCore/bin/Debug/net9.0/Microsoft.Restier.EntityFrameworkCore.dll
EOF
```

- [ ] **Step 3: Verify**

```bash
cat src/Microsoft.Restier.Docs/assembly-list.txt
wc -l src/Microsoft.Restier.Docs/assembly-list.txt
```

Expected: 6 lines, all starting with `../Microsoft.Restier.`, all targeting `net9.0`, no `Microsoft.Restier.AspNet/` (without "Core") and no Windows-style paths.

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.Docs/assembly-list.txt
git commit -m "$(cat <<'EOF'
docs: rewrite assembly-list.txt for feature/vnext source set

Replaces main's stale list (hardcoded Windows paths, references
Microsoft.Restier.AspNet which no longer exists, mixed TFMs) with the
six current projects at net9.0 using relative paths from the docsproj.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 3: Wire ProjectReferences in the docsproj

**Files:**
- Modify: `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`

Add explicit `<ProjectReference>` items so a clean `dotnet build RESTier.slnx` builds the source projects before doc generation runs.

- [ ] **Step 1: Read the current docsproj to locate insertion point**

```bash
cat src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Note: there is an existing `<ItemGroup>` near the bottom containing `<Folder Include="snippets\" />`. Add the new ProjectReferences as a sibling `<ItemGroup>` immediately before the closing `</Project>`.

- [ ] **Step 2: Edit the docsproj — add ProjectReferences before `</Project>`**

Use `Edit` to insert this block immediately before the existing `</Project>` line. The existing `<ItemGroup>` for `snippets/` stays where it is.

```xml
    <ItemGroup>
        <ProjectReference Include="..\Microsoft.Restier.Core\Microsoft.Restier.Core.csproj" />
        <ProjectReference Include="..\Microsoft.Restier.AspNetCore\Microsoft.Restier.AspNetCore.csproj" />
        <ProjectReference Include="..\Microsoft.Restier.AspNetCore.Swagger\Microsoft.Restier.AspNetCore.Swagger.csproj" />
        <ProjectReference Include="..\Microsoft.Restier.Breakdance\Microsoft.Restier.Breakdance.csproj" />
        <ProjectReference Include="..\Microsoft.Restier.EntityFramework\Microsoft.Restier.EntityFramework.csproj" />
        <ProjectReference Include="..\Microsoft.Restier.EntityFrameworkCore\Microsoft.Restier.EntityFrameworkCore.csproj" />
    </ItemGroup>

</Project>
```

- [ ] **Step 3: Verify XML is well-formed**

```bash
xmllint --noout src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj && echo "OK"
```

Expected: `OK`. If `xmllint` is unavailable, skip and rely on Phase 2 build verification.

- [ ] **Step 4: Verify all six ProjectReferences are present**

```bash
grep -c '<ProjectReference Include=' src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

Expected: `6`.

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
git commit -m "$(cat <<'EOF'
docs: wire ProjectReferences in docsproj for clean-build ordering

Adds ProjectReference items for the six documented source projects so
dotnet build RESTier.slnx builds the assemblies before doc generation
runs. Without this, a clean or parallel build can hit the docsproj
before its referenced DLLs exist.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 4: Add `api-reference/` to `.gitignore`

**Files:**
- Modify: `.gitignore`

The DotNetDocs SDK regenerates `api-reference/` on build. Treat it as build output, not source.

- [ ] **Step 1: Check current .gitignore for any existing entries**

```bash
grep -nE 'api-reference|Microsoft.Restier.Docs' .gitignore || echo "no existing entries"
```

Expected: `no existing entries` (or any pre-existing matches you should NOT duplicate).

- [ ] **Step 2: Append the ignore rule**

Use `Edit` to add the rule. Find a sensible section in `.gitignore` (commonly under a "Build output" or similar comment). If unsure, append at the end:

```
# DotNetDocs SDK regenerates this on build
src/Microsoft.Restier.Docs/api-reference/
```

- [ ] **Step 3: Verify**

```bash
grep -A1 'DotNetDocs SDK' .gitignore
```

Expected: shows the comment and the `api-reference/` line.

- [ ] **Step 4: Commit**

```bash
git add .gitignore
git commit -m "$(cat <<'EOF'
docs: gitignore regenerated DotNetDocs api-reference output

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 2 — SDK restore gate (BLOCKING)

If any task in this phase fails and cannot be unblocked by the documented fallback, **stop and ask the user**. Do not proceed to Phase 3.

### Task 5: Restore the docsproj — try public NuGet first, then known feeds

**Files:**
- (potentially modify) `NuGet.Config`

- [ ] **Step 1: Inspect existing NuGet.Config for current feed configuration**

```bash
cat NuGet.Config
```

Note the existing feeds — you'll add to them, not replace.

- [ ] **Step 2: Try a clean restore against public NuGet**

```bash
dotnet restore src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -30
```

Expected outcomes:
- **Success** ("Restore completed") → restore worked from public feed; skip to step 6.
- **Failure** with "Unable to find package DotNetDocs.Sdk" → continue to step 3.
- **Other failure** → stop and ask the user.

- [ ] **Step 3: Probe known CloudNimble / partner feeds (manual, one at a time)**

Try these feeds in order. For each, attempt a `dotnet nuget search` against the feed to confirm `DotNetDocs.Sdk` is hosted there:

```bash
# Candidate feeds — run each search separately, observe results.
dotnet nuget search DotNetDocs.Sdk --source https://www.myget.org/F/cloudnimble-staging/api/v3/index.json 2>&1 | head -10
dotnet nuget search DotNetDocs.Sdk --source https://nuget.cloudnimble.com/v3/index.json 2>&1 | head -10
```

If `dotnet nuget search` is unavailable, fall back to `curl` against the feed's index.json + a query to its search service:

```bash
curl -sS https://www.myget.org/F/cloudnimble-staging/api/v3/index.json | head -20
```

If neither resolves a feed without credentials, **stop and ask the user**. Do not proceed.

- [ ] **Step 4: Add the resolving feed to `NuGet.Config`**

Once a feed has been confirmed to host the SDK, edit `NuGet.Config` to add it as a `<packageSource>`. Example shape (adapt to the actual existing structure of `NuGet.Config`):

```xml
<add key="cloudnimble" value="<feed-url-confirmed-in-step-3>" />
```

- [ ] **Step 5: Re-run restore**

```bash
dotnet restore src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -30
```

Expected: `Restore completed`. If still failing, **stop and ask the user**.

- [ ] **Step 6: Commit (only if NuGet.Config was modified)**

```bash
git add NuGet.Config
git commit -m "$(cat <<'EOF'
build: add NuGet feed for DotNetDocs.Sdk

Required to restore the Microsoft.Restier.Docs project SDK reference.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

If no feed change was needed, commit nothing for this task.

---

### Task 6: Build the docsproj and verify api-reference regeneration

**Files:**
- (verifies, no edits)

- [ ] **Step 1: Build the source projects first to ensure DLLs exist**

```bash
dotnet build RESTier.slnx 2>&1 | tail -20
```

Expected: `Build succeeded`. The docsproj is not yet in the slnx (Phase 5), so this only builds the source projects.

- [ ] **Step 2: Build the docsproj alone**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -40
```

Expected outcomes:
- **`Build succeeded`** → continue to step 3.
- **Failure with "could not find file ../Microsoft.Restier.…/bin/…"** → check whether the assemblies built in step 1 actually live where `assembly-list.txt` says (paths and TFM). Adjust `assembly-list.txt` (e.g., if `Configuration` defaults differ, hard-code `Debug`) and retry.
- **Other failure** → capture the diagnostic, fix forward only if tractable; otherwise stop and ask.

- [ ] **Step 3: Verify api-reference/ was regenerated**

```bash
ls src/Microsoft.Restier.Docs/api-reference/ 2>&1 | head -10
find src/Microsoft.Restier.Docs/api-reference -name '*.mdx' | wc -l
```

Expected: a directory tree exists; the `find` count is in the hundreds (one mdx per public type across six assemblies).

- [ ] **Step 4: Verify api-reference is gitignored**

```bash
git status --porcelain src/Microsoft.Restier.Docs/api-reference/ | head -5
```

Expected: empty output (the regenerated tree is not staged or tracked).

- [ ] **Step 5: No commit (this task only verifies)**

---

### Task 7: Determine docs.json regeneration behavior

**Files:**
- (probe, no permanent edits)

The spec needs to know whether the SDK regenerates `docs.json` from the `<MintlifyTemplate>` block in the docsproj, or whether `docs.json` is hand-maintained. This decision drives Phase 4.

- [ ] **Step 1: Snapshot the current docs.json**

```bash
cp src/Microsoft.Restier.Docs/docs.json /tmp/docs.json.before
```

- [ ] **Step 2: Add a harmless probe marker to the MintlifyTemplate**

Use `Edit` on `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj` to change the existing `<Name>Restier</Name>` line to `<Name>Restier-PROBE</Name>`.

- [ ] **Step 3: Build and observe**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
diff /tmp/docs.json.before src/Microsoft.Restier.Docs/docs.json | head -10
```

Expected outcomes:
- **`diff` shows `"name": "Restier-PROBE"` in the new file** → SDK regenerates docs.json from `.docsproj`. **Source of truth: `.docsproj` only.** Record this for Phase 4 and the CLAUDE.md update.
- **`diff` is empty** → SDK does NOT regenerate docs.json. **Source of truth: both files; keep them in sync.** Record this.

- [ ] **Step 4: Revert the probe marker**

Use `Edit` to change `<Name>Restier-PROBE</Name>` back to `<Name>Restier</Name>`.

If the SDK regenerated `docs.json`, also rebuild once to revert that file:

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -5
```

If the SDK did NOT regenerate `docs.json`, restore the snapshot:

```bash
cp /tmp/docs.json.before src/Microsoft.Restier.Docs/docs.json
rm /tmp/docs.json.before
```

- [ ] **Step 5: Verify nothing is staged from the probe**

```bash
git status --porcelain src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj src/Microsoft.Restier.Docs/docs.json
```

Expected: empty output.

- [ ] **Step 6: Record the finding**

Add a short note to your scratch (or the eventual PR description) capturing whether `.docsproj` is the sole source of truth, or whether `docs.json` is also hand-edited. This drives:
- Phase 4 (whether you edit one file or two)
- Phase 6 task 30 (CLAUDE.md update)

No commit for this task.

---

## Phase 3 — Content conversion

**General recipe for every prose conversion task in this phase:**

1. Read the source `.md` file.
2. Create the target `.mdx` file with the frontmatter shown in the task.
3. Apply the body-transforms from the spec's body-transforms table:
   - Strip the leading `# H1` (Mintlify renders title from frontmatter).
   - Demote remaining headings if needed so `##` is the highest in-body heading.
   - Convert blockquote callouts (`> **Note:**`, etc.) to Mintlify components (`<Note>`, `<Tip>`, `<Warning>`, `<Info>`).
   - Convert numbered lists with multi-sentence steps to `<Steps>` / `<Step title="…">`.
   - Convert adjacent multi-language code blocks showing parallel content to `<CodeGroup>`.
   - Convert parallel sections like `### ASP.NET` / `### ASP.NET Core` to `<Tabs>` / `<Tab>`.
   - Convert end-of-page next-steps lists to `<CardGroup>` / `<Card>`.
   - Drop `.md`/`.mdx` extensions from internal links.
   - Remap absolute-root links (`/server/foo/` → `/guides/server/foo`, etc.).
4. Run the per-file output checks (listed in each task's verification step).
5. Build the docsproj.
6. Commit.

**Default to `<Info>` when a blockquote's intent is ambiguous.**

---

### Task 8: Convert `index.mdx`

**Files:**
- Modify: `src/Microsoft.Restier.Docs/index.mdx` (overwrite imported file from main)
- Source: `docs/msdocs/index.md`

**Frontmatter (keep main's, do not change):**
```yaml
---
title: "Microsoft Restier"
description: "OData V4 API development framework for building standardized RESTful services on .NET"
icon: "house"
sidebarTitle: "Home"
---
```

**Body source:** Replace the *body* of `index.mdx` with the body of `docs/msdocs/index.md` (mdx-ified). Note: the source `index.md` uses raw HTML (`<div align="center">`, `<h1>`); preserve appropriate parts but lean on Mintlify components where the source uses callout-style HTML.

- [ ] **Step 1: Read the source**

```bash
wc -l docs/msdocs/index.md
cat docs/msdocs/index.md
```

- [ ] **Step 2: Read main's existing index.mdx (for badge/header conventions)**

```bash
cat src/Microsoft.Restier.Docs/index.mdx
```

- [ ] **Step 3: Write the new `index.mdx`**

Use `Write` to overwrite `src/Microsoft.Restier.Docs/index.mdx`. Preserve main's frontmatter shown above; replace the body with feature/vnext's content from `docs/msdocs/index.md`, applying the conversion rules. Pay attention to:
- Centered intro block: keep the `<div align="center">` only if it renders correctly in Mintlify; otherwise convert to plain markdown headings.
- Component import blocks (the "Restier Components" / "Supported Platforms" sections in main): preserve the `<Tabs>` shape from main if feature/vnext's content fits the same ASP.NET / ASP.NET Core split.
- Replace any links to `/server/...` with `/guides/server/...` per the absolute-root link rule.

- [ ] **Step 4: Per-file output checks**

```bash
head -7 src/Microsoft.Restier.Docs/index.mdx                          # frontmatter present
grep -nE '^# ' src/Microsoft.Restier.Docs/index.mdx                   # no leftover # H1
grep -nE '\]\(/(server|extending-restier|clients)/' src/Microsoft.Restier.Docs/index.mdx   # no old absolute links
grep -nE '\]\([^)]+\.mdx?[\)#]' src/Microsoft.Restier.Docs/index.mdx  # no leftover .md/.mdx extensions
```

Expected: frontmatter has 4 fields; the three `grep` commands return zero matches.

- [ ] **Step 5: Build the docsproj**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
```

Expected: `Build succeeded`.

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.Docs/index.mdx
git commit -m "docs: port index.mdx body to feature/vnext content

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 9: Convert `quickstart.mdx`

**Files:**
- Modify: `src/Microsoft.Restier.Docs/quickstart.mdx` (overwrite imported placeholder)
- Source: `docs/msdocs/getting-started.md`

**Frontmatter (keep main's, do not change):**
```yaml
---
title: "Quickstart"
description: "Get started with Restier in minutes"
icon: "rocket"
sidebarTitle: "Quickstart"
---
```

The imported `quickstart.mdx` body is literally `[THIS IS A PLACEHOLDER FOR FUTURE CONTENT]`. Replace it entirely.

- [ ] **Step 1: Read the source**

```bash
wc -l docs/msdocs/getting-started.md
cat docs/msdocs/getting-started.md
```

- [ ] **Step 2: Write the new `quickstart.mdx`**

Use `Write` to overwrite `src/Microsoft.Restier.Docs/quickstart.mdx` with the frontmatter shown above plus the body from `docs/msdocs/getting-started.md`, applying conversion rules. The Quickstart is a step-by-step tutorial — any sequential setup walk-through should likely use `<Steps>` / `<Step title="…">`.

- [ ] **Step 3: Per-file output checks**

```bash
head -7 src/Microsoft.Restier.Docs/quickstart.mdx
grep -nE '^# ' src/Microsoft.Restier.Docs/quickstart.mdx
grep -nE '\]\(/(server|extending-restier|clients)/' src/Microsoft.Restier.Docs/quickstart.mdx
grep -nE '\]\([^)]+\.mdx?[\)#]' src/Microsoft.Restier.Docs/quickstart.mdx
grep -n 'PLACEHOLDER' src/Microsoft.Restier.Docs/quickstart.mdx
```

Expected: frontmatter has 4 fields; all four `grep` commands return zero matches.

- [ ] **Step 4: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/quickstart.mdx
git commit -m "docs: replace quickstart.mdx placeholder with feature/vnext getting-started content

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 10: Convert `contribution-guidelines.mdx`

**Files:**
- Modify: `src/Microsoft.Restier.Docs/contribution-guidelines.mdx` (overwrite imported file)
- Source: `docs/msdocs/contribution-guidelines.md`

**Frontmatter (keep main's, do not change):**
```yaml
---
title: "Contribution Guidelines"
description: "Learn how to contribute to the Restier project"
icon: "code-pull-request"
sidebarTitle: "Contributing"
---
```

- [ ] **Step 1: Read source and target**

```bash
cat docs/msdocs/contribution-guidelines.md
cat src/Microsoft.Restier.Docs/contribution-guidelines.mdx
```

- [ ] **Step 2: Write the new file**

Apply the standard recipe. The source starts with `# How Can I Contribute?`; preserve the spirit (the body opens with that question) but the H1 itself is removed because the frontmatter title is "Contribution Guidelines" — the first body heading becomes `## How Can I Contribute?`.

- [ ] **Step 3: Per-file output checks**

```bash
head -7 src/Microsoft.Restier.Docs/contribution-guidelines.mdx
grep -nE '^# ' src/Microsoft.Restier.Docs/contribution-guidelines.mdx
grep -nE '\]\(/(server|extending-restier|clients)/' src/Microsoft.Restier.Docs/contribution-guidelines.mdx
grep -nE '\]\([^)]+\.mdx?[\)#]' src/Microsoft.Restier.Docs/contribution-guidelines.mdx
```

Expected: frontmatter has 4 fields; all three `grep` commands return zero matches.

- [ ] **Step 4: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/contribution-guidelines.mdx
git commit -m "docs: port contribution-guidelines.mdx body to feature/vnext content

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 11: Replace `license.md` with feature/vnext content

**Files:**
- Modify: `src/Microsoft.Restier.Docs/license.md` (overwrite imported file)
- Source: `docs/msdocs/license.md`

**No conversion** — `license.md` stays `.md` (matches main, not in nav).

- [ ] **Step 1: Copy the file verbatim**

```bash
cp docs/msdocs/license.md src/Microsoft.Restier.Docs/license.md
```

- [ ] **Step 2: Verify**

```bash
diff docs/msdocs/license.md src/Microsoft.Restier.Docs/license.md && echo "OK: identical"
```

Expected: `OK: identical`.

- [ ] **Step 3: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -5
```

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.Docs/license.md
git commit -m "docs: replace license.md with feature/vnext content

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 12: Create `why-restier.mdx` placeholder

**Files:**
- Create: `src/Microsoft.Restier.Docs/why-restier.mdx`

This file does not exist on main and has no source. It's a stub so the navigation reference from Phase 4 doesn't break the build.

- [ ] **Step 1: Write the placeholder**

```bash
cat > src/Microsoft.Restier.Docs/why-restier.mdx <<'EOF'
---
title: "Why Restier?"
description: "What problems Restier solves and when to choose it"
icon: "lightbulb"
sidebarTitle: "Why Restier?"
---

<Warning>Coming Soon!</Warning>
EOF
```

- [ ] **Step 2: Verify**

```bash
cat src/Microsoft.Restier.Docs/why-restier.mdx
head -7 src/Microsoft.Restier.Docs/why-restier.mdx | grep -c '^title:\|^description:\|^icon:\|^sidebarTitle:'
```

Expected: file shows the frontmatter and the `<Warning>` line; the `grep -c` returns `4`.

- [ ] **Step 3: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -5
```

- [ ] **Step 4: Commit**

```bash
git add src/Microsoft.Restier.Docs/why-restier.mdx
git commit -m "docs: add why-restier.mdx placeholder for future content

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 13: Convert `guides/server/model-building.mdx`

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/server/model-building.mdx`
- Source: `docs/msdocs/server/model-building.md`

**Frontmatter:**
```yaml
---
title: "Customizing the Entity Model"
description: "Customize and extend your Entity Data Model (EDM) in Restier"
icon: "sitemap"
sidebarTitle: "Model Building"
---
```

- [ ] **Step 1: Read the source and verify the target dir exists**

```bash
mkdir -p src/Microsoft.Restier.Docs/guides/server
cat docs/msdocs/server/model-building.md | head -50
wc -l docs/msdocs/server/model-building.md
```

- [ ] **Step 2: Write the new file**

Apply the standard recipe (frontmatter above, body from source with conversion rules).

- [ ] **Step 3: Per-file output checks**

```bash
head -7 src/Microsoft.Restier.Docs/guides/server/model-building.mdx
grep -nE '^# ' src/Microsoft.Restier.Docs/guides/server/model-building.mdx
grep -nE '\]\(/(server|extending-restier|clients)/' src/Microsoft.Restier.Docs/guides/server/model-building.mdx
grep -nE '\]\([^)]+\.mdx?[\)#]' src/Microsoft.Restier.Docs/guides/server/model-building.mdx
```

Expected: frontmatter has 4 fields; all three `grep` commands return zero matches.

- [ ] **Step 4: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/model-building.mdx
git commit -m "docs: convert server/model-building.md → mdx with Mintlify components

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 14: Convert `guides/server/method-authorization.mdx`

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/server/method-authorization.mdx`
- Source: `docs/msdocs/server/method-authorization.md`

**Frontmatter:**
```yaml
---
title: "Method Authorization"
description: "Fine-grain control over API request execution with security rules"
icon: "shield-halved"
sidebarTitle: "Authorization"
---
```

- [ ] **Step 1: Read source**

```bash
cat docs/msdocs/server/method-authorization.md | head -50
wc -l docs/msdocs/server/method-authorization.md
```

- [ ] **Step 2: Write the new file**

Apply the standard recipe.

- [ ] **Step 3: Per-file output checks** (same four `grep` calls as Task 13, with the new path)

```bash
head -7 src/Microsoft.Restier.Docs/guides/server/method-authorization.mdx
grep -nE '^# ' src/Microsoft.Restier.Docs/guides/server/method-authorization.mdx
grep -nE '\]\(/(server|extending-restier|clients)/' src/Microsoft.Restier.Docs/guides/server/method-authorization.mdx
grep -nE '\]\([^)]+\.mdx?[\)#]' src/Microsoft.Restier.Docs/guides/server/method-authorization.mdx
```

- [ ] **Step 4: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/method-authorization.mdx
git commit -m "docs: convert server/method-authorization.md → mdx with Mintlify components

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 15: Convert `guides/server/filters.mdx`

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/server/filters.mdx`
- Source: `docs/msdocs/server/filters.md`

**Frontmatter:**
```yaml
---
title: "EntitySet Filters"
description: "Control query results by filtering EntitySets based on business rules"
icon: "filter-list"
sidebarTitle: "Filters"
---
```

**Special-case note:** This file is referenced by absolute-root links from other pages (we observed `/server/method-authorization/` link in `interceptors.md`). The slug here will be `/guides/server/filters` after the move.

- [ ] **Step 1: Read source**

```bash
cat docs/msdocs/server/filters.md | head -50
wc -l docs/msdocs/server/filters.md
```

- [ ] **Step 2: Write the new file**

Apply the standard recipe.

- [ ] **Step 3: Per-file output checks**

```bash
head -7 src/Microsoft.Restier.Docs/guides/server/filters.mdx
grep -nE '^# ' src/Microsoft.Restier.Docs/guides/server/filters.mdx
grep -nE '\]\(/(server|extending-restier|clients)/' src/Microsoft.Restier.Docs/guides/server/filters.mdx
grep -nE '\]\([^)]+\.mdx?[\)#]' src/Microsoft.Restier.Docs/guides/server/filters.mdx
```

- [ ] **Step 4: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/filters.mdx
git commit -m "docs: convert server/filters.md → mdx with Mintlify components

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 16: Convert `guides/server/interceptors.mdx`

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/server/interceptors.mdx`
- Source: `docs/msdocs/server/interceptors.md`

**Frontmatter:**
```yaml
---
title: "Interceptors"
description: "Process validation and business logic before and after database operations"
icon: "filter"
sidebarTitle: "Interceptors"
---
```

**Special-case note:** Source contains an absolute-root link `/server/method-authorization/`. That MUST become `/guides/server/method-authorization` per the body-transforms table.

- [ ] **Step 1: Read source and confirm absolute-root link presence**

```bash
cat docs/msdocs/server/interceptors.md | head -30
grep -nE '\]\(/(server|extending-restier|clients)/' docs/msdocs/server/interceptors.md
```

Expected: at least one match for `/server/method-authorization/`.

- [ ] **Step 2: Write the new file**

Apply the standard recipe. Convert `/server/method-authorization/` → `/guides/server/method-authorization`.

- [ ] **Step 3: Per-file output checks**

```bash
head -7 src/Microsoft.Restier.Docs/guides/server/interceptors.mdx
grep -nE '^# ' src/Microsoft.Restier.Docs/guides/server/interceptors.mdx
grep -nE '\]\(/(server|extending-restier|clients)/' src/Microsoft.Restier.Docs/guides/server/interceptors.mdx
grep -nE '\]\([^)]+\.mdx?[\)#]' src/Microsoft.Restier.Docs/guides/server/interceptors.mdx
```

Expected: third `grep` returns zero (link was successfully remapped).

- [ ] **Step 4: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/interceptors.mdx
git commit -m "docs: convert server/interceptors.md → mdx with Mintlify components

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 17: Convert `guides/server/operations.mdx`

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/server/operations.mdx`
- Source: `docs/msdocs/server/operations.md`

**Frontmatter (new — not on main):**
```yaml
---
title: "Operations"
description: "OData functions and actions for custom server-side operations"
icon: "bolt"
sidebarTitle: "Operations"
---
```

**Special-case note:** Source contains absolute-root links to `/server/interceptors/` and `/server/method-authorization/` (lines 319-320). Both MUST be remapped.

- [ ] **Step 1: Read source and confirm absolute-root links**

```bash
cat docs/msdocs/server/operations.md | head -50
grep -nE '\]\(/(server|extending-restier|clients)/' docs/msdocs/server/operations.md
```

Expected: at least two matches.

- [ ] **Step 2: Write the new file**

Apply the standard recipe. Remap each `/server/...` link to `/guides/server/...`.

- [ ] **Step 3: Per-file output checks**

```bash
head -7 src/Microsoft.Restier.Docs/guides/server/operations.mdx
grep -nE '^# ' src/Microsoft.Restier.Docs/guides/server/operations.mdx
grep -nE '\]\(/(server|extending-restier|clients)/' src/Microsoft.Restier.Docs/guides/server/operations.mdx
grep -nE '\]\([^)]+\.mdx?[\)#]' src/Microsoft.Restier.Docs/guides/server/operations.mdx
```

Expected: third `grep` returns zero (links were successfully remapped).

- [ ] **Step 4: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/operations.mdx
git commit -m "docs: convert server/operations.md → mdx with Mintlify components

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 18: Convert `guides/server/swagger.mdx`

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/server/swagger.mdx`
- Source: `docs/msdocs/server/swagger.md`

**Frontmatter (new):**
```yaml
---
title: "OpenAPI / Swagger Support"
description: "Generate OpenAPI documents from your Restier API automatically"
icon: "code"
sidebarTitle: "OpenAPI"
---
```

- [ ] **Step 1: Read source**

```bash
cat docs/msdocs/server/swagger.md | head -50
wc -l docs/msdocs/server/swagger.md
```

- [ ] **Step 2: Write the new file**

Apply the standard recipe.

- [ ] **Step 3: Per-file output checks**

```bash
head -7 src/Microsoft.Restier.Docs/guides/server/swagger.mdx
grep -nE '^# ' src/Microsoft.Restier.Docs/guides/server/swagger.mdx
grep -nE '\]\(/(server|extending-restier|clients)/' src/Microsoft.Restier.Docs/guides/server/swagger.mdx
grep -nE '\]\([^)]+\.mdx?[\)#]' src/Microsoft.Restier.Docs/guides/server/swagger.mdx
```

- [ ] **Step 4: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/swagger.mdx
git commit -m "docs: convert server/swagger.md → mdx with Mintlify components

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 19: Convert `guides/server/testing.mdx`

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/server/testing.mdx`
- Source: `docs/msdocs/server/testing.md`

**Frontmatter (new):**
```yaml
---
title: "Testing with Breakdance"
description: "In-memory integration testing for Restier APIs using Microsoft.Restier.Breakdance"
icon: "vial"
sidebarTitle: "Testing"
---
```

- [ ] **Step 1: Read source**

```bash
cat docs/msdocs/server/testing.md | head -50
wc -l docs/msdocs/server/testing.md
```

- [ ] **Step 2: Write the new file**

Apply the standard recipe.

- [ ] **Step 3: Per-file output checks**

```bash
head -7 src/Microsoft.Restier.Docs/guides/server/testing.mdx
grep -nE '^# ' src/Microsoft.Restier.Docs/guides/server/testing.mdx
grep -nE '\]\(/(server|extending-restier|clients)/' src/Microsoft.Restier.Docs/guides/server/testing.mdx
grep -nE '\]\([^)]+\.mdx?[\)#]' src/Microsoft.Restier.Docs/guides/server/testing.mdx
```

- [ ] **Step 4: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/testing.mdx
git commit -m "docs: convert server/testing.md → mdx with Mintlify components

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 20: Convert `guides/server/naming-conventions.mdx`

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/server/naming-conventions.mdx`
- Source: `docs/msdocs/server/naming-conventions.md`

**Frontmatter (new):**
```yaml
---
title: "Naming Conventions"
description: "Configure JSON property naming for your OData API (PascalCase, camelCase)"
icon: "tag"
sidebarTitle: "Naming"
---
```

- [ ] **Step 1: Read source**

```bash
cat docs/msdocs/server/naming-conventions.md | head -50
wc -l docs/msdocs/server/naming-conventions.md
```

- [ ] **Step 2: Write the new file**

Apply the standard recipe.

- [ ] **Step 3: Per-file output checks**

```bash
head -7 src/Microsoft.Restier.Docs/guides/server/naming-conventions.mdx
grep -nE '^# ' src/Microsoft.Restier.Docs/guides/server/naming-conventions.mdx
grep -nE '\]\(/(server|extending-restier|clients)/' src/Microsoft.Restier.Docs/guides/server/naming-conventions.mdx
grep -nE '\]\([^)]+\.mdx?[\)#]' src/Microsoft.Restier.Docs/guides/server/naming-conventions.mdx
```

- [ ] **Step 4: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/naming-conventions.mdx
git commit -m "docs: convert server/naming-conventions.md → mdx with Mintlify components

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 21: Convert `guides/server/concurrency.mdx`

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/server/concurrency.mdx`
- Source: `docs/msdocs/server/concurrency.md`

**Frontmatter (new):**
```yaml
---
title: "Optimistic Concurrency"
description: "Built-in OData ETag-based concurrency control for safe updates"
icon: "key"
sidebarTitle: "Concurrency"
---
```

- [ ] **Step 1: Read source**

```bash
cat docs/msdocs/server/concurrency.md | head -50
wc -l docs/msdocs/server/concurrency.md
```

- [ ] **Step 2: Write the new file**

Apply the standard recipe.

- [ ] **Step 3: Per-file output checks**

```bash
head -7 src/Microsoft.Restier.Docs/guides/server/concurrency.mdx
grep -nE '^# ' src/Microsoft.Restier.Docs/guides/server/concurrency.mdx
grep -nE '\]\(/(server|extending-restier|clients)/' src/Microsoft.Restier.Docs/guides/server/concurrency.mdx
grep -nE '\]\([^)]+\.mdx?[\)#]' src/Microsoft.Restier.Docs/guides/server/concurrency.mdx
```

- [ ] **Step 4: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/concurrency.mdx
git commit -m "docs: convert server/concurrency.md → mdx with Mintlify components

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 22: Convert `guides/server/performance.mdx`

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/server/performance.mdx`
- Source: `docs/msdocs/server/performance.md`

**Frontmatter (new — but source already has docfx-style frontmatter; replace it):**
```yaml
---
title: "Performance Considerations"
description: "Performance notes and known limitations for RESTier query execution"
icon: "gauge-high"
sidebarTitle: "Performance"
---
```

**Special-case note:** Source already has its own frontmatter (see lines 1-4: `--- title: Performance Considerations description: …`). DROP the source's frontmatter — replace with the four-field Mintlify-style frontmatter shown above.

- [ ] **Step 1: Read source**

```bash
cat docs/msdocs/server/performance.md | head -50
wc -l docs/msdocs/server/performance.md
```

- [ ] **Step 2: Write the new file**

Apply the standard recipe; drop the source frontmatter; use the frontmatter above.

- [ ] **Step 3: Per-file output checks**

```bash
head -7 src/Microsoft.Restier.Docs/guides/server/performance.mdx
grep -nE '^# ' src/Microsoft.Restier.Docs/guides/server/performance.mdx
grep -nE '\]\(/(server|extending-restier|clients)/' src/Microsoft.Restier.Docs/guides/server/performance.mdx
grep -nE '\]\([^)]+\.mdx?[\)#]' src/Microsoft.Restier.Docs/guides/server/performance.mdx
```

- [ ] **Step 4: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/server/performance.mdx
git commit -m "docs: convert server/performance.md → mdx with Mintlify components

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 23: Convert `guides/extending-restier/in-memory-provider.mdx`

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/extending-restier/in-memory-provider.mdx`
- Source: `docs/msdocs/extending-restier/in-memory-provider.md`

**Frontmatter:**
```yaml
---
title: "In-Memory Data Provider"
description: "Build OData services with all-in-memory resources, no database required"
icon: "database"
sidebarTitle: "In-Memory Provider"
---
```

**Special-case note:** Source's first heading is `## In-Memory Data Provider` (already H2, not H1). Standard "strip the leading H1" rule doesn't apply; just remove that opening heading too because the title is in frontmatter, OR keep it as the first body heading — pick consistency with siblings (other extending-restier files use the body-heading-redundant-with-title pattern, so prefer to remove it).

- [ ] **Step 1: Verify target dir exists; read source**

```bash
mkdir -p src/Microsoft.Restier.Docs/guides/extending-restier
cat docs/msdocs/extending-restier/in-memory-provider.md | head -50
wc -l docs/msdocs/extending-restier/in-memory-provider.md
```

- [ ] **Step 2: Write the new file**

Apply the standard recipe; address the H2-as-first-heading note above.

- [ ] **Step 3: Per-file output checks**

```bash
head -7 src/Microsoft.Restier.Docs/guides/extending-restier/in-memory-provider.mdx
grep -nE '^# ' src/Microsoft.Restier.Docs/guides/extending-restier/in-memory-provider.mdx
grep -nE '\]\(/(server|extending-restier|clients)/' src/Microsoft.Restier.Docs/guides/extending-restier/in-memory-provider.mdx
grep -nE '\]\([^)]+\.mdx?[\)#]' src/Microsoft.Restier.Docs/guides/extending-restier/in-memory-provider.mdx
```

- [ ] **Step 4: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/extending-restier/in-memory-provider.mdx
git commit -m "docs: convert extending-restier/in-memory-provider.md → mdx

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 24: Convert `guides/extending-restier/temporal-types.mdx`

**Files:**
- Create: `src/Microsoft.Restier.Docs/guides/extending-restier/temporal-types.mdx`
- Source: `docs/msdocs/extending-restier/temporal-types.md`

**Frontmatter:**
```yaml
---
title: "Temporal Types"
description: "Working with date and time types in Restier across EF6 and EF Core"
icon: "clock"
sidebarTitle: "Temporal Types"
---
```

- [ ] **Step 1: Read source**

```bash
cat docs/msdocs/extending-restier/temporal-types.md | head -50
wc -l docs/msdocs/extending-restier/temporal-types.md
```

- [ ] **Step 2: Write the new file**

Apply the standard recipe.

- [ ] **Step 3: Per-file output checks**

```bash
head -7 src/Microsoft.Restier.Docs/guides/extending-restier/temporal-types.mdx
grep -nE '^# ' src/Microsoft.Restier.Docs/guides/extending-restier/temporal-types.mdx
grep -nE '\]\(/(server|extending-restier|clients)/' src/Microsoft.Restier.Docs/guides/extending-restier/temporal-types.mdx
grep -nE '\]\([^)]+\.mdx?[\)#]' src/Microsoft.Restier.Docs/guides/extending-restier/temporal-types.mdx
```

- [ ] **Step 4: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
```

- [ ] **Step 5: Commit**

```bash
git add src/Microsoft.Restier.Docs/guides/extending-restier/temporal-types.mdx
git commit -m "docs: convert extending-restier/temporal-types.md → mdx

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>"
```

---

### Task 25: Copy release notes and add `release-notes/index.md`

**Files:**
- Create: `src/Microsoft.Restier.Docs/release-notes/index.md`
- Create: `src/Microsoft.Restier.Docs/release-notes/0-3-0-beta1.md`
- Create: `src/Microsoft.Restier.Docs/release-notes/0-3-0-beta2.md`
- Create: `src/Microsoft.Restier.Docs/release-notes/0-4-0-rc.md`
- Create: `src/Microsoft.Restier.Docs/release-notes/0-4-0-rc2.md`
- Create: `src/Microsoft.Restier.Docs/release-notes/0-5-0-beta.md`

Release notes are pure prose; no conversion. Just copy.

- [ ] **Step 1: Create directory and copy verbatim**

```bash
mkdir -p src/Microsoft.Restier.Docs/release-notes
cp docs/msdocs/release-notes/0-3-0-beta1.md src/Microsoft.Restier.Docs/release-notes/0-3-0-beta1.md
cp docs/msdocs/release-notes/0-3-0-beta2.md src/Microsoft.Restier.Docs/release-notes/0-3-0-beta2.md
cp docs/msdocs/release-notes/0-4-0-rc.md src/Microsoft.Restier.Docs/release-notes/0-4-0-rc.md
cp docs/msdocs/release-notes/0-4-0-rc2.md src/Microsoft.Restier.Docs/release-notes/0-4-0-rc2.md
cp docs/msdocs/release-notes/0-5-0-beta.md src/Microsoft.Restier.Docs/release-notes/0-5-0-beta.md
```

- [ ] **Step 2: Verify the five files match**

```bash
for f in 0-3-0-beta1 0-3-0-beta2 0-4-0-rc 0-4-0-rc2 0-5-0-beta; do
  diff "docs/msdocs/release-notes/$f.md" "src/Microsoft.Restier.Docs/release-notes/$f.md" >/dev/null && echo "OK: $f" || echo "MISMATCH: $f"
done
```

Expected: five `OK:` lines.

- [ ] **Step 3: Create the new `release-notes/index.md`**

```bash
cat > src/Microsoft.Restier.Docs/release-notes/index.md <<'EOF'
---
title: "Release Notes"
description: "Restier release history and notable changes"
icon: "clipboard-list"
sidebarTitle: "Overview"
---

## Release Notes

This section lists notable changes for each Restier release. Pages are listed newest-first.
EOF
```

- [ ] **Step 4: Verify the index**

```bash
cat src/Microsoft.Restier.Docs/release-notes/index.md
```

Expected: shows the frontmatter and the brief intro.

- [ ] **Step 5: Build**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -10
```

- [ ] **Step 6: Commit**

```bash
git add src/Microsoft.Restier.Docs/release-notes/
git commit -m "$(cat <<'EOF'
docs: import release notes from feature/vnext + add index page

Five release notes copied verbatim (.md → .md, no conversion). New
release-notes/index.md provides the entry page for the nav group.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 4 — Navigation update

### Task 26: Update navigation in `.docsproj` (and `docs.json` if hand-maintained)

**Files:**
- Modify: `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`
- Modify (conditional): `src/Microsoft.Restier.Docs/docs.json` — only if Task 7 found `docs.json` is hand-maintained.

The current `<MintlifyTemplate>` block reflects main's structure (with `Providers`, `Learnings`, only 4 server pages, etc.). Replace it with feature/vnext's structure.

- [ ] **Step 1: Read the current `<MintlifyTemplate>` block**

```bash
grep -n -A50 '<MintlifyTemplate>' src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj | head -80
```

- [ ] **Step 2: Replace the `<MintlifyTemplate>` block**

Use `Edit` to replace the entire `<MintlifyTemplate>` ... `</MintlifyTemplate>` block (preserve `<Name>`, `<Theme>`, and `<Colors>` from main). Replace ONLY the `<Navigation>` block. The new navigation:

```xml
<Navigation Mode="Unified">
    <Pages>
        <Groups>
            <Group Name="Getting Started" Icon="stars">
                <Pages>index;why-restier;quickstart;contribution-guidelines</Pages>
            </Group>
            <Group Name="Guides" Icon="dog-leashed">
                <Pages>guides/index</Pages>
                <Group Name="Server" Icon="server">
                    <Pages>
                        guides/server/model-building;
                        guides/server/method-authorization;
                        guides/server/filters;
                        guides/server/interceptors;
                        guides/server/operations;
                        guides/server/swagger;
                        guides/server/testing;
                        guides/server/naming-conventions;
                        guides/server/concurrency;
                        guides/server/performance;
                    </Pages>
                </Group>
                <Group Name="Extending Restier" Icon="puzzle">
                    <Pages>
                        guides/extending-restier/in-memory-provider;
                        guides/extending-restier/temporal-types;
                    </Pages>
                </Group>
                <Group Name="Clients" Icon="laptop-code">
                    <Pages>
                        guides/clients/dot-net;
                        guides/clients/dot-net-standard;
                        guides/clients/typescript;
                    </Pages>
                </Group>
            </Group>
            <Group Name="Release Notes" Icon="clipboard-list">
                <Pages>
                    release-notes/index;
                    release-notes/0-5-0-beta;
                    release-notes/0-4-0-rc2;
                    release-notes/0-4-0-rc;
                    release-notes/0-3-0-beta2;
                    release-notes/0-3-0-beta1;
                </Pages>
            </Group>
        </Groups>
    </Pages>
</Navigation>
```

- [ ] **Step 3: Verify XML is well-formed**

```bash
xmllint --noout src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj && echo "OK"
```

If `xmllint` is unavailable, rely on Step 5 build.

- [ ] **Step 4: Verify all 22 nav targets are present (sanity)**

```bash
grep -oE 'guides/server/[a-z-]+|guides/extending-restier/[a-z-]+|guides/clients/[a-z-]+|release-notes/[0-9a-z-]+|index|why-restier|quickstart|contribution-guidelines' src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj | sort -u | wc -l
```

Expected: 22 unique nav targets (10 server + 2 extending + 3 clients + 6 release-notes + index, why-restier, quickstart, contribution-guidelines, guides/index — count varies based on grep dedup; just inspect the list to confirm all 10 server pages, both extending pages, three clients, six release-notes, and four root-level pages appear).

- [ ] **Step 5: Build to confirm the nav references resolve**

```bash
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj 2>&1 | tail -20
```

Expected: `Build succeeded`. If the SDK warns about a missing target page (e.g., a typo in a slug), fix and rebuild.

- [ ] **Step 6: If Task 7 determined `docs.json` is hand-maintained, mirror the structure there**

Only do this step if Task 7 step 3 showed `diff` was empty (no auto-regeneration). Use `Edit` to update `src/Microsoft.Restier.Docs/docs.json` so its `navigation.pages` array matches the structure above. Then verify both files describe the same navigation:

```bash
# Sanity: same number of leaf pages on both sides.
grep -oE 'guides/server/[a-z-]+' src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj | sort -u | wc -l
grep -oE 'guides/server/[a-z-]+' src/Microsoft.Restier.Docs/docs.json | sort -u | wc -l
```

Expected: both report `10`.

If Task 7 showed the SDK regenerates `docs.json`, **skip this step** — the build already wrote the new `docs.json`.

- [ ] **Step 7: Commit**

```bash
git add src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
# Only add docs.json if you edited it manually (Task 7 said hand-maintained).
git add src/Microsoft.Restier.Docs/docs.json 2>/dev/null || true
git commit -m "$(cat <<'EOF'
docs: update navigation for feature/vnext content set

Drops Providers and Learnings groups (placeholder scaffolding never
finished on main). Adds a Release Notes group. Server group lists all
10 pages. Extending Restier drops additional-operations (superseded by
server/operations). Clients group keeps main's three stub pages.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 5 — Solution and project integration

### Task 27: Add the docsproj to `RESTier.slnx` under a `/docs/` solution folder

**Files:**
- Modify: `RESTier.slnx`

- [ ] **Step 1: Read the current slnx**

```bash
cat RESTier.slnx
```

Note the existing solution-folder shape (e.g., `<Folder Name="/src/" Id="…">`). The slnx schema uses `<Folder>` and `<Project>` elements.

- [ ] **Step 2: Add a `/docs/` folder containing the docsproj**

Use `Edit` to insert this block. Place it after the `/src/Web/` folder block and before the `/test/` folder block (matches the logical flow: source → docs → tests):

```xml
  <Folder Name="/docs/">
    <Project Path="src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj" />
  </Folder>
```

- [ ] **Step 3: Verify the slnx is well-formed XML**

```bash
xmllint --noout RESTier.slnx && echo "OK"
```

- [ ] **Step 4: Verify the docsproj is now in the solution**

```bash
dotnet sln RESTier.slnx list 2>&1 | grep -i 'Restier.Docs' || echo "MISSING"
```

Expected: shows `src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj`. If "MISSING", re-check Step 2.

- [ ] **Step 5: Commit**

```bash
git add RESTier.slnx
git commit -m "$(cat <<'EOF'
docs: add Microsoft.Restier.Docs to RESTier.slnx under /docs/ folder

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 28: Verify build ordering from a fully clean state

**Files:**
- (verifies, no edits)

- [ ] **Step 1: Wipe all build output**

```bash
dotnet clean RESTier.slnx 2>&1 | tail -5
git clean -fdX -- 'src/**/bin' 'src/**/obj' 2>&1 | tail -5
```

Note the `-X` (uppercase) only removes gitignored files (bin/obj). This will NOT touch `api-reference/` even though it's also gitignored — it's an intentional regenerated dir under `Microsoft.Restier.Docs/`. To be safe, rebuild also overwrites it.

- [ ] **Step 2: Confirm bin/obj are gone**

```bash
find src -type d -name bin -o -type d -name obj | head -10
```

Expected: empty output (no bin/obj dirs).

- [ ] **Step 3: Build the solution from clean**

```bash
dotnet build RESTier.slnx 2>&1 | tail -30
```

Expected: `Build succeeded`. If the build fails because the docsproj couldn't find a referenced DLL, it means the `<ProjectReference>` wiring from Task 3 is incomplete. Diagnose and fix the docsproj.

- [ ] **Step 4: Build under parallel MSBuild**

```bash
dotnet clean RESTier.slnx 2>&1 | tail -3
dotnet build RESTier.slnx -m 2>&1 | tail -30
```

Expected: `Build succeeded` again. If doc generation races ahead of `Microsoft.Restier.Core` build completion, the dependency wiring is incomplete (likely an SDK quirk where `<ProjectReference>` doesn't establish the build-graph edge for doc generation). Fall back to MSBuild item-driven integration per Phase 1, step 4 in the spec.

- [ ] **Step 5: No commit (this task only verifies)**

---

## Phase 6 — Cleanup

### Task 29: Delete `docs/msdocs/` and the legacy docfx/mkdocs scaffolding

**Files:**
- Delete: `docs/msdocs/` (recursive)
- Delete: `docs/mkdocs.yml`
- Delete: `docs/CODEOWNERS`
- Delete: `docs/README.md`

- [ ] **Step 1: Sanity-check what gets removed**

```bash
ls -la docs/
find docs/msdocs -type f | wc -l
```

Expected: shows `msdocs/`, `mkdocs.yml`, `CODEOWNERS`, `README.md`, `superpowers/`. The `find` should report `21+` files (the `_site/` build output adds more).

- [ ] **Step 2: Confirm `docs/superpowers/` is the only thing we keep**

```bash
ls docs/superpowers/ | head -20
```

Expected: a `plans/` and `specs/` directory.

- [ ] **Step 3: Delete the legacy directories and files**

```bash
git rm -rf docs/msdocs/
git rm docs/mkdocs.yml docs/CODEOWNERS docs/README.md
```

- [ ] **Step 4: Verify only `docs/superpowers/` remains under `docs/`**

```bash
ls docs/
```

Expected: shows only `superpowers/`. (And maybe a stray `_site/` if it existed on disk — see step 5.)

- [ ] **Step 5: Remove any untracked leftovers (e.g., `_site/`)**

```bash
ls docs/_site 2>/dev/null && rm -rf docs/_site
ls docs/
```

Expected: `superpowers/` only.

- [ ] **Step 6: Commit**

```bash
git commit -m "$(cat <<'EOF'
docs: remove legacy docs/msdocs and docfx/mkdocs scaffolding

Content has been migrated to src/Microsoft.Restier.Docs/. Also drops
docs/mkdocs.yml (legacy mkdocs config), docs/CODEOWNERS (eight-line
file from 2019), and docs/README.md (referenced the old docfx setup).
docs/superpowers/ (specs/plans) is preserved.

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

### Task 30: Update `CLAUDE.md` Documentation section

**Files:**
- Modify: `CLAUDE.md`

- [ ] **Step 1: Locate the current Documentation section**

```bash
grep -n '## Documentation' CLAUDE.md
sed -n '/## Documentation/,/^## /p' CLAUDE.md | head -30
```

Note the shape: it documents the docfx/`docs/msdocs/build.sh` flow that no longer exists.

- [ ] **Step 2: Replace the Documentation section**

Use `Edit` to replace the entire `## Documentation` block. The new content should describe the DotNetDocs flow. Use this template (adjust based on what Task 7 found about `docs.json` regeneration):

```markdown
## Documentation

Documentation lives in `src/Microsoft.Restier.Docs/` and is built with the **DotNetDocs SDK** (`<Project Sdk="DotNetDocs.Sdk/1.2.0">`), which generates Mintlify-flavored MDX.

```bash
# Build the docs project (regenerates api-reference/ and docs.json)
dotnet build src/Microsoft.Restier.Docs/Microsoft.Restier.Docs.docsproj
```

The docs project is part of `RESTier.slnx`, so a full solution build also builds the docs:

```bash
dotnet build RESTier.slnx
```

**Authoring conventions:**
- Hand-written content lives under `guides/`, `release-notes/`, and the project root (`index.mdx`, `quickstart.mdx`, etc.).
- API reference under `api-reference/` is auto-generated from XML doc comments and gitignored — do NOT hand-edit it.
- Pages use Mintlify components: `<Info>`, `<Note>`, `<Tip>`, `<Warning>`, `<Steps>`, `<CodeGroup>`, `<Tabs>`, `<CardGroup>`. See existing pages for examples.

**Navigation source of truth:** Pick ONE of the two paragraphs below based on Task 7's finding, and keep only that one in the final CLAUDE.md (delete the other).

- *If Task 7 showed the SDK regenerates docs.json:* Navigation is defined ONLY in the `<MintlifyTemplate>` block of `Microsoft.Restier.Docs.docsproj`. The `docs.json` file is regenerated by the SDK on build — do not hand-edit it.
- *If Task 7 showed docs.json is hand-maintained:* Navigation must be kept in sync between the `<MintlifyTemplate>` block of `Microsoft.Restier.Docs.docsproj` and `docs.json`. Both files matter.
```

- [ ] **Step 3: Verify the section reads correctly**

```bash
sed -n '/^## Documentation/,/^## /p' CLAUDE.md | head -40
```

Expected: shows the new Documentation section, ending at the next `## ` heading.

- [ ] **Step 4: Confirm no stale references to `docs/msdocs` remain**

```bash
grep -n 'msdocs\|docfx\|mkdocs' CLAUDE.md || echo "OK: no stale references"
```

Expected: `OK: no stale references`.

- [ ] **Step 5: Commit**

```bash
git add CLAUDE.md
git commit -m "$(cat <<'EOF'
docs: update CLAUDE.md Documentation section for DotNetDocs

Replaces the docfx/docs/msdocs/build.sh instructions with the
DotNetDocs build flow and authoring conventions. Notes which file is
the navigation source of truth (per Phase 2 finding).

Co-Authored-By: Claude Opus 4.7 (1M context) <noreply@anthropic.com>
EOF
)"
```

---

## Phase 7 — Final verification

### Task 31: Final cross-cutting verification

**Files:**
- (verifies, no edits)

- [ ] **Step 1: Full clean build of the solution**

```bash
dotnet clean RESTier.slnx 2>&1 | tail -5
git clean -fdX -- 'src/**/bin' 'src/**/obj' 2>&1 | tail -5
dotnet build RESTier.slnx 2>&1 | tail -20
```

Expected: `Build succeeded` from a single invocation, no priming build needed.

- [ ] **Step 2: Verify api-reference regenerated and matches feature/vnext**

```bash
ls src/Microsoft.Restier.Docs/api-reference/Microsoft/Restier/ 2>&1
find src/Microsoft.Restier.Docs/api-reference -name '*.mdx' | wc -l
# No stale Microsoft.Restier.AspNet directory should be present (it was removed from feature/vnext).
ls src/Microsoft.Restier.Docs/api-reference/Microsoft/Restier/AspNet 2>&1 | head -5
```

Expected: api-reference exists; mdx count is in the hundreds; the `AspNet/` (without "Core") directory does NOT exist.

- [ ] **Step 3: No broken absolute-root links anywhere in the new project**

```bash
grep -rnE '\]\(/(server|extending-restier|clients)/' src/Microsoft.Restier.Docs/ || echo "OK: no broken absolute-root links"
```

Expected: `OK: no broken absolute-root links`.

- [ ] **Step 4: No leftover `.md`/`.mdx` extensions in internal links**

```bash
grep -rnE '\]\([^)]+\.mdx?[\)#]' src/Microsoft.Restier.Docs/ --include='*.mdx' --include='*.md' || echo "OK: no extension-bearing links"
```

Expected: `OK: no extension-bearing links` (note: this scans both .md and .mdx authored files; the api-reference/ tree may legitimately have its own internal linking style — if it shows hits, inspect to confirm they're SDK-generated and OK).

- [ ] **Step 5: `docs/msdocs/` is gone; `docs/superpowers/` is intact**

```bash
test ! -e docs/msdocs && echo "OK: msdocs gone"
test -d docs/superpowers/specs && test -d docs/superpowers/plans && echo "OK: superpowers intact"
ls docs/
```

Expected: both `OK:` lines; `ls docs/` shows only `superpowers/`.

- [ ] **Step 6: All 21 source `.md` files have a counterpart in the new project**

```bash
# Each source path should map to either a new .mdx or a copied .md.
for src in $(find docs/msdocs -name '*.md' 2>/dev/null); do
  echo "--- source missing? You deleted msdocs in Phase 6, so this loop is intentionally empty"
done
# Instead, verify the destinations exist:
for path in \
  src/Microsoft.Restier.Docs/index.mdx \
  src/Microsoft.Restier.Docs/quickstart.mdx \
  src/Microsoft.Restier.Docs/contribution-guidelines.mdx \
  src/Microsoft.Restier.Docs/license.md \
  src/Microsoft.Restier.Docs/why-restier.mdx \
  src/Microsoft.Restier.Docs/guides/index.mdx \
  src/Microsoft.Restier.Docs/guides/server/{model-building,method-authorization,filters,interceptors,operations,swagger,testing,naming-conventions,concurrency,performance}.mdx \
  src/Microsoft.Restier.Docs/guides/extending-restier/{in-memory-provider,temporal-types}.mdx \
  src/Microsoft.Restier.Docs/guides/clients/{dot-net,dot-net-standard,typescript}.mdx \
  src/Microsoft.Restier.Docs/release-notes/index.md \
  src/Microsoft.Restier.Docs/release-notes/{0-3-0-beta1,0-3-0-beta2,0-4-0-rc,0-4-0-rc2,0-5-0-beta}.md ; do
  test -f "$path" && echo "OK: $path" || echo "MISSING: $path"
done | grep -v '^OK:' | head -10
```

Expected: empty output (no `MISSING:` lines).

- [ ] **Step 7: Spot-check a converted page renders cleanly**

If the SDK exposes a Mintlify dev preview, run it and click through. Otherwise:

```bash
# Verify a representative converted page has the expected shape.
head -10 src/Microsoft.Restier.Docs/guides/server/filters.mdx
grep -nE '<(Info|Note|Warning|Tip|Steps|CodeGroup|Tabs|CardGroup)' src/Microsoft.Restier.Docs/guides/server/filters.mdx | head -5
```

Expected: frontmatter present; at least one Mintlify component appears (the source has callouts and a `<Steps>` candidate).

- [ ] **Step 8: No commit (this task only verifies)**

If everything passes, the migration is complete and ready for PR review.

---

## Self-review notes (for plan author)

Spec coverage check (each spec section maps to one or more tasks):

- Phase 1, step 1 (scaffold import) → Task 1
- Phase 1, step 3 (assembly-list) → Task 2
- Phase 1, step 4 (ProjectReferences) → Task 3
- Phase 1 + Phase 2 step 4 (gitignore api-reference) → Task 4
- Phase 2, step 1-2 (restore gate) → Task 5
- Phase 2, step 3-4 (build, api-reference verify) → Task 6
- Phase 2, step 5 (docs.json regeneration determination) → Task 7
- Phase 3 (content conversion, 15 prose files + 5 release notes + 1 stub) → Tasks 8-25
- Phase 4 (nav update + why-restier placeholder) → Task 12 (placeholder), Task 26 (nav)
- Phase 5 (slnx integration + clean-build verification) → Tasks 27, 28
- Phase 6 (cleanup + CLAUDE.md) → Tasks 29, 30
- Phase 7 (final verification) → Task 31

All scope items in the spec are covered.

Risks check:
- "Doc generation runs before referenced DLLs exist" → covered by Tasks 3 (ProjectReferences) and 28 (clean + parallel build verify).
- "assembly-list.txt carries main's stale set" → Task 2.
- "Old absolute-root links survive" → per-task grep checks (8-24) + Task 31 step 3.
- "docs.json silently drifts" → Task 7 + Task 26 step 6 + Task 30 (CLAUDE.md note).
- "SDK not publicly restorable" → Task 5 (probe-then-stop fallback).
