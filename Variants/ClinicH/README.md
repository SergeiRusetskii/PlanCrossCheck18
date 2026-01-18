# PlanCrossCheck - ClinicH Variant (Hadassah)

Quality assurance validation tool for Eclipse 16.1 with TrueBeam STX treatment machines.

> **⚠️ MEDICAL DISCLAIMER**: This software has not undergone FDA clearance. Users must validate performance in their clinical environment per institutional requirements.

---

## Variant Overview

**ClinicH** is designed specifically for Hadassah Medical Center using:
- **Eclipse Version**: 16.1
- **Machine Types**: TrueBeam STX (2 machines)
- **.NET Framework**: 4.6.1
- **ESAPI Version**: RTM\16.1
- **Platform**: Windows x64

**Current Version**: v1.0.0.1

---

## Features

### 11 Specialized Validators

#### 1. Course & Plan Validation
- **CourseValidator**: Validates course ID follows naming convention (RT[n]_*)
- **PlanValidator**: Orchestrates plan-level validation checks and treatment orientation

#### 2. Patient & CT Validation
- **CTAndPatientValidator**:
  - User origin validation (**5mm tolerance** for all coordinates)
  - CT imaging device information
  - Hadassah-specific tolerance settings

#### 3. Dose Calculation & Energy
- **DoseValidator**:
  - Dose grid resolution (≤1.25mm for SRS, ≤2.5mm standard)
  - **TrueBeam STX energy validation**: 6X, 10X, 15X, 6X-FFF, 10X-FFF
  - **TrueBeam STX dose rates**:
    - 6X-FFF: 1400 MU/min
    - 10X-FFF: 2400 MU/min
    - 6X, 10X, 15X: 600 MU/min

#### 4. Field Configuration
- **FieldsValidator**: Orchestrates field-level checks
- **FieldNamesValidator**:
  - Validates naming conventions for static and arc fields
  - Supports couch rotation field naming
- **GeometryValidator**:
  - Collimator angle validation
  - Duplicate angle detection
  - Invalid angle range checking
- **SetupFieldsValidator**:
  - **Required setup fields**: CBCT, SF_0, SF_270 or SF_90 (total: 3 fields)
  - **Setup field energy**: 2.5X-FFF (all setup fields)

#### 5. Reference Points & Prescription
- **ReferencePointValidator**:
  - Reference point naming (RP_*)
  - Dose limits validation (Total+0.1, Fraction+0.1)
  - Prescription dose matching

#### 6. Fixation Devices
- **FixationValidator**:
  - Density override validation for fixation structures
  - Supported prefixes: z_AltaHD_, z_AltaLD_, z_H&NFrame_, z_MaskLock_, z_FrameHead_, z_LocBar_, z_ArmShuttle_, z_EncFrame_, z_VacBag_, z_Contrast_
  - Validates HU values match structure naming (e.g., z_AltaLD_-390HU)

---

## TrueBeam STX Specifications

### Energy Configuration
- **Photon Energies**: 6X, 10X, 15X, 6X-FFF, 10X-FFF
- **Setup Field Energy**: 2.5X-FFF (fixed)

### Dose Rates
| Energy | Dose Rate (MU/min) |
|--------|-------------------|
| 6X | 600 |
| 10X | 600 |
| 15X | 600 |
| 6X-FFF | 1400 |
| 10X-FFF | 2400 |

### Setup Fields
- **CBCT** (Cone-Beam CT imaging)
- **SF_0** (Setup field at gantry 0°)
- **SF_270** or **SF_90** (Lateral setup field)

All setup fields must use **2.5X-FFF** energy.

---

## Hadassah-Specific Settings

### User Origin Tolerance
- **X, Y, Z coordinates**: 5mm tolerance
- More relaxed than ClinicE variant (2mm for Edge)
- Accounts for institutional workflow preferences

### Setup Field Configuration
- **Three setup fields required** (CBCT + 2 planar)
- **Specific naming**: CBCT, SF_0, SF_270/90
- **Fixed energy**: 2.5X-FFF for all setup fields

### Dose Grid
- SRS plans: ≤1.25mm
- Standard plans: ≤2.5mm

---

## Requirements

- **Eclipse**: Version 16.1
- **.NET Framework**: 4.6.1
- **ESAPI**: RTM\16.1 assemblies
  - `VMS.TPS.Common.Model.API.dll`
  - `VMS.TPS.Common.Model.Types.dll`
- **Platform**: Windows x64
- **Permissions**: Read access to patient plans, structure sets, and dose distributions

---

## Building

### Build Command

```bash
msbuild Variants/ClinicH/ClinicH.csproj /p:Configuration=Release /p:Platform=x64
```

### Output

- **Assembly**: `PlanCrossCheck.dll`
- **Location**: `Variants/ClinicH/Release/`
- **Window Title**: "Plan Cross-check"

---

## Installation

1. **Build the project** (see above)

2. **Locate the DLL**:
   ```
   Variants/ClinicH/Release/PlanCrossCheck.dll
   ```

3. **Deploy to Eclipse**:
   - Copy DLL to Eclipse plugin directory:
     ```
     C:\Program Files (x86)\Varian\Vision\16.1\ExternalBeam\Plugins\
     ```
   - Or your institution's custom plugin directory

4. **Restart Eclipse** and approve the plugin

5. **Run the script**:
   - Open a treatment plan
   - Navigate to **Scripts** menu
   - Select **"Plan Cross-check"**

---

## Usage

### Running Validation

