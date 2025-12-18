using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;

namespace PlanCrossCheck
{
    /// <summary>
    /// Validates fixation device structures and their density override assignments.
    /// Ensures required fixation structures exist (Halcyon-specific) and verifies
    /// density overrides match naming conventions (e.g., structure ending in "_120HU" should have 120 HU override).
    /// </summary>
    public class FixationValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            if (context.StructureSet != null)
            {
                string machineId = context.PlanSetup?.Beams?.FirstOrDefault()?.TreatmentUnit.Id;
                bool isHalcyonMachine = PlanUtilities.IsHalcyonMachine(machineId);
                bool isEdgeMachine = PlanUtilities.IsEdgeMachine(machineId);

                // Required structures for Halcyon plans
                if (isHalcyonMachine)
                {
                    var requiredPrefixes = new[]
                    {
                        "z_AltaHD_", "z_AltaLD_",
                        "CouchSurface", "CouchInterior"
                    };

                    var missing = requiredPrefixes
                        .Where(prefix => !context.StructureSet.Structures.Any(s =>
                            s.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                        .ToList();

                    if (!missing.Any())
                    {
                        results.Add(CreateResult(
                            "Fixation.Structures",
                            "All required Halcyon structures (z_AltaHD_*, z_AltaLD_*, CouchSurface*, CouchInterior*) exist",
                            ValidationSeverity.Info
                        ));
                    }
                    else
                    {
                        foreach (var prefix in requiredPrefixes)
                        {
                            bool structureExists = !missing.Contains(prefix);
                            results.Add(CreateResult(
                                "Fixation.Structures",
                                structureExists
                                    ? $"Required Halcyon structure '{prefix}*' exists"
                                    : $"Required Halcyon structure '{prefix}*' is missing",
                                structureExists ? ValidationSeverity.Info : ValidationSeverity.Error
                            ));
                        }
                    }
                }

                // Required structures for Edge plans - Option A OR Option B
                if (isEdgeMachine)
                {
                    // Option A: Same as Halcyon fixation list
                    var optionAPrefixes = new[]
                    {
                        "z_AltaHD_", "z_AltaLD_",
                        "CouchSurface", "CouchInterior"
                    };

                    // Option B: Encompass fixation
                    var optionBStructures = new[] { "Encompass", "Encompass Base" };

                    // Check if Option A exists (all required)
                    bool hasOptionA = optionAPrefixes.All(prefix =>
                        context.StructureSet.Structures.Any(s =>
                            s.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));

                    // Check if Option B exists (both required)
                    bool hasOptionB = optionBStructures.All(structName =>
                        context.StructureSet.Structures.Any(s =>
                            s.Id.Equals(structName, StringComparison.OrdinalIgnoreCase)));

                    if (hasOptionA || hasOptionB)
                    {
                        string fixationType = hasOptionA ? "Alta/Couch fixation" : "Encompass fixation";
                        results.Add(CreateResult(
                            "Fixation.Structures",
                            $"Edge machine: Required fixation structures exist ({fixationType})",
                            ValidationSeverity.Info
                        ));
                    }
                    else
                    {
                        results.Add(CreateResult(
                            "Fixation.Structures",
                            "Edge machine requires EITHER (z_AltaHD_*, z_AltaLD_*, CouchSurface*, CouchInterior*) OR (Encompass, Encompass Base). Neither set found.",
                            ValidationSeverity.Error
                        ));
                    }
                }

                // Check density overrides for all fixation structures
                var fixationPrefixes = new[]
                {
                    "z_AltaHD_", "z_AltaLD_", "z_FrameHN_", "z_MaskLock_",
                    "z_FrameHead_", "z_LocBar_", "z_ArmShuttle_", "z_EncFrame_",
                    "z_VacBag_", "z_Contrast_", "z_ArmHoldR_", "z_ArmHoldR_",
                    "z_FlexHigh_", "z_FlexLow_", "z_LocBarMR_", "z_VacIndex_"
                };

                var correctDensityStructures = new List<(string Id, double Density)>();
                var densityErrors = new List<ValidationResult>();

                foreach (var structure in context.StructureSet.Structures)
                {
                    var matchingPrefix = fixationPrefixes.FirstOrDefault(prefix =>
                        structure.Id.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

                    if (matchingPrefix != null)
                    {
                        if (structure.Id.Contains("_") && structure.Id.EndsWith("HU", StringComparison.OrdinalIgnoreCase))
                        {
                            string densityStr = structure.Id.Substring(structure.Id.LastIndexOf('_') + 1);

                            if (double.TryParse(densityStr.Substring(0, densityStr.Length - 2), out double expectedDensity))
                            {
                                double actualDensity;
                                bool hasAssignedHU = structure.GetAssignedHU(out actualDensity);

                                if (hasAssignedHU)
                                {
                                    bool isDensityCorrect = Math.Abs(actualDensity - expectedDensity) < 1;

                                    if (isDensityCorrect)
                                    {
                                        correctDensityStructures.Add((structure.Id, actualDensity));
                                    }
                                    else
                                    {
                                        densityErrors.Add(CreateResult(
                                            "Fixation.Density",
                                            $"Structure '{structure.Id}' has incorrect density override: {actualDensity} HU (expected: {expectedDensity} HU)",
                                            ValidationSeverity.Error
                                        ));
                                    }
                                }
                                else
                                {
                                    densityErrors.Add(CreateResult(
                                        "Fixation.Density",
                                        $"Structure '{structure.Id}' has no density override assigned (expected: {expectedDensity} HU)",
                                        ValidationSeverity.Error
                                    ));
                                }
                            }
                        }
                    }
                }

                // If all structures have correct density, show combined message
                if (correctDensityStructures.Any() && !densityErrors.Any())
                {
                    string structureNames = string.Join(", ", correctDensityStructures.Select(s => s.Id));
                    string densityValues = string.Join(", ", correctDensityStructures.Select(s => $"{s.Density} HU"));

                    results.Add(CreateResult(
                        "Fixation.Density",
                        $"Structures {structureNames}\nhave correct density override ({densityValues})",
                        ValidationSeverity.Info
                    ));
                }
                else
                {
                    // Add any errors
                    results.AddRange(densityErrors);
                }
            }

            return results;
        }
    }
}
