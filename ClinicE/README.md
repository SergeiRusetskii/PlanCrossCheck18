# ClinicE - TEST_Cross-check v1.8.3

Quality assurance validation tool for Varian Edge and Halcyon radiation therapy treatment planning.

> **⚠️ MEDICAL DISCLAIMER**
>
> This software has not undergone FDA clearance. Clinical decisions must be made by qualified professionals. This tool supplements, not replaces, institutional QA procedures.

---

## System Configuration

- **Eclipse Version:** 18.0
- **.NET Framework:** 4.8
- **Platform:** x64
- **Treatment Machines:** Varian Edge, Halcyon
- **Script Status:** Production
- **Validators:** 18 components
- **Architecture:** Hierarchical composite pattern with machine-specific logic

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
- Treatment orientation (Head First-Supine vs other orientations)
- DIBH gating status (Edge machines only)
- Aggregates results from child validators (listed below)

**Treatment Orientation:**
- **Info:** Head First-Supine orientation detected
- **Warning:** Non-standard orientation detected

**DIBH Gating (Edge Machines):**
- Detects "DIBH" keyword in:
  - CT Image ID
  - Structure Set ID
  - CT Series Comment
- If DIBH detected:
  - **Info:** Gating enabled (expected)
  - **Error:** Gating not enabled (must enable for DIBH)

**Clinical Relevance:** Patient orientation affects collision geometry. DIBH requires gating for respiratory motion management on Edge machines.

---

### 3. CT & Patient Validator

**What It Checks:**

**User Origin Coordinates:**
- X coordinate: Must be within ±0.5 cm (±5 mm) of CT zero
- Z coordinate (Eclipse UI Y): Must be within ±0.5 cm of CT zero
- Y coordinate (Eclipse UI Z): Must be between 8-50 cm from CT zero

**Output (User Origin):**
- **Info:** All coordinates within limits
- **Warning:** X coordinate outside ±0.5 cm limit
- **Warning:** Y coordinate outside ±0.5 cm limit
- **Warning:** Z coordinate outside 8-50 cm range

**CT Imaging Device:**
- Reads CT series description (comment field)
- Determines scan type:
  - Head scan: Starts with "Head" but NOT "Head and Neck" or "Head & Neck"
  - Non-head scan: All others
- Expected devices:
  - Head scan: `CT130265 HEAD`
  - Non-head scan: `CT130265`

**Output (Imaging Device):**
- **Info:** Correct imaging device for scan type
- **Error:** Incorrect imaging device (shows expected vs actual)

**Clinical Relevance:** User origin coordinates ensure accurate patient position transfer between imaging and treatment. Imaging device determines HU-to-density calibration curve for dose calculation.

---

### 4. User Origin Marker Validator

**What It Checks:**
Detects 3 radiopaque ball bearing markers placed at user origin position during CT simulation.

**Skip Conditions:**
- HyperArc plans (skipped automatically)
- Edge machines with Encompass fixation (both "Encompass" and "Encompass Base" structures present)

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
- No output for skipped cases (HyperArc or Encompass fixation)

**Clinical Relevance:** Ball bearing markers at user origin verify correct CT reference point setup. Missing markers may indicate:
- Markers placed outside search radius
- Markers not radiopaque enough (HU <500)
- User origin incorrectly positioned relative to actual marker locations
- Affects patient positioning coordinate system accuracy

---

### 5. Dose Validator

**What It Checks:**
- Dose grid resolution
  - SBRT/SRS (≥5 Gy/fraction): Must be ≤2mm
  - Conventional (<5 Gy/fraction): Must be ≤3mm
- Dose algorithm type (AAA, Acuros XB, etc.)
- Prescription dose presence and format
- Normalization method
- Total dose and fractionation

**Output:**
- **Error:** Grid resolution exceeds threshold for treatment type
- **Warning:** Missing or unusual dose parameters
- **Info:** Dose configuration within acceptable parameters

**Clinical Relevance:** Grid resolution affects dose calculation accuracy, particularly for small targets and high-gradient regions.

