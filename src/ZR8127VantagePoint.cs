using System;
using System.Collections.Generic;
using Elements;
using Elements.Geometry;

namespace NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluation
{
    public class PlanAndSectionAngle
    {
        public double plan;
        public double section;

        public PlanAndSectionAngle(double plan, double section)
        {
            this.plan = plan;
            this.section = section;
        }
    }
    public class VantagePoint
    {
        public Vector3 point;
        public Vector3 startDirection;
        public Vector3 ninetyDegreeDirection;

        private Plane sPlane;
        private Plane dPlane;

        private static double vantageDistance = Units.FeetToMeters(250.0);
        private static double longCenterlineLength = Units.FeetToMeters(500.0);

        public VantagePoint(Vector3 point, Vector3 startDirection, Vector3 ninetyDegreeDirection)
        {
            this.point = point;
            this.startDirection = startDirection;
            this.ninetyDegreeDirection = ninetyDegreeDirection;

            this.sPlane = new Plane(point, ninetyDegreeDirection);
            this.dPlane = new Plane(point, startDirection);
        }

        public PlanAndSectionAngle GetPlanAndSectionAngle(Vector3 point)
        {
            var s = this.GetS(point);
            var d = this.GetD(point);
            var h = point.Z;
            var planAngle = GetPlanAngle(s, d);
            var sectionAngle = GetSectionAngle(h, s);
            // Console.WriteLine("--");
            // Console.WriteLine($"{Math.Round(Units.MetersToFeet(s))}, {Math.Round(Units.MetersToFeet(d))}, {planAngle}");
            // Console.WriteLine($"{Math.Round(Units.MetersToFeet(h))}, {Math.Round(Units.MetersToFeet(s))}, {sectionAngle}");
            return new PlanAndSectionAngle(planAngle, sectionAngle);
        }

        public static double GetPlanAngle(double s, double d)
        {
            return Math.Atan(s / d) * (180 / Math.PI);
        }

        public static double GetSectionAngle(double h, double s)
        {
            return Math.Atan(h / s) * (180 / Math.PI);
        }

        private double GetS(Vector3 point)
        {
            return Math.Abs(point.DistanceTo(this.sPlane));
        }

        private double GetD(Vector3 point)
        {
            return point.DistanceTo(this.dPlane);
        }

        /// <summary>
        /// Gets vantage points from a given centerline and near lot line.
        /// Assumes that the centerline and the front lot line are parallel
        /// same-length offsets of each other.
        /// </summary>
        /// <param name="centerline"></param>
        /// <param name="frontLotLine"></param>
        /// <param name="model">Optional: used to help debug visualizations</param>
        /// <returns></returns>
        public static List<VantagePoint> GetVantagePoints(Line centerline, Line frontLotLine, Model model = null)
        {
            var vantagePoints = new List<VantagePoint>();
            var ninetyDegreeDirection = (frontLotLine.PointAt(0.5) - centerline.PointAt(0.5)).Unitized();

            // VP 1
            var base1 = new Vector3(centerline.Start);
            var dir1 = new Vector3(centerline.End - centerline.Start).Unitized();
            var origin1 = base1 + (dir1 * vantageDistance);
            var vp1 = new VantagePoint(origin1, dir1 * -1, ninetyDegreeDirection);
            vantagePoints.Add(vp1);

            // VP 2
            var base2 = new Vector3(centerline.End);
            var dir2 = new Vector3(centerline.Start - centerline.End).Unitized();
            var origin2 = base2 + (dir2 * vantageDistance);
            var vp2 = new VantagePoint(origin2, dir2 * -1, ninetyDegreeDirection);
            vantagePoints.Add(vp2);

            if (centerline.Length() > longCenterlineLength)
            {
                var lineBetweenVps = new Line(origin1, origin2);
                var dir3 = dir1;
                var origin3 = lineBetweenVps.PointAt(0.5);
                var vp3 = new VantagePoint(origin3, dir3 * -1, ninetyDegreeDirection);
                vantagePoints.Add(vp3);
            }

            if (model != null)
            {
                foreach (var vp in vantagePoints)
                {
                    model.AddElement(new ModelCurve(new Circle(vp.point, 1.0).ToPolygon()));
                    model.AddElement(new ModelCurve(new Line(vp.point, vp.point + vp.startDirection), new Material("red", Colors.Red)));
                    model.AddElement(new ModelCurve(new Line(vp.point, vp.point + vp.ninetyDegreeDirection), new Material("blue", Colors.Blue)));
                }
            }

            return vantagePoints;
        }
    }
}