1. Open a plan in Eclipse 16.1
2. Scripts → "Plan Cross-check"
3. Validation runs automatically
4. Review results:
   - 🔴 **Errors**: Must be addressed before plan approval
   - 🟡 **Warnings**: Should be reviewed and documented
   - 🔵 **Info**: Confirmations of expected configurations

### Interpreting Results

**Example Validation Messages:**

✅ **Info** - "All treatment fields passed Fields.Energy checks"
- All fields use correct TrueBeam STX energies

✅ **Info** - "Plan has the required 3 setup fields"
- CBCT, SF_0, and SF_270/90 are all present

❌ **Error** - "User Origin X coordinate (12.3 mm) is outside 5 mm tolerance"
- User origin needs adjustment

❌ **Error** - "Setup field 'SF_0' has incorrect energy (6X). Should be 2.5X-FFF"
- Setup field energy configuration error

---

## Project Structure

```
Variants/ClinicH/
├── ClinicH.csproj              # Project file (links to Core/)
├── README.md                   # This file
│
├── Properties/
│   └── AssemblyInfo.cs         # Version: 1.0.0.1
│
├── Script.cs                   # ESAPI entry point
│
├── Utilities/
│   └── PlanUtilities.cs        # Helper methods
│                               # - IsTrueBeamSTX()
│                               # - IsArcBeam()
│                               # - HasAnyFieldWithCouch()
│                               # - ContainsSRS()
│
└── Validators/                 # 11 clinic-specific validators
    ├── RootValidator.cs
    ├── CourseValidator.cs
    ├── PlanValidator.cs
    ├── CTAndPatientValidator.cs
    ├── DoseValidator.cs
    ├── FieldsValidator.cs
    ├── FieldNamesValidator.cs
    ├── GeometryValidator.cs
    ├── SetupFieldsValidator.cs
    ├── ReferencePointValidator.cs
    └── FixationValidator.cs
```

**Shared Components** (from Core/):
- Base validation framework (`ValidatorBase`, `CompositeValidator`)
- UI components (`MainControl.xaml`, `ValidationViewModel.cs`)

---

## Differences from ClinicE

### What's Different

| Feature | ClinicE (Edge/Halcyon) | ClinicH (TrueBeam STX) |
|---------|----------------------|----------------------|
| **Eclipse Version** | 18.0 | 16.1 |
| **.NET Framework** | 4.8 | 4.6.1 |
| **User Origin Tolerance** | 2mm (Edge), 5mm (Halcyon) | 5mm (all) |
| **Energies** | 6X, 10X, 6X-FFF, 10X-FFF | 6X, 10X, 15X, 6X-FFF, 10X-FFF |
| **Setup Fields** | CBCT+SF-0 (Edge), kVCBCT (Halcyon) | CBCT, SF_0, SF_270/90 |
| **Setup Energy** | 6X or 10X (Edge) | 2.5X-FFF (all) |
| **Validators** | 18 validators | 11 validators |
| **Collision Detection** | ✓ (machine-specific) | ✗ (not implemented) |
| **Optimization Checks** | ✓ (jaw tracking, ASC) | ✗ (not implemented) |
| **PTV-Body Proximity** | ✓ | ✗ (not implemented) |
| **Marker Detection** | ✓ (with Encompass skip) | ✗ (not implemented) |
| **Contrast Validation** | ✓ | ✗ (not implemented) |

### What's the Same

- Course ID validation (RT[n]_* format)
- Treatment orientation checking
- Dose grid resolution validation
- Field naming conventions
- Reference point validation (RP_*, dose limits)
- Fixation density override validation
- Prescription dose matching

---

## Version History

### v1.0.0.1 (Current)
- Initial release for Hadassah Medical Center
- Migrated from monolithic 682-line Validators.cs to modular structure
- 11 validators split into separate files
- TrueBeam STX-specific energy and dose rate validation
- 5mm user origin tolerance
- 3 setup fields with 2.5X-FFF energy requirement

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
   - `Script.cs` (window title)

4. Update `ClinicH.csproj` to include new file:
   ```xml
   <Compile Include="Validators\MyNewValidator.cs" />
   ```

5. Build and test in Eclipse 16.1

### Potential Enhancements

Based on ClinicE features, consider adding:
- **CollisionValidator** (if collision issues occur)
- **OptimizationValidator** (jaw tracking, ASC)
- **PTVBodyProximityValidator** (4mm threshold check)
- **UserOriginMarkerValidator** (CT marker detection)
- **ContrastStructureValidator** (contrast structure validation)
- **BeamEnergyValidator** (all-fields-same-energy check)
- **PlanningStructuresValidator** (air structure validation)

---

## Clinical Considerations

### TrueBeam STX Plans
- Verify energy selection matches prescription
- Confirm setup field energy is 2.5X-FFF
- Check user origin is within 5mm tolerance on all axes
- Ensure all 3 setup fields are present (CBCT, SF_0, SF_270/90)

### Common Issues
- **Setup field energy**: Must be 2.5X-FFF (Eclipse may default to 6X)
- **Setup field count**: Exactly 3 required (not 2 or 4)
- **User origin**: 5mm tolerance more forgiving than some sites

---

## Support

- **Issues**: Report via GitHub Issues
- **Email**: rusetskiy.s@gmail.com
- **Institutional**: Consult Hadassah medical physics team

---

## License

PlanCrossCheck Community License - Free for internal organizational use.

Copyright (c) 2025 Sergei Rusetskii

---

*ClinicH Variant - Eclipse 16.1 | TrueBeam STX (Hadassah)*
