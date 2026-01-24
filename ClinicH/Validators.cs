using System;
using System.Collections.Generic;
using System.Linq;
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

    // Utility methods for ClinicH configuration
    public static class PlanUtilities
    {
        // Clinic has 2 TrueBeam STX machines
        public static bool IsTrueBeamSTX(string machineId) =>
            machineId?.Contains("STX") ?? false;

        public static bool IsArcBeam(Beam beam) =>
            beam.ControlPoints.First().GantryAngle != beam.ControlPoints.Last().GantryAngle;

        public static bool HasAnyFieldWithCouch(IEnumerable<Beam> beams) =>
            beams?.Any(b => Math.Abs(b.ControlPoints.First().PatientSupportAngle) > 0.1) ?? false;

        public static bool ContainsSRS(string technique) =>
            technique?.Contains("SRS") ?? false;
    }

    // Base validator class (unchanged)
    public abstract class ValidatorBase
    {
        public abstract IEnumerable<ValidationResult> Validate(ScriptContext context);

        protected ValidationResult CreateResult(string category, string message, ValidationSeverity severity)
        {
            return new ValidationResult
            {
                Category = category,
                Message = message,
                Severity = severity
            };
        }
    }

    // Composite validator (unchanged)
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

    // Root validator
    public class RootValidator : CompositeValidator
    {
        public RootValidator()
        {
            AddValidator(new CourseValidator());
            AddValidator(new PlanValidator());
        }
    }

    // 1. Course validation (unchanged)
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

    // 2. Plan validation
    public class PlanValidator : CompositeValidator
    {
        public PlanValidator()
        {
            AddValidator(new CTAndPatientValidator());
            AddValidator(new DoseValidator());
            AddValidator(new FieldsValidator());
            AddValidator(new ReferencePointValidator());
            AddValidator(new FixationValidator());
        }

        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();
            results.AddRange(base.Validate(context));

            if (context.PlanSetup != null)
            {
                // Treatment orientation
                string treatmentOrientation = context.PlanSetup.TreatmentOrientation.ToString();
                bool isHFS = treatmentOrientation.Equals("Head First-Supine", StringComparison.OrdinalIgnoreCase);
                results.Add(CreateResult(
                    "Plan.Info",
                    $"Treatment orientation: {treatmentOrientation}" + (!isHFS ? " (non-standard orientation)" : ""),
                    isHFS ? ValidationSeverity.Info : ValidationSeverity.Warning
                ));
            }

            return results;
        }
    }

    // 2.1 CT and Patient validator - adapted for ClinicH
    public class CTAndPatientValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            // User Origin validation - ClinicH uses 5mm tolerance
            if (context.StructureSet?.Image != null)
            {
                var userOrigin = context.StructureSet.Image.UserOrigin;

                // All coordinates check within 5mm
                double tolerance = 5.0; // mm
                bool isXvalid = Math.Abs(userOrigin.x) <= tolerance;
                bool isYvalid = Math.Abs(userOrigin.y) <= tolerance;
                bool isZvalid = Math.Abs(userOrigin.z) <= tolerance;

                results.Add(CreateResult(
                    "CT.UserOrigin",
                    isXvalid ? $"User Origin X coordinate ({userOrigin.x:F1} mm) is within {tolerance} mm tolerance"
                            : $"User Origin X coordinate ({userOrigin.x:F1} mm) is outside {tolerance} mm tolerance",
                    isXvalid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                results.Add(CreateResult(
                    "CT.UserOrigin",
                    isYvalid ? $"User Origin Y coordinate ({userOrigin.y:F1} mm) is within {tolerance} mm tolerance"
                            : $"User Origin Y coordinate ({userOrigin.y:F1} mm) is outside {tolerance} mm tolerance",
                    isYvalid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                results.Add(CreateResult(
                    "CT.UserOrigin",
                    isZvalid ? $"User Origin Z coordinate ({userOrigin.z:F1} mm) is within {tolerance} mm tolerance"
                            : $"User Origin Z coordinate ({userOrigin.z:F1} mm) is outside {tolerance} mm tolerance",
                    isZvalid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                // ClinicH uses only one CT curve - simplified check
                string imagingDevice = context.StructureSet.Image.Series.ImagingDeviceId;
                results.Add(CreateResult(
                    "CT.Curve",
                    $"CT acquired with imaging device: '{imagingDevice}'",
                    ValidationSeverity.Info
                ));
            }

            return results;
        }
    }

    // 2.2 Dose validator - adapted for ClinicH
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

                // Dose grid size validation - ClinicH specific
                double doseGridSize = context.PlanSetup.Dose.XRes; // Already in mm
                bool isValidGrid = isSRSPlan ? doseGridSize <= 1.25 : doseGridSize <= 2.5;

                results.Add(CreateResult(
                    "Dose.Grid",
                    isValidGrid
                        ? $"Dose grid size ({doseGridSize:F2} mm) is valid" + (isSRSPlan ? " for SRS plan" : "")
                        : $"Dose grid size ({doseGridSize:F2} mm) is too large" + (isSRSPlan
                            ? " (should be ≤ 1.25 mm for SRS plans)"
                            : " (should be ≤ 2.5 mm)"),
                    isValidGrid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                // Validate energies for TrueBeam STX
                var validEnergies = new HashSet<string> { "6X", "6X-FFF", "10X", "10X-FFF", "15X" };
                foreach (var beam in context.PlanSetup.Beams.Where(b => !b.IsSetupField))
                {
                    string energy = beam.EnergyModeDisplayName;
                    bool isValidEnergy = validEnergies.Contains(energy);

                    results.Add(CreateResult(
                        "Dose.Energy",
                        isValidEnergy
                            ? $"Field '{beam.Id}' uses valid energy ({energy})"
                            : $"Field '{beam.Id}' uses invalid energy ({energy}). Valid energies: {string.Join(", ", validEnergies)}",
                        isValidEnergy ? ValidationSeverity.Info : ValidationSeverity.Error
                    ));

                    // Dose rate validation for TrueBeam STX
                    double doseRate = beam.DoseRate;
                    double expectedDoseRate = -1;

                    if (energy == "6X-FFF") expectedDoseRate = 1400;
                    else if (energy == "10X-FFF") expectedDoseRate = 2400;
                    else if (energy == "6X" || energy == "10X" || energy == "15X") expectedDoseRate = 600;

                    if (expectedDoseRate > 0)
                    {
                        bool isValidDoseRate = doseRate == expectedDoseRate;
                        results.Add(CreateResult(
                            "Dose.DoseRate",
                            isValidDoseRate
                                ? $"Field '{beam.Id}' has correct dose rate ({doseRate} MU/min) for {energy}"
                                : $"Field '{beam.Id}' has incorrect dose rate ({doseRate} MU/min) for {energy} (should be {expectedDoseRate} MU/min)",
                            isValidDoseRate ? ValidationSeverity.Info : ValidationSeverity.Error
                        ));
                    }
                }
            }

            return results;
        }
    }

    // 2.3 Fields validator
    public class FieldsValidator : CompositeValidator
    {
        public FieldsValidator()
        {
            AddValidator(new FieldNamesValidator());
            AddValidator(new GeometryValidator());
            AddValidator(new SetupFieldsValidator());
        }
    }

    // 2.3.1 Field names validator (will be updated later by user)
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
                            isValid ? ValidationSeverity.Info : ValidationSeverity.Warning
                        ));
                    }
                }
            }

            return results;
        }

        private bool IsValidTreatmentFieldName(Beam beam, IEnumerable<Beam> allBeams, bool hasAnyFieldWithCouch)
        {
            int couchAngle = (int)Math.Round(beam.ControlPoints.First().PatientSupportAngle);
            double startGantryExact = beam.ControlPoints.First().GantryAngle;
            double endGantryExact = beam.ControlPoints.Last().GantryAngle;

            // Standard rounding for other techniques
            int startGantry = (int)Math.Round(startGantryExact);
            int endGantry = (int)Math.Round(endGantryExact);

            bool isArc = startGantry != endGantry;
            string id = beam.Id;

            if (isArc)
            {
                var arcPattern = hasAnyFieldWithCouch ? @"^T(\d+)_G(\d+)(CW|CCW)(\d+)_(\d)$" : @"^G(\d+)(CW|CCW)(\d+)_(\d)$";
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
                var staticPattern = hasAnyFieldWithCouch ? @"^T(\d+)_G(\d+)_(\d+)$" : @"^G(\d+)_(\d+)$";
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

    // 2.3.2 Geometry validator - simplified for TrueBeam STX
    public class GeometryValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.PlanSetup?.Beams != null)
            {
                // Collimator angle validation
                var collimatorAngles = context.PlanSetup.Beams
                    .Where(b => !b.IsSetupField)
                    .Select(b => b.ControlPoints.First().CollimatorAngle)
                    .ToList();

                var duplicateAngles = new HashSet<double>(
                    collimatorAngles
                        .GroupBy(a => a)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                );

                foreach (var beam in context.PlanSetup.Beams.Where(b => !b.IsSetupField))
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
                        severity
                    ));
                }
            }

            return results;
        }
    }

    // 2.3.3 Setup fields validator - ClinicH specific
    public class SetupFieldsValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.PlanSetup?.Beams != null)
            {
                var setupFields = context.PlanSetup.Beams.Where(b => b.IsSetupField).ToList();

                // ClinicH requires 3 setup fields
                bool hasCorrectCount = setupFields.Count == 3;
                results.Add(CreateResult(
                    "Fields.SetupFields",
                    hasCorrectCount ? "Plan has the required 3 setup fields"
                                   : $"Invalid setup field count: {setupFields.Count} (should be 3)",
                    hasCorrectCount ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                // Check for required setup fields
                bool hasCBCT = setupFields.Any(f => f.Id.ToUpperInvariant() == "CBCT");
                bool hasSF0 = setupFields.Any(f => f.Id.ToUpperInvariant() == "SF_0");
                bool hasSF270or90 = setupFields.Any(f =>
                    f.Id.ToUpperInvariant() == "SF_270" || f.Id.ToUpperInvariant() == "SF_90");

                if (!hasCBCT)
                    results.Add(CreateResult("Fields.SetupFields", "Missing required CBCT setup field", ValidationSeverity.Error));
                if (!hasSF0)
                    results.Add(CreateResult("Fields.SetupFields", "Missing required SF_0 setup field", ValidationSeverity.Error));
                if (!hasSF270or90)
                    results.Add(CreateResult("Fields.SetupFields", "Missing required SF_270/90 setup field", ValidationSeverity.Error));

                // Validate setup field energies
                foreach (var beam in setupFields)
                {
                    string energy = beam.EnergyModeDisplayName;
                    bool isValidEnergy = energy == "2.5X-FFF";

                    results.Add(CreateResult(
                        "Fields.SetupFields",
                        isValidEnergy
                            ? $"Setup field '{beam.Id}' has correct energy ({energy})"
                            : $"Setup field '{beam.Id}' has incorrect energy ({energy}). Should be 2.5X-FFF",
                        isValidEnergy ? ValidationSeverity.Info : ValidationSeverity.Error
                    ));
                }
            }

            return results;
        }
    }

    // 3. Reference points validator (unchanged from original)
    public class ReferencePointValidator : ValidatorBase
    {
        // Keep the original implementation
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
                        ? $"Total reference point dose ({actualTotalDose:F2} Gy) is correct: Total+0.1={expectedTotalDose:F2} Gy"
                        : $"Total reference point dose ({actualTotalDose:F2} Gy) is incorrect: Total+0.1={expectedTotalDose:F2} Gy",
                    isTotalDoseValid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                // Validate daily dose
                bool isDailyDoseValid = Math.Abs(actualDailyDose - expectedDailyDose) <= 0.09;
                results.Add(CreateResult(
                    "Dose.ReferencePoint",
                    isDailyDoseValid
                        ? $"Daily reference point dose ({actualDailyDose:F2} Gy) is correct: Fraction+0.1=({expectedDailyDose:F2} Gy)"
                        : $"Daily reference point dose ({actualDailyDose:F2} Gy) is incorrect: Fraction+0.1=({expectedDailyDose:F2} Gy)",
                    isDailyDoseValid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                // Validate session dose
                bool isSessionDoseValid = Math.Abs(actualSessionDose - expectedDailyDose) <= 0.09;
                results.Add(CreateResult(
                    "Dose.ReferencePoint",
                    isSessionDoseValid
                        ? $"Session reference point dose ({actualSessionDose:F2} Gy) is correct: Fraction+0.1=({expectedDailyDose:F2} Gy)"
                        : $"Session reference point dose ({actualSessionDose:F2} Gy) is incorrect: Fraction+0.1=({expectedDailyDose:F2} Gy)",
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
                                ? $"Plan dose ({totalPrescribedDose:F2} Gy) matches prescription dose ({PrescriptionTotalDose:F2} Gy)"
                                : $"Plan dose ({totalPrescribedDose:F2} Gy) does not match prescription dose ({PrescriptionTotalDose:F2} Gy)",
                            isTotalDoseMatch ? ValidationSeverity.Info : ValidationSeverity.Error
                        ));

                        results.Add(CreateResult(
                            "Dose.Prescription",
                            isFractionDoseMatch
                                ? $"Plan fraction dose ({dosePerFraction:F2} Gy) matches prescription dose per fraction ({PrescriptionFractionDose:F2} Gy)"
                                : $"Plan fraction dose ({dosePerFraction:F2} Gy) does not match prescription dose per fraction ({PrescriptionFractionDose:F2} Gy)",
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

    // 4. Fixation devices validator - placeholder for ClinicH
    public class FixationValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.StructureSet != null)
            {
                // Check density overrides for all fixation structures
                var fixationPrefixes = new[]
                {
                "z_AltaHD_", "z_AltaLD_", "z_H&NFrame_", "z_MaskLock_",
                "z_FrameHead_", "z_LocBar_", "z_ArmShuttle_", "z_EncFrame_",
                "z_VacBag_", "z_Contrast_"
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
                                            ? $"Structure '{structure.Id}' has correct density override ({actualDensity} HU)"
                                            : $"Structure '{structure.Id}' has incorrect density override: {actualDensity} HU (expected: {expectedDensity} HU)",
                                        isDensityCorrect ? ValidationSeverity.Info : ValidationSeverity.Error
                                    ));
                                }
                                else
                                {
                                    results.Add(CreateResult(
                                        "Fixation.Density",
                                        $"Structure '{structure.Id}' has no density override assigned (expected: {expectedDensity} HU)",
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