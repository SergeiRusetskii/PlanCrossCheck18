# Developer Guide — Adding New Validators

*PlanCrossCheck ESAPI Plugin*
*Framework: Claude Code Starter v2.1*

---

## Quick Start

This guide explains how to add new validation checks to the PlanCrossCheck plugin.

---

## Step 1: Choose Validator Location

Decide where your validator belongs in the hierarchy:

```
RootValidator
├── CourseValidator ← Course-level checks
└── PlanValidator ← Plan-level checks
    ├── CTAndPatientValidator ← CT/imaging checks
    ├── DoseValidator ← Dose calculation checks
    ├── FieldsValidator ← Beam/field checks
    │   ├── FieldNamesValidator
    │   ├── GeometryValidator
    │   └── SetupFieldsValidator
    ├── ReferencePointValidator ← Prescription/reference point checks
    ├── FixationValidator ← Fixation device checks
    ├── OptimizationValidator ← Optimization parameter checks
    ├── PlanningStructuresValidator ← Planning structure checks
    └── PTVBodyProximityValidator ← Structure geometry checks
```

---

## Step 2: Create Validator Class

### Option A: Simple Validator

For standalone validation logic:

```csharp
public class MyNewValidator : ValidatorBase
{
    public override IEnumerable<ValidationResult> Validate(ScriptContext context)
    {
        var results = new List<ValidationResult>();

        // Your validation logic here
        if (context.PlanSetup != null)
        {
            bool isValid = /* your check */;

            results.Add(CreateResult(
                "Category Name",              // Category for UI grouping
                isValid ? "Pass message"
                        : "Fail message",
                isValid ? ValidationSeverity.Info
                        : ValidationSeverity.Error  // or Warning
            ));
        }

        return results;
    }
}
```

### Option B: Composite Validator

For grouping related validators:

```csharp
public class MyCompositeValidator : CompositeValidator
{
    public MyCompositeValidator()
    {
        AddValidator(new ChildValidator1());
        AddValidator(new ChildValidator2());
        AddValidator(new ChildValidator3());
    }

    // Optionally override Validate() to add own logic
    public override IEnumerable<ValidationResult> Validate(ScriptContext context)
    {
        var results = new List<ValidationResult>();

        // Run child validators
        results.AddRange(base.Validate(context));

        // Add own validation
        // ...

        return results;
    }
}
```

---

## Step 3: Register Validator

Add your validator to the appropriate parent:

**In RootValidator (Validators.cs:325):**
```csharp
public RootValidator()
{
    AddValidator(new CourseValidator());
    AddValidator(new PlanValidator());
    // AddValidator(new YourNewValidator());  // If course-level
}
```

**In PlanValidator (Validators.cs:357):**
```csharp
public PlanValidator()
{
    AddValidator(new CTAndPatientValidator());
    AddValidator(new UserOriginMarkerValidator());
    // ... existing validators ...
    AddValidator(new YourNewValidator());  // Add here for plan-level
}
```

**In FieldsValidator (Validators.cs:941):**
```csharp
public FieldsValidator()
{
    AddValidator(new FieldNamesValidator());
    AddValidator(new GeometryValidator());
    AddValidator(new SetupFieldsValidator());
    // AddValidator(new YourNewValidator());  // If field-level
}
```

---

## Step 4: Validation Severity Guidelines

Choose appropriate severity for each result:

### **Info** (Green)
- Validation passed
- Everything is correct
- **Example:** "Dose grid size (0.2 cm) meets requirements"

### **Warning** (Yellow/Orange)
- Non-standard but acceptable
- Requires review but not critical
- Plan can proceed with caution
- **Example:** "Non-standard patient orientation (Prone)"

### **Error** (Red)
- Must be corrected before treatment
- Violates safety or protocol requirements
- Plan should NOT proceed
- **Example:** "Gating must be enabled for DIBH plan"

---

## Step 5: ESAPI Access Patterns

### Common ESAPI Properties

