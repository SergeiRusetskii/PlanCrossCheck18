# ClinicH - Cross-check v1.3.3

Quality assurance validation tool for TrueBeam STX radiation therapy treatment planning.

> **⚠️ MEDICAL DISCLAIMER**
>
> This software has not undergone FDA clearance. Clinical decisions must be made by qualified professionals. This tool supplements, not replaces, institutional QA procedures.

---

## System Configuration

- **Eclipse Version:** 16.1
- **.NET Framework:** 4.6.1
- **Platform:** x64
- **Treatment Machine:** TrueBeam STX
- **Script Status:** Clinical Use
- **Architecture:** Hierarchical validator pattern

---

## Validation Components

### 1. Course Validator

**What It Checks:**
- Course ID matches pattern: `RT[optional digit(s)]_*`
- Uses regular expression: `^RT\d*_`

**Output:**
- **Info:** Course ID follows required format
- **Error:** Course ID does not start with RT[n]_

**Clinical Relevance:** Course naming standard required for institutional record tracking and billing.

---

### 2. Plan Validator

**What It Checks:**
- Treatment orientation (HeadFirstSupine vs other orientations)
- Aggregates results from child validators (listed below)

**Output:**
- **Info:** HeadFirstSupine orientation detected
- **Warning:** Non-standard orientation detected

**Clinical Relevance:** Patient orientation affects collision geometry and setup procedures.

---

### 3. Dose Validator

**What It Checks:**
- Dose grid resolution
  - SBRT/SRS (≥5 Gy/fraction): Must be ≤2mm
  - Conventional (<5 Gy/fraction): Must be ≤3mm
- Prescription dose presence and format
- Total dose and fractionation

**Output:**
- **Error:** Grid resolution exceeds threshold for treatment type
- **Warning:** Missing or unusual dose parameters
- **Info:** Dose configuration within acceptable parameters

**Clinical Relevance:** Grid resolution affects dose calculation accuracy, particularly for small targets and high-gradient regions.

---

### 4. Field Validators

#### 4.1 Field Names Validator

**What It Checks:**
- Treatment field naming: `G[gantry angle]C[couch angle]` format
- Setup field naming
- Duplicate field names across plan
- Special characters in field IDs

**Output:**
- **Error:** Field name does not match expected format
- **Error:** Duplicate field names found
- **Info:** All field names follow standard format

**Clinical Relevance:** Standardized naming prevents delivery errors and improves treatment record clarity.

#### 4.2 Geometry Validator

**What It Checks:**
- Collimator angle for all treatment fields (excludes setup fields)
- Invalid angle ranges:
  - 268-272° (near 270°)
  - 358-2° (near 0°/360°)
  - 88-92° (near 90°)
- Duplicate collimator angles across fields

**Output:**
- **Error:** Collimator angle in invalid range (268-272°, 358-2°, or 88-92°)
- **Warning:** Duplicate collimator angle detected
- **Info:** Collimator angle valid

**Clinical Relevance:** Certain collimator angles near cardinal positions (0°, 90°, 270°) may cause delivery issues or collision risks on TrueBeam STX. Duplicate angles across fields may indicate planning errors.

#### 4.3 Setup Fields Validator

**What It Checks:**
- Presence of kV imaging fields
- Setup field technique type (planar kV imaging)
- Setup field naming conventions
- Imaging energy (kV range)

**Output:**
- **Warning:** Missing or improperly configured setup fields
- **Info:** Setup fields present and properly configured

**Clinical Relevance:** Setup imaging is required for patient positioning verification before treatment.

---

### 5. Reference Point Validator

**What It Checks:**
- Primary reference point exists
- Reference point has dose specification
- Reference point coordinates are within structure set bounds
- Prescription relationship to reference point

**Output:**
- **Error:** No primary reference point defined
- **Error:** Reference point missing dose specification
- **Warning:** Reference point location outside patient bounds
- **Info:** Primary reference point properly configured

**Clinical Relevance:** Reference point defines prescription dose and plan normalization.

---

### 6. Fixation Validator

**What It Checks:**
- Patient support device assignment (couch model)
- Fixation accessories documented in treatment fields
- Accessory stack configuration (base plates, head frames, etc.)

**Output:**
- **Warning:** Patient support device not assigned
- **Warning:** Fixation accessories not documented
- **Info:** Fixation devices properly documented

**Clinical Relevance:** Fixation device documentation is required for accurate collision detection and treatment reproducibility.

---

### 7. Optimization Validator

**Machine Type:** TrueBeam STX only

**What It Checks:**
- Jaw Tracking usage in optimization setup
- Checks for presence of `OptimizationJawTrackingUsedParameter` in plan optimization parameters

**Output:**
- **Info:** Jaw Tracking is used for TrueBeam STX plan
- **Warning:** Jaw Tracking is NOT used for TrueBeam STX plan

