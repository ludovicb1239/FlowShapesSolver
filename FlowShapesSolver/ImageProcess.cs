using System;
using System.Collections.Generic;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System.Drawing;
using System.Security.Policy;
using static FlowShapesSolver.CellDetector;
using Emgu.CV.Features2D;

namespace FlowShapesSolver
{
    public class CellDetector
    {
        /// <summary>
        /// Detects cells in the given image and returns their contours and centers.
        /// </summary>
        /// <param name="imagePath">Path to the input image.</param>
        /// <returns>A list of detected cells with border points and center.</returns>
        public static List<Cell> DetectCells(Bitmap bitmap, string name)
        {
            Mat colorMat = bitmap.ToMat();
            Mat grayMat = new Mat();
            CvInvoke.CvtColor(colorMat, grayMat, ColorConversion.Bgr2Gray);

            // Mat grayMat = CvInvoke.Imread(imagePath, ImreadModes.Grayscale);

            // Create a binary mask:
            Mat mask = new Mat();
            CvInvoke.Threshold(grayMat, mask, 45, 255, ThresholdType.Binary);

            // Use bitwise AND to apply the mask:
            // For pixels where mask is 0, the output will be 0 (black);
            // otherwise, the original pixel value is retained.
            CvInvoke.BitwiseAnd(grayMat, mask, grayMat);

            // (Optional) Apply a blur to reduce noise
            // CvInvoke.GaussianBlur(grayMat, grayMat, new System.Drawing.Size(3, 3), 0);

            // 3. Threshold or Canny edge detection to highlight cell borders
            Mat edgeMat = new();
            grayMat.Save(name + "/Gray.png");
            mask.Save(name + "/mask.png");

            // 4. Find contours
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(
                grayMat,
                contours,
                null,
                RetrType.List,
                ChainApproxMethod.ChainApproxSimple
            );

            Mat debugContourImage = new Mat(grayMat.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 3);
            // Set background to black (you could use white or another color)
            debugContourImage.SetTo(new MCvScalar(0, 0, 0));


            Cluster[] clusters = new Cluster[contours.Size];

            // 5. Iterate over all contours
            for (int i = 0; i < contours.Size; i++)
            {
                clusters[i].indx = i;
                clusters[i].area = CvInvoke.ContourArea(contours[i]);
                // Filter out too-small or too-large contours
                if (clusters[i].area < 1000 || clusters[i].area > 60000)
                {
                    // Skip this contour if it's out of size range
                    // Console.WriteLine(imageName + " - Contour " + i + " has area " + clusters[i].area + " (bad)");
                    clusters[i].isBad = true;
                    continue;
                }
                clusters[i].isBad = false;
                // Console.WriteLine(imageName + " - Contour " + i + " has area " + clusters[i].area);
            }
            FindBiggestCluster(clusters);

            List<Line> lines = new();
            int indx = 0;

            List<Cell> cells = new();
            for (int i = 0; i < contours.Size; i++)
            {
                Color col = ColorFromH(i * 360 / contours.Size);
                CvInvoke.DrawContours(
                    debugContourImage,
                    contours,
                    i,
                    new MCvScalar(col.R, col.G, col.B), // green color
                    clusters[i].isBad ? 1 : 2 // line thickness
                );

                if (clusters[i].isBad)
                    continue;


                // Approximate polygon to simplify shape
                using (VectorOfPoint approxContour = new VectorOfPoint())
                {
                    double epsilon = 0.01 * CvInvoke.ArcLength(contours[i], true);
                    CvInvoke.ApproxPolyDP(contours[i], approxContour, epsilon, true);

                    if (approxContour.Size < 3)
                    {
                        // Console.WriteLine(imageName + " - Contour " + i + " has too few points");
                        // Skip this contour if it's too small
                        continue;
                    }

                    Point center = CenterOfContour(approxContour);

                    // Convert contour points to a more convenient structure
                    for (int j = 0; j < approxContour.Size - 1; j++)
                    {
                        Point a = new(approxContour[j].X, approxContour[j].Y);
                        Point b = new(approxContour[j + 1].X, approxContour[j + 1].Y);
                        lines.Add(new(a, b, indx));
                    }
                    Point c = new(approxContour[0].X, approxContour[0].Y);
                    Point d = new(approxContour[approxContour.Size - 1].X, approxContour[approxContour.Size - 1].Y);
                    lines.Add(new(c, d, indx));

                    Color cellColor = bitmap.GetPixel(center.x, center.y);
                    if (cellColor.GetBrightness() < 0.1f)
                        cellColor = Color.Black;
                    else
                        cellColor = Color.FromArgb(cellColor.R & 0b11111000, cellColor.G & 0b11111000, cellColor.B & 0b11111000);

                    // Console.WriteLine(imageName + " - Color of cell " + indx + " is " + cellColor);

                    Cell newCell = new Cell(cellColor, center);
                    cells.Add(newCell);

                    indx++;
                }
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (Distance(lines[i].start, lines[i].end) < 12.0)
                    continue;
                for (int j = i + 1; j < lines.Count; j++)
                {
                    if (lines[i].indx == lines[j].indx)
                        continue;

                    if (Distance(lines[j].start, lines[j].end) < 12.0)
                        continue;

                    if (Distance(lines[i].center, lines[j].center) < 30)
                    {
                        // Check if point if close to line
                        if (DistancePointToLine(lines[i].center, lines[j]) < 5.0 ||
                            DistancePointToLine(lines[j].center, lines[i]) < 5.0)
                        {

                            cells[lines[i].indx].AddNeighbor(cells[lines[j].indx]);
                            // Console.WriteLine(j + " is a neighbor of " + i);
                        }
                    }
                }
            }
            // Make sure cells without neighbors are removed
            List<Cell> toRemove = new();
            foreach (Cell cell in cells)
            {
                if (cell.isOrphan)
                {
                    // Console.WriteLine("Cell " + cells.IndexOf(cell) + " has no neighbors");
                    toRemove.Add(cell);
                }
            }
            foreach (Cell cell in toRemove)
                cells.Remove(cell);

            // Finally, save the result as PNG
            string outputPath = name + "/contours_output.png";
            CvInvoke.Imwrite(outputPath, debugContourImage);

            return cells;
        }


