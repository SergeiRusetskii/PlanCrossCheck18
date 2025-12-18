using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // 3 Reference points validator
    public class ReferencePointValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.PlanSetup?.PrimaryReferencePoint != null)
            {
                var refPoint = context.PlanSetup.PrimaryReferencePoint;

                // Check reference point naming convention
                bool isNameValid = refPoint.Id.StartsWith("RP_", StringComparison.OrdinalIgnoreCase);
                if (!isNameValid)
                {
                    results.Add(CreateResult(
                        "Dose.ReferencePoint",
                        $"Primary reference point name '{refPoint.Id}' should start with 'RP_'",
                        ValidationSeverity.Error
                    ));
                }

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

                // Validate total, daily, and session doses
                bool isTotalDoseValid = Math.Abs(actualTotalDose - expectedTotalDose) <= 0.09;
                bool isDailyDoseValid = Math.Abs(actualDailyDose - expectedDailyDose) <= 0.09;
                bool isSessionDoseValid = Math.Abs(actualSessionDose - expectedDailyDose) <= 0.09;

                // Always show all dose validation results (both passes and errors)
                results.Add(CreateResult(
                    "Dose.ReferencePoint",
                    isTotalDoseValid
                        ? $"Total reference point dose is correct ({actualTotalDose:F2} Gy)"
                        : $"Total reference point dose ({actualTotalDose:F2} Gy) is incorrect: Total+0.1={expectedTotalDose:F2} Gy",
                    isTotalDoseValid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                results.Add(CreateResult(
                    "Dose.ReferencePoint",
                    isDailyDoseValid
                        ? $"Daily reference point dose is correct ({actualDailyDose:F2} Gy)"
                        : $"Daily reference point dose ({actualDailyDose:F2} Gy) is incorrect: Fraction+0.1={expectedDailyDose:F2} Gy",
                    isDailyDoseValid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                results.Add(CreateResult(
                    "Dose.ReferencePoint",
                    isSessionDoseValid
                        ? $"Session reference point dose is correct ({actualSessionDose:F2} Gy)"
                        : $"Session reference point dose ({actualSessionDose:F2} Gy) is incorrect: Fraction+0.1={expectedDailyDose:F2} Gy",
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

                        // Always show all prescription validation results (both passes and errors)
                        results.Add(CreateResult(
                            "Dose.Prescription",
                            isTotalDoseMatch
                                ? $"Plan total dose matches prescription ({totalPrescribedDose:F2} Gy)"
                                : $"Plan dose ({totalPrescribedDose:F2} Gy) does not match prescription dose ({PrescriptionTotalDose:F2} Gy)",
                            isTotalDoseMatch ? ValidationSeverity.Info : ValidationSeverity.Error
                        ));

                        results.Add(CreateResult(
                            "Dose.Prescription",
                            isFractionDoseMatch
                                ? $"Plan fraction dose matches prescription ({dosePerFraction:F2} Gy)"
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
}
