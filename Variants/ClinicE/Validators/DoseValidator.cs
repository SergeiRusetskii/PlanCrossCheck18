using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
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
                    !b.IsSetupField && PlanUtilities.ContainsSRS(b));

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
                        bool hasSRSTechnique = PlanUtilities.ContainsSRS(beam);
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
                        // Dose rate expectations for Edge machine with high dose/fraction
                        // (Energy validation moved to BeamEnergyValidator)
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
}
