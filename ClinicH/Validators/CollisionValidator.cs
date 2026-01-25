using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    /// <summary>
    /// Validates clearance between treatment machine (gantry) and patient/fixation devices
    /// to prevent potential collisions during treatment delivery.
    ///
    /// CONSERVATIVE APPROACH: This validator checks the maximum radial distance from isocenter
    /// across a full 360° sweep, regardless of the actual arc angles in the plan. This ensures
    /// safety even if gantry motion occurs outside planned treatment angles (e.g., during
    /// gantry travel to start position, emergency stops, or plan modifications).
    ///
    /// Thresholds for TrueBeam STX (same as Edge):
    /// - &gt;36.5 cm from isocenter = Warning (limited clearance)
    /// - &gt;37.5 cm from isocenter = Error (potential collision risk)
    /// </summary>
    public class CollisionValidator : ValidatorBase
    {
        // Helper class to store structure collision details
        private class StructureCollisionInfo
        {
            public Structure Structure { get; set; }
            public double MaxDistance { get; set; }
            public VVector FurthestPoint { get; set; }
            public double DistanceCm { get; set; }
        }

        // Fixation structure prefixes used for collision assessment
        private static readonly string[] FixationStructurePrefixesForCollision = new[]
        {
            "BODY",
            "CouchSurface",
            "MP_Optek_BP",
            "MP_WingSpan",
            "MP_BrB_Up_BaPl",
            "MP_BrB_Bott_BaPl",
            "MP_Solo_BPl",
            "MP_Enc_BPl",
            "MP_Enc_HFr"
        };

        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.StructureSet == null || context.PlanSetup?.Beams == null || !context.PlanSetup.Beams.Any())
            {
                return results;
            }

            string machineId = context.PlanSetup.Beams.FirstOrDefault()?.TreatmentUnit.Id;
            bool isTrueBeamSTX = PlanUtilities.IsTrueBeamSTX(machineId);

            if (isTrueBeamSTX)
            {
                results.AddRange(ValidateTrueBeamSTXCollision(context));
            }
            else
            {
                results.Add(CreateResult(
                    "Collision",
                    $"Collision validation skipped - not a TrueBeam STX machine (detected: {machineId ?? "unknown"})",
                    ValidationSeverity.Info
                ));
            }

            return results;
        }

        /// <summary>
        /// Validates collision risk for TrueBeam STX machines.
        /// Uses conservative approach: checks maximum distance from isocenter across full 360°.
        /// Skips check if couch rotation is present (requires manual verification).
        /// Thresholds: &gt;36.5 cm = Warning, &gt;37.5 cm = Error
        /// </summary>
        private IEnumerable<ValidationResult> ValidateTrueBeamSTXCollision(ScriptContext context)
        {
            var results = new List<ValidationResult>();
            var allBeams = context.PlanSetup.Beams.ToList();

            // Skip if couch rotation present (requires manual verification)
            if (PlanUtilities.HasAnyFieldWithCouch(allBeams))
            {
                results.Add(CreateResult(
                    "Collision",
                    "Collision assessment skipped for plans with couch rotation - manual verification required",
                    ValidationSeverity.Info
                ));
                return results;
            }

            VVector isocenter = context.PlanSetup.Beams.First().IsocenterPosition;

            // TrueBeam STX thresholds (same as Edge):
            // >36.5 cm from iso -> warning
            // >37.5 cm from iso -> error
            // Conservative approach: check full 360° coverage for maximum safety
            var structureDetails = new List<StructureCollisionInfo>();

            foreach (var prefix in FixationStructurePrefixesForCollision)
            {
                var matchingStructures = context.StructureSet.Structures
                    .Where(s => s.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var structure in matchingStructures)
                {
                    double maxRadialDistance = 0;
                    VVector furthestPoint = new VVector();

                    // Scan all contour points across all CT slices
                    for (int i = 0; i < context.StructureSet.Image.ZSize; i++)
                    {
                        var contours = structure.GetContoursOnImagePlane(i);
                        foreach (var contour in contours)
                        {
                            foreach (var point in contour)
                            {
                                // Calculate 2D radial distance from isocenter (ignore Z)
                                // This represents the distance in the plane perpendicular to gantry rotation
                                double radialDistance = Math.Sqrt(
                                    Math.Pow(point.x - isocenter.x, 2) +
                                    Math.Pow(point.y - isocenter.y, 2));

                                // Check all points (full 360° coverage - conservative approach)
                                if (radialDistance > maxRadialDistance)
                                {
                                    maxRadialDistance = radialDistance;
                                    furthestPoint = point;
                                }
                            }
                        }
                    }

                    if (maxRadialDistance > 0)
                    {
                        double distanceCm = maxRadialDistance / 10.0;
                        structureDetails.Add(new StructureCollisionInfo
                        {
                            Structure = structure,
                            MaxDistance = maxRadialDistance,
                            FurthestPoint = furthestPoint,
                            DistanceCm = distanceCm
                        });
                    }
                }
            }

            if (structureDetails.Any())
            {
                // Find structure with maximum distance (closest to collision)
                var closestToCollision = structureDetails
                    .OrderByDescending(item => item.DistanceCm)
                    .First();

                var structure = closestToCollision.Structure;
                var maxRadialDistance = closestToCollision.MaxDistance;
                var furthestPoint = closestToCollision.FurthestPoint;
                var maxDistanceCm = closestToCollision.DistanceCm;

                // Determine anatomical direction
                string direction = "";
                if (maxRadialDistance > 0)
                {
                    double angleRad = Math.Atan2(furthestPoint.y - isocenter.y, furthestPoint.x - isocenter.x);
                    double angleDeg = angleRad * 180.0 / Math.PI;
                    direction = angleDeg >= -45 && angleDeg < 45 ? "left" :
                                angleDeg >= 45 && angleDeg < 135 ? "anterior" :
                                angleDeg >= 135 || angleDeg < -135 ? "right" :
                                "posterior";
                }

                // Determine severity based on thresholds
                ValidationSeverity severity = ValidationSeverity.Info;
                if (maxDistanceCm > 37.5)
                    severity = ValidationSeverity.Error;
                else if (maxDistanceCm > 36.5)
                    severity = ValidationSeverity.Warning;

                string message = $"Max distance {maxDistanceCm:F1} cm from isocenter to " +
                    $"'{structure.Id}' ({direction} edge) - full arc check";
                if (maxDistanceCm > 37.5)
                    message += " - potential collision risk";
                else if (maxDistanceCm > 36.5)
                    message += " - limited clearance";

                results.Add(CreateResult(
                    "Collision",
                    message,
                    severity
                ));
            }
            else
            {
                results.Add(CreateResult(
                    "Collision",
                    "Cannot assess collision risk - no BODY or fixation structures found",
                    ValidationSeverity.Warning
                ));
            }

            return results;
        }
    }
}
