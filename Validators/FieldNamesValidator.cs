using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // 2.3.1 Field names validator
    public class FieldNamesValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.PlanSetup?.Beams != null)
            {
                var beams = context.PlanSetup.Beams;
                bool hasAnyFieldWithCouch = PlanUtilities.HasAnyFieldWithCouch(beams);

                foreach (var beam in beams)
                {
                    if (!beam.IsSetupField)
                    {
                        bool isValid = IsValidTreatmentFieldName(beam, beams, hasAnyFieldWithCouch);
                        results.Add(CreateResult(
                            "Fields.Names",
                            isValid ? $"Field '{beam.Id}' follows naming convention"
                                   : $"Field '{beam.Id}' does not follow naming convention",
                            isValid ? ValidationSeverity.Info : ValidationSeverity.Warning,
                            true
                        ));
                    }
                }
            }

            return results;
        }

        private bool IsValidTreatmentFieldName(Beam beam, IEnumerable<Beam> allBeams, bool hasAnyFieldWithCouch)
        {
            int couchAngle = (int)Math.Round(beam.ControlPoints.First().PatientSupportAngle);
            double startGantryExact = Math.Round(beam.ControlPoints.First().GantryAngle, 1);
            double endGantryExact = Math.Round(beam.ControlPoints.Last().GantryAngle, 1);

            // Special handling for HyperArc (ONLY with couch rotation)
            bool isHyperArc = PlanUtilities.IsHyperArc(beam);

            int startGantry, endGantry;

            if (isHyperArc && hasAnyFieldWithCouch)
            {
                // Special handling for HyperArc with couch rotation
                // If 180.1, use 181; if 179.9, use 179
                startGantry = (Math.Abs(startGantryExact - 180.1) < 0.01) ? 181 :
                              (Math.Abs(startGantryExact - 179.9) < 0.01) ? 179 :
                              (int)Math.Round(startGantryExact);

                endGantry = (Math.Abs(endGantryExact - 180.1) < 0.01) ? 181 :
                            (Math.Abs(endGantryExact - 179.9) < 0.01) ? 179 :
                            (int)Math.Round(endGantryExact);
            }
            else
            {
                // Standard rounding for other techniques
                startGantry = (int)Math.Round(startGantryExact);
                endGantry = (int)Math.Round(endGantryExact);
            }

            bool isArc = startGantry != endGantry;
            string id = beam.Id;

            if (isArc)
            {
                var arcPattern = hasAnyFieldWithCouch ? @"^T(\d+)-(\d+)(CW|CCW)(\d+)-[A-Z]$" : @"^(\d+)(CW|CCW)(\d+)-[A-Z]$";
                var arcMatch = Regex.Match(id, arcPattern);
                if (!arcMatch.Success) return false;

                if (hasAnyFieldWithCouch)
                {
                    int nameCouchAngle = int.Parse(arcMatch.Groups[1].Value);
                    int nameStartAngle = int.Parse(arcMatch.Groups[2].Value);
                    string nameDirection = arcMatch.Groups[3].Value;
                    int nameEndAngle = int.Parse(arcMatch.Groups[4].Value);

                    if (nameCouchAngle != couchAngle) return false;
                    if (nameStartAngle != startGantry) return false;
                    if (nameEndAngle != endGantry) return false;
                    if (((beam.GantryDirection == GantryDirection.Clockwise) && (nameDirection != "CW")) ||
                        ((beam.GantryDirection == GantryDirection.CounterClockwise) && (nameDirection != "CCW")))
                        return false;

                    return true;
                }
                else
                {
                    int nameStartAngle = int.Parse(arcMatch.Groups[1].Value);
                    string nameDirection = arcMatch.Groups[2].Value;
                    int nameEndAngle = int.Parse(arcMatch.Groups[3].Value);

                    if (nameStartAngle != startGantry) return false;
                    if (nameEndAngle != endGantry) return false;
                    if (((beam.GantryDirection == GantryDirection.Clockwise) && (nameDirection != "CW")) ||
                        ((beam.GantryDirection == GantryDirection.CounterClockwise) && (nameDirection != "CCW")))
                        return false;

                    return true;
                }
            }
            else
            {
                var staticPattern = hasAnyFieldWithCouch ? @"^T(\d+)-G(\d+)-[A-Z]$" : @"^G(\d+)-[A-Z]$";
                var staticMatch = Regex.Match(id, staticPattern);
                if (!staticMatch.Success) return false;

                if (hasAnyFieldWithCouch)
                {
                    int nameCouchAngle = int.Parse(staticMatch.Groups[1].Value);
                    int nameGantryAngle = int.Parse(staticMatch.Groups[2].Value);

                    if (nameCouchAngle != couchAngle) return false;
                    if (nameGantryAngle != startGantry) return false;
                    return true;
                }
                else
                {
                    int nameGantryAngle = int.Parse(staticMatch.Groups[1].Value);
                    if (nameGantryAngle != startGantry) return false;
                    return true;
                }
            }
        }
    }
}
