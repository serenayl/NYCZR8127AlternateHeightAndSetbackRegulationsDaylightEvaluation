using System;
using System.Collections.Generic;
using Elements;
using Elements.Geometry;

namespace NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluation
{
    public class VantagePoint
    {
        public Vector3 point;
        public Vector3 direction;

        private static double vantageDistance = Units.FeetToMeters(250.0);
        private static double longCenterlineLength = Units.FeetToMeters(500.0);

        public VantagePoint(Vector3 point, Vector3 direction)
        {
            this.point = point;
            this.direction = direction;
        }

        public static List<VantagePoint> getVantagePoints(Line centerline, List<Line> lotLines, Model model = null)
        {
            var vantagePoints = new List<VantagePoint>();

            // VP 1
            var base1 = new Vector3(centerline.Start);
            var dir1 = new Vector3(centerline.End - centerline.Start).Unitized();
            var origin1 = base1 + (dir1 * vantageDistance);
            var vp1 = new VantagePoint(origin1, dir1 * -1);
            vantagePoints.Add(vp1);

            // VP 2
            var base2 = new Vector3(centerline.End);
            var dir2 = new Vector3(centerline.Start - centerline.End).Unitized();
            var origin2 = base2 + (dir2 * vantageDistance);
            var vp2 = new VantagePoint(origin2, dir2 * -1);
            vantagePoints.Add(vp2);

            if (centerline.Length() > Units.FeetToMeters(longCenterlineLength))
            {
                var lineBetweenVps = new Line(origin1, origin2);
                var dir3 = dir1;
                var origin3 = lineBetweenVps.PointAt(0.5);
                var vp3 = new VantagePoint(origin3, dir3 * -1);
                vantagePoints.Add(vp3);
            }

            if (model != null)
            {
                foreach (var vp in vantagePoints)
                {
                    Console.WriteLine($"adding vantage point: {vp.point.X}, {vp.point.Y}");
                    Console.WriteLine(vp.point.X);
                    model.AddElement(new ModelCurve(new Circle(vp.point, 1.0).ToPolygon()));
                }
            }

            return vantagePoints;
        }
    }
}