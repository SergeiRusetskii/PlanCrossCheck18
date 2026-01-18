using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // Utility methods for Hadassah configuration
    public static class PlanUtilities
    {
        // Hadassah has 2 TrueBeam STX machines
        public static bool IsTrueBeamSTX(string machineId) =>
            machineId?.Contains("STX") ?? false;

        public static bool IsArcBeam(Beam beam) =>
            beam.ControlPoints.First().GantryAngle != beam.ControlPoints.Last().GantryAngle;

        public static bool HasAnyFieldWithCouch(IEnumerable<Beam> beams) =>
            beams?.Any(b => Math.Abs(b.ControlPoints.First().PatientSupportAngle) > 0.1) ?? false;

        public static bool ContainsSRS(string technique) =>
            technique?.Contains("SRS") ?? false;
    }
}
