using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
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
}
