# SNAPSHOT — PlanCrossCheck

*Framework: Claude Code Starter v2.5.1*
*Last updated: 2026-01-24*

---

## Current State

**Version:** v1.8.3 (ClinicE)
**Status:** Production - Conservative two-clinic structure
**Branch:** main
**Architecture:** Independent clinics (zero shared code)

---

## Project Overview

**Name:** PlanCrossCheck
**Description:** C# Eclipse Scripting API (ESAPI) plugin for Varian Eclipse treatment planning system that performs comprehensive quality assurance checks on radiation therapy treatment plans.

**Architecture:** Conservative two-clinic structure with completely independent projects

**Tech Stack:**
- C# / .NET Framework 4.6.1 - 4.8
- WPF (Windows Presentation Foundation)
- Varian Eclipse Scripting API (ESAPI) v16.1 - 18.0
  - VMS.TPS.Common.Model.API
  - VMS.TPS.Common.Model.Types
- Platform: x64

---

## Planning Documents

> **Planning:**
> - 🎯 Current sprint tasks: [BACKLOG.md](./BACKLOG.md)
> - 🗺️ Strategic roadmap: [ROADMAP.md](./ROADMAP.md)
> - 💡 Ideas & experiments: [IDEAS.md](./IDEAS.md)
> - 📊 Architecture & code structure: [ARCHITECTURE.md](./ARCHITECTURE.md)

---

## Current Structure

```
PlanCrossCheck/
├── .claude/                        # SHARED FRAMEWORK ONLY
├── backup/                         # Backup of experimental validators
│   └── ClinicH-new-validators/
├── ClinicH/                        # Independent ClinicH project
│   ├── README.md
│   └── .gitkeep                    # Placeholder for user files
├── Documentation/                  # ESAPI reference docs
├── Properties/                     # ClinicE assembly info
├── Validators/                     # ClinicE validators (18 files)
│   ├── Base/                       # Base classes & interfaces
│   ├── Utilities/                  # Helper utilities
│   └── [18 validator files]
├── MainControl.xaml                # ClinicE UI layout
├── MainControl.xaml.cs             # ClinicE UI code-behind
├── Script.cs                       # ClinicE entry point
├── SeverityToColorConverter.cs     # ClinicE UI converter
├── ValidationViewModel.cs          # ClinicE view model
├── PlanCrossCheck.csproj           # ClinicE project file
├── PlanCrossCheck.sln              # ClinicE solution
└── README.md                       # Two-clinic documentation
```

---

## Recent Progress

### Conservative Restoration (2026-01-24)
- [x] **Restored ClinicE v1.8.3 to root** from commit ccc4eb6
- [x] Created backup/ folder for ClinicH experimental validators
- [x] Removed shared Core/ architecture
- [x] Removed Variants/ folder structure
- [x] Created ClinicH/ as independent project folder
- [x] Updated README.md for two-clinic structure
- [x] Created git tag: pre-restoration-checkpoint

**Rationale:**
- Prioritize clinical safety with proven ClinicE version
- Eliminate debugging complexity from shared code
- Enable independent clinic development
- Simplify feature porting from ClinicE to ClinicH

### Architecture Migration (2026-01-18) - REVERTED
- ~~Migrated to multi-clinic variant architecture~~ → Reverted to conservative structure
- ~~Created Core/ with shared base classes and UI~~ → Removed for simplicity
- ~~Created Variants/ClinicE/ for Eclipse 18.0~~ → Moved to root
- ~~Created Variants/ClinicH/ for Eclipse 16.1~~ → Moved to independent folder

### Previous Progress (ClinicE v1.8.3)
- [x] Implemented composite validator pattern architecture
- [x] Created WPF UI with severity-based color coding
- [x] Added PTV-to-Body surface proximity check (v1.5.x)
- [x] Fixed critical Y voxel position scaling bug in air density validator
- [x] Migrated to Claude Code Starter Framework v2.1 (2025-12-10)
- [x] Refactored collision detection into separate CollisionValidator (2025-12-12)
- [x] Improved validation reporting with category ordering and message consolidation (2025-12-12)
- [x] Fixed HyperArc field naming validation (180.1→181, 179.9→179 mapping) (2025-12-12)
- [x] Fixed PTV-Body proximity to show all PTVs within threshold (2025-12-12)
- [x] Removed TEST_ prefix from assembly names (v1.7.0, 2025-12-12)
- [x] Re-added TEST_ prefix for development (v1.7.1, 2025-12-18)
- [x] Added auto-version-bump rules to CLAUDE.md (2025-12-18)
- [x] Implemented BeamEnergyValidator - checks all treatment fields use same energy (2025-12-18)
- [x] Enhanced FixationValidator for Edge machine - accepts Alta/Couch OR Encompass fixation (2025-12-18)
- [x] Implemented ContrastStructureValidator - checks for z_Contrast* when Study.Comment contains CONTRAST (2025-12-18)
- [x] Skip marker detection for Edge machine with Encompass fixation (2025-12-18)
- [x] Changed marker detection threshold from 2000HU to 500HU (2025-12-18)
- [x] Enhanced allPass summary messages with custom descriptions (2025-12-18)
- [x] **Release v1.8.0: Removed TEST_ prefix - Production ready** (2025-12-18)
- [x] **Release v1.8.3: Edge collision simplified to full 360° check** (2025-12-20)

