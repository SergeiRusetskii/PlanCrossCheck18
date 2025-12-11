using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // 2. Plan validation (parent)
    public class PlanValidator : CompositeValidator
    {
        public PlanValidator()
        {
            AddValidator(new CTAndPatientValidator());
            AddValidator(new UserOriginMarkerValidator());
            AddValidator(new DoseValidator());
            AddValidator(new FieldsValidator());
            AddValidator(new ReferencePointValidator());
            AddValidator(new FixationValidator());
            AddValidator(new OptimizationValidator());
            AddValidator(new PlanningStructuresValidator());
            AddValidator(new PTVBodyProximityValidator());
        }

        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            // Run all child validators
            results.AddRange(base.Validate(context));

            if (context.PlanSetup != null)
            {
                // Treatment orientation
                string treatmentOrientation = context.PlanSetup.TreatmentOrientationAsString;
                bool isHFS = treatmentOrientation.Equals("Head First-Supine", StringComparison.OrdinalIgnoreCase);
                results.Add(CreateResult(
                    "Plan.Info",
                    $"Treatment orientation: {treatmentOrientation}" + (!isHFS ? " (non-standard orientation)" : ""),
                    isHFS ? ValidationSeverity.Info : ValidationSeverity.Warning
                ));

                // Gated validation for Edge machines with DIBH in CT ID
                if (context.PlanSetup.Beams.Any() &&
                    PlanUtilities.IsEdgeMachine(context.PlanSetup.Beams.First().TreatmentUnit.Id))
                {
                    var ss = context.StructureSet;
                    if ((ss.Image?.Id?.IndexOf("DIBH", StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (ss.Id?.IndexOf("DIBH", StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (ss.Image?.Series?.Comment?.IndexOf("DIBH", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        bool isGated = context.PlanSetup.UseGating;
                        results.Add(CreateResult(
                            "Plan.Info",
                            isGated ? "Gating is correctly enabled for DIBH plan"
                                    : "Gating should be enabled for DIBH plan",
                            isGated ? ValidationSeverity.Info : ValidationSeverity.Error
                        ));
                    }
                }
            }

            return results;
        }
    }
}
