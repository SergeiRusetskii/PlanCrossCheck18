using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // 5 Planning structures validator
    public class PlanningStructuresValidator : ValidatorBase
    {
        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            // Check z_Air_ structure
            var airStructures = context.StructureSet.Structures
                .Where(s => s.Id.StartsWith("z_Air_", StringComparison.OrdinalIgnoreCase));

            foreach (var structure in airStructures)
            {
                // Extract expected density from structure name (e.g., z_Air_-800HU)
                if (structure.Id.Contains("_") && structure.Id.EndsWith("HU", StringComparison.OrdinalIgnoreCase))
                {
                    string densityStr = structure.Id.Substring(structure.Id.LastIndexOf('_') + 1);

                    if (double.TryParse(densityStr.Substring(0, densityStr.Length - 2), out double expectedDensity))
                    {
                        // Check assigned HU
                        double actualDensity;
                        bool hasAssignedHU = structure.GetAssignedHU(out actualDensity);

                        if (hasAssignedHU)
                        {
                            bool isDensityCorrect = Math.Abs(actualDensity - expectedDensity) < 1;

                            results.Add(CreateResult(
                                "PlanningStructures.z_Air Density",
                                isDensityCorrect
                                    ? $"Air structure '{structure.Id}' has correct density override ({actualDensity} HU)"
                                    : $"Air structure '{structure.Id}' has incorrect density override: {actualDensity} HU " +
                                    $"(expected: {expectedDensity} HU)",
                                isDensityCorrect ? ValidationSeverity.Info : ValidationSeverity.Error
                            ));
                        }
                        else
                        {
                            results.Add(CreateResult(
                                "PlanningStructures.z_Air Density",
                                $"Air structure '{structure.Id}' has no density override assigned (expected: {expectedDensity} HU)",
                                ValidationSeverity.Error
                            ));
                        }

                        // Check original density distribution with sampling
                        if (context.StructureSet.Image != null)
                        {
                            int totalVoxels = 0;
                            int voxelsAboveThreshold = 0;

                            int xSize = context.StructureSet.Image.XSize;
                            int ySize = context.StructureSet.Image.YSize;
                            int zSize = context.StructureSet.Image.ZSize;

                            // Sampling parameters - adjust for speed vs accuracy
                            int sampleStep = 2; // Check every 2nd voxel in each dimension
                            int zStep = 2; // Check every 2nd slice

                            // Expected density for distribution
                            var densityThreshold = expectedDensity + 25; // Allow 25 HU tolerance

                            // Preallocate buffer for voxel data
                            int[,] voxelBuffer = new int[xSize, ySize];

                            // Iterate through sampled image planes
                            for (int z = 0; z < zSize; z += zStep)
                            {
                                // Get voxels for this plane
                                context.StructureSet.Image.GetVoxels(z, voxelBuffer);

                                // Check if structure has contours on this plane
                                var contours = structure.GetContoursOnImagePlane(z);
                                if (!contours.Any()) continue;

                                // Get image geometry
                                VVector origin = context.StructureSet.Image.Origin;
                                double xRes = context.StructureSet.Image.XRes;
                                double yRes = context.StructureSet.Image.YRes;
                                double zPos = origin.z + z * context.StructureSet.Image.ZRes;

                                // Check sampled voxels in the plane
                                for (int x = 0; x < xSize; x += sampleStep)
                                {
                                    for (int y = 0; y < ySize; y += sampleStep)
                                    {
                                        // Get voxel position in DICOM coordinates
                                        double xPos = origin.x + x * xRes;
                                        double yPos = origin.y + y * yRes;

                                        // Check if voxel is inside structure
                                        if (structure.IsPointInsideSegment(new VVector(xPos, yPos, zPos)))
                                        {
                                            totalVoxels++;

                                            // Convert voxel value to HU
                                            int voxelValue = voxelBuffer[x, y];
                                            double huValue = context.StructureSet.Image.VoxelToDisplayValue(voxelValue);

                                            if (huValue > densityThreshold)
                                            {
                                                voxelsAboveThreshold++;
                                            }
                                        }
                                    }
                                }
                            }

                            if (totalVoxels > 0)
                            {
                                // Adjust for sampling
                                double samplingFactor = sampleStep * sampleStep * zStep;
                                double percentageAbove = (double)voxelsAboveThreshold / totalVoxels * 100;
                                bool isPercentageValid = percentageAbove <= 5.0;

                                results.Add(CreateResult(
                                    "PlanningStructures.z_Air Density",
                                    isPercentageValid
                                        ? $"Air structure '{structure.Id}': {percentageAbove:F1}% " +
                                          $"of voxels exceed {densityThreshold} HU (within 5% limit)"
                                        : $"Air structure '{structure.Id}': {percentageAbove:F1}% " +
                                          $"of voxels exceed {densityThreshold} HU (exceeds 5% limit)",
                                    isPercentageValid ? ValidationSeverity.Info : ValidationSeverity.Warning
                                ));
                            }
                        }
                    }
                }
            }

            return results;
        }
    }
}
