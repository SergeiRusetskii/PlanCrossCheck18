# PlanCrossCheck

A comprehensive quality assurance tool for Varian Eclipse treatment planning systems that performs automated validation checks on radiation therapy treatment plans.

## Overview

PlanCrossCheck is an Eclipse Scripting API (ESAPI) plugin designed to systematically validate treatment plans to ensure quality and safety in radiation therapy. The tool performs extensive checks across multiple categories including plan parameters, dose calculations, beam configurations, machine-specific validations, and clinical protocols.

Developed for medical physicists and dosimetrists, PlanCrossCheck provides real-time feedback within the Eclipse treatment planning environment, helping to identify potential issues before plan delivery.

## Features

### Comprehensive Validation Coverage

PlanCrossCheck includes **18 specialized validators** organized into the following categories:

#### 1. **Course & Plan Validation**
- **Course ID Format**: Validates course identifier follows institutional naming conventions
- **Plan Setup**: Verifies plan is properly configured and ready for validation

#### 2. **Patient & CT Validation**
- **User Origin**: Ensures user origin is set at the CT origin (standard workflow requirement)
- **CT Device Information**: Validates CT scanner information is present in imaging dataset
- **User Origin Markers**: Detects CT markers (BB markers) at expected locations with configurable HU thresholds (500 HU minimum)
  - Skips check for Edge machines with Encompass fixation (markerless setup)

#### 3. **Dose Calculation & Energy**
- **Dose Grid Resolution**: Validates dose calculation grid meets minimum resolution requirements (typically ≤3mm)
- **Beam Energy Consistency**: Ensures all treatment fields use the same energy (critical for dose calculation accuracy)
- **Energy Validation for Edge Machines**: Enforces FFF (flattening filter-free) beams for high-dose prescriptions on Edge machines

#### 4. **Field Configuration & Geometry**
- **Field Naming Conventions**: Validates field names follow institutional standards
  - Special handling for HyperArc plans (180.1°→181°, 179.9°→179° angle mapping)
- **Collimator Angle**: Checks collimator positioning for treatment delivery
- **Isocenter Configuration**: Validates single-isocenter requirement for VMAT plans
- **MLC Positioning**: Ensures multi-leaf collimator is properly configured
- **Setup Field Configuration**: Validates imaging fields (kV, MV, CBCT) are properly defined with appropriate energy and technique

#### 5. **Optimization & Beam Delivery**
- **Jaw Tracking**: Verifies jaw tracking is enabled for VMAT plans (optimization feature)
- **Arc Spacing Control (ASC)**: Validates ASC optimization is enabled where required
- **Reference Point & Prescription**: Ensures prescription is properly defined with correct dose specification

#### 6. **Fixation Devices & Safety**
- **Machine-Specific Fixation Validation**:
  - **Halcyon**: Requires AltaLD/AltaHD OR Couch fixation structures
  - **Edge**: Accepts AltaLD/AltaHD/Couch OR Encompass fixation systems
- **Density Overrides**: Validates density overrides are applied to fixation structures (prevents incorrect dose calculation)

#### 7. **Collision Detection (Conservative Mode)**
- **Halcyon Collision Assessment**:
  - Checks clearance to 47.5 cm ring radius across **full 360° gantry rotation** (conservative approach)
  - Thresholds: <4.5 cm = Error, <5.0 cm = Warning
  - Reports minimum clearance distance and anatomical direction
- **Edge Collision Assessment**:
  - Checks maximum distance from isocenter across **full 360° coverage** for maximum safety
  - Thresholds: >36.5 cm = Warning, >37.5 cm = Error
  - Automatically skips check for plans with couch rotation (requires manual verification)
  - Reports maximum distance and anatomical direction

#### 8. **Planning Structures**
- **Air Structure Validation**: Ensures air structures (z_Air) are properly contoured for dose calculation accuracy
- **Contrast Structure Validation**: Checks for contrast structures (z_Contrast) when CT study includes contrast administration

#### 9. **PTV-to-Body Proximity**
- **Surface Distance Check**: Measures and reports minimum distance from PTV to body surface
- Helps identify targets close to skin surface (relevant for dose buildup and bolus considerations)

### User Interface

- **Severity-Based Color Coding**:
  - 🔴 **Error**: Critical issues requiring immediate attention
  - 🟡 **Warning**: Items needing review or verification
  - 🔵 **Info**: Informational messages and confirmations
- **Grouped Display**: Results organized by validation category for efficient review
- **Real-Time Feedback**: Instant validation within Eclipse planning workflow
- **Summary Messages**: "All validation checks passed" messages for categories with no issues

### Technical Features

