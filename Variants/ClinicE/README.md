# PlanCrossCheck - ClinicE Variant

Quality assurance validation tool for Eclipse 18.0 with Varian Edge and Halcyon treatment machines.

> **⚠️ MEDICAL DISCLAIMER**: This software has not undergone FDA clearance. Users must validate performance in their clinical environment per institutional requirements.

---

## Variant Overview

**ClinicE** is designed for clinics using:
- **Eclipse Version**: 18.0
- **Machine Types**: Varian Edge (TrueBeam), Varian Halcyon
- **.NET Framework**: 4.8
- **ESAPI Version**: RTM\18.0
- **Platform**: Windows x64

**Current Version**: v1.8.3

---

## Features

### 18 Specialized Validators

#### 1. Course & Plan Validation
- **CourseValidator**: Validates course ID follows naming convention (RT[n]_*)
- **PlanValidator**: Orchestrates plan-level validation checks

#### 2. Patient & CT Validation
- **CTAndPatientValidator**:
  - User origin validation (2mm tolerance for Edge, 5mm for Halcyon)
  - CT device information verification
  - Machine-specific tolerance settings
- **UserOriginMarkerValidator**:
  - Detects CT markers (BB markers) with 500 HU threshold
  - Automatically skips for Edge machines with Encompass fixation

#### 3. Dose Calculation & Energy
- **DoseValidator**:
  - Dose grid resolution (≤1.25mm for SRS, ≤2.5mm standard)
  - SRS technique validation for ≥5Gy/fraction
  - Machine-specific dose rate validation
- **BeamEnergyValidator**:
  - Ensures all treatment fields use same energy
  - Edge FFF energy enforcement for high-dose plans (≥5Gy/fraction)

#### 4. Field Configuration
- **FieldsValidator**: Orchestrates field-level checks
- **FieldNamesValidator**:
  - Validates naming conventions (static/arc fields)
  - Special HyperArc handling (180.1°→181°, 179.9°→179°)
- **GeometryValidator**:
  - Collimator angle validation
  - Single-isocenter requirement for VMAT
  - MLC positioning checks
- **SetupFieldsValidator**:
  - Edge: CBCT + SF-0 (energy: 6X or 10X)
  - Halcyon: kVCBCT only

#### 5. Optimization
- **OptimizationValidator**:
  - Jaw tracking verification (VMAT plans)
  - Arc Spacing Control (ASC) validation

#### 6. Reference Points & Prescription
- **ReferencePointValidator**:
  - Reference point naming (RP_*)
  - Dose limits validation (Total+0.1, Fraction+0.1)
  - Prescription dose matching

#### 7. Fixation Devices
- **FixationValidator**:
  - **Edge**: Alta/Couch OR Encompass fixation systems
  - **Halcyon**: Alta/Couch fixation required
  - Density override validation

#### 8. Collision Detection
- **CollisionValidator**:
  - **Halcyon**: Ring radius clearance (47.5 cm ring, full 360°)
    - <4.5 cm = Error, <5.0 cm = Warning
  - **Edge**: Maximum distance from isocenter (full 360° coverage)
    - >36.5 cm = Warning, >37.5 cm = Error
    - Auto-skip for couch rotation plans

#### 9. Planning Structures
- **PlanningStructuresValidator**: Air structure (z_Air) validation
- **ContrastStructureValidator**: Contrast structure (z_Contrast*) when CT includes contrast
- **PTVBodyProximityValidator**: PTV-to-Body surface distance (4mm threshold)

---

## Machine-Specific Features

### Varian Edge
- User origin tolerance: 2mm
- Energies: 6X, 10X, 6X-FFF, 10X-FFF
- Setup fields: CBCT, SF-0 (energy: 6X or 10X)
- FFF energy enforcement for ≥5Gy/fraction plans
- Encompass fixation support (markerless setup)
- HyperArc plan support with couch rotation
- Collision: Distance-based (>37.5 cm = Error)

### Varian Halcyon
- User origin tolerance: 5mm
- Energy: 6X-FFF only
- Setup field: kVCBCT
- Fixation: Alta/Couch systems
- Collision: Ring clearance (47.5 cm ring, <4.5 cm = Error)
- VMAT-optimized validations

---

## Requirements

- **Eclipse**: Version 18.0
- **.NET Framework**: 4.8
- **ESAPI**: RTM\18.0 assemblies
  - `VMS.TPS.Common.Model.API.dll`
  - `VMS.TPS.Common.Model.Types.dll`
- **Platform**: Windows x64
- **Permissions**: Read access to patient plans, structure sets, and dose distributions

---

## Building

### Build Command

```bash
msbuild Variants/ClinicE/ClinicE.csproj /p:Configuration=Release /p:Platform=x64
```

### Output

- **Assembly**: `TEST_Cross_Check.esapi.dll`
- **Location**: `Variants/ClinicE/Release/`
- **Window Title**: "TEST_Cross-check v1.8.3"

---

## Installation

1. **Build the project** (see above)

2. **Locate the DLL**:
   ```
   Variants/ClinicE/Release/TEST_Cross_Check.esapi.dll
   ```

3. **Deploy to Eclipse**:
   - Copy DLL to Eclipse plugin directory:
     ```
     C:\Program Files (x86)\Varian\Vision\18.0\ExternalBeam\Plugins\
     ```
   - Or your institution's custom plugin directory

