using System;
using System.Collections.Generic;
using Elements;
using Elements.Geometry;
using System.Linq;

namespace NYCZR8127DaylightEvaluation
{
    public class AnalysisPoint
    {
        public Vector3 PlanAndSection;

        public Vector3 DrawCoordinate;

        public AnalysisPoint(double plan, double section)
        {
            this.PlanAndSection = new Vector3(plan, section);
        }
    }
    public class VantagePoint
    {
        public VantageStreets VantageStreet;
        public Vector3 Point;
        public Vector3 StartDirection;
        public Vector3 FrontDirection;

        public double CenterlineOffsetDist;
        public Polyline Centerline;
        public Line NearLotLine;
        public Line FarLotLine;
        public Line FrontLotLine;
        public Line RearLotLine;

        public Domain1d DaylightBoundaries;
        public List<Vector3> DaylightBoundariesPoints = new List<Vector3>() { new Vector3(), new Vector3() };

        public Diagram Diagram;

        private Plane sPlane;
        private Plane dPlane;

        public static double VantageDistanceInFt = 250.0;
        public static double VantageDistance = Units.FeetToMeters(VantageDistanceInFt);
        private static double longCenterlineLength = VantageDistance * 2;

        public VantagePoint(
            VantageStreets vantageStreet,
            Vector3 point,
            Vector3 startDirection,
            Vector3 ninetyDegreeDirection,
            List<Line> lotLines,
            double centerlineOffsetDist = 0.0
        )
        {
            this.VantageStreet = vantageStreet;
            this.Point = point;
            this.StartDirection = startDirection;
            this.FrontDirection = ninetyDegreeDirection;
            this.CenterlineOffsetDist = centerlineOffsetDist;

            this.sPlane = new Plane(point, ninetyDegreeDirection);

            if (Math.Abs(90 - startDirection.PlaneAngleTo(ninetyDegreeDirection)) < Vector3.EPSILON)
            {
                this.dPlane = new Plane(point, startDirection);
            }
            else
            {
                this.dPlane = new Plane(point, startDirection * -1);
            }

            var lotLinesBySDist = new List<Line>(lotLines).OrderBy(line => this.GetS(line.PointAt(0.5))).ToList();

            this.FrontLotLine = lotLinesBySDist[0];
            this.RearLotLine = lotLinesBySDist[3];

            var move = -1 * this.FrontDirection * centerlineOffsetDist;
            var points = new List<Vector3>() { this.FrontLotLine.Start + move, this.FrontLotLine.End + move };
            this.Centerline = new Polyline(points);

            var nearFarLines = new List<Line>() { lotLinesBySDist[1], lotLinesBySDist[2] }.OrderBy(line => Math.Abs(this.GetD(line.PointAt(0.5)))).ToList();

            this.NearLotLine = nearFarLines[0];
            this.FarLotLine = nearFarLines[1];

            this.Diagram = new Diagram(this);
        }

        public AnalysisPoint GetAnalysisPoint(Vector3 point, Boolean useDebugVisualization = false)
        {
            var s = this.GetS(point);
            var d = this.GetD(point);
            var h = point.Z;
            var planAngle = GetPlanAngle(s, d);
            var sectionAngle = GetSectionAngle(h, s);
            return this.GetAnalysisPoint(planAngle, sectionAngle, useDebugVisualization);
        }

        public AnalysisPoint GetAnalysisPoint(double planAngle, double sectionAngle, Boolean useDebugVisualization = false)
        {
            var planAndSectionAngle = new AnalysisPoint(planAngle, sectionAngle);
            if (useDebugVisualization)
            {
                planAndSectionAngle.DrawCoordinate = new Vector3(planAndSectionAngle.PlanAndSection);
            }
            else
            {
                planAndSectionAngle.DrawCoordinate = Diagram.MapCoordinate(planAngle, sectionAngle);
            }
            return planAndSectionAngle;
        }

