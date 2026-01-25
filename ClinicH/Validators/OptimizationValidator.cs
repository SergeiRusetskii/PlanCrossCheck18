using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // 2.4 Optimization parameters validator - Jaw tracking validation
    public class OptimizationValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            // Check optimization options
            if (context.PlanSetup != null && context.PlanSetup.Beams.Any())
            {
                string machineId = context.PlanSetup.Beams.First().TreatmentUnit.Id;
                bool isTrueBeamSTX = PlanUtilities.IsTrueBeamSTX(machineId);

                if (isTrueBeamSTX)
                {
                    // Check JawTracking usage for TrueBeam STX machine
                    bool jawTrackingUsed = context.PlanSetup.OptimizationSetup.
                        Parameters.Any(p => p is OptimizationJawTrackingUsedParameter);

                    results.Add(CreateResult(
                        "Plan.Optimization",
                        jawTrackingUsed
                            ? "Jaw Tracking is used for TrueBeam STX plan"
                            : "Jaw Tracking is NOT used for TrueBeam STX plan",
                        jawTrackingUsed
                            ? ValidationSeverity.Info
                            : ValidationSeverity.Warning));
                }
            }

            return results;
        }
    }
}
