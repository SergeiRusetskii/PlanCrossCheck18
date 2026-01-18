# PlanCrossCheck

A comprehensive quality assurance tool for Varian Eclipse treatment planning systems that performs automated validation checks on radiation therapy treatment plans.

> **⚠️ IMPORTANT MEDICAL DISCLAIMER**
>
> This tool is provided for quality assurance purposes in radiation therapy planning. **This software has not undergone FDA clearance.** Clinical decisions must always be made by qualified medical physics and radiation oncology professionals. This software does not replace professional clinical judgment or institutional quality assurance procedures. Users must validate performance in their clinical environment per institutional requirements.

---

## Overview

PlanCrossCheck is an Eclipse Scripting API (ESAPI) plugin designed to systematically validate treatment plans to ensure quality and safety in radiation therapy. The tool performs extensive checks across multiple categories including plan parameters, dose calculations, beam configurations, machine-specific validations, and clinical protocols.

**Architecture:** Multi-clinic variant structure supporting different Eclipse versions and machine configurations.

---

## Multi-Clinic Architecture

PlanCrossCheck uses a **variant-based architecture** to support different clinics with different Eclipse versions and machine types:

### Available Variants

| Variant | Eclipse Version | .NET Framework | Machine Types | Validators |
|---------|----------------|----------------|---------------|------------|
| **[ClinicE](Variants/ClinicE/)** | 18.0 | 4.8 | Edge, Halcyon | 18 |
| **[ClinicH](Variants/ClinicH/)** | 16.1 | 4.6.1 | TrueBeam STX | 11 |

### Structure

```
PlanCrossCheck/
├── Core/                    # Shared validation framework and UI
│   ├── Base/                # Base classes (ValidatorBase, CompositeValidator)
│   └── UI/                  # Shared WPF UI components
│
├── Variants/
│   ├── ClinicE/             # Eclipse 18.0 variant (Edge & Halcyon)
│   │   └── README.md        # ClinicE-specific documentation
│   │
│   └── ClinicH/             # Eclipse 16.1 variant (TrueBeam STX)
│       └── README.md        # ClinicH-specific documentation
│
└── README.md                # This file
```

**Benefits:**
- No code duplication for common functionality
- Easy to add new clinic variants
- Clinic-specific validation rules isolated
- Independent versioning per clinic

---

## Getting Started

### Choose Your Variant

1. **ClinicE** - For clinics using Eclipse 18.0 with Varian Edge or Halcyon machines
   - [View ClinicE Documentation →](Variants/ClinicE/README.md)

2. **ClinicH** - For clinics using Eclipse 16.1 with TrueBeam STX machines
   - [View ClinicH Documentation →](Variants/ClinicH/README.md)

### Quick Start

Each variant has its own:
- Build instructions
- Installation guide
- Validator documentation
- Version history

See the variant-specific README for detailed information.

---

## Key Features

### Validation Coverage

- **Course & Plan Validation** - Course ID format, plan setup
- **Patient & CT Validation** - User origin, CT device, markers
- **Dose Calculation** - Grid resolution, energy consistency
- **Field Configuration** - Naming conventions, geometry, setup fields
- **Optimization** - Jaw tracking, arc spacing control
- **Safety Checks** - Fixation devices, collision detection
- **Planning Structures** - Air structures, contrast structures
- **Clinical Metrics** - PTV-to-Body proximity

### User Interface

- **Severity-Based Color Coding**: Error (red), Warning (yellow), Info (blue)
- **Grouped Display**: Results organized by validation category
- **Real-Time Feedback**: Instant validation within Eclipse workflow
- **Summary Messages**: Consolidated results for passed checks

---

## Requirements

### Common Requirements
- Windows x64
- Varian Eclipse Treatment Planning System
- Eclipse Scripting API (ESAPI) access enabled
- Minimum Permissions: Read access to patient plans, structure sets, and dose distributions

### Variant-Specific Requirements

See variant-specific README files for:
- Eclipse version requirements
- .NET Framework version
- ESAPI version
- Machine-specific requirements

---

## Building from Source

### Build All Variants
```bash
msbuild PlanCrossCheck.sln /p:Configuration=Release /p:Platform=x64
```

### Build Specific Variant
```bash
# ClinicE only
msbuild Variants/ClinicE/ClinicE.csproj /p:Configuration=Release /p:Platform=x64

# ClinicH only
msbuild Variants/ClinicH/ClinicH.csproj /p:Configuration=Release /p:Platform=x64
```

**Note**: x64 platform is **required** - ESAPI only supports 64-bit architectures.

---

## Installation

Each variant produces a separate plugin DLL:

- **ClinicE**: `Variants/ClinicE/Release/TEST_Cross_Check.esapi.dll`
- **ClinicH**: `Variants/ClinicH/Release/PlanCrossCheck.dll`

Deploy to your Eclipse plugin directory and restart Eclipse.

See variant-specific documentation for detailed deployment instructions.

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
- .NET Framework
- Windows Presentation Foundation (WPF)

---

*PlanCrossCheck - Quality assurance automation for radiation therapy treatment planning*
