using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // 2.3 Fields validator (parent)
    public class FieldsValidator : CompositeValidator
    {
        public FieldsValidator()
        {
            AddValidator(new FieldNamesValidator());
            AddValidator(new GeometryValidator());
            AddValidator(new SetupFieldsValidator());
            AddValidator(new BeamEnergyValidator());
        }
    }
}
