using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // Base validator class (unchanged)
    public abstract class ValidatorBase
    {
        public abstract IEnumerable<ValidationResult> Validate(ScriptContext context);

        protected ValidationResult CreateResult(string category, string message, ValidationSeverity severity)
        {
            return new ValidationResult
            {
                Category = category,
                Message = message,
                Severity = severity
            };
        }
    }
}
