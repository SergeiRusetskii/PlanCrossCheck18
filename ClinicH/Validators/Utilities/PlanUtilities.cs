using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // Utility methods for ClinicH configuration
    public static class PlanUtilities
    {
        // Clinic has 2 TrueBeam machines: SN4625 and SN4664
        public static bool IsTrueBeamSTX(string machineId) =>
            machineId == "TrueBeamSN4625" || machineId == "TrueBeamSN4664";

        public static bool IsArcBeam(Beam beam) =>
            beam.ControlPoints.First().GantryAngle != beam.ControlPoints.Last().GantryAngle;

        public static bool HasAnyFieldWithCouch(IEnumerable<Beam> beams) =>
            beams?.Any(b => Math.Abs(b.ControlPoints.First().PatientSupportAngle) > 0.1) ?? false;

        public static bool ContainsSRS(string technique) =>
            technique?.Contains("SRS") ?? false;
    }
}