---

### 6. Field Validators

#### 6.1 Field Names Validator

**What It Checks:**
- Treatment field naming: `G[gantry angle]C[couch angle]` format
- Setup field naming format
- Duplicate field names across plan

**Output:**
- **Error:** Field name does not match expected format
- **Error:** Duplicate field names found
- **Info:** All field names follow standard format

**Clinical Relevance:** Standardized naming prevents delivery errors and improves treatment record clarity.

#### 6.2 Geometry Validator

**What It Checks:**

**Collimator Angle (All Machines):**
- Invalid angle ranges for treatment fields:
  - 268-272° (near 270°)
  - 358-2° (near 0°/360°)
  - 88-92° (near 90°)
- Duplicate collimator angles across fields

**Halcyon Isocenter Y Position:**
- IEC Y coordinate (DICOM Z relative to user origin)
- Valid range: -30 to +17 cm

**Tolerance Table (Edge/Halcyon):**
- Expected: "EDGE" for Edge machines
- Expected: "HAL" for Halcyon machines

**First Field Start Angle (Plans Without Couch Rotation):**
- First treatment field gantry angle should be close to 180°
- Uses `BeamsInTreatmentOrder` to determine actual first field

**MLC Overlap (Halcyon Only, No Couch Rotation):**
- For fields with same collimator angle
- Calculates jaw overlap in X direction (X1/X2 positions)
- Indicates divided field configuration

**Output:**
- **Error:** Collimator angle in invalid range
- **Error:** Halcyon isocenter Y outside limits (-30 to +17 cm)
- **Warning:** Duplicate collimator angle
- **Warning:** Incorrect tolerance table for machine type
- **Warning:** First field starts far from 180° gantry angle
- **Warning:** Halcyon divided fields have no jaw overlap
- **Info:** Valid collimator angle
- **Info:** Halcyon isocenter Y within limits
- **Info:** Correct tolerance table
- **Info:** First field correctly starts close to 180°
- **Info:** Halcyon divided fields have jaw overlap (shows overlap distance)

**Clinical Relevance:**
- Collimator angles near cardinal positions may cause delivery issues or collision risks
- Halcyon isocenter limits prevent collision with ring gantry structure
- Incorrect tolerance tables affect imaging and positioning tolerances
- Starting near 180° gantry angle minimizes patient setup time
- Jaw overlap in divided fields ensures contiguous dose coverage

#### 6.3 Setup Fields Validator

**What It Checks:**
- Presence of kV imaging fields
- Setup field technique type (planar kV imaging)
- Setup field naming conventions
- Imaging energy (kV range)

**Output:**
- **Warning:** Missing or improperly configured setup fields
- **Info:** Setup fields present and properly configured

**Clinical Relevance:** Setup imaging is required for patient positioning verification before treatment.

#### 6.4 Beam Energy Validator

**What It Checks:**

**Energy Consistency:**
- Compares energy modes across all treatment fields (excludes setup fields)
- Identifies mixed energy plans (e.g., 6X and 10X in same plan)

**Output (Consistency):**
- **Info:** All treatment fields use same energy
- **Warning:** Treatment fields use different energies (lists all energies found)
- **Info:** Per-field energy breakdown (shown when mixed energies detected)

**Edge Machine FFF Requirements:**
- For plans with dose/fraction ≥5 Gy on Edge machines:
  - Valid energies: 6X-FFF or 10X-FFF
  - Checks each treatment field individually

**Output (Edge FFF):**
- **Info:** Field correctly uses FFF energy for dose/fraction ≥5 Gy
- **Error:** Field does not use FFF energy for dose/fraction ≥5 Gy (shows actual energy)

**Clinical Relevance:** Energy consistency ensures predictable dose delivery. Edge machines require FFF (Flattening Filter Free) beams for SBRT/SRS treatments (≥5 Gy/fraction) to achieve appropriate dose rates and beam characteristics.

---

### 7. Reference Point Validator

**What It Checks:**
- Primary reference point exists
- Reference point has dose specification
- Prescription relationship to reference point

