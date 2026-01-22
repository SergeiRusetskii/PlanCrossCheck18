# SNAPSHOT — PlanCrossCheck

*Framework: Claude Code Starter v2.5.1*
*Last updated: 2026-01-18*

---

## Current State

**Version:** 2.5.1 (Framework) | v1.8.3 (ClinicE) | v1.0.0.1 (ClinicH)
**Status:** Production - Multi-clinic quality assurance validation tool
**Branch:** main
**Architecture:** Multi-clinic variant structure

---

## Project Overview

**Name:** PlanCrossCheck
**Description:** C# Eclipse Scripting API (ESAPI) plugin for Varian Eclipse treatment planning system that performs comprehensive quality assurance checks on radiation therapy treatment plans.

**Architecture:** Multi-clinic variant structure with shared Core and clinic-specific Variants

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
├── Core/                           # SHARED BASE CLASSES
│   ├── Base/                       # Validation framework
│   │   ├── ValidationSeverity.cs
│   │   ├── ValidationResult.cs
│   │   ├── ValidatorBase.cs
│   │   └── CompositeValidator.cs
│   └── UI/                         # Shared WPF UI
│       ├── MainControl.xaml
│       ├── MainControl.xaml.cs
│       ├── SeverityToColorConverter.cs
│       └── ValidationViewModel.cs
│
├── Variants/
│   ├── ClinicE/                    # Eclipse 18.0 (.NET 4.8)
│   │   ├── ClinicE.csproj
│   │   ├── Script.cs
│   │   ├── Utilities/PlanUtilities.cs
│   │   └── Validators/             # 18 validators
│   │       └── [Edge & Halcyon validators]
│   │
│   └── ClinicH/                    # Eclipse 16.1 (.NET 4.6.1)
│       ├── ClinicH.csproj
│       ├── Script.cs
│       ├── Utilities/PlanUtilities.cs
│       └── Validators/             # 11 validators
│           └── [TrueBeam STX validators]
│
├── .claude/                        # Framework files
├── Documentation/                  # ESAPI reference docs
├── PlanCrossCheck.sln              # Solution (both variants)
└── MIGRATION_COMPLETE.md           # Migration record
```

---

## Recent Progress

### Architecture Migration (2026-01-18)
- [x] **Migrated to multi-clinic variant architecture**
- [x] Created Core/ with shared base classes and UI
- [x] Created Variants/ClinicE/ for Eclipse 18.0 (Edge & Halcyon)
- [x] Created Variants/ClinicH/ for Eclipse 16.1 (TrueBeam STX)
- [x] Split ClinicH monolithic Validators.cs into 11 modular validators
- [x] Updated solution file for both variants
- [x] Cleaned up old monolithic structure
- [x] Updated ARCHITECTURE.md for multi-clinic pattern
- [x] Added .DS_Store to .gitignore
- [x] **Created variant-specific README files**
- [x] Updated root README.md with high-level summary and variant comparison
- [x] Created Variants/ClinicE/README.md (386 lines - Eclipse 18.0 docs)
- [x] Created Variants/ClinicH/README.md (423 lines - Eclipse 16.1 docs)

### Previous Progress
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
- [x] Added TEST_ prefix management guide to DEVELOPER_GUIDE.md (2025-12-18)
- [x] Removed obsolete CLAUDE_project.md and FRAMEWORK_GUIDE.md (2025-12-18)
- [x] Added ESAPI reference guidelines to DEVELOPER_GUIDE.md (2025-12-18)
- [x] Implemented BeamEnergyValidator - checks all treatment fields use same energy (2025-12-18)
- [x] Enhanced FixationValidator for Edge machine - accepts Alta/Couch OR Encompass fixation (2025-12-18)
- [x] Added "Always Clarify Ambiguity" principle to CLAUDE.md (2025-12-18)
- [x] Merged Dose.Energy and Field Energy into Fields.Energy category (2025-12-18)
- [x] Moved Edge high-dose FFF energy check to BeamEnergyValidator (2025-12-18)
- [x] Implemented ContrastStructureValidator - checks for z_Contrast* when Study.Comment contains CONTRAST (2025-12-18)
- [x] Skip marker detection for Edge machine with Encompass fixation (2025-12-18)
- [x] Changed marker detection threshold from 2000HU to 500HU (2025-12-18)
- [x] Enhanced allPass summary messages with custom descriptions (2025-12-18)
- [x] Reverted ReferencePointValidator to combined message logic (2025-12-18)
- [x] **Release v1.8.0: Removed TEST_ prefix - Production ready** (2025-12-18)

---

## Active Work

**Current Phase: Multi-Clinic Architecture Validation**
- [ ] Build and test ClinicE variant in Eclipse 18.0
- [ ] Build and test ClinicH variant in Eclipse 16.1
- [ ] Verify all validators produce expected results
- [x] Update deployment documentation (completed in variant READMEs)

See [BACKLOG.md](./BACKLOG.md) for detailed task list.

---

## Next Steps

**Immediate:**
- Build and test both variants
- Verify deployment process
- ~~Update user documentation for variant selection~~ ✓ Completed

**Short-term:**
- Add third clinic variant if needed
- Enhance validator test coverage
- ~~Document variant-specific validation rules~~ ✓ Completed

**Long-term:**
- User configuration system per variant (v2.0.0)
- PDF/CSV export capabilities
- Protocol compliance validation (v2.5.0)

See [ROADMAP.md](./ROADMAP.md) for full strategic plan.

---

## Key Concepts

### Multi-Clinic Variant Architecture
- **Core/**: Shared validation framework and UI (linked, not duplicated)
- **Variants/**: Clinic-specific validators and machine logic
- **Benefits**: No code duplication, easy to add clinics, isolated clinic rules

### Validation Architecture
- **Composite Pattern:** Hierarchical validator structure with RootValidator orchestrating checks
- **ValidatorBase:** Abstract base class for all validators
- **CompositeValidator:** Base for validators containing child validators
- **ValidationResult:** Contains message, severity (Error/Warning/Info), and category

### ESAPI Integration
- Plugin DLL: `TEST_Cross_Check.esapi.dll` (ClinicE) or `PlanCrossCheck.dll` (ClinicH)
- Requires x64 platform targeting
- Accessed via Eclipse Script menu
- Works with ScriptContext providing access to plan data

### Build Process

**Build both variants:**
```bash
msbuild PlanCrossCheck.sln /p:Configuration=Release /p:Platform=x64
```

**Build ClinicE only:**
```bash
msbuild Variants/ClinicE/ClinicE.csproj /p:Configuration=Release /p:Platform=x64
```

**Build ClinicH only:**
```bash
msbuild Variants/ClinicH/ClinicH.csproj /p:Configuration=Release /p:Platform=x64
```

---

## Clinic Variants

### ClinicE (Eclipse 18.0)
- **Machines:** Varian Edge, Varian Halcyon
- **Version:** 1.8.3
- **Assembly:** `TEST_Cross_Check.esapi`
- **Validators:** 18 (incl. collision, optimization, PTV-Body proximity)

### ClinicH (Eclipse 16.1)
- **Machines:** TrueBeam STX (2 machines)
- **Version:** 1.0.0.1
- **Assembly:** `PlanCrossCheck`
- **Validators:** 11 (TrueBeam STX-specific)

---

## Reference Documentation

**ESAPI XML Documentation:**
- API Classes: [/Documentation/VMS.TPS.Common.Model.API.xml](../Documentation/VMS.TPS.Common.Model.API.xml)
- Types: [/Documentation/VMS.TPS.Common.Model.Types.xml](../Documentation/VMS.TPS.Common.Model.Types.xml)

**Use Context7 MCP:** For ESAPI code examples and best practices when implementing validators.

---

*Quick-start context for AI sessions*
