using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
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
}
