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

                bool allFieldsValid = true;
                var validationIssues = new List<ValidationResult>();

                // Check setup field count and configuration based on machine type
                if (PlanUtilities.IsHalcyonMachine(machineId))
                {
                    bool hasCorrectCount = setupFields.Count == 1;

                    if (!hasCorrectCount)
                    {
                        allFieldsValid = false;
                        validationIssues.Add(CreateResult(
                            "Fields.SetupFields",
                            $"Invalid setup field count for Halcyon: {setupFields.Count} (should be 1)",
                            ValidationSeverity.Error
                        ));
                    }

                    // Validate each setup field's parameters
                    foreach (var beam in setupFields)
                    {
                        string id = beam.Id.ToUpperInvariant();
                        bool isValid = id == "KVCBCT";

                        if (!isValid)
                        {
                            allFieldsValid = false;
                            validationIssues.Add(CreateResult(
                                "Fields.SetupFields",
                                $"Invalid setup field for Halcyon: should be 'kVCBCT'",
                                ValidationSeverity.Error,
                                true
                            ));
                        }
                    }
                }
                else if (PlanUtilities.IsEdgeMachine(machineId))
                {
                    bool hasCorrectCount = setupFields.Count == 2;
                    bool hasCBCT = setupFields.Any(f => f.Id.ToUpperInvariant() == "CBCT");
                    bool hasSF0 = setupFields.Any(f => f.Id.ToUpperInvariant() == "SF-0");
                    bool hasCorrectFields = hasCBCT && hasSF0;

                    if (!hasCorrectCount)
                    {
                        allFieldsValid = false;
                        validationIssues.Add(CreateResult(
                            "Fields.SetupFields",
                            $"Invalid setup field count for Edge: {setupFields.Count} (should be 2)",
                            ValidationSeverity.Error
                        ));
                    }

                    if (hasCorrectCount && !hasCorrectFields)
                    {
                        allFieldsValid = false;
                        validationIssues.Add(CreateResult(
                            "Fields.SetupFields",
                            "Edge setup fields should be named 'CBCT' and 'SF-0'",
                            ValidationSeverity.Error
                        ));
                    }

                    // Validate each setup field's parameters
                    foreach (var beam in setupFields)
                    {
                        string id = beam.Id.ToUpperInvariant();
                        string energy = beam.EnergyModeDisplayName;
                        bool isValidName = id == "CBCT" || id.StartsWith("SF-");
                        bool isValidEnergy = energy == "6X" || energy == "10X";

                        bool isValid = isValidName && isValidEnergy;

                        if (!isValid)
                        {
                            allFieldsValid = false;
                            validationIssues.Add(CreateResult(
                                "Fields.SetupFields",
                                $"Invalid setup field configuration: {beam.Id} with energy {energy}",
                                ValidationSeverity.Error,
                                true
                            ));
                        }
                    }
                }

                // Add combined OK message if all valid, otherwise add individual issues
                if (allFieldsValid && setupFields.Any())
                {
                    results.Add(CreateResult(
                        "Fields.SetupFields",
                        "All Setup fields have required configuration",
                        ValidationSeverity.Info
                    ));
                }
                else
                {
                    results.AddRange(validationIssues);
                }
            }

            return results;
        }
    }
}
