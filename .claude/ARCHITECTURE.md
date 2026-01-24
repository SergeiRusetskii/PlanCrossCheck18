# ARCHITECTURE — PlanCrossCheck

*Independent Two-Clinic Architecture Documentation*

---

## Overview

PlanCrossCheck is a C# Eclipse Scripting API (ESAPI) plugin that provides comprehensive quality assurance validation for radiation therapy treatment plans in Varian Eclipse treatment planning system.

The project uses an **independent two-clinic architecture** where:
- **ClinicE/** contains Eclipse 18.0 variant (complete standalone project)
- **ClinicH/** contains Eclipse 16.1 variant (complete standalone project)
- **No shared code** between clinics (zero dependencies)

**Tech Stack:**
- C# / .NET Framework 4.6.1 - 4.8
- WPF (Windows Presentation Foundation) for UI
- Varian Eclipse Scripting API (ESAPI) v16.1 - 18.0
- Target Platform: x64 (required by ESAPI)

---

## Directory Structure

```
PlanCrossCheck/
├── .claude/                        # Shared framework files only
├── backup/                         # Backup of experimental validators
│   └── ClinicH-new-validators/
├── ClinicE/                        # ECLIPSE 18.0 (.NET 4.8)
│   ├── Properties/
│   │   └── AssemblyInfo.cs         # v1.8.3
│   ├── Validators/                 # 18 modular validators
│   │   ├── Base/                   # Base classes & interfaces
│   │   │   ├── ValidationSeverity.cs
│   │   │   ├── ValidationResult.cs
│   │   │   ├── ValidatorBase.cs
│   │   │   └── CompositeValidator.cs
│   │   ├── Utilities/
│   │   │   └── PlanUtilities.cs    # IsEdgeMachine, IsHalcyonMachine
│   │   ├── RootValidator.cs
│   │   ├── CourseValidator.cs
│   │   ├── PlanValidator.cs
│   │   ├── CTAndPatientValidator.cs
│   │   ├── UserOriginMarkerValidator.cs
│   │   ├── DoseValidator.cs
│   │   ├── FieldsValidator.cs
│   │   ├── BeamEnergyValidator.cs
│   │   ├── FieldNamesValidator.cs
│   │   ├── OptimizationValidator.cs
│   │   ├── GeometryValidator.cs
│   │   ├── CollisionValidator.cs
│   │   ├── SetupFieldsValidator.cs
│   │   ├── FixationValidator.cs
│   │   ├── PlanningStructuresValidator.cs
│   │   ├── ContrastStructureValidator.cs
│   │   ├── PTVBodyProximityValidator.cs
│   │   └── ReferencePointValidator.cs
│   ├── MainControl.xaml            # WPF UI
│   ├── MainControl.xaml.cs
│   ├── SeverityToColorConverter.cs
│   ├── ValidationViewModel.cs
│   ├── Script.cs                   # ESAPI entry point
│   ├── PlanCrossCheck.csproj
│   └── PlanCrossCheck.sln
│
├── ClinicH/                        # ECLIPSE 16.1 (.NET 4.6.1)
│   ├── Properties/
│   │   └── AssemblyInfo.cs         # v1.0.0.1
│   ├── Validators.cs               # Monolithic validators (682 lines)
│   ├── MainControl.xaml            # WPF UI
│   ├── MainControl.xaml.cs
│   ├── SeverityToColorConverter.cs
│   ├── ValidationViewModel.cs
│   ├── Script.cs                   # ESAPI entry point
│   ├── PlanCrossCheck.csproj
│   ├── PlanCrossCheck.sln
│   └── README.md
│
├── Documentation/                  # ESAPI reference materials
└── README.md                       # Project overview
```

---

## Architecture Philosophy

### Independent Projects

Each clinic is a **completely independent project** with:
- ✅ Own .csproj and .sln files
- ✅ Own validators (modular or monolithic)
- ✅ Own base classes and utilities
- ✅ Own UI components
- ✅ Own build output
- ✅ Zero dependencies on other clinics

### Benefits

1. **Simplicity** - No shared code means no abstraction complexity
2. **Independence** - Changes to one clinic don't affect the other
3. **Clarity** - Each project is self-contained and easy to understand
4. **Safety** - Clinical versions remain untouched when working on other variants
5. **Portability** - Each clinic can be copied/forked independently

### Feature Porting

Since clinics are independent, features are ported **manually**:
1. Identify useful validator in ClinicE
2. Copy file to ClinicH
3. Adapt for Eclipse 16.1 API differences if needed
4. Test independently

---

## ClinicE Architecture (Eclipse 18.0)

### Version & Configuration

- **Eclipse:** 18.0
- **.NET Framework:** 4.8
- **Platform:** x64
- **Version:** v1.8.3
- **Status:** Production (proven clinical deployment)

### Machine Support

- **Varian Edge** (TrueBeam with imaging)
- **Varian Halcyon** (Ring gantry system)

### Validator Architecture

**Modular design** with 18 validators organized by validation domain:

1. **RootValidator** - Composite orchestrator
2. **CourseValidator** - Course ID format
3. **PlanValidator** - Plan type, approval status
4. **CTAndPatientValidator** - User origin, CT device
5. **UserOriginMarkerValidator** - Marker detection (500HU threshold)
6. **DoseValidator** - Grid resolution, dose coverage
7. **FieldsValidator** - Field configuration
8. **BeamEnergyValidator** - Energy consistency, FFF validation
9. **FieldNamesValidator** - Field naming conventions, HyperArc support
10. **OptimizationValidator** - Jaw tracking, arc spacing
11. **GeometryValidator** - Gantry, collimator, couch angles
12. **CollisionValidator** - Full 360° collision detection
13. **SetupFieldsValidator** - Setup field requirements
14. **FixationValidator** - Fixation device verification (Alta/Encompass)
15. **PlanningStructuresValidator** - Air structures, PRV structures
16. **ContrastStructureValidator** - Contrast structure validation
17. **PTVBodyProximityValidator** - PTV-to-Body distance checks
18. **ReferencePointValidator** - Reference point validation

### Base Classes

**Location:** `ClinicE/Validators/Base/`

- **ValidationSeverity** - Enum: Info, Warning, Error
- **ValidationResult** - Data class for validation messages
- **ValidatorBase** - Abstract base for all validators
- **CompositeValidator** - Base for hierarchical validators

### Utilities

**Location:** `ClinicE/Validators/Utilities/`

- **PlanUtilities** - Machine detection helpers
  - `IsEdgeMachine(machineId)`
  - `IsHalcyonMachine(machineId)`

### Build Output

```bash
cd ClinicE
msbuild PlanCrossCheck.csproj /p:Configuration=Release /p:Platform=x64
```

**Output:** `ClinicE/Release/TEST_Cross_Check.esapi.dll`

---

## ClinicH Architecture (Eclipse 16.1)

### Version & Configuration

- **Eclipse:** 16.1
- **.NET Framework:** 4.6.1
- **Platform:** x64
- **Version:** v1.0.0.1
- **Status:** Clinical

### Machine Support

- **TrueBeam STX** (2 machines)

### Validator Architecture

**Monolithic design** with all validators in single file:

**Location:** `ClinicH/Validators.cs` (682 lines)

Contains:
- PlanUtilities class
- All validator classes in single file
- Inline base classes and result structures

### Build Output

```bash
cd ClinicH
msbuild PlanCrossCheck.csproj /p:Configuration=Release /p:Platform=x64
```

**Output:** `ClinicH/Release/PlanCrossCheck.dll`

---

## UI Architecture (Both Clinics)

### WPF Components

Both clinics have identical UI structure (independent implementations):

**MainControl.xaml** - XAML markup
- TreeView for hierarchical validation results
- DataTemplate for validation items
- Severity-based styling

**MainControl.xaml.cs** - Code-behind
- UserControl initialization
- TreeView setup

**ValidationViewModel.cs** - MVVM ViewModel
- `ValidationResults` observable collection
- UI data binding

**SeverityToColorConverter.cs** - Value Converter
- Info → Blue
- Warning → Gold
- Error → Red

### Entry Point

**Script.cs** - ESAPI Entry Point
```csharp
public void Execute(ScriptContext context)
{
    var validator = new RootValidator();
    var results = validator.Validate(context);
    // Display results in MainControl
}
```

---

## Validation Result Flow

### ClinicE (Modular)

```
Script.cs
  └─> RootValidator (composite)
       ├─> CourseValidator
       ├─> PlanValidator
       ├─> FieldsValidator (composite)
       │    ├─> BeamEnergyValidator
       │    ├─> FieldNamesValidator
       │    └─> ...
       └─> [other validators]
            └─> List<ValidationResult>
                 └─> MainControl.xaml (TreeView binding)
```

### ClinicH (Monolithic)

```
Script.cs
  └─> RootValidator (from Validators.cs)
       ├─> [inline validator methods]
       └─> List<ValidationResult>
            └─> MainControl.xaml (TreeView binding)
```

---

## Key Design Patterns

### Composite Pattern (ClinicE)

**CompositeValidator** enables hierarchical validation:
- Parent validators orchestrate child validators
- Results are aggregated and categorized
- Enables modular organization

### Result Aggregation

**ValidationResult** structure:
```csharp
public class ValidationResult
{
    public string Message { get; set; }
    public ValidationSeverity Severity { get; set; }
    public string Category { get; set; }
}
```

### MVVM Pattern

**Separation of concerns:**
- Model: ValidationResult, ESAPI data
- ViewModel: ValidationViewModel
- View: MainControl.xaml

---

## ESAPI Integration

### Required References

Both projects reference:
- `VMS.TPS.Common.Model.API.dll`
- `VMS.TPS.Common.Model.Types.dll`

### Context Access

```csharp
public void Execute(ScriptContext context)
{
    var patient = context.Patient;
    var planSetup = context.PlanSetup;
    var course = context.Course;
    // Validation logic
}
```

### Common ESAPI Objects

- `Patient` - Patient demographics, structures
- `PlanSetup` - Plan configuration, beams, dose
- `Course` - Course information
- `Beam` - Individual beam parameters
- `Structure` - Structure set contours
- `Dose` - Dose distribution

---

## Build Configuration

### Both Clinics

- **Platform:** x64 (ESAPI requirement)
- **Configuration:** Release (for deployment)
- **Output:** ESAPI plugin DLL

### ClinicE Specific

- **Assembly:** `TEST_Cross_Check.esapi`
- **.NET:** 4.8
- **ESAPI:** 18.0

### ClinicH Specific

- **Assembly:** `PlanCrossCheck`
- **.NET:** 4.6.1
- **ESAPI:** 16.1

---

## Deployment

### Installation Steps

1. **Build** appropriate clinic variant
2. **Locate DLL** in Release folder
3. **Copy** to Eclipse plugin directory:
   - `C:\Program Files (x86)\Varian\RTM\[Version]\ExternalBeam\PlugIns\`
4. **Restart** Eclipse
5. **Access** via Scripts menu

### Version Management

Each clinic manages versions independently:
- **ClinicE:** Properties/AssemblyInfo.cs (v1.8.3)
- **ClinicH:** Properties/AssemblyInfo.cs (v1.0.0.1)

---

## Development Workflow

### Working on ClinicE

1. Navigate to `ClinicE/`
2. Make changes to validators
3. Build: `msbuild PlanCrossCheck.csproj`
4. Test in Eclipse 18.0
5. Commit changes

### Working on ClinicH

1. Navigate to `ClinicH/`
2. Make changes to validators
3. Build: `msbuild PlanCrossCheck.csproj`
4. Test in Eclipse 16.1
5. Commit changes

### Porting Features

To copy validator from ClinicE to ClinicH:
1. Review `ClinicE/Validators/[ValidatorName].cs`
2. Adapt code for Eclipse 16.1 API if needed
3. Integrate into `ClinicH/Validators.cs` or create new file
4. Test in ClinicH environment

---

## Testing Strategy

### Manual Testing

Both clinics require manual testing in Eclipse:
- Load test patient
- Open plan
- Run script from Scripts menu
- Verify validation results
- Check all severity levels

### Test Coverage

Validation areas:
- ✅ Course and plan metadata
- ✅ CT and patient setup
- ✅ Dose calculation
- ✅ Field configuration
- ✅ Beam geometry
- ✅ Collision detection
- ✅ Safety checks
- ✅ Clinical protocols

---

## Documentation References

### ESAPI Documentation

**Location:** `Documentation/`
- `VMS.TPS.Common.Model.API.xml` - API classes
- `VMS.TPS.Common.Model.Types.xml` - Types and enums
- `Eclipse Scripting API Reference Guide 18.0.pdf`

### Framework Documentation

**Location:** `.claude/`
- `SNAPSHOT.md` - Current project state
- `BACKLOG.md` - Current sprint tasks
- `ROADMAP.md` - Strategic direction
- `ARCHITECTURE.md` - This file

---

## Key Principles

1. **Clinical Safety First** - Proven versions remain unchanged
2. **Independence** - No dependencies between clinics
3. **Simplicity** - Each project self-contained
4. **Manual Porting** - Explicit feature copying
5. **Version Isolation** - Each clinic manages own version

---

*Architecture documentation for PlanCrossCheck independent two-clinic structure*
