using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    /// <summary>
    /// Validates clearance between treatment machine (gantry ring) and fixation devices
    /// to prevent potential collisions during treatment delivery.
    /// Supports both Halcyon and Edge machines with machine-specific thresholds.
    /// </summary>
    public class CollisionValidator : ValidatorBase
    {
        // Fixation structure prefixes used for collision assessment (both Halcyon and Edge)
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

            if (context.StructureSet == null || context.PlanSetup?.Beams == null || !context.PlanSetup.Beams.Any())
            {
                return results;
            }

            string machineId = context.PlanSetup.Beams.FirstOrDefault()?.TreatmentUnit.Id;
            bool isHalcyonMachine = PlanUtilities.IsHalcyonMachine(machineId);
            bool isEdgeMachine = PlanUtilities.IsEdgeMachine(machineId);

            // Halcyon collision check
            if (isHalcyonMachine)
            {
                results.AddRange(ValidateHalcyonCollision(context));
            }

            // Edge collision check
            if (isEdgeMachine)
            {
                results.AddRange(ValidateEdgeCollision(context));
            }

            return results;
        }

        /// <summary>
        /// Validates collision risk for Halcyon machines.
        /// Checks clearance to 47.5 cm ring radius across full 360 degrees.
        /// Thresholds: &lt;4.5 cm = Error, &lt;5.0 cm = Warning
        /// </summary>
        private IEnumerable<ValidationResult> ValidateHalcyonCollision(ScriptContext context)
        {
            var results = new List<ValidationResult>();
            VVector isocenter = context.PlanSetup.Beams.First().IsocenterPosition;
            var ringRadius = 475.0; // 47.5 cm in mm

            var structureDetails = new List<(Structure Structure, double MaxDistance, VVector FurthestPoint, double Clearance)>();

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
                                double radialDistance = Math.Sqrt(
                                    Math.Pow(point.x - isocenter.x, 2) +
                                    Math.Pow(point.y - isocenter.y, 2));

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
                        double clearance = (ringRadius - maxRadialDistance) / 10.0; // Convert to cm
                        structureDetails.Add((structure, maxRadialDistance, furthestPoint, clearance));
                    }
                }
            }

            if (structureDetails.Any())
            {
                var closestStructure = structureDetails
                    .OrderBy(item => item.Clearance)
                    .First();

                var structure = closestStructure.Structure;
                var maxRadialDistance = closestStructure.MaxDistance;
                var furthestPoint = closestStructure.FurthestPoint;
                var clearance = closestStructure.Clearance;

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

                // Determine severity
                ValidationSeverity severity = ValidationSeverity.Info;
                if (clearance < 4.5)
                    severity = ValidationSeverity.Error;
                else if (clearance < 5.0)
                    severity = ValidationSeverity.Warning;

                string message = $"Clearance {clearance:F1} cm between " +
                    $"fixation device '{structure.Id}' ({direction} edge) and Halcyon ring";
                if (clearance < 5.0)
                    message += clearance < 4.5 ? " - potential collision risk" : " - limited clearance";

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
                    "Cannot assess Halcyon collision risk - none of the required fixation devices found",
                    ValidationSeverity.Warning
                ));
            }

            return results;
        }

        /// <summary>
        /// Validates collision risk for Edge machines.
        /// Checks maximum distance from isocenter across full 360° (all gantry angles).
        /// Skips check if couch rotation is present (requires manual verification).
        /// Thresholds: &gt;36.5 cm = Warning, &gt;37.5 cm = Error
        /// </summary>
        private IEnumerable<ValidationResult> ValidateEdgeCollision(ScriptContext context)
        {
            var results = new List<ValidationResult>();
            var allBeams = context.PlanSetup.Beams.ToList();

            // Skip if couch rotation present (includes HyperArc plans)
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

            // Edge thresholds: >36.5 cm from iso -> warning, >37.5 cm -> error.
            // Check full 360° coverage for maximum safety (conservative approach).
            var ringRadius = 375.0; // 37.5 cm in mm (error threshold)

            var structureDetails = new List<(Structure Structure, double MaxDistance, VVector FurthestPoint, double Clearance, double DistanceCm)>();

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
                                double radialDistance = Math.Sqrt(
                                    Math.Pow(point.x - isocenter.x, 2) +
                                    Math.Pow(point.y - isocenter.y, 2));

                                // Check all points (full 360° coverage)
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
                        double clearance = (ringRadius - maxRadialDistance) / 10.0; // cm
                        double distanceCm = maxRadialDistance / 10.0;
                        structureDetails.Add((structure, maxRadialDistance, furthestPoint, clearance, distanceCm));
                    }
                }
            }

            if (structureDetails.Any())
            {
                var closestStructure = structureDetails
                    .OrderBy(item => item.Clearance)
                    .First();

                var structure = closestStructure.Structure;
                var maxRadialDistance = closestStructure.MaxDistance;
                var furthestPoint = closestStructure.FurthestPoint;
                var clearance = closestStructure.Clearance;
                var maxDistanceCm = closestStructure.DistanceCm;

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

                // Determine severity
                ValidationSeverity severity = ValidationSeverity.Info;
                if (maxDistanceCm > 37.5)
                    severity = ValidationSeverity.Error;
                else if (maxDistanceCm > 36.5)
                    severity = ValidationSeverity.Warning;

                string message = $"Max distance {maxDistanceCm:F1} cm from isocenter to " +
                    $"fixation device '{structure.Id}' ({direction} edge)";
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

            return results;
        }
    }
}
