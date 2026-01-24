# PlanCrossCheck

A comprehensive quality assurance tool for Varian Eclipse treatment planning systems that performs automated validation checks on radiation therapy treatment plans.

> **⚠️ IMPORTANT MEDICAL DISCLAIMER**
>
> This tool is provided for quality assurance purposes in radiation therapy planning. **This software has not undergone FDA clearance.** Clinical decisions must always be made by qualified medical physics and radiation oncology professionals. This software does not replace professional clinical judgment or institutional quality assurance procedures. Users must validate performance in their clinical environment per institutional requirements.

---

## Overview

PlanCrossCheck is an Eclipse Scripting API (ESAPI) plugin designed to systematically validate treatment plans to ensure quality and safety in radiation therapy. The tool performs extensive checks across multiple categories including plan parameters, dose calculations, beam configurations, machine-specific validations, and clinical protocols.

**Current Version:** v1.8.3 (ClinicE - proven clinical deployment)

---

## Clinic Variants

This repository supports two independent clinic configurations:

### ClinicE (Root Directory) ⭐

**Primary production variant** - root directory files

- **Eclipse Version:** 18.0
- **.NET Framework:** 4.8
- **Machine Types:** Varian Edge, Halcyon
- **Validators:** 18 comprehensive validators
- **Version:** v1.8.3
- **Status:** ✅ Production - proven clinical deployment
- **Last Updated:** Dec 20, 2024 (commit ccc4eb6)

**Key Features:**
- Full 360° collision detection
- Edge-specific validations
- Halcyon-specific validations
- Optimization jaw tracking
- Arc spacing control
- Comprehensive safety checks

### ClinicH (ClinicH/ Directory)

**Independent project** - separate folder

- **Eclipse Version:** 16.1
- **.NET Framework:** 4.6.1
- **Machine Types:** TrueBeam STX
- **Status:** 📁 Placeholder - awaiting user files
- **Documentation:** [ClinicH/README.md](ClinicH/README.md)

**Note:** ClinicH is completely independent from ClinicE. Features can be manually copied from ClinicE validators as needed.

---

## Repository Structure

```
PlanCrossCheck/
├── .claude/                    # Shared framework (only this is shared)
├── backup/                     # Backup of experimental validators
│   └── ClinicH-new-validators/
├── ClinicH/                    # Independent ClinicH project
│   ├── README.md
│   └── .gitkeep
├── Documentation/              # General documentation
├── Properties/                 # ClinicE assembly info
├── Validators/                 # ClinicE validators (18 files)
│   ├── Base/                   # Base classes & interfaces
│   ├── Utilities/              # Helper utilities
│   └── [18 validator files]
├── MainControl.xaml            # ClinicE UI layout
├── MainControl.xaml.cs         # ClinicE UI code-behind
├── Script.cs                   # ClinicE entry point
├── SeverityToColorConverter.cs # ClinicE UI converter
├── ValidationViewModel.cs      # ClinicE view model
├── PlanCrossCheck.csproj       # ClinicE project file
├── PlanCrossCheck.sln          # ClinicE solution
└── README.md                   # This file
```

**Architecture Note:** This is a conservative two-clinic structure with **zero shared code** between clinics (except `.claude/` framework files). ClinicE at root is the proven clinical version. ClinicH is an independent project.

---

## Getting Started

### For ClinicE Users (Eclipse 18.0)

ClinicE is ready to build and deploy from the root directory.

**Requirements:**
- Eclipse 18.0
- .NET Framework 4.8
- Windows x64
- ESAPI access enabled

**Build:**
```bash
msbuild PlanCrossCheck.csproj /p:Configuration=Release /p:Platform=x64
```

**Output:**
- `Release/TEST_Cross_Check.esapi.dll`

**Installation:**
1. Copy DLL to Eclipse plugin directory
2. Restart Eclipse
3. Access via Scripts menu

### For ClinicH Users (Eclipse 16.1)

See [ClinicH/README.md](ClinicH/README.md) for setup instructions.

ClinicH folder is currently a placeholder. Copy your working ClinicH project files into this directory.

---

## Key Features (ClinicE v1.8.3)

### Validation Categories

**18 comprehensive validators:**

