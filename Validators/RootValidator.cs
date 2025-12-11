using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // Root validator that coordinates all validation
    public class RootValidator : CompositeValidator
    {
        public RootValidator()
        {
            AddValidator(new CourseValidator());
            AddValidator(new PlanValidator());
        }
    }
}
