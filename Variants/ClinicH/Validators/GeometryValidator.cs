using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // 2.3.2 Geometry validator - simplified for TrueBeam STX
    public class GeometryValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.PlanSetup?.Beams != null)
            {
                // Collimator angle validation
                var collimatorAngles = context.PlanSetup.Beams
                    .Where(b => !b.IsSetupField)
                    .Select(b => b.ControlPoints.First().CollimatorAngle)
                    .ToList();

                var duplicateAngles = new HashSet<double>(
                    collimatorAngles
                        .GroupBy(a => a)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                );

                foreach (var beam in context.PlanSetup.Beams.Where(b => !b.IsSetupField))
                {
                    var angle = beam.ControlPoints.First().CollimatorAngle;
                    bool isInvalidRange = (angle > 268 && angle < 272) ||
                                         (angle > 358 || angle < 2) ||
                                         (angle > 88 && angle < 92);
                    bool isDuplicate = duplicateAngles.Contains(angle);

                    ValidationSeverity severity;
                    if (isInvalidRange)
                        severity = ValidationSeverity.Error;
                    else if (isDuplicate)
                        severity = ValidationSeverity.Warning;
                    else
                        severity = ValidationSeverity.Info;

                    results.Add(CreateResult(
                        "Fields.Geometry.Collimator",
                        severity == ValidationSeverity.Info ? $"Collimator angle {angle:F1}° is valid" :
                        severity == ValidationSeverity.Warning ? $"Collimator angle {angle:F1}° is duplicated" :
                        $"Invalid collimator angle {angle:F1}°",
                        severity
                    ));
                }
            }

            return results;
        }
    }
}
