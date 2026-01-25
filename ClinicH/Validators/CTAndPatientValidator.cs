using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // 2.1 CT and Patient validator - adapted for ClinicH
    public class CTAndPatientValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            // User Origin validation - ClinicH uses 5mm tolerance
            if (context.StructureSet?.Image != null)
            {
                var userOrigin = context.StructureSet.Image.UserOrigin;

                // All coordinates check within 5mm
                double tolerance = 5.0; // mm
                bool isXvalid = Math.Abs(userOrigin.x) <= tolerance;
                bool isYvalid = Math.Abs(userOrigin.y) <= tolerance;
                bool isZvalid = Math.Abs(userOrigin.z) <= tolerance;

                results.Add(CreateResult(
                    "CT.UserOrigin",
                    isXvalid ? $"User Origin X coordinate ({userOrigin.x:F1} mm) is within {tolerance} mm tolerance"
                            : $"User Origin X coordinate ({userOrigin.x:F1} mm) is outside {tolerance} mm tolerance",
                    isXvalid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                results.Add(CreateResult(
                    "CT.UserOrigin",
                    isYvalid ? $"User Origin Y coordinate ({userOrigin.y:F1} mm) is within {tolerance} mm tolerance"
                            : $"User Origin Y coordinate ({userOrigin.y:F1} mm) is outside {tolerance} mm tolerance",
                    isYvalid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                results.Add(CreateResult(
                    "CT.UserOrigin",
                    isZvalid ? $"User Origin Z coordinate ({userOrigin.z:F1} mm) is within {tolerance} mm tolerance"
                            : $"User Origin Z coordinate ({userOrigin.z:F1} mm) is outside {tolerance} mm tolerance",
                    isZvalid ? ValidationSeverity.Info : ValidationSeverity.Error
                ));

                // ClinicH uses only one CT curve - simplified check
                string imagingDevice = context.StructureSet.Image.Series.ImagingDeviceId;
                results.Add(CreateResult(
                    "CT.Curve",
                    $"CT acquired with imaging device: '{imagingDevice}'",
                    ValidationSeverity.Info
                ));
            }

            return results;
        }
    }
}