**Output:**
- **Error:** No primary reference point defined
- **Error:** Reference point missing dose specification
- **Info:** Primary reference point properly configured

**Clinical Relevance:** Reference point defines prescription dose and plan normalization.

---

### 8. Fixation Validator

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

### 9. Collision Validator

**Machine Detection:**
- Edge machines: Full collision validation
- Halcyon machines: Validation skipped (fixed ring geometry, different collision model)

**What It Checks (Edge Machines):**
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

**Thresholds (Edge):**
- ≤36.5 cm from isocenter: Safe clearance
- 36.5-37.5 cm: Limited clearance
- >37.5 cm: Potential collision risk

**Output:**
- **Info:** Maximum distance ≤36.5 cm (safe clearance)
- **Warning:** Maximum distance 36.5-37.5 cm (limited clearance)
- **Error:** Maximum distance >37.5 cm (collision risk)
- **Info:** Collision validation skipped - Halcyon machine
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

### 10. Optimization Validator

**Machine Type:** Edge only

**What It Checks:**

**Jaw Tracking:**
- Checks for presence of `OptimizationJawTrackingUsedParameter` in plan optimization parameters

**Aperture Shape Controller (ASC) - Edge SRS Plans Only:**
- Reads VMAT optimization parameter `VMAT/ApertureShapeController`
- Valid values for SRS: "High" or "Very High"
- SRS detection: Checks if any treatment beam contains SRS identifier

**Output:**
- **Info:** Jaw Tracking is used for Edge plan
- **Warning:** Jaw Tracking is NOT used for Edge plan
- **Info:** Aperture Shape Controller is set to 'High' or 'Very High' for Edge SRS plan
- **Warning:** Aperture Shape Controller not set to 'High' or 'Very High' for Edge SRS plan (shows actual value)
- **Warning:** Cannot determine Aperture Shape Controller setting for Edge SRS plan

**Clinical Relevance:**
- Jaw Tracking dynamically adjusts jaw positions to conform to MLC aperture shape, reducing out-of-field dose and improving MU efficiency for IMRT/VMAT plans
- Aperture Shape Controller "High" or "Very High" settings produce tighter MLC conformity to small SRS targets, reducing dose to surrounding normal tissue

---

### 11. Planning Structures Validator

**What It Checks:**
Validates air cavity structures used for density override in dose calculation.

**Structure Pattern:** `z_Air_[density]HU` (e.g., `z_Air_-800HU`)

**Density Override Validation:**
- Extracts expected density from structure name
- Verifies assigned HU override matches expected value
- Tolerance: ±1 HU

**Original CT Density Distribution:**
- Samples voxels inside z_Air structure
- Checks percentage of voxels exceeding threshold
- Threshold: Expected density + 25 HU
- Sampling: Every 2nd voxel in X/Y, every 2nd slice in Z
- Valid limit: ≤5% of voxels above threshold

**Output:**
- **Info:** Air structure has correct density override (shows HU value)
- **Info:** [X]% of voxels exceed [threshold] HU (within 5% limit)
- **Error:** Air structure has incorrect density override (shows expected vs actual)
- **Error:** Air structure has no density override assigned
- **Warning:** [X]% of voxels exceed [threshold] HU (exceeds 5% limit)

**Clinical Relevance:** Air cavity structures (e.g., sinus, lung) require density overrides when CT imaging artifacts or contrast affect HU accuracy. Validation ensures:
- Override value matches structure naming convention
- Original CT shows predominantly low-density tissue (confirms structure delineation accuracy)
- High percentage (>5%) above threshold suggests structure includes non-air tissue or imaging artifacts

---

### 12. Contrast Structure Validator

**What It Checks:**
- Reads Study.Comment field from DICOM study metadata
- Searches for "CONTRAST" keyword (case-insensitive)
- If keyword found, verifies presence of z_Contrast* structure

**Structure Pattern:** `z_Contrast*` (e.g., `z_Contrast`, `z_Contrast_Bladder`)