```csharp
// Course
string courseId = context.Course.Id;

// Plan
var plan = context.PlanSetup;
string orientation = plan.TreatmentOrientationAsString;
double dosePerFx = plan.DosePerFraction.Dose;  // in Gy
double totalDose = plan.TotalDose.Dose;
bool isGated = plan.UseGating;

// Beams
foreach (var beam in plan.Beams.Where(b => !b.IsSetupField))
{
    string beamId = beam.Id;
    string machineId = beam.TreatmentUnit.Id;
    string energy = beam.EnergyModeDisplayName;
    double doseRate = beam.DoseRate;

    double startAngle = beam.ControlPoints.First().GantryAngle;
    double endAngle = beam.ControlPoints.Last().GantryAngle;
    GantryDirection direction = beam.GantryDirection;
}

// Dose
double gridSize = plan.Dose.XRes / 10.0;  // mm to cm

// Structures
var body = context.StructureSet.Structures
    .FirstOrDefault(s => s.DicomType == "EXTERNAL");
var ptvs = context.StructureSet.Structures
    .Where(s => s.Id.StartsWith("PTV", StringComparison.OrdinalIgnoreCase));

// Image
var image = context.StructureSet.Image;
VVector userOrigin = image.UserOrigin;
double xRes = image.XRes;  // mm
int[,] voxelBuffer = new int[image.XSize, image.YSize];
image.GetVoxels(sliceIndex, voxelBuffer);

// Reference Points
var primaryRP = plan.PrimaryReferencePoint;
double totalDoseLimit = primaryRP.TotalDoseLimit.Dose;
```

### ESAPI Reference

Use the XML documentation files:
- `/Documentation/VMS.TPS.Common.Model.API.xml` - API classes
- `/Documentation/VMS.TPS.Common.Model.Types.xml` - Types/enums

For code examples:
```csharp
// Use Context7 MCP when implementing validators
// Example: "How to check if beam is arc in ESAPI?"
```

---

## Step 6: Null Safety

Always check for null before accessing ESAPI properties:

```csharp
// ✅ GOOD
if (context.PlanSetup?.Beams != null)
{
    foreach (var beam in context.PlanSetup.Beams)
    {
        // Safe to access beam properties
    }
}

// ✅ GOOD
var beams = context.PlanSetup?.Beams?.Where(b => !b.IsSetupField).ToList();
if (beams?.Any() == true)
{
    // Safe to use beams
}

// ❌ BAD - Can throw NullReferenceException
foreach (var beam in context.PlanSetup.Beams)
{
    // Unsafe if PlanSetup is null
}
```

---

## Step 7: Helper Methods

Use `CreateResult()` for consistency:

```csharp
protected ValidationResult CreateResult(
    string category,        // UI grouping category
    string message,         // User-visible message
    ValidationSeverity severity,  // Info/Warning/Error
    bool isFieldResult = false    // true if result is per-field
)
```

### Field-Specific Results

For validators that check each beam individually:

```csharp
foreach (var beam in plan.Beams.Where(b => !b.IsSetupField))
{
    bool isValid = /* check beam */;

    results.Add(CreateResult(
        "Field Geometry",
        $"Field '{beam.Id}': {(isValid ? "Pass" : "Fail")} message",
        isValid ? ValidationSeverity.Info : ValidationSeverity.Error,
        isFieldResult: true  // Enables result collapsing in UI
    ));
}
```

**UI Behavior:**
- If ALL fields pass (Info) → UI shows single summary: "All treatment fields passed Field Geometry checks"
- If ANY field fails → UI shows individual results for each field

---

## Step 8: Machine-Specific Validation

Use `PlanUtilities` helper methods:

```csharp
string machineId = plan.Beams.First().TreatmentUnit.Id;

if (PlanUtilities.IsEdgeMachine(machineId))
{
    // Edge-specific validation
    // machineId == "TrueBeamSN6368"
}
else if (PlanUtilities.IsHalcyonMachine(machineId))
{
    // Halcyon-specific validation
    // machineId starts with "Halcyon"
}

// Check if beam is arc
bool isArc = PlanUtilities.IsArcBeam(beam);

// Check if plan has couch rotation
bool hasCouch = PlanUtilities.HasAnyFieldWithCouch(plan.Beams);

// Check if SRS plan
bool isSRS = PlanUtilities.ContainsSRS(beam.Technique.ToString());
```

---

## Step 9: Testing

### Manual Testing

1. Build the plugin:
   ```bash
   msbuild PlanCrossCheck.sln /p:Configuration=Release /p:Platform=x64
   ```

2. Copy DLL to Eclipse plugins directory
3. Open test plan in Eclipse
4. Run script and verify:
   - Validation executes without errors
   - Results display correctly in UI
   - Messages are clear and actionable
   - Severity colors are appropriate

### Test Cases

Create test scenarios for:
- ✅ Pass case (Info)
- ⚠️ Warning case (Warning)
- ❌ Fail case (Error)
- 🔧 Edge cases (null values, empty collections)
- 🏥 Clinical edge cases (unusual but valid plans)

---

## Step 10: Documentation

Add XML documentation comments to your validator:

