using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // 4. Fixation devices validator - ClinicH
    public class FixationValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.StructureSet != null)
            {
                // Check density overrides for all fixation structures
                var fixationPrefixes = new[]
                {
                "z_AltaHD_", "z_AltaLD_", "z_H&NFrame_", "z_MaskLock_",
                "z_FrameHead_", "z_LocBar_", "z_ArmShuttle_", "z_EncFrame_",
                "z_VacBag_", "z_Contrast_"
            };

                foreach (var structure in context.StructureSet.Structures)
                {
                    // Check if this structure matches any of our prefixes
                    var matchingPrefix = fixationPrefixes.FirstOrDefault(prefix =>
                        structure.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                    if (matchingPrefix != null)
                    {
                        // Extract the expected density from structure name (+200HU, -390HU, etc.)
                        if (structure.Id.Contains("_") && structure.Id.EndsWith("HU", StringComparison.OrdinalIgnoreCase))
                        {
                            string densityStr = structure.Id.Substring(structure.Id.LastIndexOf('_') + 1);

                            // Remove "HU" and parse the value
                            if (double.TryParse(densityStr.Substring(0, densityStr.Length - 2),
                                               out double expectedDensity))
                            {
                                // Get actual density override using out parameter
                                double actualDensity;
                                bool hasAssignedHU = structure.GetAssignedHU(out actualDensity);

                                if (hasAssignedHU)
                                {
                                    bool isDensityCorrect = Math.Abs(actualDensity - expectedDensity) < 1; // Small tolerance

                                    results.Add(CreateResult(
                                        "Fixation.Density",
                                        isDensityCorrect
                                            ? $"Structure '{structure.Id}' has correct density override ({actualDensity} HU)"
                                            : $"Structure '{structure.Id}' has incorrect density override: {actualDensity} HU (expected: {expectedDensity} HU)",
                                        isDensityCorrect ? ValidationSeverity.Info : ValidationSeverity.Error
                                    ));
                                }
                                else
                                {
                                    results.Add(CreateResult(
                                        "Fixation.Density",
                                        $"Structure '{structure.Id}' has no density override assigned (expected: {expectedDensity} HU)",
                                        ValidationSeverity.Error
                                    ));
                                }
                            }
                        }
                    }
                }
            }

            return results;
        }
    }
}
