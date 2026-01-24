# ClinicH - Eclipse 16.1 Variant

## Overview

This folder contains the ClinicH variant of PlanCrossCheck, designed for Eclipse 16.1 with .NET 4.6.1.

**Status:** Placeholder - awaiting user files

## Configuration

- **Eclipse Version:** 16.1
- **.NET Framework:** 4.6.1
- **Target Platform:** x64
- **Machines:** TrueBeam STX

## Setup Instructions

This is an independent project, completely separate from the root ClinicE variant.

### To populate this folder:

1. Copy your working ClinicH project files from your clinical environment
2. Ensure you include:
   - `.csproj` file
   - `.sln` file (or reuse root solution)
   - `Script.cs`
   - `MainControl.xaml` / `MainControl.xaml.cs`
   - `Properties/AssemblyInfo.cs`
   - `Validators/` folder with all your validators

3. Update ESAPI references to point to Eclipse 16.1 installation

### Building

```bash
msbuild YourProject.csproj /p:Configuration=Release /p:Platform=x64
```

## Porting Features from ClinicE

Since ClinicH is completely independent, you can manually copy validators and features from the root ClinicE project as needed:

1. Review ClinicE validator implementation (root `Validators/` folder)
2. Copy relevant validator to `ClinicH/Validators/`
3. Adapt for Eclipse 16.1 API differences if needed
4. Test in Eclipse 16.1 environment

## Validator Backups

Recent ClinicH validators created during the multi-clinic experiment are backed up in:
- `/backup/ClinicH-new-validators/CollisionValidator.cs`
- `/backup/ClinicH-new-validators/OptimizationValidator.cs`
- `/backup/ClinicH-new-validators/UserOriginMarkerValidator.cs`

These can be integrated into your ClinicH project as needed.

## Notes

- This folder is **completely independent** from root ClinicE
- No shared code between clinics (except `.claude/` framework files)
- ClinicE (root) is the proven clinical version (v1.8.3)
- Copy features from ClinicE manually as needed

---

*For ClinicE documentation, see root README.md*
