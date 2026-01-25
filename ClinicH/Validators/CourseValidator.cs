using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // 1. Course validation (unchanged)
    public class CourseValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.Course != null)
            {
                bool isValid = Regex.IsMatch(context.Course.Id, @"^RT\d*_");
                results.Add(CreateResult(
                    "Course",
                    isValid ? $"Course ID '{context.Course.Id}' follows the required format (RT[n]_*)"
                           : $"Course ID '{context.Course.Id}' does not start with (RT[n]_*)",
                    isValid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));
            }

            return results;
        }
    }
}