```csharp
/// <summary>
/// Validates [what your validator checks].
/// </summary>
/// <remarks>
/// <para><strong>Pass Criteria (Info):</strong></para>
/// <list type="bullet">
/// <item>[Condition 1 for passing]</item>
/// <item>[Condition 2 for passing]</item>
/// </list>
///
/// <para><strong>Warning Criteria:</strong></para>
/// <list type="bullet">
/// <item>[Condition for warning]</item>
/// </list>
///
/// <para><strong>Error Criteria:</strong></para>
/// <list type="bullet">
/// <item>[Condition for error]</item>
/// </list>
///
/// <para><strong>Clinical Significance:</strong></para>
/// <para>[Why this validation is important for patient safety]</para>
/// </remarks>
public class MyNewValidator : ValidatorBase
{
    // ...
}
```

---

## Example: Adding Fractionation Validator

Let's add a validator to check if fraction count is reasonable:

### 1. Create Validator Class

```csharp
/// <summary>
/// Validates fractionation scheme for plan.
/// </summary>
public class FractionationValidator : ValidatorBase
{
    public override IEnumerable<ValidationResult> Validate(ScriptContext context)
    {
        var results = new List<ValidationResult>();

        if (context.PlanSetup != null)
        {
            int numFractions = context.PlanSetup.NumberOfFractions ?? 0;
            double dosePerFx = context.PlanSetup.DosePerFraction.Dose;

            // Check 1: Fraction count reasonable
            bool isReasonable = numFractions >= 1 && numFractions <= 50;
            results.Add(CreateResult(
                "Fractionation",
                isReasonable
                    ? $"Number of fractions ({numFractions}) is within normal range"
                    : $"Number of fractions ({numFractions}) is unusual - verify prescription",
                isReasonable ? ValidationSeverity.Info : ValidationSeverity.Warning
            ));

            // Check 2: Hypofractionation warning
            if (dosePerFx >= 5.0 && numFractions < 5)
            {
                results.Add(CreateResult(
                    "Fractionation",
                    $"Hypofractionated plan detected: {dosePerFx:F1} Gy x {numFractions} fractions - verify protocol",
                    ValidationSeverity.Warning
                ));
            }

            // Check 3: Conventional fractionation
            bool isConventional = Math.Abs(dosePerFx - 2.0) < 0.5;
            if (isConventional)
            {
                results.Add(CreateResult(
                    "Fractionation",
                    $"Conventional fractionation: {dosePerFx:F1} Gy per fraction",
                    ValidationSeverity.Info
                ));
            }
        }

        return results;
    }
}
```

### 2. Register in PlanValidator

```csharp
public PlanValidator()
{
    AddValidator(new CTAndPatientValidator());
    AddValidator(new UserOriginMarkerValidator());
    AddValidator(new DoseValidator());
    AddValidator(new FractionationValidator());  // NEW
    AddValidator(new FieldsValidator());
    // ... rest of validators
}
```

### 3. Build and Test

```bash
msbuild PlanCrossCheck.sln /p:Configuration=Release /p:Platform=x64
```

Test with:
- Standard plan (2 Gy x 25)
- Hypofractionated plan (5 Gy x 5)
- SBRT plan (12 Gy x 3)
- Unusual plan (1 fraction, 50 fractions)

---

## Best Practices

### DO ✅

- **Always use ESAPI reference libraries (.xml)** - Check `/Documentation/VMS.TPS.Common.Model.API.xml` and `/Documentation/VMS.TPS.Common.Model.Types.xml` for available methods, properties, and types
- **Check for existing helper methods** - Before creating new utility methods, check if one already exists in `PlanUtilities.cs` (e.g., `IsHyperArc()`, `IsSRS()`, `IsEdgeMachine()`, `IsArcBeam()`)
- **Always null-check** before ESAPI access
- **Use clear, actionable messages** - tell user what to fix
- **Choose appropriate severity** - don't cry wolf with errors
- **Group related checks** in same category
- **Test with real clinical plans**
- **Document why validation matters** (clinical significance)
- **Use helper utilities** (PlanUtilities, CreateResult)
- **Follow existing patterns** in codebase

### DON'T ❌

- **Don't assume properties exist** - use null-coalescing (`?.`)
- **Don't use vague messages** - "Invalid plan" → "Course ID must start with RT_"
- **Don't over-use Error severity** - reserve for actual safety issues
- **Don't duplicate validation logic** - create helpers instead
- **Don't skip testing** - untested code will fail in production
- **Don't forget documentation** - XML comments help future developers
- **Don't access unvalidated data** - check nulls first

---

## Common Pitfalls

