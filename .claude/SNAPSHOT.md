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
├── ClinicE/                        # Eclipse 18.0 variant
│   ├── Properties/
│   ├── Validators/                 # 18 modular validators
│   │   ├── Base/
│   │   ├── Utilities/
│   │   └── [18 validator files]
│   ├── MainControl.xaml
│   ├── MainControl.xaml.cs
│   ├── PlanCrossCheck.csproj
│   ├── PlanCrossCheck.sln
│   ├── Script.cs
│   ├── SeverityToColorConverter.cs
│   └── ValidationViewModel.cs
├── ClinicH/                        # Eclipse 16.1 variant
│   ├── Properties/
│   ├── MainControl.xaml
│   ├── MainControl.xaml.cs
│   ├── PlanCrossCheck.csproj
│   ├── PlanCrossCheck.sln
│   ├── README.md
│   ├── Script.cs
│   ├── SeverityToColorConverter.cs
│   ├── ValidationViewModel.cs
│   ├── Validators/                 # 14 modular validators + 3 new
│   │   ├── Base/
│   │   ├── Utilities/
│   │   └── [14 validator files]
│   └── Validators.cs.backup        # Backup of monolithic version
├── Documentation/                  # ESAPI reference docs
└── README.md                       # Project overview
```

---

## Recent Progress

### ClinicH Modular Refactoring (2026-01-25)
- [x] **Refactored ClinicH to modular architecture** matching ClinicE structure
- [x] Split 682-line Validators.cs into 19 modular files
- [x] Created Validators/Base/ folder (4 base classes)
- [x] Created Validators/Utilities/ folder (PlanUtilities)
- [x] **Integrated 3 new validators from backup:**
  - CollisionValidator: Gantry clearance validation for TrueBeam STX
  - UserOriginMarkerValidator: Radiopaque marker detection (3 markers at user origin)
  - OptimizationValidator: Jaw Tracking validation
- [x] Updated PlanCrossCheck.csproj with 19 file references
- [x] Version bumped: v1.0.0.1 → v1.1.0.0
- [x] Original Validators.cs backed up as Validators.cs.backup

**Rationale:**
- Match ClinicE modular structure for consistency
- Enable easier maintenance (one validator per file)
- Add collision detection and marker validation features
- Simplify future validator additions

### Repository Structure Finalization (2026-01-24)
- [x] **Moved ClinicE v1.8.3 to ClinicE/ folder** for symmetry
- [x] **User added ClinicH clinical files** to ClinicH/ folder
- [x] Created backup/ folder for ClinicH experimental validators
- [x] Removed shared Core/ architecture
- [x] Removed Variants/ folder structure
- [x] Updated README.md (short summary format)
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

**Current Phase: ClinicH Modular Architecture Complete**
- [x] Restore ClinicE v1.8.3 to ClinicE/ folder
- [x] Create ClinicH/ independent folder
- [x] Backup experimental validators
- [x] User populated ClinicH/ with clinical files
- [x] Update documentation and framework files
- [x] Repository structure finalized
- [x] Refactor ClinicH to modular structure
- [x] Integrate 3 new validators into ClinicH
- [ ] Build ClinicH v1.1.0.0 in Eclipse 16.1
- [ ] Test ClinicH v1.1.0.0 in Eclipse 16.1
- [ ] Verify all 14 validators (11 original + 3 new) work correctly

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
- **ClinicE/ directory:** ClinicE v1.8.3 (proven clinical version)
- **ClinicH/ directory:** Independent project (clinical version)
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

### Build Process

**ClinicE:**
```bash
cd ClinicE
msbuild PlanCrossCheck.csproj /p:Configuration=Release /p:Platform=x64
```
Produces: `ClinicE/Release/TEST_Cross_Check.esapi.dll`

**ClinicH:**
```bash
cd ClinicH
msbuild PlanCrossCheck.csproj /p:Configuration=Release /p:Platform=x64
```
Produces: `ClinicH/Release/TEST_CrossCheck.esapi.dll`

**Deployment:**
Copy `TEST_CrossCheck.esapi.dll` to Eclipse script folder (typically `C:\Users\Public\Documents\Varian\Vision\16.1\ExternalBeam\Scripts\`)

---

## Clinic Variants

### ClinicE (ClinicE/ Directory)
- **Location:** ClinicE/ folder
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
- **Version:** v1.3.2.0
- **Assembly:** `TEST_CrossCheck.esapi.dll`
- **Window Title:** "Cross-check v1.3.2"
- **Validators:** Modular structure (18 files: 10 original + 3 new + 5 base/utility)
- **Status:** ✅ Bug fixes from Eclipse testing
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