- **Modular Architecture**: Extensible validator system using composite design pattern
- **Machine Detection**: Automatic detection of Halcyon vs. Edge machines with machine-specific validation rules
- **VMAT/IMRT Support**: Handles both static and arc delivery techniques
- **HyperArc Support**: Special handling for HyperArc stereotactic plans with couch rotation
- **Clinical Safety**: Built-in safeguards to prevent common planning errors

## Requirements

- **Varian Eclipse Treatment Planning System**: v18.0 or later
- **.NET Framework**: 4.8
- **Eclipse Scripting API (ESAPI)**: Access enabled for your user account
- **Platform**: Windows x64
- **Minimum Permissions**: Read access to patient plans, structure sets, and dose distributions

## Installation

### Building from Source

1. Clone or download this repository

2. Open the solution in Visual Studio 2017 or later:
   ```
   PlanCrossCheck.sln
   ```

3. Build the project using MSBuild:
   ```bash
   msbuild PlanCrossCheck.sln /p:Configuration=Release /p:Platform=x64
   ```

4. Locate the compiled plugin DLL:
   ```
   bin/Release/Cross_Check.esapi.dll
   ```

### Deployment to Eclipse

1. Copy `Cross_Check.esapi.dll` to your Eclipse plugin directory:
   ```
   C:\Program Files (x86)\Varian\Vision\[version]\ExternalBeam\Plugins\
   ```
   (Exact path may vary based on Eclipse version and installation location)

2. Restart Eclipse treatment planning system

3. The plugin will appear in the **Scripts** menu as "Cross-check v1.8.3"

## Usage

### Running the Validation

1. Open a treatment plan in Eclipse
2. Navigate to **Scripts** menu
3. Select **"Cross-check v1.8.3"**
4. The validation window will open and automatically analyze the current plan
5. Review results grouped by category:
   - Errors (red) must be addressed before plan approval
   - Warnings (yellow) should be reviewed and documented
   - Info messages (blue) confirm expected configurations

### Interpreting Results

Each validation result includes:
- **Category**: The type of check performed (e.g., "Collision", "Fields.Energy", "Dose.Grid")
- **Message**: Detailed description of the finding
- **Severity**: Visual indicator of importance level

**Example Results:**
- ✅ "All Dose.Grid validation checks passed" (Info - no issues found)
- ⚠️ "Max distance 36.8 cm from isocenter to fixation device 'z_AltaLD' (left edge) - limited clearance" (Warning)
- ❌ "All treatment fields must use the same energy. Found: 6X (3 fields), 10X FFF (1 field)" (Error)

## Development

### Project Structure

```
PlanCrossCheck/
├── Script.cs                       # ESAPI plugin entry point
├── ValidationViewModel.cs          # MVVM view model
├── MainControl.xaml                # WPF UI markup
├── MainControl.xaml.cs             # WPF UI code-behind
├── SeverityToColorConverter.cs     # UI value converter
│
├── Validators/                     # Validation engine
│   ├── Base/                       # Base classes and enums
│   │   ├── ValidatorBase.cs        # Abstract validator base
│   │   ├── CompositeValidator.cs   # Composite pattern base
│   │   ├── ValidationResult.cs     # Result data structure
│   │   └── ValidationSeverity.cs   # Severity enumeration
│   ├── Utilities/
│   │   └── PlanUtilities.cs        # Helper methods
│   ├── RootValidator.cs            # Main orchestrator
│   ├── CourseValidator.cs
│   ├── PlanValidator.cs
│   ├── CTAndPatientValidator.cs
│   ├── UserOriginMarkerValidator.cs
│   ├── DoseValidator.cs
│   ├── BeamEnergyValidator.cs
│   ├── FieldsValidator.cs
│   ├── FieldNamesValidator.cs
│   ├── GeometryValidator.cs
│   ├── SetupFieldsValidator.cs
│   ├── OptimizationValidator.cs
│   ├── ReferencePointValidator.cs
│   ├── FixationValidator.cs
│   ├── CollisionValidator.cs
│   ├── ContrastStructureValidator.cs
│   ├── PlanningStructuresValidator.cs
│   └── PTVBodyProximityValidator.cs
│
└── Properties/
    └── AssemblyInfo.cs             # Assembly metadata
```

### Architecture

PlanCrossCheck uses a **composite validator pattern** for extensibility:

- **ValidatorBase**: Abstract base class defining the `Validate(ScriptContext)` interface
- **CompositeValidator**: Container for child validators, enabling hierarchical organization
- **RootValidator**: Top-level orchestrator coordinating all validation checks
- **Individual Validators**: Specialized classes for specific validation tasks

