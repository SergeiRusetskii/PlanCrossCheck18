using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
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
                bool hasSF_CBCT = setupFields.Any(f => f.Id.ToUpperInvariant() == "SF_CBCT");
                bool hasSF0 = setupFields.Any(f => f.Id.ToUpperInvariant() == "SF_0");
                bool hasSF270or90 = setupFields.Any(f =>
                    f.Id.ToUpperInvariant() == "SF_270" || f.Id.ToUpperInvariant() == "SF_90");

                if (!hasSF_CBCT)
                    results.Add(CreateResult("Fields.SetupFields", "Missing required SF_CBCT setup field", ValidationSeverity.Error));
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
}
