# Changelog

All notable changes to PlanCrossCheck will be documented in this file.

## ClinicH v1.3.0.0 - 2026-01-25

### Changed
- **Assembly Name**: Changed from `PlanCrossCheck.dll` to `CrossCheck.esapi.dll`
  - Required for Eclipse Script module to recognize the script
  - Must have `.esapi` extension in assembly name
- **Window Title**: Now displays version number ("Cross-check v1.3.0")
- **Version**: Updated to v1.3.0.0

### Fixed
- **Script Visibility**: Fixed issue where script didn't appear in Eclipse Script module
  - Root cause: Assembly name must end with `.esapi` for Eclipse recognition
  - Solution: Changed assembly name to `CrossCheck.esapi`

### Deployment Notes
- Build produces: `CrossCheck.esapi.dll`
- Deploy to: `C:\Users\Public\Documents\Varian\Vision\16.1\ExternalBeam\Scripts\`
- Script will appear in Eclipse as "CrossCheck" in Script menu
- Window title will show "Cross-check v1.3.0"

## ClinicH v1.1.0.0 - 2026-01-25

### Added
- **CollisionValidator**: Gantry clearance validation for TrueBeam STX machines
  - Conservative 360° scan approach
  - Error threshold: >37.5 cm from isocenter
  - Warning threshold: >36.5 cm from isocenter
  - Checks 11 fixation structure types including BODY
- **UserOriginMarkerValidator**: Radiopaque marker detection at user origin
  - Detects 3 ball bearing markers (Left, Right, Upper)
  - 5mm search radius around expected positions
  - 500 HU threshold for marker detection
  - Provides detailed feedback on missing markers
- **OptimizationValidator**: Jaw Tracking validation for TrueBeam STX
  - Validates if Jaw Tracking is enabled for optimization
  - Warning if not used

### Changed
- **Architecture**: Refactored from monolithic (682 lines) to modular structure (19 files)
  - Created `Validators/Base/` folder with 4 base classes
  - Created `Validators/Utilities/` folder with PlanUtilities helper
  - Split into 14 individual validator files
  - Matches ClinicE modular architecture for consistency
- **Version**: Bumped from v1.0.0.1 to v1.1.0.0
- **Project File**: Updated PlanCrossCheck.csproj with 19 new file references

### Deprecated
- Monolithic `Validators.cs` file (backed up as `Validators.cs.backup`)

### Technical Details
- Total validators: 14 (11 original + 3 new)
- File structure:
  - 4 base classes (ValidationSeverity, ValidationResult, ValidatorBase, CompositeValidator)
  - 1 utility class (PlanUtilities)
  - 14 validator implementations
- Framework: .NET 4.6.1, Eclipse ESAPI 16.1
- Platform: x64

---

## ClinicE v1.8.3 - 2025-12-20

### Changed
- **CollisionValidator**: Simplified Edge collision detection to full 360° check for maximum safety
- Conservative approach: checks maximum distance from isocenter across full arc

---

## ClinicE v1.8.0 - 2025-12-18

### Changed
- **Production Release**: Removed TEST_ prefix from assembly name
- Assembly now: `Cross_Check.esapi.dll` (was `TEST_Cross_Check.esapi.dll`)

### Added
- **BeamEnergyValidator**: Validates all treatment fields use same energy mode
- **Enhanced FixationValidator**: Edge machine accepts Alta/Couch OR Encompass fixation
- **ContrastStructureValidator**: Checks for z_Contrast* structures when Study.Comment contains CONTRAST
- **Enhanced Marker Detection**:
  - Skip marker detection for Edge machine with Encompass fixation
  - Lowered marker threshold from 2000HU to 500HU for better detection
- **Enhanced UI**: AllPass summary messages with custom descriptions

---

*Framework: Claude Code Starter v2.5.1*
