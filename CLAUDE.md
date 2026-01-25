# CLAUDE.md - AI Agent Instructions

**Build/Test Ownership:** The user handles all builds and Eclipse testing; do not run tests or builds locally—just note required steps for them.

**Framework:** Claude Code Starter v2.5.1
**Type:** Meta-framework extending Claude Code capabilities

---

## Triggers

**"start", "начать":**
→ Execute Cold Start Protocol

**"заверши", "завершить", "finish", "done":**
→ Execute Completion Protocol

---

## Cold Start Protocol

### Step 0: First Launch Detection

**Check for migration context first:**
```bash
cat .claude/migration-context.json 2>/dev/null
```

If file exists, this is first launch after installation.

**Read context and route:**
- If `"mode": "legacy"` → Execute Legacy Migration workflow (see below)
- If `"mode": "upgrade"` → Execute Framework Upgrade workflow (see below)
- If `"mode": "new"` → Execute New Project Setup workflow (see below)

After completing workflow, delete marker:
```bash
rm .claude/migration-context.json
```

If no migration context, continue to Step 0.1 (Crash Recovery).

---

### Step 0.1: Crash Recovery
```bash
cat .claude/.last_session
```
- If `"status": "active"` → Previous session crashed:
  1. `git status` — check uncommitted changes
  2. Read `.claude/SNAPSHOT.md` for context
  3. Ask: "Continue or commit first?"
- If `"status": "clean"` → OK, continue to Step 1

### Step 1: Mark Session Active
```bash
echo '{"status": "active", "timestamp": "'$(date -Iseconds)'"}' > .claude/.last_session
```

### Step 2: Load Context
Read `.claude/SNAPSHOT.md` — current version, what's in progress

### Step 3: Context (on demand)
- `.claude/BACKLOG.md` — current sprint tasks (always read)
- `.claude/ROADMAP.md` — strategic direction (read to understand context)
- `.claude/ARCHITECTURE.md` — code structure (read if working with code)

### Step 4: Confirm
```
Context loaded. Directory: [pwd]
Framework: Claude Code Starter v2.5.1
Ready to work!
```

---

## Completion Protocol

### 1. Build (if code changed)
Note the build command for user:
```bash
msbuild PlanCrossCheck.sln /p:Configuration=Release /p:Platform=x64
```

### 2. Update Metafiles
- `.claude/BACKLOG.md` — mark completed tasks `[x]`
- `.claude/SNAPSHOT.md` — update version and status
- `CHANGELOG.md` — add entry (if release)
- `.claude/ARCHITECTURE.md` — update if code structure changed

### 3. Git Commit
```bash
git add -A && git status
git commit -m "$(cat <<'EOF'
type: Brief description

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
EOF
)"
```

### 4. Ask About Push & PR

**Push:**
- Ask user: "Push to remote?"
- If yes: `git push`

**Check PR status:**
```bash
git log origin/main..HEAD --oneline
```
- If **empty** → All merged, no PR needed
- If **has commits** → Ask: "Create PR?"

### 5. Mark Session Clean
```bash
echo '{"status": "clean", "timestamp": "'$(date -Iseconds)'"}' > .claude/.last_session
```

---

## Slash Commands

**Core:** `/fi`, `/commit`, `/pr`
**Dev:** `/fix`, `/feature`, `/review`, `/test`, `/security`
**Quality:** `/explain`, `/refactor`, `/optimize`
**Database:** `/db-migrate`
**Installation:** `/migrate-legacy`, `/upgrade-framework`

## Key Principles

1. **Framework as AI Extension** — not just docs, but functionality
2. **Privacy by Default** — dialogs private in .gitignore
3. **Local Processing** — no external APIs
4. **Token Economy** — minimal context loading
5. **Always Clarify Ambiguity** — when user requests are ambiguous or unclear, ALWAYS ask clarifying questions instead of guessing. Use AskUserQuestion tool to get specific details.

## Token Economy Rules

**ALWAYS prioritize framework files over project searches:**

1. **Before Grep/Glob:** Check if `.claude/ARCHITECTURE.md` has the answer
2. **Before reading code:** Check if `.claude/ARCHITECTURE.md` describes the logic
3. **For questions about "how X works":** Read framework files FIRST

**Search order:**
1. `.claude/ARCHITECTURE.md` — comprehensive code structure and logic descriptions
2. `.claude/BACKLOG.md` / `.claude/SNAPSHOT.md` — current context
3. Project files (only if framework files lack the answer)

**Why:** Framework files are curated, comprehensive, and token-efficient. Random project searches waste tokens.

## Version Management Rules

**CRITICAL: Eclipse Version Requirement**

Eclipse blocks launching scripts if they are changed without a version update. Follow these rules strictly:

### Auto-Version Bump Protocol