1. **RootValidator** - Top-level plan validation orchestration
2. **CourseValidator** - Course ID format, plan naming
3. **CTAndPatientValidator** - User origin, CT device verification
4. **UserOriginMarkerValidator** - User origin markers and positioning
5. **DoseValidator** - Grid resolution, dose coverage
6. **FieldsValidator** - Field configuration, energy consistency
7. **BeamEnergyValidator** - Energy selection validation
8. **FieldNamesValidator** - Field naming conventions
9. **OptimizationValidator** - Jaw tracking, arc spacing
10. **GeometryValidator** - Gantry, collimator, couch angles
11. **CollisionValidator** - Full 360° collision detection
12. **SetupFieldsValidator** - Setup field requirements
13. **FixationValidator** - Fixation device verification
14. **PlanningStructuresValidator** - Air structures, PRV structures
15. **ContrastStructureValidator** - Contrast structure identification
16. **PTVBodyProximityValidator** - PTV-to-body distance checks
17. **PlanValidator** - Plan type, approval status
18. **ReferencePointValidator** - Reference point validation

### User Interface

- **Severity-Based Color Coding**: Error (red), Warning (yellow), Info (blue)
- **Grouped Display**: Results organized by validation category
- **Real-Time Feedback**: Instant validation within Eclipse workflow
- **Summary Messages**: Consolidated results for passed checks

---

## Building from Source

### ClinicE (Root)
```bash
msbuild PlanCrossCheck.csproj /p:Configuration=Release /p:Platform=x64
```

Produces: `Release/TEST_Cross_Check.esapi.dll`

### ClinicH (Separate Project)
See [ClinicH/README.md](ClinicH/README.md)

**Note**: x64 platform is **required** - ESAPI only supports 64-bit architectures.

---

## Installation (ClinicE)

### Standard Installation

1. **Build the project** (see above)
2. **Locate the DLL**: `Release/TEST_Cross_Check.esapi.dll`
3. **Copy to Eclipse plugin directory**:
   - Typical location: `C:\Program Files (x86)\Varian\RTM\[Version]\ExternalBeam\PlugIns\`
4. **Restart Eclipse**
5. **Verify installation**: Look for "Cross-check" in Scripts menu

### Testing Installation

1. Open test patient in Eclipse
2. Navigate to Scripts menu
3. Select "Cross-check v1.8.3"
4. Verify UI loads and validation runs

---

## Porting Features Between Clinics

Since ClinicE and ClinicH are completely independent:

**To copy a validator from ClinicE to ClinicH:**

1. Review ClinicE validator implementation (root `Validators/` folder)
2. Copy relevant validator file to your ClinicH project
3. Adapt for Eclipse 16.1 API differences if needed
4. Update namespace if necessary
5. Test in Eclipse 16.1 environment

**Backup validators available:**
- `/backup/ClinicH-new-validators/` contains validators from previous multi-clinic experiment

---

## Clinical Safety

This tool is designed to **supplement**, not replace, clinical judgment and institutional quality assurance protocols. All validation results should be reviewed by qualified medical physics staff.

**Recommendations:**
- Use as part of your comprehensive QA workflow
- Review all Error-level findings before plan approval
- Document Warning-level findings in plan review
- Customize thresholds to match institutional policies
- Regularly update to latest version for bug fixes and enhancements

---

## Version History (ClinicE)

**v1.8.3** (Dec 20, 2024) - Current
- Simplified edge collision detection to full 360° check
- Enhanced stability and performance
- Proven clinical deployment

See commit history for detailed changelog.

---

## Contributing

Contributions are welcome! To contribute:

1. Fork the repository
2. Create a feature branch
3. Implement your changes with appropriate tests
4. Ensure code follows existing style and patterns
5. Submit a pull request with clear description

For major changes, please open an issue first to discuss the proposed modifications.

---

## License

PlanCrossCheck Community License - See [LICENSE](LICENSE) file for details.

This software is free for internal organizational use and non-profit purposes. Commercial use requires a separate license.

Copyright (c) 2025 Sergei Rusetskii

---

## Support

For questions, bug reports, or feature requests:

- **Email**: rusetskiy.s@gmail.com
- **GitHub Issues**: Use the issue tracker for this repository
- **Institutional Support**: Consult your local medical physics team for clinical questions

### Security

For reporting security vulnerabilities:
- **Preferred**: Use [GitHub Security Advisories](https://github.com/SergeiRusetskii/PlanCrossCheck/security/advisories/new)
- **Alternative**: Email **rusetskiy.s@gmail.com** with subject line "SECURITY: PlanCrossCheck"

---

## Acknowledgments

Built using:
- Varian Eclipse Scripting API (ESAPI)
- .NET Framework 4.8 (ClinicE)
- Windows Presentation Foundation (WPF)

---

*PlanCrossCheck - Quality assurance automation for radiation therapy treatment planning*