---

## Active Work

**Current Phase: Conservative Restoration Complete**
- [x] Restore ClinicE v1.8.3 to root
- [x] Create ClinicH/ independent folder
- [x] Backup experimental validators
- [x] Update documentation
- [ ] User to populate ClinicH/ with their clinical files
- [ ] Verify ClinicE builds successfully
- [ ] Test ClinicE in Eclipse 18.0

See [BACKLOG.md](./BACKLOG.md) for detailed task list.

---

## Next Steps

**Immediate:**
- User copies working ClinicH project into ClinicH/ folder
- Build and test ClinicE to verify restoration
- Integrate backup validators into ClinicH if needed

**Short-term:**
- Port useful validators from ClinicE to ClinicH manually
- Enhance validator test coverage
- Document porting process

**Long-term:**
- User configuration system per clinic
- PDF/CSV export capabilities
- Protocol compliance validation

See [ROADMAP.md](./ROADMAP.md) for full strategic plan.

---

## Key Concepts

### Conservative Two-Clinic Architecture
- **Root directory:** ClinicE v1.8.3 (proven clinical version)
- **ClinicH/ directory:** Independent project (user-provided)
- **Zero shared code:** No dependencies between clinics
- **Manual porting:** Copy validators from ClinicE to ClinicH as needed
- **Shared framework only:** `.claude/` files are the only shared component

### Validation Architecture (ClinicE)
- **Composite Pattern:** Hierarchical validator structure with RootValidator orchestrating checks
- **ValidatorBase:** Abstract base class for all validators
- **CompositeValidator:** Base for validators containing child validators
- **ValidationResult:** Contains message, severity (Error/Warning/Info), and category

### ESAPI Integration
- Plugin DLL: `TEST_Cross_Check.esapi.dll` (ClinicE)
- Requires x64 platform targeting
- Accessed via Eclipse Script menu
- Works with ScriptContext providing access to plan data

### Build Process (ClinicE)

```bash
msbuild PlanCrossCheck.csproj /p:Configuration=Release /p:Platform=x64
```

Produces: `Release/TEST_Cross_Check.esapi.dll`

---

## Clinic Variants

### ClinicE (Root Directory)
- **Location:** Root directory
- **Machines:** Varian Edge, Varian Halcyon
- **Eclipse Version:** 18.0
- **.NET Framework:** 4.8
- **Version:** v1.8.3
- **Assembly:** `TEST_Cross_Check.esapi`
- **Validators:** 18 (incl. collision, optimization, PTV-Body proximity)
- **Status:** ✅ Production - proven clinical deployment
- **Commit:** ccc4eb6 (Dec 20, 2024)

### ClinicH (ClinicH/ Directory)
- **Location:** ClinicH/ folder
- **Machines:** TrueBeam STX (2 machines)
- **Eclipse Version:** 16.1
- **.NET Framework:** 4.6.1
- **Status:** 📁 Placeholder - awaiting user files
- **Independence:** Completely separate from ClinicE

---

## Backups

**Experimental validators saved in `/backup/ClinicH-new-validators/`:**
- CollisionValidator.cs (modified Jan 24)
- OptimizationValidator.cs (new)
- UserOriginMarkerValidator.cs (modified Jan 24)

These can be integrated into ClinicH project if needed.

**Safety checkpoint:**
- Git tag: `pre-restoration-checkpoint`
- Can be pushed to remote if needed

---

## Reference Documentation

**ESAPI XML Documentation:**
- API Classes: [/Documentation/VMS.TPS.Common.Model.API.xml](../Documentation/VMS.TPS.Common.Model.API.xml)
- Types: [/Documentation/VMS.TPS.Common.Model.Types.xml](../Documentation/VMS.TPS.Common.Model.Types.xml)

**Use Context7 MCP:** For ESAPI code examples and best practices when implementing validators.

---

*Quick-start context for AI sessions*
