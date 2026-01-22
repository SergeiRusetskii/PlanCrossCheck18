using System;
using System.Collections.Generic;
using System.Linq;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanCrossCheck
{
    // 2.1.1 User Origin CT Marker validator - Ball bearing detection
    // Detects 3 radiopaque markers (ball bearings) at user origin position
    public class UserOriginMarkerValidator : ValidatorBase
    {
        // Configuration constants
        private const double THRESHOLD_HU = 500.0;  // HU threshold for marker detection
        private const double RADIUS_MM = 5.0;       // Search radius around expected marker position

        public override IEnumerable<ValidationResult> Validate(ScriptContext context)
        {
            var results = new List<ValidationResult>();

            // Check prerequisites
            if (context.StructureSet?.Image == null || context.PlanSetup?.Beams == null)
                return results;

            var image = context.StructureSet.Image;
            var userOrigin = image.UserOrigin;

            // Find BODY structure
            var bodyStructure = context.StructureSet.Structures
                .FirstOrDefault(s => s.Id.Equals("BODY", StringComparison.OrdinalIgnoreCase)
                                  && s.DicomType == "EXTERNAL");

            if (bodyStructure == null)
            {
                results.Add(CreateResult(
                    "CT.UserOrigin",
                    "Cannot validate user origin markers: BODY structure (type EXTERNAL) not found",
                    ValidationSeverity.Warning
                ));
                return results;
            }

            // Get treatment orientation
            string orientation = context.PlanSetup?.TreatmentOrientationAsString ?? "Head First-Supine";
            bool isSupine = orientation.Contains("Supine");

            // Calculate slice index
            int sliceIndex = GetSliceIndex(userOrigin.z, image);

            // Check slice bounds
            if (sliceIndex < 0 || sliceIndex >= image.ZSize)
            {
                results.Add(CreateResult(
                    "CT.UserOrigin",
                    $"User Origin Z coordinate vs CT zero (slice {sliceIndex}, image has {image.ZSize} slices) is outside acceptable limits",
                    ValidationSeverity.Error
                ));
                return results;
            }

            // Calculate slice search span based on radius and slice thickness
            double sliceThickness = image.ZRes;
            int sliceSpan = (int)Math.Ceiling(RADIUS_MM / sliceThickness);

            // Search for markers
            var detectedMarkers = new List<string>();
            var detectedPositions = new List<VVector>();

            // Try to find horizontal intersections (left and right)
            var horizontalPoints = FindHorizontalSkinIntersections(
                bodyStructure, userOrigin, sliceIndex, image);

            if (horizontalPoints != null && horizontalPoints.Count == 2)
            {
                // Check left marker
                if (HasMarker(horizontalPoints[0], sliceIndex, sliceSpan, image))
                {
                    detectedMarkers.Add("Left");
                    detectedPositions.Add(horizontalPoints[0]);
                }

                // Check right marker
                if (HasMarker(horizontalPoints[1], sliceIndex, sliceSpan, image))
                {
                    detectedMarkers.Add("Right");
                    detectedPositions.Add(horizontalPoints[1]);
                }
            }

            // Try to find vertical intersection (upper)
            var verticalPoint = FindVerticalSkinIntersection(
                bodyStructure, userOrigin, sliceIndex, image, isSupine);

            if (verticalPoint != null)
            {
                if (HasMarker(verticalPoint.Value, sliceIndex, sliceSpan, image))
                {
                    detectedMarkers.Add("Upper");
                    detectedPositions.Add(verticalPoint.Value);
                }
            }

            // Determine severity and create message
            int markersDetected = detectedMarkers.Count;
            ValidationSeverity severity;
            string message;

            if (markersDetected == 3)
            {
                severity = ValidationSeverity.Info;
                message = $"3 of 3 markers detected in {RADIUS_MM:F0} mm radius around User origin placement";
            }
            else
            {
                // Determine which markers are missing
                var allMarkers = new List<string> { "Left", "Right", "Upper" };
                var missingMarkers = allMarkers.Except(detectedMarkers).ToList();

                severity = ValidationSeverity.Warning;
                message = $"{markersDetected} of 3 markers detected in {RADIUS_MM:F0} mm radius around User origin placement\n" +
                         $"{string.Join("/", missingMarkers)} marker(s) not found (on screen direction)";
            }

            results.Add(CreateResult("CT.UserOrigin", message, severity));

            return results;
        }

        private int GetSliceIndex(double userOriginZ, Image image)
        {
            return (int)Math.Round((userOriginZ - image.Origin.z) / image.ZRes);
        }

        private List<VVector> FindHorizontalSkinIntersections(
            Structure body, VVector userOrigin, int sliceIndex, Image image)
        {
            // Get contours on the slice containing user origin
            var contours = body.GetContoursOnImagePlane(sliceIndex);

            if (!contours.Any())
                return null;

            // Find the contour that contains the user origin (or closest)
            var targetContour = FindContourContainingPoint(contours, userOrigin.x, userOrigin.y);

            if (targetContour == null)
                return null;

            // Find intersections of horizontal line y = userOrigin.y with the contour
            double y0 = userOrigin.y;
            double x0 = userOrigin.x;

            var intersections = new List<VVector>();

            for (int i = 0; i < targetContour.Length; i++)
            {
                var p1 = targetContour[i];
                var p2 = targetContour[(i + 1) % targetContour.Length];

                // Check if the segment crosses the horizontal line
                if ((p1.y <= y0 && p2.y >= y0) || (p1.y >= y0 && p2.y <= y0))
                {
                    // Avoid division by zero
                    if (Math.Abs(p2.y - p1.y) < 0.001)
                        continue;

                    // Interpolate to find x coordinate at y = y0
                    double t = (y0 - p1.y) / (p2.y - p1.y);
                    double x = p1.x + t * (p2.x - p1.x);
                    double z = p1.z + t * (p2.z - p1.z);

                    intersections.Add(new VVector(x, y0, z));
                }
            }

            if (intersections.Count < 2)
                return null;

            // Find the two intersections closest to x0 (one on each side)
            var leftIntersections = intersections.Where(p => p.x < x0).OrderByDescending(p => p.x).ToList();
            var rightIntersections = intersections.Where(p => p.x > x0).OrderBy(p => p.x).ToList();

            if (!leftIntersections.Any() || !rightIntersections.Any())
                return null;

            return new List<VVector> { leftIntersections.First(), rightIntersections.First() };
        }

        private VVector? FindVerticalSkinIntersection(
            Structure body, VVector userOrigin, int sliceIndex, Image image, bool isSupine)
        {
            var contours = body.GetContoursOnImagePlane(sliceIndex);

            if (!contours.Any())
                return null;

            var targetContour = FindContourContainingPoint(contours, userOrigin.x, userOrigin.y);

            if (targetContour == null)
                return null;

            // Find intersections of vertical line x = userOrigin.x with the contour
            double x0 = userOrigin.x;
            double y0 = userOrigin.y;

            var intersections = new List<VVector>();

            for (int i = 0; i < targetContour.Length; i++)
            {
                var p1 = targetContour[i];
                var p2 = targetContour[(i + 1) % targetContour.Length];

                // Check if the segment crosses the vertical line
                if ((p1.x <= x0 && p2.x >= x0) || (p1.x >= x0 && p2.x <= x0))
                {
                    // Avoid division by zero
                    if (Math.Abs(p2.x - p1.x) < 0.001)
                        continue;

                    // Interpolate to find y coordinate at x = x0
                    double t = (x0 - p1.x) / (p2.x - p1.x);
                    double y = p1.y + t * (p2.y - p1.y);
                    double z = p1.z + t * (p2.z - p1.z);

                    intersections.Add(new VVector(x0, y, z));
                }
            }

            if (intersections.Count == 0)
                return null;

            // Select the "upper" intersection based on orientation
            // In DICOM: y increases toward posterior (back)
            if (isSupine)
            {
                // For supine: upper = most anterior = smallest y
                return intersections.OrderBy(p => p.y).First();
            }
            else
            {
                // For prone: upper = most posterior = largest y
                return intersections.OrderByDescending(p => p.y).First();
            }
        }

        private VVector[] FindContourContainingPoint(VVector[][] contours, double x, double y)
        {
            // First try to find a contour that contains the point
            foreach (var contour in contours)
            {
                if (IsPointInPolygon(contour, x, y))
                    return contour;
            }

            // If no contour contains it, find the closest one
            double minDistance = double.MaxValue;
            VVector[] closestContour = null;

            foreach (var contour in contours)
            {
                foreach (var point in contour)
                {
                    double dist = Math.Sqrt(Math.Pow(point.x - x, 2) + Math.Pow(point.y - y, 2));
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        closestContour = contour;
                    }
                }
            }

            return closestContour;
        }

        private bool IsPointInPolygon(VVector[] polygon, double x, double y)
        {
            // Ray casting algorithm
            bool inside = false;
            int j = polygon.Length - 1;

            for (int i = 0; i < polygon.Length; i++)
            {
                if ((polygon[i].y > y) != (polygon[j].y > y) &&
                    x < (polygon[j].x - polygon[i].x) * (y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
                j = i;
            }

            return inside;
        }

        private bool HasMarker(VVector point, int centerSlice, int sliceSpan, Image image)
        {
            // Search in a radius around the point, across multiple slices
            int radiusPixelsX = (int)Math.Ceiling(RADIUS_MM / image.XRes);
            int radiusPixelsY = (int)Math.Ceiling(RADIUS_MM / image.YRes);

            // Convert point to voxel indices
            int centerX = (int)Math.Round((point.x - image.Origin.x) / image.XRes);
            int centerY = (int)Math.Round((point.y - image.Origin.y) / image.YRes);

            // Search across slices
            for (int sliceOffset = -sliceSpan; sliceOffset <= sliceSpan; sliceOffset++)
            {
                int z = centerSlice + sliceOffset;

                // Check slice bounds
                if (z < 0 || z >= image.ZSize)
                    continue;

                // Get voxel data for this slice
                int[,] voxelBuffer = new int[image.XSize, image.YSize];
                image.GetVoxels(z, voxelBuffer);

                // Search in a circular region
                for (int dx = -radiusPixelsX; dx <= radiusPixelsX; dx++)
                {
                    for (int dy = -radiusPixelsY; dy <= radiusPixelsY; dy++)
                    {
                        int xIdx = centerX + dx;
                        int yIdx = centerY + dy;

                        // Check bounds
                        if (xIdx < 0 || xIdx >= image.XSize || yIdx < 0 || yIdx >= image.YSize)
                            continue;

                        // Check if within radius (spherical, not cubic)
                        double distMm = Math.Sqrt(
                            Math.Pow(dx * image.XRes, 2) +
                            Math.Pow(dy * image.YRes, 2) +
                            Math.Pow(sliceOffset * image.ZRes, 2)
                        );

                        if (distMm > RADIUS_MM)
                            continue;

                        // Convert voxel value to HU
                        double hu = image.VoxelToDisplayValue(voxelBuffer[xIdx, yIdx]);

                        // Check threshold - if at least 1 voxel is above threshold, marker is detected
                        if (hu >= THRESHOLD_HU)
                            return true;
                    }
                }
            }

            return false;
        }
    }
}
