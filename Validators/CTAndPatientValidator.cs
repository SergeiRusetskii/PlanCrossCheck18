using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // 2.1 Structure Set validator
    public class CTAndPatientValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            // User Origin validation
            if (context.StructureSet?.Image != null)
            {
                var userOrigin = context.StructureSet.Image.UserOrigin;

                // X coordinate check
                double xOffset = Math.Abs(userOrigin.x / 10.0); // mm to cm
                bool isXvalid = xOffset <= 0.5;

                results.Add(CreateResult(
                        "CT.UserOrigin",
                        isXvalid ? $"User Origin X coordinate ({userOrigin.x / 10:F1} cm) is within 0.5 cm limits"
                            : $"User Origin X coordinate ({userOrigin.x / 10:F1} cm) is outside acceptable limits",
                        isXvalid ? ValidationSeverity.Info : ValidationSeverity.Warning
                    ));

                // Z coordinate is shown as Y in Eclipse UI
                double zOffset = Math.Abs(userOrigin.z / 10.0); // mm to cm
                bool isZvalid = zOffset <= 0.5;

                results.Add(CreateResult(
                        "CT.UserOrigin",
                        isZvalid ? $"User Origin Y coordinate ({userOrigin.z / 10:F1} cm) is within 0.5 cm limits"
                            : $"User Origin Y coordinate ({userOrigin.z / 10:F1} cm) is outside acceptable limits",
                        isZvalid ? ValidationSeverity.Info : ValidationSeverity.Warning
                    ));

                // Y coordinate is shown as Z in Eclipse UI (with negative sign)
                bool isYValid = userOrigin.y >= -500 && userOrigin.y <= -80;
                results.Add(CreateResult(
                    "CT.UserOrigin",
                    isYValid ? $"User Origin Z coordinate ({-userOrigin.y / 10:F1} cm) is within limits"
                             : $"User Origin Z coordinate ({-userOrigin.y / 10:F1} cm) is outside limits (8 to 50 cm)",
                    isYValid ? ValidationSeverity.Info : ValidationSeverity.Warning
                ));

                // CT imaging device information
                // Get CT series description and imaging device
                string ctSeriesDescription = context.StructureSet.Image.Series.Comment;
                string imagingDevice = context.StructureSet.Image.Series.ImagingDeviceId;

                // Determine expected imaging device based on CT series description
                bool isHeadScan = false;
                if (!string.IsNullOrEmpty(ctSeriesDescription))
                {
                    isHeadScan = ctSeriesDescription.StartsWith("Head", StringComparison.OrdinalIgnoreCase) &&
                                !ctSeriesDescription.StartsWith("Head and Neck", StringComparison.OrdinalIgnoreCase) &&
                                !ctSeriesDescription.StartsWith("Head & Neck", StringComparison.OrdinalIgnoreCase);
                }

                string expectedDevice = isHeadScan ? "CT130265 HEAD" : "CT130265";
                bool isCorrectDevice = imagingDevice == expectedDevice;

                results.Add(CreateResult(
                    "CT.Curve",
                    isCorrectDevice
                        ? $"Correct imaging device '{imagingDevice}' used for {(isHeadScan ? "head" : "non-head")} CT series"
                        : $"Incorrect imaging device '{imagingDevice}' used. " +
                        $"Expected: '{expectedDevice}' for {(isHeadScan ? "head" : "non-head")} " +
                        $"scan (CT series: '{ctSeriesDescription}')",
                    isCorrectDevice ? ValidationSeverity.Info : ValidationSeverity.Error
                ));
            }

            return results;
        }
    }
}