**Clinical Relevance:** Jaw Tracking dynamically adjusts jaw positions to conform to MLC aperture shape, reducing out-of-field dose and improving MU efficiency for IMRT/VMAT plans on TrueBeam STX.

---

### 8. Collision Validator

**Machine Type:** TrueBeam STX

**What It Checks:**
- Maximum radial distance from isocenter to patient/fixation structures
- Checks all contour points on all CT slices
- Structures evaluated:
  - BODY
  - CouchSurface
  - MP_Optek_BP
  - MP_WingSpan
  - MP_BrB_Up_BaPl
  - MP_BrB_Bott_BaPl
  - MP_Solo_BPl
  - MP_Enc_BPl
  - MP_Enc_HFr
- Calculates 2D radial distance (ignores Z axis)
- Identifies anatomical direction of maximum extension (left/right/anterior/posterior)

**Thresholds:**
- ≤36.5 cm from isocenter: Safe clearance
- 36.5-37.5 cm: Limited clearance
- >37.5 cm: Potential collision risk

**Output:**
- **Info:** Maximum distance ≤36.5 cm (safe clearance)
- **Warning:** Maximum distance 36.5-37.5 cm (limited clearance)
- **Error:** Maximum distance >37.5 cm (collision risk)
- **Info:** Collision check skipped (couch rotation present - requires manual verification)
- **Warning:** Cannot assess collision (no BODY or fixation structures found)

**Algorithm:**
Conservative approach - evaluates full 360° gantry rotation regardless of actual treatment angles. This accounts for:
- Gantry travel to start position
- Emergency stops
- Plan modifications
- Non-treatment gantry positions

**Clinical Relevance:** Prevents gantry-to-patient/fixation collisions that could cause injury or equipment damage.

---

### 9. User Origin Marker Validator

**What It Checks:**
Detects 3 radiopaque ball bearing markers placed at user origin position during CT simulation.

**Detection Algorithm:**
1. Locates user origin slice on CT
2. Finds BODY structure contour on that slice
3. Calculates intersection points with BODY surface:
   - Horizontal line through user origin → **Left** and **Right** markers
   - Vertical line through user origin → **Upper** marker (anterior for supine, posterior for prone)
4. Searches 5mm radius sphere around each intersection point
5. Detects markers with HU ≥500 (radiopaque threshold)

**Search Parameters:**
- HU threshold: ≥500
- Search radius: 5mm (3D spherical region)
- Marker count: 3 expected (Left, Right, Upper)

**Output:**
- **Info:** 3 of 3 markers detected in 5mm radius around User origin placement
- **Warning:** [0-2] of 3 markers detected - [missing marker names] marker(s) not found (on screen direction)
- **Warning:** Cannot validate - BODY structure not found
- **Error:** User Origin Z coordinate outside CT slice bounds

**Clinical Relevance:** Ball bearing markers at user origin verify correct CT reference point setup. Missing markers may indicate:
- Markers placed outside search radius
- Markers not radiopaque enough (HU <500)
- User origin incorrectly positioned relative to actual marker locations
- Affects patient positioning coordinate system accuracy

---

## Validation Architecture

```
RootValidator
├── CourseValidator
└── PlanValidator
    ├── DoseValidator
    ├── FieldsValidator
    │   ├── FieldNamesValidator
    │   ├── GeometryValidator
    │   └── SetupFieldsValidator
    ├── ReferencePointValidator
    ├── FixationValidator
    ├── OptimizationValidator
    ├── CollisionValidator
    └── UserOriginMarkerValidator
```

**Execution:** Validators execute hierarchically. Child validators run first, then parent validators append additional checks.

---

## Output Format

### Severity Levels

- **Error:** Critical issue requiring resolution before treatment
- **Warning:** Issue requiring clinical review
- **Info:** Configuration confirmed acceptable

### Display Window

- Dimensions: 650×1000 pixels
- Scrollable results list
- Color-coded severity indicators
- Hierarchical category organization

---

## Build Instructions

```bash
cd ClinicH
msbuild PlanCrossCheck.csproj /p:Configuration=Release /p:Platform=x64
```

**Output:** `ClinicH/Release/PlanCrossCheck.dll`

---

## Installation

1. Build project using above command
2. Copy `PlanCrossCheck.dll` to Eclipse plugin directory:
   - Default: `C:\Program Files (x86)\Varian\RTM\16.1\ExternalBeam\PlugIns\`
3. Restart Eclipse
4. Access via Scripts menu → PlanCrossCheck

---

## Requirements

- Windows x64
- Varian Eclipse 16.1 with ESAPI
- .NET Framework 4.6.1
- MSBuild tools (Visual Studio 2017+)

---

## License

PlanCrossCheck Community License - See [LICENSE](../LICENSE)

Free for internal organizational and non-profit use. Commercial use requires separate license.

Copyright (c) 2025 Sergei Rusetskii

---

*Built with Varian Eclipse Scripting API (ESAPI) 16.1 and .NET Framework 4.6.1*
