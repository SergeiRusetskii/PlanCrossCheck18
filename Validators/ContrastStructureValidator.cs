using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace PlanCrossCheck
{
    /// <summary>
    /// Validates presence of contrast structure when study contains contrast imaging.
    /// Checks if Study.Comment contains "CONTRAST" keyword and verifies corresponding structure exists.
    /// </summary>
    /// <remarks>
    /// <para><strong>Pass Criteria (Info):</strong></para>
    /// <list type="bullet">
    /// <item>Study comment contains "CONTRAST" and z_Contrast* structure exists</item>
    /// <item>Study comment does not contain "CONTRAST" (no check needed)</item>
    /// </list>
    ///
    /// <para><strong>Warning Criteria:</strong></para>
    /// <list type="bullet">
    /// <item>Study comment contains "CONTRAST" but z_Contrast* structure is missing</item>
    /// </list>
    ///
    /// <para><strong>Clinical Significance:</strong></para>
    /// <para>Contrast structures must be delineated to account for density changes in dose calculation
    /// when contrast media is present in imaging studies.</para>
    /// </remarks>
    public class ContrastStructureValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.StructureSet?.Image?.Series?.Study != null)
            {
                var study = context.StructureSet.Image.Series.Study;
                string studyComment = study.Comment ?? string.Empty;

                // Check if study comment contains "CONTRAST"
                bool hasContrastKeyword = studyComment.IndexOf("CONTRAST", StringComparison.OrdinalIgnoreCase) >= 0;

                if (hasContrastKeyword)
                {
                    // Study mentions contrast - verify structure exists
                    bool hasContrastStructure = context.StructureSet.Structures.Any(s =>
                        s.Id.StartsWith("z_Contrast", StringComparison.OrdinalIgnoreCase));

                    if (hasContrastStructure)
                    {
                        results.Add(CreateResult(
                            "Structure.Contrast",
                            "Study contains contrast imaging and z_Contrast* structure exists",
                            ValidationSeverity.Info
                        ));
                    }
                    else
                    {
                        results.Add(CreateResult(
                            "Structure.Contrast",
                            "Study comment contains 'CONTRAST' but z_Contrast* structure is missing. \nConsider adding z_Contrast structure if needed.",
                            ValidationSeverity.Warning
                        ));
                    }
                }
                // If no contrast keyword, no validation needed (implicitly passes)
            }

            return results;
        }
    }
}
