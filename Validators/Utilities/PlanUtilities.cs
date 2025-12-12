using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // Utility methods to avoid duplicated code
    public static class PlanUtilities
    {
        // Machine helpers
        public static bool IsEdgeMachine(string machineId) => machineId == "TrueBeamSN6368";
        public static bool IsHalcyonMachine(string machineId) =>
            machineId?.StartsWith("Halcyon", StringComparison.OrdinalIgnoreCase) ?? false;
        public static bool IsArcBeam(Beam beam) =>
            beam.ControlPoints.First().GantryAngle != beam.ControlPoints.Last().GantryAngle;
        public static bool HasAnyFieldWithCouch(IEnumerable<Beam> beams) =>
            beams?.Any(b => Math.Abs(b.ControlPoints.First().PatientSupportAngle) > 0.1) ?? false;
        public static bool ContainsSRS(string technique) =>
            technique?.IndexOf("SRS", StringComparison.OrdinalIgnoreCase) >= 0
            || IsHyperArc(technique);

        public static bool ContainsSRS(Beam beam)
        {
            if (beam == null) return false;
            if (IsHyperArc(beam)) return true;
            return ContainsSRS(beam.Technique?.ToString());
        }

        public static bool IsHyperArc(string technique) =>
            technique?.IndexOf("HYPERARC", StringComparison.OrdinalIgnoreCase) >= 0;

        public static bool IsHyperArc(Beam beam)
        {
            if (beam == null) return false;
            if (beam.SetupTechnique == SetupTechnique.HyperArc) return true;
            return IsHyperArc(beam.Technique?.ToString());
        }

        // Constants for collision assessment
        private const double ANGLE_TOLERANCE_DEGREES = 0.1;
        private const double STATIC_FIELD_SECTOR_DEGREES = 10.0;

        /// <summary>
        /// Calculate the angular span (in degrees) covered by an arc beam.
        /// </summary>
        public static double GetArcSpanDegrees(Beam beam)
        {
            double startAngle = beam.ControlPoints.First().GantryAngle;
            double endAngle = beam.ControlPoints.Last().GantryAngle;

            // If start == end, it's a static field (0 degrees)
            if (Math.Abs(startAngle - endAngle) < ANGLE_TOLERANCE_DEGREES)
                return 0;

            // Calculate span based on gantry direction (TPS cannot traverse through 180 degrees)
            double span;
            if (beam.GantryDirection == GantryDirection.Clockwise)
            {
                // CW: angles increase (0 -> 90 -> 270 -> 0)
                span = (endAngle - startAngle + 360) % 360;
            }
            else // CounterClockwise
            {
                // CCW: angles decrease (0 -> 270 -> 90 -> 0)
                span = (startAngle - endAngle + 360) % 360;
            }

            // Handle the case where span is 0 (full 360)
            if (span < ANGLE_TOLERANCE_DEGREES)
                span = 360;

            return span;
        }

        /// <summary>
        /// Determine if treatment beams provide full arc coverage (>= 180 degrees).
        /// </summary>
        public static bool IsFullArcCoverage(IEnumerable<Beam> treatmentBeams)
        {
            if (treatmentBeams == null || !treatmentBeams.Any())
                return false;

            // Check if any single arc spans >= 180 degrees
            foreach (var beam in treatmentBeams)
            {
                double span = GetArcSpanDegrees(beam);
                if (span >= 180)
                    return true;
            }

            // Check if combined coverage >= 180 degrees
            var sectors = GetCoveredAngularSectors(treatmentBeams);
            double totalCoverage = CalculateTotalCoverage(sectors);
            return totalCoverage >= 180;
        }

        /// <summary>
        /// Build list of angular sectors covered by treatment beams.
        /// Returns normalized (startAngle, endAngle) pairs where start <= end.
        /// Wraparound sectors are split into multiple non-wrapping sectors.
        /// Optional margins allow expanding coverage for collision checks.
        /// </summary>
        public static List<(double start, double end)> GetCoveredAngularSectors(
            IEnumerable<Beam> treatmentBeams,
            double arcMarginDegrees = 0,
            double staticMarginDegrees = STATIC_FIELD_SECTOR_DEGREES)
        {
            var sectors = new List<(double start, double end)>();

            foreach (var beam in treatmentBeams)
            {
                double startAngle = beam.ControlPoints.First().GantryAngle;
                double endAngle = beam.ControlPoints.Last().GantryAngle;

                // Normalize angles to 0-360
                startAngle = (startAngle + 360) % 360;
                endAngle = (endAngle + 360) % 360;

                if (Math.Abs(startAngle - endAngle) < ANGLE_TOLERANCE_DEGREES)
                {
                    // Static field: add +/- staticMarginDegrees
                    double staticStart = startAngle - staticMarginDegrees;
                    double staticEnd = startAngle + staticMarginDegrees;

                    // Normalize and add (may create wraparound sector)
                    staticStart = (staticStart + 360) % 360;
                    staticEnd = (staticEnd + 360) % 360;

                    // Add normalized sectors (split if wraparound)
                    sectors.AddRange(NormalizeSector(staticStart, staticEnd));
                }
                else
                {
                    // Arc: respect gantry direction when building sectors
                    // Apply margin on both ends along travel direction
                    if (beam.GantryDirection == GantryDirection.Clockwise)
                    {
                        double cwStart = (startAngle - arcMarginDegrees + 360) % 360;
                        double cwEnd = (endAngle + arcMarginDegrees + 360) % 360;

                        bool wraps = startAngle > endAngle || cwStart > cwEnd;
                        if (wraps)
                        {
                            // Wraparound case: e.g., 181 CW 179 -> [(181,360), (0,179)]
                            sectors.AddRange(NormalizeSector(cwStart, cwEnd));
                        }
                        else
                        {
                            // Normal case: e.g., 200 CW 220 -> [(200,220)]
                            sectors.Add((cwStart, cwEnd));
                        }
                    }
                    else // CounterClockwise
                    {
                        double ccwStart = (startAngle + arcMarginDegrees + 360) % 360;
                        double ccwEnd = (endAngle - arcMarginDegrees + 360) % 360;

                        bool wraps = startAngle < endAngle || ccwEnd > ccwStart;
                        if (wraps)
                        {
                            // Wraparound case: e.g., 10 CCW 350 -> [(350,360), (0,10)]
                            sectors.AddRange(NormalizeSector(ccwEnd, ccwStart));
                        }
                        else
                        {
                            // Normal case: e.g., 220 CCW 200 -> [(200,220)]
                            sectors.Add((ccwEnd, ccwStart));
                        }
                    }
                }
            }

            // Normalize all sectors and merge overlapping ones
            return MergeSectors(sectors);
        }

        /// <summary>
        /// Check if a given angle falls within any of the covered sectors.
        /// </summary>
        public static bool IsAngleInSectors(double angle, List<(double start, double end)> sectors)
        {
            // Null/empty check
            if (sectors == null || !sectors.Any())
                return false;

            // Normalize angle to 0-360
            angle = (angle + 360) % 360;

            foreach (var sector in sectors)
            {
                if (IsAngleInSector(angle, sector.start, sector.end))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Normalize a sector by splitting wraparound sectors into non-wrapping parts.
        /// Example: (350, 10) becomes [(350, 360), (0, 10)].
        /// Example: (90, 270) stays as [(90, 270)].
        /// </summary>
        private static List<(double start, double end)> NormalizeSector(double start, double end)
        {
            var normalized = new List<(double start, double end)>();

            // Ensure angles are in 0-360 range
            start = (start + 360) % 360;
            end = (end + 360) % 360;

            if (start <= end)
            {
                // Normal sector - no wraparound
                normalized.Add((start, end));
            }
            else
            {
                // Wraparound sector - split into two parts
                normalized.Add((start, 360));
                normalized.Add((0, end));
            }

            return normalized;
        }

        /// <summary>
        /// Check if angle is within a single normalized sector (where start <= end, no wraparound).
        /// </summary>
        private static bool IsAngleInSector(double angle, double start, double end)
        {
            return angle >= start && angle <= end;
        }

        /// <summary>
        /// Merge overlapping or adjacent normalized sectors.
        /// Assumes all sectors have start <= end (no wraparound).
        /// </summary>
        private static List<(double start, double end)> MergeSectors(List<(double start, double end)> sectors)
        {
            if (sectors.Count <= 1)
                return sectors;

            var sorted = sectors.OrderBy(s => s.start).ThenBy(s => s.end).ToList();
            var merged = new List<(double start, double end)>();
            var current = sorted[0];

            for (int i = 1; i < sorted.Count; i++)
            {
                var next = sorted[i];

                // Check if sectors overlap or are adjacent (within 1 degree tolerance)
                if (next.start <= current.end + 1.0)
                {
                    current = (current.start, Math.Max(current.end, next.end));
                }
                else
                {
                    merged.Add(current);
                    current = next;
                }
            }

            merged.Add(current);
            return merged;
        }

        /// <summary>
        /// Calculate total angular coverage from normalized sectors.
        /// Assumes all sectors have start <= end (no wraparound).
        /// </summary>
        private static double CalculateTotalCoverage(List<(double start, double end)> sectors)
        {
            double total = 0;
            foreach (var sector in sectors)
            {
                total += sector.end - sector.start;
            }

            return total;
        }
    }
}
