using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
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
}
