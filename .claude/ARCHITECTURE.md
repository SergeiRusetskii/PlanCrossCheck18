# ARCHITECTURE — PlanCrossCheck

*Multi-Clinic Variant Architecture Documentation*

---

## Overview

PlanCrossCheck is a C# Eclipse Scripting API (ESAPI) plugin that provides comprehensive quality assurance validation for radiation therapy treatment plans in Varian Eclipse treatment planning system.

The project uses a **multi-clinic variant architecture** where:
- **Core/** contains shared base classes and UI components
- **Variants/** contains clinic-specific implementations (ClinicE, ClinicH)

**Tech Stack:**
- C# / .NET Framework 4.6.1 - 4.8
- WPF (Windows Presentation Foundation) for UI
- Varian Eclipse Scripting API (ESAPI) v16.1 - 18.0
- Target Platform: x64 (required by ESAPI)

---

## Directory Structure

```
PlanCrossCheck/
├── Core/                           # SHARED BASE CLASSES
│   ├── Base/                       # Validation framework
│   │   ├── ValidationSeverity.cs   # Info/Warning/Error enum
│   │   ├── ValidationResult.cs     # Result data class
│   │   ├── ValidatorBase.cs        # Abstract base validator
│   │   └── CompositeValidator.cs   # Composite pattern base
│   └── UI/                         # Shared WPF UI components
│       ├── MainControl.xaml        # UI markup (linked by variants)
│       ├── MainControl.xaml.cs     # UI code-behind (linked by variants)
│       ├── SeverityToColorConverter.cs  # WPF value converter
│       └── ValidationViewModel.cs  # MVVM view model
│
├── Variants/
│   ├── ClinicE/                    # ECLIPSE 18.0 (.NET 4.8)
│   │   ├── ClinicE.csproj          # Project file (links to Core)
│   │   ├── Properties/
│   │   │   └── AssemblyInfo.cs     # Assembly metadata (v1.8.3)
│   │   ├── Script.cs               # ESAPI entry point
│   │   ├── Utilities/
│   │   │   └── PlanUtilities.cs    # IsEdgeMachine, IsHalcyonMachine
│   │   └── Validators/             # 18 clinic-specific validators
│   │       ├── RootValidator.cs
│   │       ├── CourseValidator.cs
│   │       ├── PlanValidator.cs
│   │       ├── CTAndPatientValidator.cs
│   │       ├── UserOriginMarkerValidator.cs
│   │       ├── DoseValidator.cs
│   │       ├── FieldsValidator.cs
│   │       ├── FieldNamesValidator.cs
│   │       ├── GeometryValidator.cs
│   │       ├── SetupFieldsValidator.cs
│   │       ├── BeamEnergyValidator.cs
│   │       ├── ReferencePointValidator.cs
│   │       ├── FixationValidator.cs
│   │       ├── CollisionValidator.cs
│   │       ├── OptimizationValidator.cs
│   │       ├── PlanningStructuresValidator.cs
│   │       ├── ContrastStructureValidator.cs
│   │       └── PTVBodyProximityValidator.cs
│   │
│   └── ClinicH/                    # ECLIPSE 16.1 (.NET 4.6.1)
│       ├── ClinicH.csproj          # Project file (links to Core)
│       ├── Properties/
│       │   └── AssemblyInfo.cs     # Assembly metadata (v1.0.0.1)
│       ├── Script.cs               # ESAPI entry point
│       ├── Utilities/
│       │   └── PlanUtilities.cs    # IsTrueBeamSTX
│       └── Validators/             # 11 clinic-specific validators
│           ├── RootValidator.cs
│           ├── CourseValidator.cs
│           ├── PlanValidator.cs
│           ├── CTAndPatientValidator.cs    # 5mm tolerance
│           ├── DoseValidator.cs            # TrueBeam STX energies
│           ├── FieldsValidator.cs
│           ├── FieldNamesValidator.cs
│           ├── GeometryValidator.cs
│           ├── SetupFieldsValidator.cs     # CBCT/SF_0/SF_270
│           ├── ReferencePointValidator.cs
│           └── FixationValidator.cs
│
├── .claude/                        # Framework files
│   ├── SNAPSHOT.md                 # Project snapshot
│   ├── BACKLOG.md                  # Current sprint tasks
│   ├── ROADMAP.md                  # Strategic roadmap
│   ├── IDEAS.md                    # Ideas & experiments
│   ├── ARCHITECTURE.md             # This file
│   └── DEVELOPER_GUIDE.md          # Guide for adding validators
│
├── Documentation/                  # ESAPI Reference Documentation
│   ├── VMS.TPS.Common.Model.API.xml
│   └── VMS.TPS.Common.Model.Types.xml
│
├── CLAUDE.md                       # AI Agent instructions
├── README.md                       # Project documentation
├── PlanCrossCheck.sln              # Solution file (both variants)
└── MIGRATION_COMPLETE.md           # Multi-clinic migration record
```

---

## Architecture Principles

### 1. Multi-Clinic Variant Pattern

**Problem:** Different clinics use different Eclipse versions, machine types, and validation rules.

**Solution:** Shared Core + Clinic-Specific Variants
- **Core/**: Shared validation framework and UI (linked, not duplicated)
- **Variants/**: Clinic-specific validators and machine logic

**Benefits:**
- No code duplication for common functionality
- Easy to add new clinic variants
- Clinic-specific rules isolated in their own folders
- Independent versioning per clinic

### 2. Composite Pattern for Validators

**Structure:**
```
ValidatorBase (abstract)
├── CompositeValidator (abstract)
│   ├── RootValidator
│   │   ├── CourseValidator
│   │   └── PlanValidator
│   │       ├── CTAndPatientValidator
│   │       ├── DoseValidator
│   │       ├── FieldsValidator
│   │       │   ├── FieldNamesValidator
│   │       │   ├── GeometryValidator
│   │       │   └── SetupFieldsValidator
│   │       └── [other validators...]
```

**Benefits:**
- Easy to add new validators
- Validators can be nested
- Uniform interface via `ValidatorBase.Validate()`
- Results aggregate up the tree

### 3. MVVM Pattern for UI

**Components:**
- **Model:** ESAPI objects (`Plan`, `Dose`, `Structure`)
- **View:** `MainControl.xaml` (WPF UI)
- **ViewModel:** `ValidationViewModel` (orchestrates validation)

**Benefits:**
- Testable validation logic
- UI updates automatically
- Separation of concerns

---

## Clinic Variants

### ClinicE (Eclipse 18.0)

**Configuration:**
- Eclipse Version: 18.0
- .NET Framework: 4.8
- ESAPI: RTM\18.0
- Assembly: `TEST_Cross_Check.esapi`
- Version: 1.8.3

**Machine Types:**
- Varian Edge (TrueBeam)
- Varian Halcyon

**Key Features:**
- 18 validators
- Machine-specific validation (Edge vs Halcyon)
- User origin tolerance: 2mm (Edge), 5mm (Halcyon)
- Energies: 6X, 10X, 6X-FFF, 10X-FFF (Edge); 6X-FFF (Halcyon)
- Setup fields: CBCT + SF-0 (Edge); kVCBCT (Halcyon)
- Advanced features: Collision detection, PTV-Body proximity, Optimization checks

**Build Command:**
```bash
msbuild Variants/ClinicE/ClinicE.csproj /p:Configuration=Release /p:Platform=x64
```

### ClinicH (Eclipse 16.1)

**Configuration:**
- Eclipse Version: 16.1
- .NET Framework: 4.6.1
- ESAPI: RTM\16.1
- Assembly: `PlanCrossCheck`
- Version: 1.0.0.1

**Machine Types:**
- TrueBeam STX (2 machines)

**Key Features:**
- 11 validators
- TrueBeam STX-specific validation
- User origin tolerance: 5mm (all coordinates)
- Energies: 6X, 10X, 15X, 6X-FFF, 10X-FFF
- Setup fields: CBCT, SF_0, SF_270/90
- Setup field energy: 2.5X-FFF
- Dose rates: 1400 MU/min (6X-FFF), 2400 MU/min (10X-FFF), 600 MU/min (others)

**Build Command:**
```bash
msbuild Variants/ClinicH/ClinicH.csproj /p:Configuration=Release /p:Platform=x64
```

---

## Key Components

### 1. Core/Base Classes

#### ValidatorBase.cs
**Purpose:** Abstract base for all validators

**Key Method:**
```csharp
public abstract IEnumerable<ValidationResult> Validate(ScriptContext context);
```

**Helper Method:**
```csharp
protected ValidationResult CreateResult(string category, string message, ValidationSeverity severity)
```

#### CompositeValidator.cs
**Purpose:** Composite pattern implementation

**Key Features:**
- Maintains list of child validators
- `AddValidator()` to build hierarchy
- `Validate()` aggregates results from children

#### ValidationResult.cs
**Purpose:** Data class for validation results

**Properties:**
- `string Category` - Groups results in UI
- `string Message` - Validation message
- `ValidationSeverity Severity` - Error/Warning/Info
- `bool IsFieldResult` - For field-specific results
- `string AllPassSummary` - Summary when all pass

#### ValidationSeverity.cs
**Purpose:** Enum for result severity

**Values:**
- `Error` - Critical issues (red)
- `Warning` - Important issues (yellow)
- `Info` - Passed checks (green)

### 2. Core/UI Components

#### MainControl.xaml/.cs
**Purpose:** WPF UserControl displaying validation results

**Features:**
- Grouped list display (by category)
- Color-coded by severity
- Scrollable results area
- Auto-updates via MVVM binding

#### ValidationViewModel.cs
**Purpose:** MVVM view model for validation UI

**Key Properties:**
- `ObservableCollection<ValidationResult> ValidationResults`

**Key Methods:**
- Constructor creates `RootValidator` and runs validation
- `GetCategoryOrder()` defines display order
- Post-processing collapses field-level results when all pass

#### SeverityToColorConverter.cs
**Purpose:** WPF value converter for severity-to-color mapping

**Mapping:**
- `Error` → Red
- `Warning` → Orange/Yellow
- `Info` → Green/Blue

### 3. Variant-Specific Components

#### Script.cs (per variant)
**Purpose:** ESAPI plugin entry point

**Key Features:**
- Implements `IScriptObject` interface
- `Execute()` method called by Eclipse
- Creates main WPF window with `MainControl`
- Passes `ScriptContext` to ViewModel
- Window title shows version and clinic

#### PlanUtilities.cs (per variant)
**Purpose:** Clinic-specific helper methods

**ClinicE Methods:**
- `IsEdgeMachine(string machineId)` - Detects Edge machines
- `IsHalcyonMachine(string machineId)` - Detects Halcyon machines
- `IsArcBeam(Beam beam)` - Arc vs static beam detection
- `HasAnyFieldWithCouch(IEnumerable<Beam> beams)` - Couch rotation detection
- `ContainsSRS(Beam beam)` - SRS technique detection

**ClinicH Methods:**
- `IsTrueBeamSTX(string machineId)` - Detects TrueBeam STX
- `IsArcBeam(Beam beam)` - Arc vs static beam detection
- `HasAnyFieldWithCouch(IEnumerable<Beam> beams)` - Couch rotation detection
- `ContainsSRS(string technique)` - SRS technique detection

#### Validators (per variant)
**Purpose:** Clinic-specific validation rules

Each validator implements `ValidatorBase.Validate()` and returns clinic-specific validation results.

---

## Data Flow

```
Eclipse TPS
    ↓
[User selects "Run Script"]
    ↓
Script.Execute(ScriptContext context)  ← Variant-specific (ClinicE or ClinicH)
    ↓
MainWindow created
    ↓
ValidationViewModel created (from Core/UI)
    ↓
RootValidator.Validate(context)  ← Variant-specific
    ↓
[Composite validators execute children]
    ↓
Individual validators access:
  - context.Patient
  - context.PlanSetup
  - context.PlanSetup.Dose
  - context.PlanSetup.StructureSet
  - context.PlanSetup.Beams
    ↓
ValidationResult objects created
    ↓
Results → ObservableCollection (Core/UI)
    ↓
WPF UI updates via binding (Core/UI MainControl)
    ↓
User sees color-coded results
```

---

## ESAPI Integration

### Key ESAPI Namespaces
```csharp
using VMS.TPS.Common.Model.API;    // Core API classes
using VMS.TPS.Common.Model.Types;  // Value types (DoseValue, VVector, etc.)
```

### Common ESAPI Objects Used

**From ScriptContext:**
- `context.Patient` - Patient demographic and clinical data
- `context.Course` - Treatment course
- `context.PlanSetup` - Treatment plan (most used)

**From PlanSetup:**
- `plan.Dose` - Dose distribution matrix
- `plan.StructureSet` - Contoured structures
- `plan.Beams` - Treatment beam collection
- `plan.TotalDose` - Prescription dose
- `plan.NumberOfFractions` - Fractionation

**Key Methods:**
- `dose.GetVoxels()` - Access dose matrix data
- `structure.GetContoursOnImagePlane()` - Structure geometry
- `beam.ControlPoints` - Beam parameters at each control point

---

## Build Configuration

### Building Both Variants
```bash
msbuild PlanCrossCheck.sln /p:Configuration=Release /p:Platform=x64
```

### Building Individual Variants
**ClinicE:**
```bash
msbuild Variants/ClinicE/ClinicE.csproj /p:Configuration=Release /p:Platform=x64
```

**ClinicH:**
```bash
msbuild Variants/ClinicH/ClinicH.csproj /p:Configuration=Release /p:Platform=x64
```

### Project References

**ClinicE.csproj:**
- Target: .NET Framework 4.8
- ESAPI: `C:\Program Files (x86)\Varian\RTM\18.0\esapi\API\`
- Links: `..\..\Core\Base\*.cs` and `..\..\Core\UI\*.xaml/cs`

**ClinicH.csproj:**
- Target: .NET Framework 4.6.1
- ESAPI: `C:\Program Files (x86)\Varian\RTM\16.1\esapi\API\`
- Links: `..\..\Core\Base\*.cs` and `..\..\Core\UI\*.xaml/cs`

---

## Extension Points

### Adding a New Clinic Variant

1. **Create variant directory:**
   ```
   Variants/ClinicX/
   ```

2. **Create .csproj file:**
   - Copy from ClinicE or ClinicH
   - Update target framework and ESAPI version
   - Ensure Core files are linked (not copied)

3. **Create variant-specific files:**
   - `Script.cs` (update window title and assembly name)
   - `Properties/AssemblyInfo.cs` (update version)
   - `Utilities/PlanUtilities.cs` (machine detection logic)

4. **Create validators:**
   - Copy relevant validators from existing variant
   - Modify validation rules for clinic-specific requirements

5. **Add to solution:**
   - Update `PlanCrossCheck.sln` to include new project

### Adding New Validators to Existing Variant

1. **Create validator class in variant folder:**
   ```csharp
   namespace PlanCrossCheck
   {
       public class MyNewValidator : ValidatorBase
       {
           public override IEnumerable<ValidationResult> Validate(ScriptContext context)
           {
               var results = new List<ValidationResult>();
               // Implementation
               return results;
           }
       }
   }
   ```

2. **Add to appropriate composite validator:**
   ```csharp
   AddValidator(new MyNewValidator());
   ```

3. **Update .csproj:**
   ```xml
   <Compile Include="Validators\MyNewValidator.cs" />
   ```

---

## Deployment

### ClinicE Deployment
1. Build ClinicE project
2. Locate `TEST_Cross_Check.esapi.dll` in `Variants/ClinicE/Release/`
3. Copy to Eclipse 18.0 plugin directory
4. Restart Eclipse

### ClinicH Deployment
1. Build ClinicH project
2. Locate `PlanCrossCheck.dll` in `Variants/ClinicH/Release/`
3. Copy to Eclipse 16.1 plugin directory
4. Restart Eclipse

---

## Testing Strategy

### Manual Testing
- Test in respective Eclipse environments
- Use sample clinical plans for each machine type
- Verify all validators execute correctly

### Planned Improvements
- Unit tests for individual validators
- Mock ESAPI context for testing
- Integration tests with anonymized plan data
- Automated regression testing

---

*Multi-clinic architecture enables flexible, maintainable validation across different Eclipse versions and machine configurations*
