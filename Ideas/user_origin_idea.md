Goal: verify that the planner placed the User Origin on the intended skin-marker line, without creating any new structures.

Approach (read-only):

1. Locate the slice and coordinates

* Take `plan.UserOrigin` (patient/DICOM coords) and map it to the CT's axial slice index:\
  `k = round((UserOrigin.z − Image.Origin.z) / Image.ZRes)` (mind sign/orientation; use your existing helpers for consistency).

* Keep the in-plane (x, y) in patient coords for contour/line maths.

2. Get the Body contour(s) on that slice

* Find the external structure (ROI DICOM type = `EXTERNAL`; typical Ids "External", "BODY", etc.).

* Call `GetContoursOnImagePlane(k)` to obtain one or more polylines (each a closed skin loop).

3. Compute the three skin intersections

* Construct the horizontal line `y = y0` (through the user origin) and find its two intersections with the external polyline that encloses (x0, y0). Choose the pair with minimum |x − x0| to avoid arms, vac-lock edges, etc.

* Construct the vertical line `x = x0`; take the intersection above the origin (largest y if HFS; adjust for other orientations using `TreatmentOrientation`).

Robustness: if multiple external loops exist on that slice, first pick the loop that contains (x0, y0). If none contain it (rare, e.g., origin off-skin), pick the loop with the closest point to (x0, y0).

4. Sample CT voxels around each intersection

* Convert each intersection point to image indices (i, j) on slice k.

* Define a 5 mm radius neighbourhood (convert to pixels using XRes/YRes).

* Read voxel values for that 2D patch (ESAPI `Image` exposes slice voxel reads; use the slice-overload of `GetVoxels`/equivalent).

* Compute a simple statistic: `maxHU` or `% of voxels > threshold`.

Thresholds and tolerances (configurable):

* Metal/radio-opaque sticker: start with `HU ≥ 2000` and/or strong local max. For plastic BBs, you may need ~800–1200 HU.

* Hit criterion: e.g., `maxHU ≥ threshold` OR `≥30%` of the patch above threshold.

* Slice halo: search k, and if not found, check k±1 (±2 for thick markers).

5. Decision & reporting

* Expect 3/3 "hits".

* If 2/3: "likely on the right line but one marker not detected—check imaging artefact / OOF slice."

* If ≤1/3: flag as fail; report distances from origin to nearest skin intersections to aid manual correction.

* Add parameters (threshold\_HU, radius\_mm, slice\_search\_span, external\_roi\_fallback) to your central config.

6. Fallbacks when "External" is missing or unreliable

* Derive a synthetic skin line along each axis by scanning intensities on slice k from (x0, y0) outward until you cross an air↔tissue boundary (e.g., HU threshold −350 to −200). This keeps the check structure-free if the external ROI is absent or malformed.

7. Integration points

* Best home: extend `CTAndPatientValidator` or create a dedicated `UserOriginMarkerValidator` and add it to `PlanValidator`.

* Reuse your existing DICOM↔image coordinate helpers and the contour-intersection utility style you already use in the fixation validator.

* Emit:

  * INFO: "3/3 markers detected at (xL, y0), (xR, y0), (x0, yU); maxHU=…; radius=5 mm; slice k(±…);"

  * WARN: partial detection or reliance on fallback.

  * ERROR: not detected or geometry inconsistent (e.g., fewer than 2 horizontal skin intersections).

8. Performance & safety

* This is per-slice, three small patches—negligible runtime even for large lists (100+ per day).

* Works in read-only ESAPI context; no plan/structure modifications, hence no need for additional licences (structure creation APIs like `AddStructure`/`AddContourOnImagePlane` are write operations you won't use here).

Known pitfalls & how to handle:

* Arms/immobilisation crossing the vertical: choose the "upper" intersection relative to treatment orientation, not array index.

* Origin not exactly on the marker slice: use ±1–2 slice search.

* Non-metallic stickers: lower HU threshold; consider "prominence over local background" instead of absolute HU.

* External ROI jaggy/holes: switch to the image-threshold fallback.

Deliverables:

* New validator with configurable parameters.

* Unit-style tests on a handful of anonymised CTs (metal BB, plastic BB, no marker, arms-up) to fix thresholds.

* One settings subsection in your UI (radius, HU threshold, slice halo).