**CRITICAL: Eclipse blocks launching scripts if code changes without version update**

Eclipse tracks script versions and prevents execution if the code has changed but the version remains the same. This is a safety mechanism to ensure approved scripts haven't been modified.

**WHEN to bump version:**
- **AFTER making code changes AND user will test/launch in Eclipse**
- User provides feedback from script launch in Eclipse (runtime errors, validation issues, UI problems)
- User reports issues from actual Eclipse execution
- Any code changes followed by "test this in Eclipse" feedback
- Bug fixes discovered via Eclipse testing (increment patch version: X.Y.Z → X.Y.Z+1)
- New features added (increment minor version: X.Y.0 → X.Y+1.0)

**WHEN NOT to bump version:**
- User reports build errors (msbuild compilation errors) - these are compile-time, not runtime
- Code changes that won't be tested in Eclipse yet
- Documentation-only changes (README, CHANGELOG without code changes)
- User reports syntax errors before build
- Pure code review or refactoring without Eclipse testing
- Documentation-only changes

### Version Files to Update

**CRITICAL: When bumping version, update ALL THREE locations simultaneously:**

1. **Properties/AssemblyInfo.cs** (lines 32-33):
```csharp
[assembly: AssemblyVersion("X.Y.Z.0")]
[assembly: AssemblyFileVersion("X.Y.Z.0")]
```

2. **Script.cs** (window title):
```csharp
window.Title = "Cross-check vX.Y.Z";  // ClinicH format
// or
window.Title = "TEST_Cross-check vX.Y.Z";  // ClinicE format
```

3. **Framework Files:**
- `.claude/SNAPSHOT.md`: Update version in ClinicH/ClinicE section
- `CHANGELOG.md`: Add new version section with changes

**Version Format:** Use semantic versioning: `major.minor.patch`
- Patch version increments for bug fixes and small changes
- Minor version increments for new features
- Major version increments for breaking changes

### Example Workflow

```
User: "I tested v1.7.1 in Eclipse and it crashes when opening"
→ Bump to v1.7.2, fix bug, update both files

User: "Build failed with error CS1002"
→ DO NOT bump version, fix syntax error only

User: "v1.7.2 works but validation message is unclear"
→ Bump to v1.7.3, improve message, update both files
```

---

## Warnings

- DO NOT skip Crash Recovery check
- DO NOT commit without updating metafiles
- ALWAYS mark session clean at completion
- DO NOT search project files before reading framework files
- ALWAYS bump version when user provides Eclipse runtime feedback (unless build errors)

---

## Legacy Migration Protocol

**Triggered when:** `.claude/migration-context.json` exists with `"mode": "legacy"`

**Purpose:** Analyze existing project and generate Framework files.

**Workflow:**

1. **Read migration context:**
   ```bash
   cat .claude/migration-context.json
   ```

2. **Execute `/migrate-legacy` command:**
   - Follow instructions in `.claude/commands/migrate-legacy.md`
   - Discovery → Deep Analysis → Questions → Report → Generate Files

3. **After completion:**
   - Verify all Framework files created
   - Delete migration marker:
     ```bash
     rm .claude/migration-context.json
     ```
   - Show success summary

4. **Next session:**
   - Use normal Cold Start Protocol

---

## Framework Upgrade Protocol

**Triggered when:** `.claude/migration-context.json` exists with `"mode": "upgrade"`

**Purpose:** Migrate from old Framework version to current.

**Workflow:**

1. **Read migration context:**
   ```bash
   cat .claude/migration-context.json
   ```
   Extract `old_version` field.

2. **Execute `/upgrade-framework` command:**
   - Follow instructions in `.claude/commands/upgrade-framework.md`
   - Detect Version → Migration Plan → Backup → Execute → Verify

3. **After completion:**
   - Verify migration successful
   - Delete migration marker:
     ```bash
     rm .claude/migration-context.json
     ```
   - Show success summary

4. **Next session:**
   - Use normal Cold Start Protocol with new structure

---

## New Project Setup Protocol

**Triggered when:** `.claude/migration-context.json` exists with `"mode": "new"`

**Purpose:** Verify Framework installation and welcome user.

**Workflow:**

1. **Show welcome message:**
   ```
   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
   ✅ Installation complete!
   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

   📁 Framework Files Created:

     ✅ .claude/SNAPSHOT.md
     ✅ .claude/BACKLOG.md
     ✅ .claude/ROADMAP.md
     ✅ .claude/ARCHITECTURE.md
     ✅ .claude/IDEAS.md

   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

   🚀 Next Step:

     Type "start" to launch the framework.

   ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
   ```

2. **Delete migration marker:**
   ```bash
   rm .claude/migration-context.json
   ```

3. **Next session:**
   - Use normal Cold Start Protocol

---
*Framework: Claude Code Starter v2.5.1*