**Data Flow:**
```
Eclipse → Script.Execute() → ValidationViewModel
    → RootValidator.Validate() → CompositeValidators → Individual Validators
    → ValidationResults → ObservableCollection → WPF UI
```

**MVVM Pattern:**
- **Model**: ESAPI objects (Plan, Dose, Structure, Beam, etc.)
- **View**: MainControl.xaml (WPF user interface)
- **ViewModel**: ValidationViewModel (orchestrates validation, exposes results)

### Adding New Validators

1. Create a new validator class inheriting from `ValidatorBase`:
   ```csharp
   public class MyNewValidator : ValidatorBase
   {
       public override IEnumerable<ValidationResult> Validate(ScriptContext context)
       {
           var results = new List<ValidationResult>();

           // Validation logic here

           results.Add(CreateResult(
               "Category Name",
               "Validation message",
               ValidationSeverity.Warning
           ));

           return results;
       }
   }
   ```

2. Add the validator to the appropriate composite validator in `RootValidator.cs` or `PlanValidator.cs`

3. Update version numbers in both `Properties/AssemblyInfo.cs` and `Script.cs`

4. Build and test in Eclipse

For detailed development guidance, see `.claude/DEVELOPER_GUIDE.md`

### Building

```bash
# Release build (for production deployment)
msbuild PlanCrossCheck.sln /p:Configuration=Release /p:Platform=x64

# Debug build (for development/testing)
msbuild PlanCrossCheck.sln /p:Configuration=Debug /p:Platform=x64

# Clean build artifacts
msbuild PlanCrossCheck.sln /t:Clean
```

**Note**: x64 platform is **required** - ESAPI only supports 64-bit architectures.

## Version History

### v1.8.3 (Current - Production)
- Simplified Edge collision detection to full 360° check (conservative approach)

### v1.8.0
- Production release - removed TEST_ prefix from assembly names
- Enhanced allPass summary messages for better UX
- Multi-check display logic improvements

### v1.7.2
- Added BeamEnergyValidator - ensures all treatment fields use same energy
- Enhanced FixationValidator for Edge machines (Alta/Couch OR Encompass)
- Merged energy validation into consolidated Fields.Energy category
- Implemented ContrastStructureValidator
- Marker detection threshold lowered from 2000HU to 500HU
- Skip marker check for Edge+Encompass combinations

### v1.6.0
- Edge collision assessment with sector-based filtering
- Fixed wraparound handling in angle calculations
- Improved anatomical direction reporting

### v1.5.7 and earlier
- PTV-to-Body surface proximity checking
- Fixed Y voxel position scaling in air density validator
- Initial composite validator architecture
- WPF UI with severity-based color coding

## Machine-Specific Features

### Halcyon Support
- Ring radius collision detection (47.5 cm)
- Conservative 360° clearance checking
- AltaLD/AltaHD/Couch fixation validation
- VMAT-optimized validations

### Edge Support
- Maximum distance collision assessment (36.5 cm warning, 37.5 cm error)
- Full 360° coverage checking (conservative mode)
- Automatic skip for couch rotation plans
- Encompass fixation system support
- FFF energy enforcement for high-dose plans
- HyperArc plan support

## Clinical Safety

This tool is designed to **supplement**, not replace, clinical judgment and institutional quality assurance protocols. All validation results should be reviewed by qualified medical physics staff. The tool provides automated checks to help identify common issues, but does not guarantee plan safety or clinical appropriateness.

**Recommendations:**
- Use as part of your comprehensive QA workflow
- Review all Error-level findings before plan approval
- Document Warning-level findings in plan review
- Customize thresholds to match institutional policies
- Regularly update to latest version for bug fixes and enhancements

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository
2. Create a feature branch
3. Implement your changes with appropriate tests
4. Ensure code follows existing style and patterns
5. Submit a pull request with clear description

For major changes, please open an issue first to discuss the proposed modifications.

## License

MIT License - See [LICENSE](LICENSE) file for details.

Copyright (c) 2025 Sergei Rusetskii

## Support

For questions, bug reports, or feature requests:

- **Email**: rusetskiy.s@gmail.com
- **GitHub Issues**: Use the issue tracker for this repository
- **Institutional Support**: Consult your local medical physics team for clinical questions

## Acknowledgments

Built using:
- Varian Eclipse Scripting API (ESAPI)
- .NET Framework 4.8
- Windows Presentation Foundation (WPF)

## Disclaimer

This software is provided for research and educational purposes. Users are responsible for validating the tool's performance in their clinical environment and ensuring compliance with institutional policies and regulatory requirements.

**Not FDA-cleared for clinical use. Use at your own risk.**

---

*PlanCrossCheck - Quality assurance automation for radiation therapy treatment planning*
