using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    public class ValidationResult
    {
        public string Message { get; set; }
        public string Category { get; set; }
        public ValidationSeverity Severity { get; set; }

        // Indicates whether this result relates to an individual field
        public bool IsFieldResult { get; set; }

        // Custom summary message to display when all field results pass
        public string AllPassSummary { get; set; }

        // Optional computed property for backward compatibility
        public bool IsValid => Severity != ValidationSeverity.Error;
    }
}
