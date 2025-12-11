using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // 4 Fixation devices validator
    public class FixationValidator : ValidatorBase
    {
        // Shared fixation structure prefixes for collision assessment (both Halcyon and Edge)
        private static readonly string[] FixationStructurePrefixesForCollision = new[]
        {
            "BODY",
            "z_AltaLD",
            "z_AltaHD",
            "CouchSurface",
            "z_ArmShuttle",
            "z_VacBag"
        };

        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.StructureSet != null)
            {
                // Check for Halcyon-specific structures
                string machineId = context.PlanSetup?.Beams?.FirstOrDefault()?.TreatmentUnit.Id;
                bool isHalcyonMachine = PlanUtilities.IsHalcyonMachine(machineId);

                if (isHalcyonMachine)
                {
                    // Required structures for Halcyon plans
                    var requiredPrefixes = new[] {
                        "z_AltaHD_", "z_AltaLD_",
                        "CouchSurface", "CouchInterior"
                    };

                    foreach (var prefix in requiredPrefixes)
                    {
                        bool structureExists = context.StructureSet.Structures.Any(s =>
                            s.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                        results.Add(CreateResult(
                            "Fixation.Structures",
                            structureExists
                                ? $"Required Halcyon structure '{prefix}*' exists"
                                : $"Required Halcyon structure '{prefix}*' is missing",
                            structureExists ? ValidationSeverity.Info : ValidationSeverity.Error
                        ));
                    }
                }

                // Check collision for Halcyon machine
                if (isHalcyonMachine && context.PlanSetup?.Beams?.Any() == true)
                {
                    // Get the isocenter position (from the first beam)
                    VVector isocenter = context.PlanSetup.Beams.First().IsocenterPosition;

                    var ringRadius = 475; // 47.5 cm in mm

                    // Track information for each structure
                    var structureDetails = new List<(
                        Structure Structure, double MaxDistance,
                        VVector FurthestPoint, double Clearance)>();

                    // Check each candidate structure
                    foreach (var prefix in FixationStructurePrefixesForCollision)
                    {
                        var matchingStructures = context.StructureSet.Structures
                            .Where(s => s.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        foreach (var structure in matchingStructures)
                        {
                            double maxRadialDistance = 0;
                            VVector furthestPoint = new VVector();

                            // Loop through all image planes containing the structure
                            for (int i = 0; i < context.StructureSet.Image.ZSize; i++)
                            {
                                var contours = structure.GetContoursOnImagePlane(i);
                                if (contours.Any())
                                {
                                    foreach (var contour in contours)
                                    {
                                        foreach (var point in contour)
                                        {
                                            // Calculate the radial distance in the axial plane (X and Y in DICOM)
                                            double radialDistance = Math.Sqrt(
                                                Math.Pow(point.x - isocenter.x, 2) +
                                                Math.Pow(point.y - isocenter.y, 2)
                                            );

                                            // Keep track of furthest point (largest radial distance)
                                            if (radialDistance > maxRadialDistance)
                                            {
                                                maxRadialDistance = radialDistance;
                                                furthestPoint = point;
                                            }
                                        }
                                    }
                                }
                            }

                            // Calculate clearance for this structure
                            if (maxRadialDistance > 0)
                            {
                                double clearance = (ringRadius - maxRadialDistance) / 10.0; // Convert mm to cm
                                structureDetails.Add((structure, maxRadialDistance, furthestPoint, clearance));
                            }
                        }
                    }

                    // Check if we found any structures
                    if (structureDetails.Any())
                    {
                        // Find the structure with the minimum clearance (closest to colliding)
                        var closestStructure = structureDetails
                            .OrderBy(item => item.Clearance)
                            .First();

                        var structure = closestStructure.Structure;
                        var maxRadialDistance = closestStructure.MaxDistance;
                        var furthestPoint = closestStructure.FurthestPoint;
                        var clearance = closestStructure.Clearance;

                        // Determine direction of furthest point
                        string direction = "";
                        if (maxRadialDistance > 0)
                        {
                            // Calculate direction from isocenter to furthest point
                            double angleRad = Math.Atan2(furthestPoint.y - isocenter.y, furthestPoint.x - isocenter.x);
                            double angleDeg = angleRad * 180.0 / Math.PI;
                            direction = angleDeg >= -45 && angleDeg < 45 ? "left" :
                                        angleDeg >= 45 && angleDeg < 135 ? "anterior" :
                                        angleDeg >= 135 || angleDeg < -135 ? "right" :
                                        "posterior";
                        }

                        // Set severity based on clearance
                        ValidationSeverity severity = ValidationSeverity.Info;
                        if (clearance < 4.5)
                            severity = ValidationSeverity.Error;
                        else if (clearance < 5.0)
                            severity = ValidationSeverity.Warning;

                        // Create message
                        string message = $"Clearance {clearance:F1} cm between " +
                            $"fixation device '{structure.Id}' ({direction} edge) and Halcyon ring";
                        if (clearance < 5.0)
                            message += clearance < 4.5 ? " - potential collision risk" : " - limited clearance";

                        results.Add(CreateResult(
                            "Fixation.Clearance",
                            message,
                            severity
                        ));
                    }
                    else
                    {
                        results.Add(CreateResult(
                            "Fixation.Clearance",
                            "Cannot assess Halcyon collision risk - none of the required fixation devices found",
                            ValidationSeverity.Warning
                        ));
                    }
                }

                // Check collision for Edge machine
                if (PlanUtilities.IsEdgeMachine(machineId) && context.PlanSetup?.Beams?.Any() == true)
                {
                    // Check for couch rotation - if ANY field has couch rotation, skip assessment
                    var allBeams = context.PlanSetup.Beams.ToList();
                    if (PlanUtilities.HasAnyFieldWithCouch(allBeams))
                    {
                        results.Add(CreateResult(
                            "Fixation.Clearance",
                            "Collision assessment skipped for plans with couch rotation - manual verification required",
                            ValidationSeverity.Info
                        ));
                    }
                    else
                    {
                        // Get isocenter position from first beam
                        VVector isocenter = context.PlanSetup.Beams.First().IsocenterPosition;
                        var ringRadius = 380; // 38 cm in mm

                        // Get treatment beams only (no setup fields)
                        var treatmentBeams = context.PlanSetup.Beams.Where(b => !b.IsSetupField).ToList();

                        // Determine if we need sector-based filtering or full 360° check
                        bool isFullArc = PlanUtilities.IsFullArcCoverage(treatmentBeams);
                        List<(double start, double end)> coveredSectors = null;

                        if (!isFullArc)
                        {
                            // Partial coverage - build sector list for filtering
                            coveredSectors = PlanUtilities.GetCoveredAngularSectors(treatmentBeams);
                        }

                        // Track information for each structure
                        var structureDetails = new List<(
                            Structure Structure, double MaxDistance,
                            VVector FurthestPoint, double Clearance)>();

                        // Check each candidate structure
                        foreach (var prefix in FixationStructurePrefixesForCollision)
                        {
                            var matchingStructures = context.StructureSet.Structures
                                .Where(s => s.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                                .ToList();

                            foreach (var structure in matchingStructures)
                            {
                                double maxRadialDistance = 0;
                                VVector furthestPoint = new VVector();

                                // Loop through all image planes containing the structure
                                for (int i = 0; i < context.StructureSet.Image.ZSize; i++)
                                {
                                    var contours = structure.GetContoursOnImagePlane(i);
                                    if (contours.Any())
                                    {
                                        foreach (var contour in contours)
                                        {
                                            foreach (var point in contour)
                                            {
                                                // Calculate the radial distance in the axial plane (X and Y in DICOM)
                                                double radialDistance = Math.Sqrt(
                                                    Math.Pow(point.x - isocenter.x, 2) +
                                                    Math.Pow(point.y - isocenter.y, 2)
                                                );

                                                // If partial arc, check if this point's angle is in covered sectors
                                                if (!isFullArc)
                                                {
                                                    // Calculate angle of this point from isocenter
                                                    double angleRad = Math.Atan2(point.y - isocenter.y, point.x - isocenter.x);
                                                    double angleDeg = angleRad * 180.0 / Math.PI;

                                                    // Normalize to 0-360
                                                    if (angleDeg < 0)
                                                        angleDeg += 360;

                                                    // Skip this point if it's not in a covered sector
                                                    if (!PlanUtilities.IsAngleInSectors(angleDeg, coveredSectors))
                                                        continue;
                                                }

                                                // Keep track of furthest point (largest radial distance)
                                                if (radialDistance > maxRadialDistance)
                                                {
                                                    maxRadialDistance = radialDistance;
                                                    furthestPoint = point;
                                                }
                                            }
                                        }
                                    }
                                }

                                // Calculate clearance for this structure
                                if (maxRadialDistance > 0)
                                {
                                    double clearance = (ringRadius - maxRadialDistance) / 10.0; // Convert mm to cm
                                    structureDetails.Add((structure, maxRadialDistance, furthestPoint, clearance));
                                }
                            }
                        }

                        // Check if we found any structures
                        if (structureDetails.Any())
                        {
                            // Find the structure with the minimum clearance (closest to colliding)
                            var closestStructure = structureDetails
                                .OrderBy(item => item.Clearance)
                                .First();

                            var structure = closestStructure.Structure;
                            var maxRadialDistance = closestStructure.MaxDistance;
                            var furthestPoint = closestStructure.FurthestPoint;
                            var clearance = closestStructure.Clearance;

                            // Determine direction of furthest point
                            string direction = "";
                            if (maxRadialDistance > 0)
                            {
                                // Calculate direction from isocenter to furthest point
                                double angleRad = Math.Atan2(furthestPoint.y - isocenter.y, furthestPoint.x - isocenter.x);
                                double angleDeg = angleRad * 180.0 / Math.PI;
                                direction = angleDeg >= -45 && angleDeg < 45 ? "left" :
                                            angleDeg >= 45 && angleDeg < 135 ? "anterior" :
                                            angleDeg >= 135 || angleDeg < -135 ? "right" :
                                            "posterior";
                            }

                            // Set severity based on clearance (Edge thresholds: warning <2cm, error <1cm)
                            ValidationSeverity severity = ValidationSeverity.Info;
                            if (clearance < 1.0)
                                severity = ValidationSeverity.Error;
                            else if (clearance < 2.0)
                                severity = ValidationSeverity.Warning;

                            // Create message
                            string message = $"Clearance {clearance:F1} cm between " +
                                $"fixation device '{structure.Id}' ({direction} edge) and Edge ring";
                            if (clearance < 2.0)
                                message += clearance < 1.0 ? " - potential collision risk" : " - limited clearance";

                            results.Add(CreateResult(
                                "Fixation.Clearance",
                                message,
                                severity
                            ));
                        }
                        // Note: No warning if structures not found (per requirement - different from Halcyon)
                    }
                }

                // Check density overrides for all fixation structures
                var fixationPrefixes = new[]
                {
                "z_AltaHD_", "z_AltaLD_", "z_FrameHN_", "z_MaskLock_",
                "z_FrameHead_", "z_LocBar_", "z_ArmShuttle_", "z_EncFrame_",
                "z_VacBag_", "z_Contrast_", "z_ArmHoldR_", "z_ArmHoldR_",
                "z_FlexHigh_", "z_FlexLow_", "z_LocBarMR_", "z_VacIndex_"
            };

                foreach (var structure in context.StructureSet.Structures)
                {
                    // Check if this structure matches any of our prefixes
                    var matchingPrefix = fixationPrefixes.FirstOrDefault(prefix =>
                        structure.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                    if (matchingPrefix != null)
                    {
                        // Extract the expected density from structure name (+200HU, -390HU, etc.)
                        if (structure.Id.Contains("_") && structure.Id.EndsWith("HU", StringComparison.OrdinalIgnoreCase))
                        {
                            string densityStr = structure.Id.Substring(structure.Id.LastIndexOf('_') + 1);

                            // Remove "HU" and parse the value
                            if (double.TryParse(densityStr.Substring(0, densityStr.Length - 2),
                                               out double expectedDensity))
                            {
                                // Get actual density override using out parameter
                                double actualDensity;
                                bool hasAssignedHU = structure.GetAssignedHU(out actualDensity);

                                if (hasAssignedHU)
                                {
                                    bool isDensityCorrect = Math.Abs(actualDensity - expectedDensity) < 1; // Small tolerance

                                    results.Add(CreateResult(
                                        "Fixation.Density",
                                        isDensityCorrect
                                            ? $"Structure '{structure.Id}' has correct " +
                                            $"density override ({actualDensity} HU)"
                                            : $"Structure '{structure.Id}' has incorrect " +
                                            $"density override: {actualDensity} HU (expected: {expectedDensity} HU)",
                                        isDensityCorrect ? ValidationSeverity.Info : ValidationSeverity.Error
                                    ));
                                }
                                else
                                {
                                    results.Add(CreateResult(
                                        "Fixation.Density",
                                        $"Structure '{structure.Id}' has no " +
                                        $"density override assigned (expected: {expectedDensity} HU)",
                                        ValidationSeverity.Error
                                    ));
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }
    }
}
