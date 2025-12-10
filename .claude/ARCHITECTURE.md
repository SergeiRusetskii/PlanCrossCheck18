# ARCHITECTURE — PlanCrossCheck

*Code structure and architecture documentation*

---

## Overview

PlanCrossCheck is a C# Eclipse Scripting API (ESAPI) plugin that provides comprehensive quality assurance validation for radiation therapy treatment plans in Varian Eclipse treatment planning system.

**Tech Stack:**
- C# / .NET Framework 4.8
- WPF (Windows Presentation Foundation) for UI
- Varian Eclipse Scripting API (ESAPI) v16.1+
- Target Platform: x64 (required by ESAPI)

---

## Directory Structure

```
PlanCrossCheck/
├── Script.cs                       # ESAPI plugin entry point
├── Validators.cs                   # Validation engine implementation
├── ValidationViewModel.cs          # MVVM view model
├── MainControl.xaml                # WPF UI markup
├── MainControl.xaml.cs             # WPF UI code-behind
├── SeverityToColorConverter.cs     # WPF value converter
│
├── .claude/                        # Framework files
│   ├── SNAPSHOT.md                 # Project snapshot
│   ├── BACKLOG.md                  # Current sprint tasks
│   ├── ROADMAP.md                  # Strategic roadmap
│   ├── IDEAS.md                    # Ideas & experiments
│   └── ARCHITECTURE.md             # This file
│
├── Documentation/                  # ESAPI Reference Documentation
│   ├── VMS.TPS.Common.Model.API.xml      # API reference (519KB)
│   │                                      # Contains: AddOn, ApplicationPackage,
│   │                                      # Beam, Course, Dose, Image, Patient,
│   │                                      # Plan, Structure, StructureSet, etc.
│   │
│   └── VMS.TPS.Common.Model.Types.xml    # Types reference (196KB)
│                                          # Contains: BeamNumber, DoseValue,
│                                          # DVHPoint, VVector, etc.
│
├── Final Script/                   # Version history
│   ├── Script V1.0/
│   ├── Script V1.2/
│   ├── Script V1.3/
│   └── Release 1.5.2 - z_Air sampling/
│
└── bin/Release/                    # Build output
    └── TEST_Cross_Check.esapi.dll  # Plugin DLL for Eclipse
```

---

## Key Components

### 1. Script.cs
**Location:** `Script.cs`
**Purpose:** ESAPI plugin entry point

- Implements ESAPI `IScriptObject` interface
- `Execute()` method called by Eclipse when script runs
- Creates main WPF window with `MainControl` UserControl
- Passes `ScriptContext` to ViewModel for validation
- Window title shows version (currently v1.6.0)

**ESAPI Integration:**
- Receives `ScriptContext context` parameter
- Access to: `context.Patient`, `context.Course`, `context.PlanSetup`

### 2. Validators.cs
**Location:** `Validators.cs`
**Purpose:** Composite pattern validation engine

**Class Hierarchy:**
```
ValidatorBase (abstract)
├── CompositeValidator (abstract)
│   └── RootValidator
│       ├── PlanParametersValidator
│       ├── DoseCalculationValidator
│       ├── StructureValidator
│       ├── BeamValidator
│       └── [other category validators]
└── [Individual validators]
    ├── PrescriptionValidator
    ├── DoseGridValidator
    ├── TargetCoverageValidator
    ├── AirDensityValidator
    ├── PTVBodyProximityValidator
    └── [etc.]
```

**Key Methods:**
- `Validate(ScriptContext context)` - Abstract method all validators implement
- `AddValidator(ValidatorBase)` - CompositeValidator method to build hierarchy
- Validators return `List<ValidationResult>`

**Validation Categories:**
- Plan Parameters
- Dose Calculations
- Structure Validation
- Beam Configuration
- [others as needed]

### 3. ValidationViewModel.cs
**Location:** `ValidationViewModel.cs`
**Purpose:** MVVM view model for validation UI

**Key Properties:**
- `ObservableCollection<ValidationResult> Results` - Binds to UI
- Grouped by category for display

**Key Classes:**
- `ValidationResult`:
  - `string Message` - Validation message
  - `Severity Severity` - Error/Warning/Info enum
  - `string Category` - Groups results in UI

**Execution Flow:**
1. ViewModel receives `ScriptContext`
2. Creates `RootValidator` instance
3. Calls `RootValidator.Validate(context)`
4. Populates `Results` collection
5. UI updates automatically via data binding

### 4. MainControl.xaml/.cs
**Location:** `MainControl.xaml`, `MainControl.xaml.cs`
**Purpose:** WPF UserControl displaying validation results

**UI Features:**
- Grouped list display (by category)
- Color-coded by severity (uses `SeverityToColorConverter`)
- Scrollable results area
- Auto-updates via MVVM binding

**Data Binding:**
```xaml
ItemsSource="{Binding Results}"
```

### 5. SeverityToColorConverter.cs
**Location:** `SeverityToColorConverter.cs`
**Purpose:** WPF value converter for severity-to-color mapping

**Mapping:**
- `Severity.Error` → Red
- `Severity.Warning` → Orange/Yellow
- `Severity.Info` → Green/Blue

**Implementation:**
- Implements `IValueConverter`
- Used in XAML data bindings

---

## Architecture Patterns

### Composite Pattern
**Usage:** Validator hierarchy

**Benefits:**
- Easy to add new validators
- Validators can be nested (category → individual checks)
- Uniform interface via `ValidatorBase.Validate()`
- Results aggregate up the tree

