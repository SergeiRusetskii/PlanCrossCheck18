# BACKLOG — PlanCrossCheck

*Task tracking for PlanCrossCheck ESAPI plugin*

> **Planning:** For strategic roadmap see [ROADMAP.md](./ROADMAP.md), for ideas see [IDEAS.md](./IDEAS.md)

---

## Current Sprint: Multi-Clinic Architecture Validation

### Build & Test
- [ ] Build ClinicE variant successfully
- [ ] Build ClinicH variant successfully
- [ ] Test ClinicE in Eclipse 18.0 environment
- [ ] Test ClinicH in Eclipse 16.1 environment
- [ ] Verify all validators produce expected results

### Documentation
- [x] Update README.md with variant selection guide
- [x] Document deployment process for each variant
- [x] Create Variants/ClinicE/README.md (detailed Eclipse 18.0 docs)
- [x] Create Variants/ClinicH/README.md (detailed Eclipse 16.1 docs)
- [ ] Update version management for variants (pending after testing)

---

## Phase 1: Framework Migration & Documentation

### Current Sprint Tasks
- [x] Migrate to Claude Code Starter Framework v2.5.1
- [x] **Implement multi-clinic variant architecture**
- [x] **Create Core/ shared structure**
- [x] **Create Variants/ClinicE/**
- [x] **Create Variants/ClinicH/**
- [x] **Update ARCHITECTURE.md**
- [x] **Update SNAPSHOT.md**
- [ ] Document variant-specific validation rules
- [ ] Create guide for adding new clinic variants

### Code Quality
- [ ] Add XML documentation comments to validator classes
- [ ] Review and update error message clarity
- [ ] Verify x64 platform configuration is correct (both variants)
- [ ] Test plugin loading in Eclipse environment

### Testing & Validation
- [ ] Create test plan data set for validator testing
- [ ] Verify all existing validators work correctly
- [ ] Document expected vs actual behavior for edge cases
- [ ] Performance testing with large plans

---

## Completed

### Multi-Clinic Architecture Migration (2026-01-18)
- [x] **Migrated to multi-clinic variant architecture**
- [x] Created Core/Base/ with shared validator base classes
- [x] Created Core/UI/ with shared WPF UI components
- [x] Created Variants/ClinicE/ for Eclipse 18.0 (Edge & Halcyon)
- [x] Created Variants/ClinicH/ for Eclipse 16.1 (TrueBeam STX - Hadassah)
- [x] Split ClinicH monolithic Validators.cs (682 lines) into 11 modular validators
- [x] Updated solution file to include both variants
- [x] Cleaned up old monolithic structure
- [x] Deleted unnecessary files (.DS_Store, old backups, obsolete reference docs)
- [x] Added .DS_Store to .gitignore
- [x] Updated ARCHITECTURE.md for multi-clinic pattern
- [x] Updated SNAPSHOT.md with variant information
- [x] Created MIGRATION_COMPLETE.md documentation
- [x] **Created variant-specific README files**
- [x] Updated root README.md with multi-clinic architecture overview
- [x] Created Variants/ClinicE/README.md (386 lines - Eclipse 18.0 documentation)
- [x] Created Variants/ClinicH/README.md (423 lines - Eclipse 16.1 documentation)

### Recently Completed
- [x] Initialized Claude Code Starter Framework v2.1 (2025-12-10)
- [x] Created ROADMAP.md with strategic planning (2025-12-10)
- [x] Created IDEAS.md for idea tracking (2025-12-10)
- [x] Added ESAPI XML reference documentation (2025-12-10)

---

## Notes

### Multi-Clinic Variant Structure
- **Core/**: Shared validation framework and UI (linked, not copied)
- **Variants/**: Clinic-specific validators and machine logic
- **ClinicE**: Eclipse 18.0, .NET 4.8, Edge & Halcyon (18 validators)
- **ClinicH**: Eclipse 16.1, .NET 4.6.1, TrueBeam STX (11 validators)

### Development Environment
- Visual Studio with ESAPI references configured
- Target: .NET Framework 4.6.1 - 4.8, x64 platform
- Output:
  - ClinicE: `TEST_Cross_Check.esapi.dll`
  - ClinicH: `PlanCrossCheck.dll`

### ESAPI Reference
- API documentation: `/Documentation/VMS.TPS.Common.Model.API.xml`
- Types documentation: `/Documentation/VMS.TPS.Common.Model.Types.xml`
- Use Context7 MCP for ESAPI code examples and best practices

### Build Commands

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

*Use `/feature` command to plan new validators*
*Use `/fix` command to address validation bugs*
*Use `/test` command to create validator tests*
