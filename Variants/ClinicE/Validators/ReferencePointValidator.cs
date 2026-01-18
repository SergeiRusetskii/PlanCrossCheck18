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

                // If all doses are correct, show single combined message
                if (isTotalDoseValid && isDailyDoseValid && isSessionDoseValid)
                {
                    results.Add(CreateResult(
                        "Dose.ReferencePoint",
                        $"Total, Daily and Session reference point doses are correct ({actualTotalDose:F2}, {actualDailyDose:F2}, {actualSessionDose:F2} Gy)",
                        ValidationSeverity.Info
                    ));
                }
                else
                {
                    // Show individual error messages for failed doses
                    if (!isTotalDoseValid)
                    {
                        results.Add(CreateResult(
                            "Dose.ReferencePoint",
                            $"Total reference point dose ({actualTotalDose:F2} Gy) " +
                            $"is incorrect: Total+0.1={expectedTotalDose:F2} Gy",
                            ValidationSeverity.Error
                        ));
                    }

                    if (!isDailyDoseValid)
                    {
                        results.Add(CreateResult(
                            "Dose.ReferencePoint",
                            $"Daily reference point dose ({actualDailyDose:F2} Gy) " +
                            $"is incorrect: Fraction+0.1=({expectedDailyDose:F2} Gy)",
                            ValidationSeverity.Error
                        ));
                    }

                    if (!isSessionDoseValid)
                    {
                        results.Add(CreateResult(
                            "Dose.ReferencePoint",
                            $"Session reference point dose ({actualSessionDose:F2} Gy) " +
                            $"is incorrect: Fraction+0.1=({expectedDailyDose:F2} Gy)",
                            ValidationSeverity.Error
                        ));
                    }
                }

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

                        // If both doses match, show single combined message
                        if (isTotalDoseMatch && isFractionDoseMatch)
                        {
                            results.Add(CreateResult(
                                "Dose.Prescription",
                                $"Plan total and fraction doses match prescription doses ({totalPrescribedDose:F2}, {dosePerFraction:F2} Gy)",
                                ValidationSeverity.Info
                            ));
                        }
                        else
                        {
                            // Show individual error messages
                            if (!isTotalDoseMatch)
                            {
                                results.Add(CreateResult(
                                    "Dose.Prescription",
                                    $"Plan dose ({totalPrescribedDose:F2} Gy) " +
                                    $"does not match prescription dose ({PrescriptionTotalDose:F2} Gy)",
                                    ValidationSeverity.Error
                                ));
                            }

                            if (!isFractionDoseMatch)
                            {
                                results.Add(CreateResult(
                                    "Dose.Prescription",
                                    $"Plan fraction dose ({dosePerFraction:F2} Gy) " +
                                    $"does not match prescription dose per fraction ({PrescriptionFractionDose:F2} Gy)",
                                    ValidationSeverity.Error
                                ));
                            }
                        }
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
