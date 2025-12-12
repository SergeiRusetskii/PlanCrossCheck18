# CollisionValidator — Technical Specification

*Framework: Claude Code Starter v2.1*
*Created: 2025-12-12*
*Location: `Validators/CollisionValidator.cs`*

---

## Overview

**Purpose:** Validates clearance between treatment machine gantry ring and fixation devices to prevent potential collisions during treatment delivery.

**Scope:** Supports both Halcyon and Edge (TrueBeam) linear accelerators with machine-specific thresholds and assessment methodologies.

**Validation Category:** `Collision.Clearance`

---

## Machine-Specific Implementations

### 1. Halcyon Collision Detection

#### Methodology
- **Assessment scope:** Full 360° rotation
- **Ring radius:** 47.5 cm (475 mm)
- **Metric:** Clearance from gantry ring to fixation devices

#### Thresholds
| Clearance | Severity | Message |
|-----------|----------|---------|
| < 4.5 cm | Error | "potential collision risk" |
| 4.5-5.0 cm | Warning | "limited clearance" |
| ≥ 5.0 cm | Info | No warning |

#### Algorithm Steps

**Step 1:** Get isocenter position from first beam

**Step 2:** Iterate through fixation structures matching prefixes:
- `BODY`
- `z_AltaLD`
- `z_AltaHD`
- `CouchSurface`
- `z_ArmShuttle`
- `z_VacBag`

**Step 3:** For each structure, scan all contour points across all CT slices:
```
For each CT slice i in [0, Image.ZSize):
  For each contour in structure.GetContoursOnImagePlane(i):
    For each point in contour:
      radialDistance = √[(point.x - iso.x)² + (point.y - iso.y)²]
      Track maximum radialDistance and furthest point
```

**Step 4:** Calculate clearance:
```
clearance (cm) = (ringRadius - maxRadialDistance) / 10
```

**Step 5:** Determine anatomical direction of furthest point:
```
angle = atan2(point.y - iso.y, point.x - iso.x) × 180/π

-45° to 45°   → "left"
45° to 135°   → "anterior"
135° to -135° → "right"
-135° to -45° → "posterior"
```

**Step 6:** Find structure with smallest clearance (worst-case scenario)

**Step 7:** Generate result with severity based on clearance threshold

---

### 2. Edge Collision Detection

#### Methodology
- **Assessment scope:** Only within treated gantry angles ± 10° margin
- **Ring radius:** 38 cm (380 mm)
- **Metric:** Distance from isocenter to fixation devices
- **Special handling:** Skips assessment if couch rotation present (requires manual verification)

#### Thresholds
| Distance from Iso | Severity | Message |
|-------------------|----------|---------|
| > 38 cm | Error | "potential collision risk" |
| 37-38 cm | Warning | "limited clearance" |
| ≤ 37 cm | Info | No warning |

#### Algorithm Steps

**Step 1: Machine Type & Couch Rotation Check**
- Detect if machine is Edge: `PlanUtilities.IsEdgeMachine(machineId)`
  - Checks if `machineId == "TrueBeamSN6368"`
- **Early exit:** If ANY beam has couch rotation (>0.1°):
  - Skip collision assessment
  - Return Info message: *"Collision assessment skipped for plans with couch rotation - manual verification required"*

**Step 2: Initialize Parameters**
- Get isocenter position from first beam
- Ring radius: 380.0 mm (38 cm)
- Angular margins:
  - Arc fields: ±10° expansion
  - Static fields: ±10° expansion

**Step 3: Calculate Treated Gantry Angular Sectors**
- Get all treatment beams (exclude setup fields)
- Call `PlanUtilities.GetCoveredAngularSectors(treatmentBeams, arcMarginDegrees: 10, staticMarginDegrees: 10)`
- Returns list of `(startAngle, endAngle)` pairs representing gantry coverage

##### How Sector Calculation Works
See `PlanUtilities.GetCoveredAngularSectors()`:

1. **For each beam:**
   - **Static field:** Create sector `[gantryAngle - 10°, gantryAngle + 10°]`
   - **Arc field:** Create sector from start to end gantry angle
     - Expand by ±10° along rotation direction
     - Handle gantry direction (CW vs CCW)
     - Handle wraparound angles (e.g., 350° → 10°)

2. **Normalize sectors:**
   - Convert all angles to 0-360° range
   - Split wraparound sectors into non-wrapping parts
     - Example: `(350°, 10°)` becomes `[(350°, 360°), (0°, 10°)]`

3. **Merge overlapping sectors:**
   - Sort by start angle
   - Merge adjacent/overlapping sectors (within 1° tolerance)

**Step 4: Iterate Through Fixation Structures**

Same prefixes as Halcyon:
- `BODY`, `z_AltaLD`, `z_AltaHD`, `CouchSurface`, `z_ArmShuttle`, `z_VacBag`

For each matching structure:

##### Step 4a: Scan All Contour Points
```
For each CT slice i in [0, Image.ZSize):
  For each contour in structure.GetContoursOnImagePlane(i):
    For each point in contour:
```

##### Step 4b: Calculate Radial Distance
```
radialDistance = √[(point.x - iso.x)² + (point.y - iso.y)²]
```

##### Step 4c: Angular Filtering
```
IF treatment sectors exist (not full arc):
  angle = atan2(point.y - iso.y, point.x - iso.x) × 180/π
  Normalize angle to 0-360°

  IF !IsAngleInSectors(angle, coveredSectors):
    Skip this point (gantry won't pass here)
```

