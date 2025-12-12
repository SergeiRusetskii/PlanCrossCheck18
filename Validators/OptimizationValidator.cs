using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // 2.4 Optimization parameters validator
    public class OptimizationValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            // Check optimization options
            if (context.PlanSetup != null && context.PlanSetup.Beams.Any())
            {
                string machineId = context.PlanSetup.Beams.First().TreatmentUnit.Id;
                bool isEdgeMachine = PlanUtilities.IsEdgeMachine(machineId);
                bool isSRSPlan = context.PlanSetup.Beams.Any(b =>
                    !b.IsSetupField && PlanUtilities.ContainsSRS(b));

                if (isEdgeMachine)
                {
                    // Check JawTracking usage for EDGE machine
                    bool jawTrackingUsed = context.PlanSetup.OptimizationSetup.
                        Parameters.Any(p => p is OptimizationJawTrackingUsedParameter);

                    results.Add(CreateResult(
                        "Plan.Optimization",
                        jawTrackingUsed
                            ? "Jaw Tracking is used for Edge plan"
                            : "Jaw Tracking is NOT used for Edge plan",
                        jawTrackingUsed
                            ? ValidationSeverity.Info
                            : ValidationSeverity.Warning));

                    // Check ASC for Edge SRS plans
                    if (isSRSPlan)
                    {
                        // Get VMAT optimization parameters
                        var optModel = context.PlanSetup.GetCalculationModel(CalculationType.PhotonOptimization);
                        var vmatParams = context.PlanSetup.GetCalculationOptions(optModel);

                        if (vmatParams != null && vmatParams.ContainsKey("VMAT/ApertureShapeController"))
                        {
                            string ascValue = vmatParams["VMAT/ApertureShapeController"];
                            bool isValidASC = ascValue == "High" || ascValue == "Very High";

                            results.Add(CreateResult(
                                "Plan.Optimization",
                                isValidASC
                                    ? $"Aperture Shape Controller is set to '{ascValue}' for Edge SRS plan"
                                    : $"Aperture Shape Controller is set to '{ascValue}' - " +
                                    $"not 'High' or 'Very High' for Edge SRS plans",
                                isValidASC
                                    ? ValidationSeverity.Info
                                    : ValidationSeverity.Warning
                            ));
                        }
                        else
                        {
                            results.Add(CreateResult(
                                "Plan.Optimization",
                                "Cannot determine Aperture Shape Controller setting for Edge SRS plan",
                                ValidationSeverity.Warning
                            ));
                        }
                    }
                }
            }

            return results;
        }
    }
}
