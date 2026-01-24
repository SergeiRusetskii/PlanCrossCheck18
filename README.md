# PlanCrossCheck

Quality assurance validation tool for Varian Eclipse treatment planning systems.

> **⚠️ MEDICAL DISCLAIMER**
>
> This software has not undergone FDA clearance. Clinical decisions must be made by qualified professionals. This tool supplements, not replaces, institutional QA procedures.

---

## Overview

Eclipse Scripting API (ESAPI) plugin that performs automated validation checks on radiation therapy treatment plans.

**Architecture:** Two independent clinic projects with zero shared code.

---

## Clinic Variants

### ClinicE - [ClinicE/](ClinicE/)

- **Eclipse:** 18.0 | **.NET:** 4.8 | **Platform:** x64
- **Machines:** Varian Edge, Halcyon
- **Version:** v1.8.3
- **Validators:** 18 (modular architecture)
- **Status:** ✅ Production

**Build:**
```bash
cd ClinicE
msbuild PlanCrossCheck.csproj /p:Configuration=Release /p:Platform=x64
```

**Output:** `ClinicE/Release/TEST_Cross_Check.esapi.dll`

---

### ClinicH - [ClinicH/](ClinicH/)

- **Eclipse:** 16.1 | **.NET:** 4.6.1 | **Platform:** x64
- **Machines:** TrueBeam STX
- **Version:** v1.0.0.1
- **Validators:** Monolithic architecture
- **Status:** ✅ Clinical

**Build:**
```bash
cd ClinicH
msbuild PlanCrossCheck.csproj /p:Configuration=Release /p:Platform=x64
```

**Output:** `ClinicH/Release/PlanCrossCheck.dll`

---

## Key Features

- **Comprehensive Validation:** Course, plan, dose, field configuration, geometry
- **Safety Checks:** Collision detection, fixation verification
- **Clinical Metrics:** PTV-to-Body proximity, optimization validation
- **Real-Time Feedback:** Color-coded severity (Error/Warning/Info)
- **Machine-Specific:** Tailored validations for Edge, Halcyon, TrueBeam STX

---

## Requirements

- Windows x64
- Varian Eclipse with ESAPI access
- MSBuild (Visual Studio Build Tools)

See clinic-specific folders for detailed requirements.

---

## Installation

1. **Build** the appropriate clinic variant (see above)
2. **Copy DLL** to Eclipse plugin directory:
   - Typical: `C:\Program Files (x86)\Varian\RTM\[Version]\ExternalBeam\PlugIns\`
3. **Restart Eclipse**
4. **Access** via Scripts menu

---

## Structure

```
PlanCrossCheck/
├── ClinicE/          # Eclipse 18.0 variant (Edge, Halcyon)
├── ClinicH/          # Eclipse 16.1 variant (TrueBeam STX)
├── backup/           # Experimental validator backups
└── Documentation/    # ESAPI reference materials
```

**Independence:** Each clinic is a completely separate project. No shared code between variants.

---

## Contributing

Contributions welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Submit a pull request with clear description

For major changes, open an issue first.

---

## License

PlanCrossCheck Community License - See [LICENSE](LICENSE)

Free for internal organizational and non-profit use. Commercial use requires separate license.

Copyright (c) 2025 Sergei Rusetskii

---

## Support

- **Email:** rusetskiy.s@gmail.com
- **GitHub Issues:** Use the issue tracker
- **Security:** [GitHub Security Advisories](https://github.com/SergeiRusetskii/PlanCrossCheck/security/advisories/new)

---

*Built with Varian Eclipse Scripting API (ESAPI) and .NET Framework*