**Purpose:** Only check collision risk where gantry actually rotates, not full 360°

##### Step 4d: Track Maximum
```
IF radialDistance > maxRadialDistance:
  maxRadialDistance = radialDistance
  furthestPoint = point
```

**Step 5: Determine Worst-Case Structure**
```
clearance (cm) = (380 mm - maxRadialDistance) / 10
distanceCm = maxRadialDistance / 10
```

Find structure with smallest clearance (closest to ring)

**Step 6: Calculate Direction**

Same as Halcyon (see above)

**Step 7: Severity Assessment**
```
IF distanceCm > 38.0: severity = Error
ELSE IF distanceCm > 37.0: severity = Warning
ELSE: severity = Info
```

**Step 8: Generate Result**

Message format:
```
"Max distance {X.X} cm from isocenter to fixation device '{structureId}'
({direction} edge) within treated gantry angles (+/-10 deg)"
```

Append severity-specific warning if needed

---

## Key Differences: Halcyon vs Edge

| Aspect | Halcyon | Edge |
|--------|---------|------|
| **Metric** | Clearance to ring (4.5-5.0 cm) | Distance from isocenter (37-38 cm) |
| **Angular Scope** | Full 360° | Treated angles ± 10° only |
| **Ring Radius** | 47.5 cm | 38 cm |
| **Couch Rotation** | No special handling | Skips check if present |
| **Direction Reporting** | ✓ Yes | ✓ Yes |

---

## Supporting Utilities

### PlanUtilities Helper Methods

**Machine Detection:**
- `IsEdgeMachine(machineId)` → Returns `machineId == "TrueBeamSN6368"`
- `IsHalcyonMachine(machineId)` → Returns `machineId.StartsWith("Halcyon")`

**Couch Rotation Detection:**
- `HasAnyFieldWithCouch(beams)` → Returns true if any beam has patient support angle > 0.1°

**Angular Sector Analysis:**
- `GetCoveredAngularSectors(beams, arcMargin, staticMargin)` → Returns list of `(start, end)` angle pairs
- `IsAngleInSectors(angle, sectors)` → Checks if angle falls within any sector
- `GetArcSpanDegrees(beam)` → Calculates arc span accounting for gantry direction

See `Validators/Utilities/PlanUtilities.cs` for implementation details.

---

## Usage in Validation Hierarchy

**Parent:** `PlanValidator`
**Position:** After `FixationValidator`, before `OptimizationValidator`

```csharp
public PlanValidator()
{
    AddValidator(new CTAndPatientValidator());
    AddValidator(new UserOriginMarkerValidator());
    AddValidator(new DoseValidator());
    AddValidator(new FieldsValidator());
    AddValidator(new ReferencePointValidator());
    AddValidator(new FixationValidator());
    AddValidator(new CollisionValidator());  // ← Added here
    AddValidator(new OptimizationValidator());
    AddValidator(new PlanningStructuresValidator());
    AddValidator(new PTVBodyProximityValidator());
}
```

---

## Design Rationale

### Why Separate from FixationValidator?

**Before refactoring:**
- `FixationValidator` handled both fixation device validation AND collision checks
- Mixed concerns: structural validation vs. geometric collision assessment
- Monolithic: 336 lines including complex geometry calculations

**After refactoring:**
- **`FixationValidator`** (49 lines):
  - Required structure existence (Halcyon-specific)
  - Density override validation
  - Pure fixation device concerns

- **`CollisionValidator`** (296 lines):
  - Geometric clearance calculations
  - Machine-specific collision algorithms
  - Angular sector filtering
  - Single responsibility: collision prevention

**Benefits:**
- ✓ Clear separation of concerns
- ✓ Easier testing and maintenance
- ✓ More descriptive class names
- ✓ Simpler cognitive load per file

---

## Example Outputs

### Halcyon Example
```
Category: Collision.Clearance
Severity: Warning
Message: "Clearance 4.8 cm between fixation device 'z_AltaLD_120HU' (anterior edge)
         and Halcyon ring - limited clearance"
```

### Edge Example (Acceptable)
```
Category: Collision.Clearance
Severity: Info
Message: "Max distance 35.2 cm from isocenter to fixation device 'BODY' (left edge)
         within treated gantry angles (+/-10 deg)"
```

### Edge Example (Warning)
```
Category: Collision.Clearance
Severity: Warning
Message: "Max distance 37.4 cm from isocenter to fixation device 'CouchSurface'
         (posterior edge) within treated gantry angles (+/-10 deg) - limited clearance"
```

### Edge Example (Couch Rotation)
```
Category: Collision.Clearance
Severity: Info
Message: "Collision assessment skipped for plans with couch rotation - manual verification required"
```

---

## Testing Considerations

**Test Cases:**
1. Halcyon plan with clearance < 4.5 cm → Should report Error
2. Edge plan with distance > 38 cm → Should report Error
3. Edge plan with couch rotation → Should skip and report Info
4. Edge plan with arc 0°-180° → Should only check points in 350°-190° (±10° margin)
5. Static field at 90° → Should check points in 80°-100° sector
6. Edge plan with no fixation structures → Should not crash (empty result)

**Integration:**
- Runs as part of `PlanValidator` composite
- Results appear in UI under "Collision.Clearance" category

---

*This specification documents the extracted and refactored collision detection logic as of 2025-12-12*
