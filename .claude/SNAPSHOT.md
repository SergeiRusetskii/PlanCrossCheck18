# SNAPSHOT — PlanCrossCheck

*Framework: Claude Code Starter v2.1*
*Last updated: 2025-12-10*

---

## Current State

**Version:** v1.7.1 (TEST_)
**Status:** Active development - Quality assurance validation tool
**Branch:** main

---

## Project Overview

**Name:** PlanCrossCheck
**Description:** C# Eclipse Scripting API (ESAPI) plugin for Varian Eclipse treatment planning system that performs comprehensive quality assurance checks on radiation therapy treatment plans.

**Tech Stack:**
- C# / .NET Framework 4.8
- WPF (Windows Presentation Foundation)
- Varian Eclipse Scripting API (ESAPI) v16.1+
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
├── Script.cs                    # ESAPI plugin entry point
├── Validators.cs                # Composite validation engine
├── ValidationViewModel.cs       # MVVM view model for results
├── MainControl.xaml/.cs         # WPF UI for displaying results
├── SeverityToColorConverter.cs  # UI converter for severity colors
├── .claude/                     # Framework files
│   ├── SNAPSHOT.md             # This file
│   ├── BACKLOG.md              # Current sprint tasks
│   ├── ROADMAP.md              # Strategic planning
│   ├── IDEAS.md                # Ideas & experiments
│   └── ARCHITECTURE.md         # Code architecture
├── Documentation/
│   ├── VMS.TPS.Common.Model.API.xml    # ESAPI API reference
│   └── VMS.TPS.Common.Model.Types.xml  # ESAPI Types reference
└── Final Script/               # Version history folders
    ├── Script V1.0/
    ├── Script V1.2/
    ├── Script V1.3/
    └── Release 1.5.2/
```

---

## Recent Progress

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
- [x] Updated ReferencePointValidator to always show all dose checks (passes + errors) (2025-12-18)

---

## Active Work

**Phase 1: Framework Migration & Documentation**
- [x] Migrate to Claude Code Starter Framework v2.1
- [ ] Update documentation with ESAPI reference information
- [ ] Document existing validators and their validation logic
- [ ] Create developer guide for adding new validators

See [BACKLOG.md](./BACKLOG.md) for detailed task list.

---

## Next Steps

**Immediate:**
- Complete framework migration documentation
- Document all existing validators with examples
- Add XML documentation comments to code

**Short-term (v1.7.0):**
- Expand validation coverage (beam geometry, MLC positions)
- Enhanced error messages with recommendations
- Add configurable tolerance thresholds

**Long-term:**
- User configuration system (v2.0.0)
- PDF/CSV export capabilities
- Protocol compliance validation (v2.5.0)

See [ROADMAP.md](./ROADMAP.md) for full strategic plan.

---

## Key Concepts

### Validation Architecture
- **Composite Pattern:** Hierarchical validator structure with RootValidator orchestrating checks
- **ValidatorBase:** Abstract base class for all validators
- **CompositeValidator:** Base for validators containing child validators
- **ValidationResult:** Contains message, severity (Error/Warning/Info), and category

### ESAPI Integration
- Plugin DLL: `Cross_Check.esapi.dll`
- Requires x64 platform targeting
- Accessed via Eclipse Script menu
- Works with ScriptContext providing access to plan data

### Build Process
```bash
# Release build
msbuild PlanCrossCheck.sln /p:Configuration=Release /p:Platform=x64
```

---

## Reference Documentation

**ESAPI XML Documentation:**
- API Classes: [/Documentation/VMS.TPS.Common.Model.API.xml](../Documentation/VMS.TPS.Common.Model.API.xml)
- Types: [/Documentation/VMS.TPS.Common.Model.Types.xml](../Documentation/VMS.TPS.Common.Model.Types.xml)

**Use Context7 MCP:** For ESAPI code examples and best practices when implementing validators.

---

*Quick-start context for AI sessions*