        public static double GetPlanAngle(double s, double d)
        {
            var angle = Math.Atan(s / d) * (180 / Math.PI);
            if (angle < 0)
            {
                return -90 - angle;
            }
            return 90 - angle;
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
        /// <param name="model">Used to help debug visualizations</param>
        /// <returns></returns>
        public static List<VantagePoint> GetVantagePoints(Polygon rectangularSite, VantageStreets vantageStreet, Model model = null)
        {
            if (vantageStreet.Line == null)
            {
                throw new Exception("Each vantage street must have a line designating its rough location. Please draw a line outside of your lot that represents the centerline of your vantage street. It does not need to be straight or exactly parallel to the lot line, but it must exist.");
            }
            var siteCentroid = rectangularSite.Centroid();
            var midpoint = vantageStreet.Line.PointAt(0.5);
            var lotLines = new List<Line>(rectangularSite.Segments()).OrderBy(segment => midpoint.DistanceTo(segment.PointAt(0.5))).ToList();
            var frontLotLine = lotLines[0];
            var centerlineOffsetDist = Settings.CenterlineDistances[vantageStreet.Width] / 2;
            var directionToStreet = new Vector3(frontLotLine.PointAt(0.5) - siteCentroid).Unitized() * centerlineOffsetDist;
            var centerline = new Line(frontLotLine.Start + directionToStreet, frontLotLine.End + directionToStreet);

            var vantagePoints = new List<VantagePoint>();
            var ninetyDegreeDirection = (frontLotLine.PointAt(0.5) - centerline.PointAt(0.5)).Unitized();

            var hasThirdVantagePoint = centerline.Length() > longCenterlineLength;

            // VP 1
            var base1 = new Vector3(centerline.Start);
            var dir1 = new Vector3(centerline.End - centerline.Start).Unitized();
            var origin1 = base1 + (dir1 * VantageDistance);
            var vp1 = new VantagePoint(
                vantageStreet,
                origin1,
                dir1 * -1,
                ninetyDegreeDirection,
                lotLines,
                centerlineOffsetDist
            );
            vantagePoints.Add(vp1);

            // VP 2
            var base2 = new Vector3(centerline.End);
            var dir2 = new Vector3(centerline.Start - centerline.End).Unitized();
            var origin2 = base2 + (dir2 * VantageDistance);
            var vp2 = new VantagePoint(
                vantageStreet,
                origin2,
                dir2 * -1,
                ninetyDegreeDirection,
                lotLines,
                centerlineOffsetDist
            );
            vantagePoints.Add(vp2);

            if (hasThirdVantagePoint)
            {
                var lineBetweenVps = new Line(origin1, origin2);
                var dir3 = dir1;
                var origin3 = lineBetweenVps.PointAt(0.5);
                var vp3 = new VantagePoint(
                    vantageStreet,
                    origin3,
                    dir3 * -1,
                    ninetyDegreeDirection,
                    lotLines,
                    centerlineOffsetDist
                );
                vantagePoints.Add(vp3);
            }

            calculateDaylightBoundaries(vantageStreet, vantagePoints, model);
            foreach (var vp in vantagePoints)
            {
                vp.Diagram.CalculateProfileCurvesAndBoundingSquares();
            }

            // Visualize if we are able to
            if (model != null)
            {
                model.AddElement(new ModelCurve(centerline));

                foreach (var vp in vantagePoints)
                {
                    model.AddElement(new ModelCurve(new Circle(vp.Point, 1.0).ToPolygon()));
                    model.AddElement(new ModelCurve(new Line(vp.Point, vp.Point + vp.StartDirection), new Material("red", Colors.Red)));
                    model.AddElement(new ModelCurve(new Line(vp.Point, vp.Point + vp.FrontDirection), new Material("blue", Colors.Blue)));
                }
            }

            return vantagePoints;
        }

        private static void calculateDaylightBoundaries(VantageStreets vantageStreet, List<VantagePoint> orderedStreetVantagePts, Model model = null)
        {
            if (orderedStreetVantagePts.Count < 2 || orderedStreetVantagePts.Count > 3)
            {
                throw new Exception($"Vantage streets must have a minimum of two and a maximum of three vantage points. {orderedStreetVantagePts.Count} vantage points were found.");
            }

            foreach (var vp in orderedStreetVantagePts.Take(2))
            {
                var farPoint = vp.Point + (vp.StartDirection * VantageDistance) + (vp.FrontDirection * vp.CenterlineOffsetDist);
                vp.DaylightBoundariesPoints[0] = farPoint;
            }

            if (orderedStreetVantagePts.Count == 2)
            {
                foreach (var vp in orderedStreetVantagePts)
                {
                    var nearPointOnNearLot = new List<Vector3>() { vp.NearLotLine.Start, vp.NearLotLine.End }.OrderBy(pt => pt.DistanceTo(vp.Point)).ToList()[0];
                    // Move intersection point of the near lot line and front lot line
                    // towards the rear, to the lesser of 100' or the centerline of the block from the front lot line
                    var pointForBounds = nearPointOnNearLot + vp.FrontDirection * Math.Min(Units.FeetToMeters(100), Units.FeetToMeters(vantageStreet.BlockDepthInFeet / 2));
                    vp.DaylightBoundariesPoints[1] = pointForBounds;
                }
            }

            if (orderedStreetVantagePts.Count == 3)
            {
                foreach (var vp in orderedStreetVantagePts.SkipLast(1))
                {
                    vp.DaylightBoundariesPoints[1] = vp.Point + (vp.FrontDirection * vp.CenterlineOffsetDist);
                }
                var vp1 = orderedStreetVantagePts[0];
                var vp2 = orderedStreetVantagePts[1];
                var vp3 = orderedStreetVantagePts[2];
                vp3.DaylightBoundariesPoints[0] = vp1.DaylightBoundariesPoints[1];
                vp3.DaylightBoundariesPoints[1] = vp2.DaylightBoundariesPoints[1];
            }

            foreach (var vp in orderedStreetVantagePts)
            {
                var min = vp.GetAnalysisPoint(vp.DaylightBoundariesPoints[0]).PlanAndSection.X;
                var max = vp.GetAnalysisPoint(vp.DaylightBoundariesPoints[1]).PlanAndSection.X;

                if (max < min)
                {
                    vp.DaylightBoundariesPoints.Reverse();
                    vp.DaylightBoundaries = new Domain1d(max, min);
                }
                else
                {
                    vp.DaylightBoundaries = new Domain1d(min, max);
                }
            }
        }
    }
}