**Output:**
- **Info:** Study contains contrast imaging and z_Contrast* structure exists
- **Warning:** Study comment contains 'CONTRAST' but z_Contrast* structure is missing - consider adding if needed
- No output if Study.Comment does not contain "CONTRAST"

**Clinical Relevance:** When contrast media is present in CT imaging:
- Contrast significantly increases HU in blood vessels and organs
- z_Contrast structures allow density overrides to correct HU for dose calculation
- Missing structure when contrast is documented may cause dose calculation errors in contrast-enhanced regions

---

### 13. PTV-Body Proximity Validator

**What It Checks:**
- Identifies all structures with "PTV" prefix (case-insensitive)
- Requires BODY structure (DICOM type: EXTERNAL)
- Calculates minimum 3D distance from each PTV contour point to closest BODY surface point
- Checks all slices with both PTV and BODY contours
- Handles multiple body contours per slice (outer skin + internal cavities)

**Distance Calculation:**
```
For each PTV:
  For each CT slice:
    For each PTV contour point:
      Calculate 3D distance to closest BODY contour point
  Record minimum distance found
```

**Threshold:** 4.0 mm

**Output:**
- **Info:** Closest PTV is [distance] mm from body surface (when all PTVs ≥4 mm)
- **Warning:** PTV [name] is [distance] mm from body surface - check if EVAL structure needed (for any PTV <4 mm)
- **Warning:** Cannot validate - BODY structure not found
- No output if no PTV structures present

**Clinical Relevance:** Superficial PTVs (<4 mm from skin) may require EVAL structure (PTV with skin buildup region subtracted) to account for:
- Dose buildup region in photon beams
- Skin-sparing effect
- Surface dose characteristics

Warning prompts planner to verify whether PTV or PTV_EVAL should be used for optimization.

---

## Validation Architecture

```
RootValidator
├── CourseValidator
└── PlanValidator
    ├── CTAndPatientValidator
    ├── UserOriginMarkerValidator
    ├── DoseValidator
    ├── FieldsValidator
    │   ├── FieldNamesValidator
    │   ├── GeometryValidator
    │   ├── SetupFieldsValidator
    │   └── BeamEnergyValidator
    ├── ReferencePointValidator
    ├── FixationValidator
    ├── CollisionValidator
    ├── OptimizationValidator
    ├── PlanningStructuresValidator
    ├── ContrastStructureValidator
    └── PTVBodyProximityValidator
```

**Execution:** Validators execute hierarchically. Child validators run first, then parent validators append additional checks.

---

## Machine-Specific Logic

### Edge Machines
- DIBH gating validation active
- FFF energy requirements enforced (dose/fraction ≥5 Gy)
- Collision validation with 36.5/37.5 cm thresholds

### Halcyon Machines
- Collision validation skipped (fixed ring geometry)
- Standard energy and field validations apply

**Machine Detection:**
Uses `PlanUtilities` class to identify machine type from `TreatmentUnit.Id` in beam configuration.

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
cd ClinicE
msbuild PlanCrossCheck.csproj /p:Configuration=Release /p:Platform=x64
```

**Output:** `ClinicE/Release/TEST_Cross_Check.esapi.dll`

**Note:** Assembly name includes `TEST_` prefix per institutional deployment protocol.

---

## Installation

1. Build project using above command
2. Copy `TEST_Cross_Check.esapi.dll` to Eclipse plugin directory:
   - Default: `C:\Program Files (x86)\Varian\RTM\18.0\ExternalBeam\PlugIns\`
3. Restart Eclipse
4. Access via Scripts menu → TEST_Cross_Check

---

## Requirements

- Windows x64
- Varian Eclipse 18.0 with ESAPI
- .NET Framework 4.8
- MSBuild tools (Visual Studio 2019+)

---

## License

PlanCrossCheck Community License - See [LICENSE](../LICENSE)

Free for internal organizational and non-profit use. Commercial use requires separate license.

Copyright (c) 2025 Sergei Rusetskii

---

*Built with Varian Eclipse Scripting API (ESAPI) 18.0 and .NET Framework 4.8*
