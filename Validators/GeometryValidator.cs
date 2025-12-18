using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // 2.3.2 Geometry validator
    public class GeometryValidator : ValidatorBase
    {
        // Check MLC overlapping for divided fields
        private void CheckMLCOverlapForDuplicatedCollimators(IEnumerable<Beam> beams, List<ValidationResult> results)
        {
            // Only check for Halcyon machines and plans without couch rotation
            if (!beams.Any() || !PlanUtilities.IsHalcyonMachine(beams.First().TreatmentUnit.Id))
                return;

            if (PlanUtilities.HasAnyFieldWithCouch(beams))
                return;

            var beamsByCollimator = beams
                .Where(b => !b.IsSetupField)
                .GroupBy(b => Math.Round(b.ControlPoints.First().CollimatorAngle, 1))
                .Where(g => g.Count() > 1);

            foreach (var group in beamsByCollimator)
            {
                var beamList = group.ToList();
                double collimatorAngle = group.Key;

                for (int i = 0; i < beamList.Count - 1; i++)
                {
                    for (int j = i + 1; j < beamList.Count; j++)
                    {
                        var beam1 = beamList[i];
                        var beam2 = beamList[j];

                        // Get jaw positions from first control point
                        var cp1 = beam1.ControlPoints.First();
                        var cp2 = beam2.ControlPoints.First();

                        double x1_beam1 = cp1.JawPositions.X1;
                        double x2_beam1 = cp1.JawPositions.X2;
                        double x1_beam2 = cp2.JawPositions.X1;
                        double x2_beam2 = cp2.JawPositions.X2;

                        // Calculate overlap (positive value means overlap exists)
                        double overlapStart = Math.Max(x1_beam1, x1_beam2);
                        double overlapEnd = Math.Min(x2_beam1, x2_beam2);
                        double overlap = overlapEnd - overlapStart;

                        if (overlap > 0)
                        {
                            results.Add(CreateResult(
                                "Fields.Geometry.MLCOverlap",
                                $"Fields '{beam1.Id}' and '{beam2.Id}' with collimator {collimatorAngle:F1}° have {overlap / 10:F1} cm jaw overlap " +
                                $"(X1/X2: {x1_beam1 / 10:F1}/{x2_beam1 / 10:F1} cm and {x1_beam2 / 10:F1}/{x2_beam2 / 10:F1} cm)",
                                ValidationSeverity.Info,
                                true
                            ));
                        }
                        else
                        {
                            results.Add(CreateResult(
                                "Fields.Geometry.MLCOverlap",
                                $"Fields '{beam1.Id}' and '{beam2.Id}' with collimator {collimatorAngle:F1}° have no jaw overlap",
                                ValidationSeverity.Warning,
                                true
                            ));
                        }
                    }
                }
            }
        }

        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.PlanSetup?.Beams != null)
            {
                // Fields.Geometry.Collimator - check for duplicates
                var collimatorAngles = context.PlanSetup.Beams
                    .Where(b => !b.IsSetupField)
                    .Select(b => b.ControlPoints.First().CollimatorAngle)
                    .ToList();

                var duplicateAngles = collimatorAngles
                    .GroupBy(a => a)
                    .Where(g => g.Count() > 1)
                    .Select(g => g.Key)
                    .ToHashSet();

                var beams = context.PlanSetup.Beams;

                foreach (var beam in beams)
                {
                    // Collimator angle validation
                    if (!beam.IsSetupField)
                    {
                        var angle = beam.ControlPoints.First().CollimatorAngle;
                        bool isInvalidRange = (angle > 268 && angle < 272) ||
                                             (angle > 358 || angle < 2) ||
                                             (angle > 88 && angle < 92);
                        bool isDuplicate = duplicateAngles.Contains(angle);

                        ValidationSeverity severity;
                        if (isInvalidRange)
                            severity = ValidationSeverity.Error;
                        else if (isDuplicate)
                            severity = ValidationSeverity.Warning;
                        else
                            severity = ValidationSeverity.Info;

                        results.Add(CreateResult(
                            "Fields.Geometry.Collimator",
                            severity == ValidationSeverity.Info ? $"Collimator angle {angle:F1}° is valid" :
                            severity == ValidationSeverity.Warning ? $"Collimator angle {angle:F1}° is duplicated" :
                            $"Invalid collimator angle {angle:F1}°",
                            severity,
                            true
                        ));
                    }

                    // Machine-specific validations
                    string machineId = beam.TreatmentUnit.Id;

                    // Isocenter validation for Halcyon
                    if (PlanUtilities.IsHalcyonMachine(machineId))
                    {
                        var isocenter = beam.IsocenterPosition;
                        var userOrigin = context.StructureSet?.Image?.UserOrigin ?? new VVector(0, 0, 0);

                        // In IEC, Y corresponds to Z in DICOM, relative to User Origin
                        double iecY = (isocenter.z - userOrigin.z) / 10.0; // Convert from mm to cm

                        bool isValid = iecY > -30 && iecY < 17;

                        results.Add(CreateResult(
                            "Fields.Geometry.Isocenter",
                            isValid ? $"Field '{beam.Id}' isocenter Y position ({iecY:F1} cm) " +
                            $"is within Halcyon limits (-30 to +17 cm)"
                                   : $"Field '{beam.Id}' isocenter Y position ({iecY:F1} cm) " +
                                   $"is outside Halcyon limits (-30 to +17 cm)",
                            isValid ? ValidationSeverity.Info : ValidationSeverity.Error,
                            true
                        ));
                    }

                    // Tolerance table validation
                    if (PlanUtilities.IsHalcyonMachine(machineId) || PlanUtilities.IsEdgeMachine(machineId))
                    {
                        string toleranceTable = beam.ToleranceTableLabel;
                        string expectedTable = PlanUtilities.IsHalcyonMachine(machineId) ? "HAL" : "EDGE";
                        bool isValid = toleranceTable == expectedTable;
                        string summary = $"All treatment fields have correct tolerance table ({expectedTable})";

                        results.Add(CreateResult(
                            "Fields.Geometry.ToleranceTable",
                            isValid ? $"Field '{beam.Id}' has correct tolerance table ({toleranceTable})"
                                   : $"Field '{beam.Id}' has incorrect tolerance table. " +
                                   $"Expected: {expectedTable}, Found: {toleranceTable}",
                            isValid ? ValidationSeverity.Info : ValidationSeverity.Warning,
                            true,
                            summary
                        ));
                    }
                }

                // For plans without couch rotation
                if (!PlanUtilities.HasAnyFieldWithCouch(beams))
                {
                    // Use BeamsInTreatmentOrder to get the actual first field in treatment delivery order
                    var firstBeam = context.PlanSetup.BeamsInTreatmentOrder
                        .FirstOrDefault(b => !b.IsSetupField);
                    if (firstBeam != null)
                    {
                        double firstGantryAngle = firstBeam.ControlPoints.First().GantryAngle;

                        // Find angle closest to 180
                        double deviationFrom180 = Math.Abs(firstGantryAngle - 180);
                        bool firstFieldStartOK = deviationFrom180 > 90 ? false : true;
                        results.Add(CreateResult(
                            "Fields.Geometry.1st Field Start Angle",
                            firstFieldStartOK ? $"First field '{firstBeam.Id}' correctly starts " +
                                $"at {firstGantryAngle:F1}° - closest to the 180°"
                                : $"First field '{firstBeam.Id}' starts at {firstGantryAngle:F1}° (should be close to 180°)",
                            firstFieldStartOK ? ValidationSeverity.Info : ValidationSeverity.Warning,
                            true
                        ));
                    }
                }

                // Check for MLC overlapping for divided fields
                CheckMLCOverlapForDuplicatedCollimators(beams, results);

            }

            return results;
        }
    }
}
