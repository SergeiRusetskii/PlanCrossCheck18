﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    public enum ValidationSeverity
    {
        Error,
        Warning,
        Info
    }

    // Utility methods to avoid duplicated code
    public static class PlanUtilities
    {
        public static bool IsEdgeMachine(string machineId) => machineId == "TrueBeamSN6368";
        public static bool IsHalcyonMachine(string machineId) =>
            machineId?.StartsWith("Halcyon", StringComparison.OrdinalIgnoreCase) ?? false;
        public static bool IsArcBeam(Beam beam) =>
            beam.ControlPoints.First().GantryAngle != beam.ControlPoints.Last().GantryAngle;
        public static bool HasAnyFieldWithCouch(IEnumerable<Beam> beams) =>
            beams?.Any(b => Math.Abs(b.ControlPoints.First().PatientSupportAngle) > 0.1) ?? false;
        public static bool ContainsSRS(string technique) =>
            technique?.Contains("SRS") ?? false;
    }

    // Base validator class
    public abstract class ValidatorBase
    {
        public abstract IEnumerable<ValidationResult> Validate(ScriptContext context);

        protected ValidationResult CreateResult(string category, string message, ValidationSeverity severity, bool isFieldResult = false)
        {
            return new ValidationResult
            {
                Category = category,
                Message = message,
                Severity = severity,
                IsFieldResult = isFieldResult
            };
        }
    }

    // Composite validator that can contain child validators
    public abstract class CompositeValidator : ValidatorBase
    {
        protected List<ValidatorBase> Validators { get; } = new List<ValidatorBase>();

        public void AddValidator(ValidatorBase validator)
        {
            Validators.Add(validator);
        }

        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();
            foreach (var validator in Validators)
            {
                results.AddRange(validator.Validate(context));
            }
            return results;
        }
    }

    // Root validator that coordinates all validation
    public class RootValidator : CompositeValidator
    {
        public RootValidator()
        {
            AddValidator(new CourseValidator());
            AddValidator(new PlanValidator());
        }
    }

    // 1. Course validation
    public class CourseValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.Course != null)
            {
                bool isValid = Regex.IsMatch(context.Course.Id, @"^RT\d*_");
                results.Add(CreateResult(
                    "Course",
                    isValid ? $"Course ID '{context.Course.Id}' follows the required format (RT[n]_*)"
                           : $"Course ID '{context.Course.Id}' does not start with (RT[n]_*)",
                    isValid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));
            }

            return results;
        }
    }

    // 2. Plan validation (parent)
    public class PlanValidator : CompositeValidator
    {
        public PlanValidator()
        {
            AddValidator(new CTAndPatientValidator());
            AddValidator(new DoseValidator());
            AddValidator(new FieldsValidator());
            AddValidator(new ReferencePointValidator());
            AddValidator(new FixationValidator());
            AddValidator(new OptimizationValidator());
            AddValidator(new PlanningStructuresValidator());
        }

        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            // Run all child validators
            results.AddRange(base.Validate(context));

            if (context.PlanSetup != null)
            {
                // Treatment orientation
                string treatmentOrientation = context.PlanSetup.TreatmentOrientationAsString;
                bool isHFS = treatmentOrientation.Equals("Head First-Supine", StringComparison.OrdinalIgnoreCase);
                results.Add(CreateResult(
                    "Plan.Info",
                    $"Treatment orientation: {treatmentOrientation}" + (!isHFS ? " (non-standard orientation)" : ""),
                    isHFS ? ValidationSeverity.Info : ValidationSeverity.Warning
                ));

                // Gated validation for Edge machines with DIBH in CT ID
                if (context.PlanSetup.Beams.Any() &&
                    PlanUtilities.IsEdgeMachine(context.PlanSetup.Beams.First().TreatmentUnit.Id))
                {
                    var ss = context.StructureSet;
                    if ((ss.Image?.Id?.IndexOf("DIBH", StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (ss.Id?.IndexOf("DIBH", StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (ss.Image?.Series?.Comment?.IndexOf("DIBH", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        bool isGated = context.PlanSetup.UseGating;
                        results.Add(CreateResult(
                            "Plan.Info",
                            isGated ? "Gating is correctly enabled for DIBH plan"
                                    : "Gating should be enabled for DIBH plan",
                            isGated ? ValidationSeverity.Info : ValidationSeverity.Error
                        ));
                    }

                    
                }
            }

            return results;
        }
    }

    // 2.1 Structure Set validator
    public class CTAndPatientValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            // User Origin validation
            if (context.StructureSet?.Image != null)
            {
                var userOrigin = context.StructureSet.Image.UserOrigin;

                // X coordinate check
                double xOffset = Math.Abs(userOrigin.x / 10.0); // mm to cm
                bool isXvalid = xOffset <= 0.5;

                results.Add(CreateResult(
                        "CT.UserOrigin",
                        isXvalid ? $"User Origin X coordinate ({userOrigin.x / 10:F1} cm) is within 0.5 cm limits"
                            : $"User Origin X coordinate ({userOrigin.x / 10:F1} cm) is outside acceptable limits",
                        isXvalid ? ValidationSeverity.Info : ValidationSeverity.Warning
                    ));

                // Z coordinate is shown as Y in Eclipse UI
                double zOffset = Math.Abs(userOrigin.z / 10.0); // mm to cm
                bool isZvalid = xOffset <= 0.5;

                results.Add(CreateResult(
                        "CT.UserOrigin",
                        isZvalid ? $"User Origin X coordinate ({userOrigin.z / 10:F1} cm) is within 0.5 cm limits"
                            : $"User Origin X coordinate ({userOrigin.z / 10:F1} cm) is outside acceptable limits",
                        isZvalid ? ValidationSeverity.Info : ValidationSeverity.Warning
                    ));

                // Y coordinate is shown as Z in Eclipse UI (with negative sign)
                bool isYValid = userOrigin.y >= -500 && userOrigin.y <= -80;
                results.Add(CreateResult(
                    "CT.UserOrigin",
                    isYValid ? $"User Origin Z coordinate ({-userOrigin.y / 10:F1} cm) is within limits"
                             : $"User Origin Z coordinate ({-userOrigin.y / 10:F1} cm) is outside limits (8 to 50 cm)",
                    isYValid ? ValidationSeverity.Info : ValidationSeverity.Warning
                ));

                // CT imaging device information
                // Get CT series description and imaging device
                string ctSeriesDescription = context.StructureSet.Image.Series.Comment;
                string imagingDevice = context.StructureSet.Image.Series.ImagingDeviceId;

                // Determine expected imaging device based on CT series description
                bool isHeadScan = false;
                if (!string.IsNullOrEmpty(ctSeriesDescription))
                {
                    isHeadScan = ctSeriesDescription.StartsWith("Head", StringComparison.OrdinalIgnoreCase) &&
                                !ctSeriesDescription.StartsWith("Head and Neck", StringComparison.OrdinalIgnoreCase) &&
                                !ctSeriesDescription.StartsWith("Head & Neck", StringComparison.OrdinalIgnoreCase);
                }

                string expectedDevice = isHeadScan ? "CT130265 HEAD" : "CT130265";
                bool isCorrectDevice = imagingDevice == expectedDevice;

                results.Add(CreateResult(
                    "CT.Curve",
                    isCorrectDevice
                        ? $"Correct imaging device '{imagingDevice}' used for {(isHeadScan ? "head" : "non-head")} CT series"
                        : $"Incorrect imaging device '{imagingDevice}' used. " +
                        $"Expected: '{expectedDevice}' for {(isHeadScan ? "head" : "non-head")} " +
                        $"scan (CT series: '{ctSeriesDescription})'",
                    isCorrectDevice ? ValidationSeverity.Info : ValidationSeverity.Error
                ));
            }

            return results;
        }
    }

    // 2.2 Dose validator
    public class DoseValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.PlanSetup != null && context.PlanSetup.Dose != null)
            {
                // Check if any field has SRS in technique
                bool isSRSPlan = context.PlanSetup.Beams.Any(b =>
                    !b.IsSetupField && b.Technique.ToString().Contains("SRS"));

                // Dose grid size validation
                double doseGridSize = context.PlanSetup.Dose.XRes / 10.0; // Convert mm to cm
                bool isValidGrid = isSRSPlan ? doseGridSize <= 0.125 : doseGridSize <= 0.2;

                results.Add(CreateResult(
                    "Dose.Grid",
                    isValidGrid
                        ? $"Dose grid size ({doseGridSize:F3} cm) is valid" + (isSRSPlan ? " for SRS plan" : "")
                        : $"Dose grid size ({doseGridSize:F3} cm) is too large" + (isSRSPlan
                            ? " (should be ≤ 0.125 cm for SRS plans)"
                            : " (should be ≤ 0.2 cm)"),
                    isValidGrid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                // SRS technique validation for high-dose plans
                if (context.PlanSetup.DosePerFraction.Dose >= 5)
                {
                    foreach (var beam in context.PlanSetup.Beams.Where(b => !b.IsSetupField))
                    {
                        bool hasSRSTechnique = beam.Technique.ToString().Contains("SRS");
                        results.Add(CreateResult(
                            "Dose.Technique",
                            hasSRSTechnique
                                ? $"Field '{beam.Id}' correctly uses SRS technique " +
                                $"for ≥5Gy/fraction ({context.PlanSetup.DosePerFraction})"
                                : $"Field '{beam.Id}' should use SRS technique " +
                                $"for ≥5Gy/fraction ({context.PlanSetup.DosePerFraction})",
                            hasSRSTechnique ? ValidationSeverity.Info : ValidationSeverity.Error,
                            true
                        ));
                    }
                }

                // Energy-dose rate checks
                foreach (var beam in context.PlanSetup.Beams.Where(b => !b.IsSetupField))
                {
                    string machineId = beam.TreatmentUnit.Id;
                    string energy = beam.EnergyModeDisplayName;
                    double doseRate = beam.DoseRate;

                    bool isEdgeMachine = PlanUtilities.IsEdgeMachine(machineId);
                    bool isHalcyonMachine = PlanUtilities.IsHalcyonMachine(machineId);

                    // Expected dose rates based on machine and energy
                    double expectedDoseRate = -1;

                    if (isEdgeMachine && context.PlanSetup.DosePerFraction.Dose >= 5)
                    {
                        // Energy validation for Edge machine with high dose/fraction
                        bool isValidEnergy = energy == "6X-FFF" || energy == "10X-FFF";
                        results.Add(CreateResult(
                            "Dose.Energy",
                            isValidEnergy
                                ? $"Field '{beam.Id}' correctly uses FFF energy ({energy}) for dose/fraction ≥5Gy"
                                : $"Field '{beam.Id}' should use 6FFF or 10FFF energy for dose/fraction ≥5Gy, " +
                                $"found: {energy}",
                            isValidEnergy ? ValidationSeverity.Info : ValidationSeverity.Error,
                            true
                        ));

                        if (energy == "6X-FFF") expectedDoseRate = 1400;
                        else if (energy == "10X-FFF") expectedDoseRate = 2400;
                        else if (energy == "6X" || energy == "10X") expectedDoseRate = 600;
                    }
                    else if (isHalcyonMachine)
                    {
                        if (energy == "6X-FFF") expectedDoseRate = 600;
                    }

                    // Only validate if we have an expected dose rate value
                    if (expectedDoseRate > 0)
                    {
                        bool isValidDoseRate = doseRate == expectedDoseRate;

                        results.Add(CreateResult(
                            "Dose.DoseRate",
                            isValidDoseRate
                                ? $"Field '{beam.Id}' has correct dose rate ({doseRate} MU/min) for {energy}"
                                : $"Field '{beam.Id}' has incorrect dose rate ({doseRate} MU/min) " +
                                $"for {energy} (should be {expectedDoseRate} MU/min)",
                            isValidDoseRate ? ValidationSeverity.Info : ValidationSeverity.Error,
                            true
                        ));
                    }
                }
            }

            return results;
        }
    }

    // 2.3 Fields validator (parent)
    public class FieldsValidator : CompositeValidator
    {
        public FieldsValidator()
        {
            AddValidator(new FieldNamesValidator());
            AddValidator(new GeometryValidator());
            AddValidator(new SetupFieldsValidator());
        }
    }

    // 2.3.1 Field names validator
    public class FieldNamesValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.PlanSetup?.Beams != null)
            {
                var beams = context.PlanSetup.Beams;
                bool hasAnyFieldWithCouch = PlanUtilities.HasAnyFieldWithCouch(beams);
                
                foreach (var beam in beams)
                {
                    if (!beam.IsSetupField)
                    {
                        bool isValid = IsValidTreatmentFieldName(beam, beams, hasAnyFieldWithCouch);
                        results.Add(CreateResult(
                            "Fields.Names",
                            isValid ? $"Field '{beam.Id}' follows naming convention"
                                   : $"Field '{beam.Id}' does not follow naming convention",
                            isValid ? ValidationSeverity.Info : ValidationSeverity.Warning,
                            true
                        ));
                    }
                }
            }

            return results;
        }

        private bool IsValidTreatmentFieldName(Beam beam, IEnumerable<Beam> allBeams, bool hasAnyFieldWithCouch)
        {
            int couchAngle = (int)Math.Round(beam.ControlPoints.First().PatientSupportAngle);
            double startGantryExact = Math.Round(beam.ControlPoints.First().GantryAngle, 1);
            double endGantryExact = Math.Round(beam.ControlPoints.Last().GantryAngle, 1);

            // Special handling for SRS HyperArc
            bool isSRSHyperArc = beam.Technique?.ToString().Contains("SRS HyperArc") ?? false;

            int startGantry, endGantry;

            if (isSRSHyperArc)
            {
                // Special handling for HyperArc
                // If 180.1, use 181; if 179.9, use 179
                startGantry = (startGantryExact == 180.1) ? 181 :
                              (startGantryExact == 179.9) ? 179 :
                              (int)Math.Round(startGantryExact);

                endGantry = (endGantryExact == 180.1) ? 181 :
                            (endGantryExact == 179.9) ? 179 :
                            (int)Math.Round(endGantryExact);
            }
            else
            {
                // Standard rounding for other techniques
                startGantry = (int)Math.Round(startGantryExact);
                endGantry = (int)Math.Round(endGantryExact);
            }

            bool isArc = startGantry != endGantry;
            string id = beam.Id;

            if (isArc)
            {
                var arcPattern = hasAnyFieldWithCouch ? @"^T(\d+)-(\d+)(CW|CCW)(\d+)-[A-Z]$" : @"^(\d+)(CW|CCW)(\d+)-[A-Z]$";
                var arcMatch = Regex.Match(id, arcPattern);
                if (!arcMatch.Success) return false;

                if (hasAnyFieldWithCouch)
                {
                    int nameCouchAngle = int.Parse(arcMatch.Groups[1].Value);
                    int nameStartAngle = int.Parse(arcMatch.Groups[2].Value);
                    string nameDirection = arcMatch.Groups[3].Value;
                    int nameEndAngle = int.Parse(arcMatch.Groups[4].Value);

                    if (nameCouchAngle != couchAngle) return false;
                    if (nameStartAngle != startGantry) return false;
                    if (nameEndAngle != endGantry) return false;
                    if (((beam.GantryDirection == GantryDirection.Clockwise) && (nameDirection != "CW")) ||
                        ((beam.GantryDirection == GantryDirection.CounterClockwise) && (nameDirection != "CCW")))
                        return false;

                    return true;
                }
                else
                {
                    int nameStartAngle = int.Parse(arcMatch.Groups[1].Value);
                    string nameDirection = arcMatch.Groups[2].Value;
                    int nameEndAngle = int.Parse(arcMatch.Groups[3].Value);

                    if (nameStartAngle != startGantry) return false;
                    if (nameEndAngle != endGantry) return false;
                    if (((beam.GantryDirection == GantryDirection.Clockwise) && (nameDirection != "CW")) ||
                        ((beam.GantryDirection == GantryDirection.CounterClockwise) && (nameDirection != "CCW")))
                        return false;

                    return true;
                }
            }
            else
            {
                var staticPattern = hasAnyFieldWithCouch ? @"^T(\d+)-G(\d+)-[A-Z]$" : @"^G(\d+)-[A-Z]$";
                var staticMatch = Regex.Match(id, staticPattern);
                if (!staticMatch.Success) return false;

                if (hasAnyFieldWithCouch)
                {
                    int nameCouchAngle = int.Parse(staticMatch.Groups[1].Value);
                    int nameGantryAngle = int.Parse(staticMatch.Groups[2].Value);

                    if (nameCouchAngle != couchAngle) return false;
                    if (nameGantryAngle != startGantry) return false;
                    return true;
                }
                else
                {
                    int nameGantryAngle = int.Parse(staticMatch.Groups[1].Value);
                    if (nameGantryAngle != startGantry) return false;
                    return true;
                }
            }
        }
    }

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

                        results.Add(CreateResult(
                            "Fields.Geometry.ToleranceTable",
                            isValid ? $"Field '{beam.Id}' has correct tolerance table ({toleranceTable})"
                                   : $"Field '{beam.Id}' has incorrect tolerance table. " +
                                   $"Expected: {expectedTable}, Found: {toleranceTable}",
                            isValid ? ValidationSeverity.Info : ValidationSeverity.Warning,
                            true
                        ));
                    }
                }

                // For plans without couch rotation
                if (!PlanUtilities.HasAnyFieldWithCouch(beams))
                {
                    var treatmentBeams = beams.Where(b => !b.IsSetupField).ToList();
                    if (treatmentBeams.Any())
                    {
                        var firstBeam = treatmentBeams.First();
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

    // 2.3.3. Setup fields validator
    public class SetupFieldsValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.PlanSetup?.Beams != null)
            {
                var setupFields = context.PlanSetup.Beams.Where(b => b.IsSetupField).ToList();
                string machineId = context.PlanSetup.Beams.FirstOrDefault()?.TreatmentUnit.Id;

                // Check setup field count based on machine type
                if (PlanUtilities.IsHalcyonMachine(machineId))
                {
                    bool hasCorrectCount = setupFields.Count == 1;
                    results.Add(CreateResult(
                        "Fields.SetupFields",
                        hasCorrectCount ? "Plan has the required 1 setup field for Halcyon"
                                       : $"Invalid setup field count for Halcyon: {setupFields.Count} (should be 1)",
                        hasCorrectCount ? ValidationSeverity.Info : ValidationSeverity.Error
                    ));
                }
                else if (PlanUtilities.IsEdgeMachine(machineId))
                {
                    bool hasCorrectCount = setupFields.Count == 2;
                    bool hasCBCT = setupFields.Any(f => f.Id.ToUpperInvariant() == "CBCT");
                    bool hasSF0 = setupFields.Any(f => f.Id.ToUpperInvariant() == "SF-0");
                    bool hasCorrectFields = hasCBCT && hasSF0;

                    results.Add(CreateResult(
                        "Fields.SetupFields",
                        hasCorrectCount ? "Plan has the required 2 setup fields for Edge"
                                       : $"Invalid setup field count for Edge: {setupFields.Count} (should be 2)",
                        hasCorrectCount ? ValidationSeverity.Info : ValidationSeverity.Error
                    ));

                    if (hasCorrectCount && !hasCorrectFields)
                    {
                        results.Add(CreateResult(
                            "Fields.SetupFields",
                            "Edge setup fields should be named 'CBCT' and 'SF-0'",
                            ValidationSeverity.Error  // Add the severity parameter
                        ));
                    }
                }

                // Validate each setup field's parameters (existing code)
                foreach (var beam in setupFields)
                {
                    // Original setup field parameter validation...
                    string id = beam.Id.ToUpperInvariant();
                    bool isHalcyon = PlanUtilities.IsHalcyonMachine(machineId);

                    if (isHalcyon)
                    {
                        bool isValid = id == "KVCBCT";
                        results.Add(CreateResult(
                            "Fields.SetupFields",
                            isValid ? $"Setup field '{beam.Id}' configuration is valid for Halcyon"
                                   : $"Invalid setup field for Halcyon: should be 'kVCBCT'",
                            isValid ? ValidationSeverity.Info : ValidationSeverity.Error,
                            true
                        ));
                    }
                    else
                    {
                        string energy = beam.EnergyModeDisplayName;
                        bool isValidName = id == "CBCT" || id.StartsWith("SF-");
                        bool isValidEnergy = energy == "6X" || energy == "10X";

                        bool isValid = isValidName && isValidEnergy;
                        results.Add(CreateResult(
                            "Fields.SetupFields",
                            isValid ? $"Setup field '{beam.Id}' configuration is valid"
                                   : $"Invalid setup field configuration: {beam.Id} with energy {energy}",
                            isValid ? ValidationSeverity.Info : ValidationSeverity.Error,
                            true
                        ));
                    }
                }
            }

            return results;
        }
    }

    // 2.4 Optimization parameters validator
    public class OptimizationValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            // Check optimization options
            if (context.PlanSetup != null && context.PlanSetup.Beams.Any())
            {
                string machineId = context.PlanSetup.Beams.First().TreatmentUnit.Id;
                bool isEdgeMachine = PlanUtilities.IsEdgeMachine(machineId);
                bool isSRSPlan = context.PlanSetup.Beams.Any(b =>
                    !b.IsSetupField && PlanUtilities.ContainsSRS(b.Technique.ToString()));

                if (isEdgeMachine)
                {
                    // Check JawTracking usage for EDGE machine
                    bool jawTrackingUsed = context.PlanSetup.OptimizationSetup.
                        Parameters.Any(p => p is OptimizationJawTrackingUsedParameter);

                    results.Add(CreateResult(
                        "Plan.Optimization",
                        jawTrackingUsed
                            ? "Jaw Tracking is used for Edge plan"
                            : "Jaw Tracking is NOT used for Edge plan",
                        jawTrackingUsed
                            ? ValidationSeverity.Info
                            : ValidationSeverity.Warning));

                    // Check ASC for Edge SRS plans
                    if (isSRSPlan)
                    {
                        // Get VMAT optimization parameters
                        var optModel = context.PlanSetup.GetCalculationModel(CalculationType.PhotonOptimization);
                        var vmatParams = context.PlanSetup.GetCalculationOptions(optModel);

                        if (vmatParams != null && vmatParams.ContainsKey("VMAT/ApertureShapeController"))
                        {
                            string ascValue = vmatParams["VMAT/ApertureShapeController"];
                            bool isValidASC = ascValue == "High" || ascValue == "Very High";

                            results.Add(CreateResult(
                                "Plan.Optimization",
                                isValidASC
                                    ? $"Aperture Shape Controller is set to '{ascValue}' for Edge SRS plan"
                                    : $"Aperture Shape Controller is set to '{ascValue}' - " +
                                    $"not 'High' or 'Very High' for Edge SRS plans",
                                isValidASC 
                                    ? ValidationSeverity.Info 
                                    : ValidationSeverity.Warning
                            ));
                        }
                    }
                    
                    else
                    {
                        results.Add(CreateResult(
                            "Plan.Optimization",
                            "Cannot determine Aperture Shape Controller setting for Edge SRS plan",
                            ValidationSeverity.Warning
                        ));
                    }
                }
            }

            return results;
        }
    }

    // 3 Reference points validator
    public class ReferencePointValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.PlanSetup?.PrimaryReferencePoint != null)
            {
                var refPoint = context.PlanSetup.PrimaryReferencePoint;

                // Check reference point name
                bool isNameValid = refPoint.Id.StartsWith("RP_", StringComparison.OrdinalIgnoreCase);
                results.Add(CreateResult(
                    "Dose.ReferencePoint",
                    isNameValid
                        ? $"Primary reference point name '{refPoint.Id}' follows naming convention (RP_*)"
                        : $"Primary reference point name '{refPoint.Id}' should start with 'RP_'",
                    isNameValid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                // Check reference point doses
                // Get doses from plan
                double totalPrescribedDose = context.PlanSetup.TotalDose.Dose;
                double dosePerFraction = context.PlanSetup.DosePerFraction.Dose;

                // Expected reference point doses
                double expectedTotalDose = totalPrescribedDose + 0.1;
                double expectedDailyDose = dosePerFraction + 0.1;

                // Get actual doses from reference point
                double actualTotalDose = context.PlanSetup.PrimaryReferencePoint.TotalDoseLimit.Dose;
                double actualDailyDose = context.PlanSetup.PrimaryReferencePoint.DailyDoseLimit.Dose;
                double actualSessionDose = context.PlanSetup.PrimaryReferencePoint.SessionDoseLimit.Dose;

                // Validate total dose
                bool isTotalDoseValid = Math.Abs(actualTotalDose - expectedTotalDose) <= 0.09;
                results.Add(CreateResult(
                    "Dose.ReferencePoint",
                    isTotalDoseValid
                        ? $"Total reference point dose ({actualTotalDose:F2} Gy) " +
                        $"is correct: Total+0.1={expectedTotalDose:F2} Gy"
                        : $"Total reference point dose ({actualTotalDose:F2} Gy) " +
                        $"is incorrect: Total+0.1={expectedTotalDose:F2} Gy",
                    isTotalDoseValid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                // Validate daily dose
                bool isDailyDoseValid = Math.Abs(actualDailyDose - expectedDailyDose) <= 0.09;
                results.Add(CreateResult(
                    "Dose.ReferencePoint",
                    isDailyDoseValid
                        ? $"Daily reference point dose ({actualDailyDose:F2} Gy) " +
                        $"is correct: Fraction+0.1=({expectedDailyDose:F2} Gy)"
                        : $"Daily reference point dose ({actualDailyDose:F2} Gy) " +
                        $"is incorrect: Fraction+0.1=({expectedDailyDose:F2} Gy)",
                    isDailyDoseValid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                // Validate session dose
                bool isSessionDoseValid = Math.Abs(actualSessionDose - expectedDailyDose) <= 0.09;
                results.Add(CreateResult(
                    "Dose.ReferencePoint",
                    isSessionDoseValid
                        ? $"Session reference point dose ({actualSessionDose:F2} Gy) " +
                        $"is correct: Fraction+0.1=({expectedDailyDose:F2} Gy)"
                        : $"Session reference point dose ({actualSessionDose:F2} Gy) " +
                        $"is incorrect: Fraction+0.1=({expectedDailyDose:F2} Gy)",
                    isSessionDoseValid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                // Check if prescription dose matches plan dose
                if (context.PlanSetup?.RTPrescription != null)
                {
                    double PrescriptionTotalDose = 0;
                    double PrescriptionFractionDose = 0;

                    // Iterate through all prescription targets
                    foreach (var target in context.PlanSetup.RTPrescription.Targets)
                    {
                        // Get fraction dose and calculate total dose for this target
                        double fractionTargetDose = target.DosePerFraction.Dose;
                        double totalTargetDose = fractionTargetDose * target.NumberOfFractions;

                        if (totalTargetDose > PrescriptionTotalDose)
                        {
                            PrescriptionTotalDose = totalTargetDose;
                            PrescriptionFractionDose = fractionTargetDose;
                        }
                    }

                    if (PrescriptionTotalDose > 0)
                    {
                        bool isTotalDoseMatch = Math.Abs(PrescriptionTotalDose - totalPrescribedDose) < 0.01;
                        bool isFractionDoseMatch = Math.Abs(PrescriptionFractionDose - dosePerFraction) < 0.01;

                        results.Add(CreateResult(
                            "Dose.Prescription",
                            isTotalDoseMatch
                                ? $"Plan dose ({totalPrescribedDose:F2} Gy) " +
                                $"matches prescription dose ({PrescriptionTotalDose:F2} Gy)"
                                : $"Plan dose ({totalPrescribedDose:F2} Gy) " +
                                $"does not match prescription dose ({PrescriptionTotalDose:F2} Gy)",
                            isTotalDoseMatch ? ValidationSeverity.Info : ValidationSeverity.Error
                        ));

                        results.Add(CreateResult(
                            "Dose.Prescription",
                            isFractionDoseMatch
                                ? $"Plan fraction dose ({dosePerFraction:F2} Gy) " +
                                $"matches prescription dose per fraction ({PrescriptionFractionDose:F2} Gy)"
                                : $"Plan fraction dose ({dosePerFraction:F2} Gy) " +
                                $"does not match prescription dose per fraction ({PrescriptionFractionDose:F2} Gy)",
                            isFractionDoseMatch ? ValidationSeverity.Info : ValidationSeverity.Error
                        ));
                    }
                    else
                    {
                        results.Add(CreateResult(
                            "Dose.Prescription",
                            "No dose values found in prescription targets",
                            ValidationSeverity.Warning
                        ));
                    }
                }
                else
                {
                    results.Add(CreateResult(
                        "Dose.Prescription",
                        "This plan is not linked to the Prescription",
                        ValidationSeverity.Warning
                    ));
                }

            }
            else
            {
                results.Add(CreateResult(
                    "Dose.ReferencePoint",
                    "No primary reference point found in plan",
                    ValidationSeverity.Error
                ));
            }

            return results;
        }
    }

    // 4 Fixation devices validator
    public class FixationValidator : ValidatorBase
    {
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

                    // Find all structures that match the prefixes we're interested in
                    var fixationPrefixesToCheck = new[]
                    {
                        "BODY",
                        "z_AltaLD",
                        "z_AltaHD",
                        "CouchSurface",
                        "z_ArmShuttle",
                        "z_VacBag"
                    };

                    var ringRadius = 475; // 47.5 cm in mm

                    // Track information for each structure
                    var structureDetails = new List<(
                        Structure Structure, double MaxDistance,
                        VVector FurthestPoint, double Clearance)>();

                    // Check each candidate structure
                    foreach (var prefix in fixationPrefixesToCheck)
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

    // 5 Planning structures validator
    public class PlanningStructuresValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            // Check z_Air_ structure
            var airStructures = context.StructureSet.Structures
                .Where(s => s.Id.StartsWith("z_Air_", StringComparison.OrdinalIgnoreCase));

            foreach (var structure in airStructures)
            {
                // Extract expected density from structure name (e.g., z_Air_-800HU)
                if (structure.Id.Contains("_") && structure.Id.EndsWith("HU", StringComparison.OrdinalIgnoreCase))
                {
                    string densityStr = structure.Id.Substring(structure.Id.LastIndexOf('_') + 1);

                    if (double.TryParse(densityStr.Substring(0, densityStr.Length - 2), out double expectedDensity))
                    {
                        // Check assigned HU
                        double actualDensity;
                        bool hasAssignedHU = structure.GetAssignedHU(out actualDensity);

                        if (hasAssignedHU)
                        {
                            bool isDensityCorrect = Math.Abs(actualDensity - expectedDensity) < 1;

                            results.Add(CreateResult(
                                "PlanningStructures.z_Air Density",
                                isDensityCorrect
                                    ? $"Air structure '{structure.Id}' has correct density override ({actualDensity} HU)"
                                    : $"Air structure '{structure.Id}' has incorrect density override: {actualDensity} HU " +
                                    $"(expected: {expectedDensity} HU)",
                                isDensityCorrect ? ValidationSeverity.Info : ValidationSeverity.Error
                            ));
                        }
                        else
                        {
                            results.Add(CreateResult(
                                "PlanningStructures.z_Air Density",
                                $"Air structure '{structure.Id}' has no density override assigned (expected: {expectedDensity} HU)",
                                ValidationSeverity.Error
                            ));
                        }

                        // Check original density distribution with sampling
                        if (context.StructureSet.Image != null)
                        {
                            int totalVoxels = 0;
                            int voxelsAboveThreshold = 0;

                            int xSize = context.StructureSet.Image.XSize;
                            int ySize = context.StructureSet.Image.YSize;
                            int zSize = context.StructureSet.Image.ZSize;

                            // Sampling parameters - adjust for speed vs accuracy
                            int sampleStep = 2; // Check every 2nd voxel in each dimension
                            int zStep = 2; // Check every 2nd slice

                            // Expected density for distribution
                            var densityThreshold = expectedDensity + 25; // Allow 25 HU tolerance

                            // Preallocate buffer for voxel data
                            int[,] voxelBuffer = new int[xSize, ySize];

                            // Iterate through sampled image planes
                            for (int z = 0; z < zSize; z += zStep)
                            {
                                // Get voxels for this plane
                                context.StructureSet.Image.GetVoxels(z, voxelBuffer);

                                // Check if structure has contours on this plane
                                var contours = structure.GetContoursOnImagePlane(z);
                                if (!contours.Any()) continue;

                                // Get image geometry
                                VVector origin = context.StructureSet.Image.Origin;
                                double xRes = context.StructureSet.Image.XRes;
                                double yRes = context.StructureSet.Image.YRes;
                                double zPos = origin.z + z * context.StructureSet.Image.ZRes;

                                // Check sampled voxels in the plane
                                for (int x = 0; x < xSize; x += sampleStep)
                                {
                                    for (int y = 0; y < ySize; y += sampleStep)
                                    {
                                        // Get voxel position in DICOM coordinates
                                        double xPos = origin.x + x * xRes;
                                        double yPos = origin.y + y * yRes;

                                        // Check if voxel is inside structure
                                        if (structure.IsPointInsideSegment(new VVector(xPos, yPos, zPos)))
                                        {
                                            totalVoxels++;

                                            // Convert voxel value to HU
                                            int voxelValue = voxelBuffer[x, y];
                                            double huValue = context.StructureSet.Image.VoxelToDisplayValue(voxelValue);

                                            if (huValue > densityThreshold)
                                            {
                                                voxelsAboveThreshold++;
                                            }
                                        }
                                    }
                                }
                            }

                            if (totalVoxels > 0)
                            {
                                // Adjust for sampling
                                double samplingFactor = sampleStep * sampleStep * zStep;
                                double percentageAbove = (double)voxelsAboveThreshold / totalVoxels * 100;
                                bool isPercentageValid = percentageAbove <= 5.0;

                                results.Add(CreateResult(
                                    "PlanningStructures.z_Air Density",
                                    isPercentageValid
                                        ? $"Air structure '{structure.Id}': {percentageAbove:F1}% " +
                                          $"of voxels exceed {densityThreshold} HU (within 5% limit)"
                                        : $"Air structure '{structure.Id}': {percentageAbove:F1}% " +
                                          $"of voxels exceed {densityThreshold} HU (exceeds 5% limit)",
                                    isPercentageValid ? ValidationSeverity.Info : ValidationSeverity.Warning
                                ));
                            }
                        }
                    }
                }
            }

            return results;
        }
    }
}