### 1. Null Reference Exceptions
```csharp
// ❌ WRONG
double angle = context.PlanSetup.Beams.First().ControlPoints.First().GantryAngle;

// ✅ CORRECT
double? angle = context.PlanSetup?.Beams?.FirstOrDefault()
    ?.ControlPoints?.FirstOrDefault()?.GantryAngle;
if (angle.HasValue)
{
    // Safe to use angle.Value
}
```

### 2. Empty Collections
```csharp
// ❌ WRONG
foreach (var beam in context.PlanSetup.Beams)

// ✅ CORRECT
if (context.PlanSetup?.Beams?.Any() == true)
{
    foreach (var beam in context.PlanSetup.Beams)
    {
        // Safe
    }
}
```

### 3. Unit Conversions
```csharp
// ESAPI units
double gridMM = context.PlanSetup.Dose.XRes;  // millimeters
double gridCM = gridMM / 10.0;  // convert to cm for display

VVector position = beam.IsocenterPosition;  // millimeters
double xCM = position.x / 10.0;  // convert to cm

double dose = plan.TotalDose.Dose;  // Gy (already in correct units)
```

---

## Resources

### Documentation
- **ARCHITECTURE.md** - Overall code structure
- **VALIDATORS_REFERENCE.md** - Detailed validator documentation
- **ESAPI_VALIDATION_REPORT.md** - ESAPI usage verification

### ESAPI Reference
- `/Documentation/VMS.TPS.Common.Model.API.xml`
- `/Documentation/VMS.TPS.Common.Model.Types.xml`
- Use Context7 MCP for code examples

### Build & Deploy
```bash
# Build
msbuild PlanCrossCheck.sln /p:Configuration=Release /p:Platform=x64

# Output
bin/Release/Cross_Check.esapi.dll

# Deploy (copy to Eclipse plugins directory)
# Restart Eclipse
```

---

## Need Help?

1. **Review existing validators** - See VALIDATORS_REFERENCE.md
2. **Check ESAPI docs** - XML files in /Documentation
3. **Use Context7 MCP** - For ESAPI code examples
4. **Test incrementally** - Build and test each step

---

## Managing TEST_ Prefix

### Purpose

The TEST_ prefix is used to identify development/testing versions of the plugin in Eclipse. This prevents confusion between production and test versions.

### Files to Update

When adding or removing the TEST_ prefix, update these files:

#### 1. PlanCrossCheck.csproj
**Location:** Line 11
```xml
<AssemblyName>TEST_Cross_Check.esapi</AssemblyName>
```
**Remove prefix:**
```xml
<AssemblyName>Cross_Check.esapi</AssemblyName>
```

#### 2. Properties/AssemblyInfo.cs
**Location:** Lines 8 and 12
```csharp
[assembly: AssemblyTitle("TEST_Cross Check")]
[assembly: AssemblyProduct("TEST_Cross_Check")]
```
**Remove prefix:**
```csharp
[assembly: AssemblyTitle("Cross Check")]
[assembly: AssemblyProduct("Cross_Check")]
```

#### 3. Script.cs
**Location:** Line 35 (window title)
```csharp
window.Title = "TEST_Cross-check v1.7.1";
```
**Remove prefix:**
```csharp
window.Title = "Cross-check v1.7.1";
```

### Workflow

**Development/Testing:**
- Use TEST_ prefix during development
- Build generates: `TEST_Cross_Check.esapi.dll`
- Eclipse shows: "TEST_Cross-check v1.7.1" in window title
- Clearly distinguishable from production version

**Production Release:**
- Remove TEST_ prefix from all 3 locations
- Update version number in AssemblyInfo.cs and Script.cs
- Build generates: `Cross_Check.esapi.dll`
- Eclipse shows: "Cross-check v1.7.1" in window title

### Quick Commands

**Add TEST_ prefix:**
```bash
# PlanCrossCheck.csproj line 11
<AssemblyName>TEST_Cross_Check.esapi</AssemblyName>

# Properties/AssemblyInfo.cs lines 8, 12
[assembly: AssemblyTitle("TEST_Cross Check")]
[assembly: AssemblyProduct("TEST_Cross_Check")]

# Script.cs line 35
window.Title = "TEST_Cross-check v1.7.1";
```

**Remove TEST_ prefix:**
```bash
# PlanCrossCheck.csproj line 11
<AssemblyName>Cross_Check.esapi</AssemblyName>

# Properties/AssemblyInfo.cs lines 8, 12
[assembly: AssemblyTitle("Cross Check")]
[assembly: AssemblyProduct("Cross_Check")]

# Script.cs line 35
window.Title = "Cross-check v1.7.1";
```

---

*Happy Validating!*
*Framework: Claude Code Starter v2.1*