4. **Restart Eclipse** and approve the plugin

5. **Run the script**:
   - Open a treatment plan
   - Navigate to **Scripts** menu
   - Select **"TEST_Cross-check v1.8.3"**

---

## Usage

### Running Validation

1. Open a plan in Eclipse 18.0
2. Scripts → "TEST_Cross-check v1.8.3"
3. Validation runs automatically
4. Review results:
   - 🔴 **Errors**: Must be addressed before plan approval
   - 🟡 **Warnings**: Should be reviewed and documented
   - 🔵 **Info**: Confirmations of expected configurations

### Interpreting Results

**Example Validation Messages:**

✅ **Info** - "All treatment fields passed Fields.Energy checks"
- All fields use the same energy (as required)

⚠️ **Warning** - "Max distance 36.8 cm from isocenter to fixation device 'z_AltaLD' (left edge) - limited clearance"
- Clearance is within acceptable range but approaching limits

❌ **Error** - "Field 'G180_1' does not follow naming convention"
- Field name doesn't match institutional standards

---

## Project Structure

```
Variants/ClinicE/
├── ClinicE.csproj              # Project file (links to Core/)
├── README.md                   # This file
│
├── Properties/
│   └── AssemblyInfo.cs         # Version: 1.8.3
│
├── Script.cs                   # ESAPI entry point
│
├── Utilities/
│   └── PlanUtilities.cs        # Helper methods
│                               # - IsEdgeMachine()
│                               # - IsHalcyonMachine()
│                               # - IsArcBeam()
│                               # - HasAnyFieldWithCouch()
│                               # - ContainsSRS()
│
└── Validators/                 # 18 clinic-specific validators
    ├── RootValidator.cs
    ├── CourseValidator.cs
    ├── PlanValidator.cs
    ├── CTAndPatientValidator.cs
    ├── UserOriginMarkerValidator.cs
    ├── DoseValidator.cs
    ├── BeamEnergyValidator.cs
    ├── FieldsValidator.cs
    ├── FieldNamesValidator.cs
    ├── GeometryValidator.cs
    ├── SetupFieldsValidator.cs
    ├── OptimizationValidator.cs
    ├── ReferencePointValidator.cs
    ├── FixationValidator.cs
    ├── CollisionValidator.cs
    ├── PlanningStructuresValidator.cs
    ├── ContrastStructureValidator.cs
    └── PTVBodyProximityValidator.cs
```

**Shared Components** (from Core/):
- Base validation framework (`ValidatorBase`, `CompositeValidator`)
- UI components (`MainControl.xaml`, `ValidationViewModel.cs`)

---

## Version History

### v1.8.3 (Current)
- Simplified Edge collision detection to full 360° check (conservative)

### v1.8.0
- Production release - removed TEST_ prefix
- Enhanced allPass summary messages
- Multi-check display improvements

### v1.7.2
- BeamEnergyValidator implementation
- Enhanced FixationValidator for Edge (Alta/Couch OR Encompass)
- ContrastStructureValidator implementation
- Marker detection threshold: 2000HU → 500HU
- Skip marker check for Edge+Encompass

### v1.6.0
- Edge collision assessment with sector filtering
- Improved anatomical direction reporting

### v1.5.x
- PTV-to-Body proximity checking
- Fixed Y voxel scaling bug
- Initial composite validator architecture

---

## Development

### Adding New Validators

1. Create validator in `Validators/` directory:
   ```csharp
   namespace PlanCrossCheck
   {
       public class MyNewValidator : ValidatorBase
       {
           public override IEnumerable<ValidationResult> Validate(ScriptContext context)
           {
               var results = new List<ValidationResult>();
               // Validation logic
               return results;
           }
       }
   }
   ```

2. Add to composite validator:
   ```csharp
   AddValidator(new MyNewValidator());
   ```

3. Update version in:
   - `Properties/AssemblyInfo.cs` (lines 32-33)
   - `Script.cs` (line 35: window title)

4. Build and test in Eclipse 18.0

### Version Management

**Eclipse Requirement**: Eclipse blocks script execution if assembly version doesn't change.

**When to bump version**:
- ✅ User reports runtime errors from Eclipse testing
- ✅ User reports validation issues after running script
- ❌ Build/compilation errors (fix without version bump)

---

## Clinical Considerations

### Edge Plans
- Review collision warnings for plans with large patients
- Verify Encompass fixation detection is correct
- Confirm FFF energy for high-dose prescriptions

### Halcyon Plans
- Monitor ring clearance warnings closely (<5 cm)
- Ensure Alta/Couch fixation structures are contoured
- Verify 6X-FFF energy is correctly configured

### HyperArc Plans
- Field naming handles non-standard angles automatically
- Collision check skipped (couch rotation present)
- Manual collision verification required

---

## Support

- **Issues**: Report via GitHub Issues
- **Email**: rusetskiy.s@gmail.com
- **Institutional**: Consult local medical physics team

---

## License

PlanCrossCheck Community License - Free for internal organizational use.

Copyright (c) 2025 Sergei Rusetskii

---

*ClinicE Variant - Eclipse 18.0 | Edge & Halcyon*