**Example:**
```csharp
var root = new RootValidator();
var planParams = new PlanParametersValidator();
planParams.AddValidator(new PrescriptionValidator());
planParams.AddValidator(new DoseGridValidator());
root.AddValidator(planParams);
```

### MVVM (Model-View-ViewModel)
**Usage:** UI architecture

**Components:**
- **Model:** ESAPI objects (`Plan`, `Dose`, `Structure`, etc.)
- **View:** `MainControl.xaml` (WPF UI)
- **ViewModel:** `ValidationViewModel` (orchestrates validation, exposes results)

**Benefits:**
- Testable validation logic
- UI updates automatically
- Separation of concerns

---

## Data Flow

```
Eclipse TPS
    ↓
[User selects "Run Script"]
    ↓
Script.Execute(ScriptContext context)
    ↓
MainWindow created
    ↓
ValidationViewModel created
    ↓
RootValidator.Validate(context)
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
Results → ObservableCollection
    ↓
WPF UI updates via binding
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

### ESAPI Reference Documentation

**Location:** `/Documentation/`

#### VMS.TPS.Common.Model.API.xml (519KB)
**Content:** API class documentation

**Key classes documented:**
- `AddOn` - Beam modifying devices (blocks, MLC, wedges, etc.)
- `Beam` - Treatment beam configuration
- `Course` - Treatment course
- `Dose` - Dose distribution and calculations
- `Image` - CT/MR imaging data
- `Patient` - Patient information
- `PlanSetup` - Treatment plan
- `Structure` - Contoured anatomical structures
- `StructureSet` - Collection of structures

**Usage:** Reference when using ESAPI classes

#### VMS.TPS.Common.Model.Types.xml (196KB)
**Content:** Value types and enums

**Key types documented:**
- `BeamNumber` - Unique beam identifier
- `DoseValue` - Dose with units (Gy, cGy, etc.)
- `DVHPoint` - Dose-volume histogram point
- `VVector` - 3D vector (positions, directions)
- Various enums (TreatmentOrientation, DoseValuePresentation, etc.)

**Usage:** Reference when working with ESAPI value types

### Using Context7 for ESAPI

When implementing new validators or working with ESAPI:

1. **For API usage:** Query Context7 MCP with specific ESAPI questions
   - Example: "How to access dose matrix in ESAPI?"
   - Example: "ESAPI structure volume calculation"

2. **For code examples:** Request ESAPI code snippets
   - Example: "ESAPI C# example for DVH calculation"

3. **For best practices:** Ask about patterns and performance
   - Example: "ESAPI best practices for dose voxel iteration"

---

## External Dependencies

### Required References
- `VMS.TPS.Common.Model.API.dll` - Core ESAPI API
- `VMS.TPS.Common.Model.Types.dll` - ESAPI value types
- `PresentationCore.dll` - WPF core
- `PresentationFramework.dll` - WPF framework
- `WindowsBase.dll` - WPF base

### NuGet Packages
- None (uses framework libraries only)

---

## Configuration

### Environment
- **IDE:** Visual Studio 2017 or later
- **Target Framework:** .NET Framework 4.8
- **Platform:** x64 (REQUIRED - ESAPI is x64 only)
- **Output Type:** Class Library (DLL)

### Build Configuration
**Release Build:**
```bash
msbuild PlanCrossCheck.sln /p:Configuration=Release /p:Platform=x64
```

**Debug Build:**
```bash
msbuild PlanCrossCheck.sln /p:Configuration=Debug /p:Platform=x64
```

**Output:** `TEST_Cross_Check.esapi.dll`

### Deployment
1. Build project → generates `.esapi.dll`
2. Copy DLL to Eclipse plugin directory
3. Restart Eclipse treatment planning system
4. Script appears in Scripts menu

---

## Testing Strategy

### Current State
- Manual testing in Eclipse environment
- Test with sample clinical plans

### Planned
- Unit tests for individual validators
- Mock ESAPI context for testing
- Integration tests with anonymized plan data
- Performance benchmarks for large plans

---

## Performance Considerations

### ESAPI Performance
- **Dose matrix access:** Use `GetVoxels()` efficiently (large data)
- **Structure iteration:** Cache structure lists
- **DVH calculations:** Consider pre-calculated DVH data

### Validation Performance
- Validators run sequentially (could parallelize in future)
- Lazy evaluation where possible
- Early exit on critical errors

---

## Extension Points

### Adding New Validators

1. **Create validator class:**
   ```csharp
   public class MyNewValidator : ValidatorBase
   {
       public override List<ValidationResult> Validate(ScriptContext context)
       {
           // Implementation
       }
   }
   ```

2. **Add to appropriate category validator:**
   ```csharp
   categoryValidator.AddValidator(new MyNewValidator());
   ```

3. **Set category in results:**
   ```csharp
   new ValidationResult
   {
       Message = "...",
       Severity = Severity.Warning,
       Category = "My Category"
   }
   ```

### Adding New Validation Categories

1. Create new `CompositeValidator` subclass
2. Add child validators to it
3. Add to `RootValidator` in `Script.cs`

---

## Deployment

### Installation
1. Build solution in Release configuration
2. Locate `TEST_Cross_Check.esapi.dll` in `bin/Release/`
3. Copy to Eclipse plugin directory (typically `C:\Program Files (x86)\Varian\Vision\<version>\ExternalBeam\Plugins\`)
4. Restart Eclipse

### Execution
- Open plan in Eclipse
- Scripts menu → TEST_Cross_Check
- Validation runs automatically
- Results display in popup window

---

*This architecture enables extensible, maintainable validation of treatment plans using ESAPI*