        public static Point CenterOfContour(VectorOfPoint contour)
        {
            // Calculate center using image moments
            var moments = CvInvoke.Moments(contour, true);
            Point p = new();
            p.x = (int)(moments.M10 / (moments.M00 + 1e-5));
            p.y = (int)(moments.M01 / (moments.M00 + 1e-5));
            return p;
        }



        public static Color ColorFromH(double hue)
        {
            // Hue is given in degrees (0-360)
            // Saturation and Value are given as 0-1

            int hi = (int)Math.Floor(hue / 60) % 6;
            double f = (hue / 60) - Math.Floor(hue / 60);

            int v = Convert.ToInt32(255);
            int p = Convert.ToInt32(0);
            int q = Convert.ToInt32(255 * (1 - f));
            int t = Convert.ToInt32(255 * (f));

            switch (hi)
            {
                case 0:
                    return Color.FromArgb(v, t, p);
                case 1:
                    return Color.FromArgb(q, v, p);
                case 2:
                    return Color.FromArgb(p, v, t);
                case 3:
                    return Color.FromArgb(p, q, v);
                case 4:
                    return Color.FromArgb(t, p, v);
                case 5:
                default:
                    return Color.FromArgb(v, p, q);
            }
        }




        private static float Distance(Point a, Point b)
        {
            float dx = a.x - b.x;
            float dy = a.y - b.y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public struct Line
        {
            public Point start;
            public Point end;
            public int indx;
            public Point center;
            public double length;

            public Line(Point start, Point end, int indx)
            {
                this.start = start;
                this.end = end;
                this.indx = indx;
                this.center = new Point();
                this.center.x = (start.x + end.x) / 2;
                this.center.y = (start.y + end.y) / 2;
            }
        }

        public static double DistancePointToLine(Point p, Line l)
        {
            // Compute the differences between points
            double deltaX = l.end.x - l.start.x;
            double deltaY = l.end.y - l.start.y;

            // Calculate the numerator using the absolute value of the cross product of vectors (b - a) and (p - a)
            double numerator = Math.Abs(deltaY * (p.x - l.start.x) - deltaX * (p.y - l.start.y));

            // Calculate the denominator (distance between a and b)
            double denominator = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

            // Avoid division by zero if a and b are the same point.
            if (denominator == 0)
                throw new ArgumentException("Points a and b cannot be the same.");

            // The distance is the area of the parallelogram divided by the length of the base (line ab)
            return numerator / denominator;
        }

        public struct Cluster
        {
            public double area;
            public int indx;
            public Point center;
            public bool isBad;
        }

        public static void FindBiggestCluster(Cluster[] clusters, double threshold = 0.45f)
        {
            if (clusters == null || clusters.Length == 0)
                return;

            // 1. Sort by area (make a copy so we don’t lose original order).
            //    We rely on `indx` to map back to original positions.
            var sorted = clusters.OrderBy(c => c.area).ToArray();
            // Console.WriteLine(string.Join("\n", sorted.Select(c => Math.Log (c.area ) )));

            // 2. Group items into clusters if consecutive areas differ by <= threshold.
            var listOfGroups = new List<List<int>>();
            listOfGroups.Add(new List<int> { 0 }); // first group starts with first item

            for (int i = 1; i < sorted.Length; i++)
            {
                if (sorted[i].isBad)
                    continue;
                double diff = Math.Log( sorted[i].area ) - Math.Log ( sorted[i - 1].area );
                // Console.WriteLine(diff);
                if (diff <= threshold )
                {
                    // same cluster as the previous item
                    listOfGroups[listOfGroups.Count - 1].Add(i);
                }
                else
                {
                    // start a new cluster
                    listOfGroups.Add(new List<int> { i });
                }
            }

            // 3. Find the cluster with the most members
            var biggestGroup = listOfGroups.OrderByDescending(g => g.Count).First();

            // 4. Mark all as bad initially
            for (int i = 0; i < clusters.Length; i++)
            {
                clusters[i].isBad = true;
            }

            // 5. Mark only the biggest cluster as not bad
            //    We look up the original index from `sorted[idx].indx`
            foreach (int idx in biggestGroup)
            {
                int originalIndex = sorted[idx].indx;
                clusters[originalIndex].isBad = false;
            }
        }
    }
}