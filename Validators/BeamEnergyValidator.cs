using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace PlanCrossCheck
{
    /// <summary>
    /// Validates beam energy configuration for treatment fields.
    /// Checks energy consistency across fields and validates Edge machine FFF requirements.
    /// </summary>
    /// <remarks>
    /// <para><strong>Pass Criteria (Info):</strong></para>
    /// <list type="bullet">
    /// <item>All treatment fields (excluding setup fields) have the same energy mode</item>
    /// <item>Edge machine with high dose/fraction uses appropriate FFF energy</item>
    /// </list>
    ///
    /// <para><strong>Warning Criteria:</strong></para>
    /// <list type="bullet">
    /// <item>Treatment fields use different energy modes (e.g., mix of 6X and 10X)</item>
    /// </list>
    ///
    /// <para><strong>Error Criteria:</strong></para>
    /// <list type="bullet">
    /// <item>Edge machine with dose/fraction ≥5Gy not using FFF energy</item>
    /// </list>
    ///
    /// <para><strong>Clinical Significance:</strong></para>
    /// <para>Consistent energy configuration ensures proper dose delivery.
    /// Edge machine requires FFF for high dose/fraction treatments.</para>
    /// </remarks>
    public class BeamEnergyValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.PlanSetup?.Beams != null)
            {
                // Get all treatment fields (exclude setup fields)
                var treatmentBeams = context.PlanSetup.Beams
                    .Where(b => !b.IsSetupField)
                    .ToList();

                if (treatmentBeams.Any())
                {
                    // Check 1: Edge machine high-dose energy validation
                    bool isEdgeMachine = treatmentBeams.Any() &&
                        PlanUtilities.IsEdgeMachine(treatmentBeams.First().TreatmentUnit.Id);
                    bool isHighDose = context.PlanSetup.DosePerFraction.Dose >= 5;

                    if (isEdgeMachine && isHighDose)
                    {
                        foreach (var beam in treatmentBeams)
                        {
                            string energy = beam.EnergyModeDisplayName;
                            bool isValidEnergy = energy == "6X-FFF" || energy == "10X-FFF";

                            results.Add(CreateResult(
                                "Fields.Energy",
                                isValidEnergy
                                    ? $"Field '{beam.Id}' correctly uses FFF energy ({energy}) for dose/fraction ≥5Gy"
                                    : $"Field '{beam.Id}' should use 6FFF or 10FFF energy for dose/fraction ≥5Gy, found: {energy}",
                                isValidEnergy ? ValidationSeverity.Info : ValidationSeverity.Error,
                                true
                            ));
                        }
                    }

                    // Check 2: Energy consistency across all fields
                    var energyModes = treatmentBeams
                        .Select(b => b.EnergyModeDisplayName)
                        .Distinct()
                        .ToList();

                    if (energyModes.Count == 1)
                    {
                        // All fields use same energy - Info
                        results.Add(CreateResult(
                            "Fields.Energy",
                            $"All treatment fields use the same energy: {energyModes.First()}",
                            ValidationSeverity.Info
                        ));
                    }
                    else
                    {
                        // Multiple energies detected - Warning
                        string energyList = string.Join(", ", energyModes);
                        results.Add(CreateResult(
                            "Fields.Energy",
                            $"Treatment fields use different energies: {energyList}. Verify this is clinically intended.",
                            ValidationSeverity.Warning
                        ));

                        // Show breakdown per field
                        foreach (var beam in treatmentBeams)
                        {
                            results.Add(CreateResult(
                                "Fields.Energy",
                                $"Field '{beam.Id}': {beam.EnergyModeDisplayName}",
                                ValidationSeverity.Info,
                                isFieldResult: true
                            ));
                        }
                    }
                }
            }

            return results;
        }
    }
}
