using System;
using System.Collections.Generic;
using Elements;
using Elements.Geometry;
using System.Linq;

namespace NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluation
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
        public Vector3 Point;
        public Vector3 StartDirection;
        public Vector3 FrontDirection;

        public double CenterlineOffsetDist;
        public Line NearLotLine;
        public Line FarLotLine;
        public Line FrontLotLine;
        public Line RearLotLine;

        public List<double> DaylightBoundaries = new List<double>() { -90.0, -90.0 };
        public List<Vector3> DaylightBoundariesPoints = new List<Vector3>() { new Vector3(), new Vector3() };

        public Diagram Diagram;

        private Plane sPlane;
        private Plane dPlane;

        public static double VantageDistanceInFt = 250.0;
        public static double VantageDistance = Units.FeetToMeters(VantageDistanceInFt);
        private static double longCenterlineLength = VantageDistance * 2;

        public VantagePoint(
            Vector3 point,
            Vector3 startDirection,
            Vector3 ninetyDegreeDirection,
            List<Line> lotLinesByDistToStreet,
            double centerlineOffsetDist = 0.0
        )
        {
            this.Point = point;
            this.StartDirection = startDirection;
            this.FrontDirection = ninetyDegreeDirection;
            this.CenterlineOffsetDist = centerlineOffsetDist;

            this.sPlane = new Plane(point, ninetyDegreeDirection);
            this.dPlane = new Plane(point, startDirection * -1);

            this.FrontLotLine = lotLinesByDistToStreet[0];
            this.RearLotLine = lotLinesByDistToStreet[3];

            var nearFarLines = new List<Line>() { lotLinesByDistToStreet[1], lotLinesByDistToStreet[2] }.OrderBy(line => this.Point.DistanceTo(line)).ToList();
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
            var siteCentroid = rectangularSite.Centroid();
            var midpoint = vantageStreet.Line.PointAt(0.5);
            var lotLines = rectangularSite.Segments().OrderBy(segment => midpoint.DistanceTo(segment)).ToList();
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
                    origin3,
                    dir3 * -1,
                    ninetyDegreeDirection,
                    lotLines,
                    centerlineOffsetDist
                );
                vantagePoints.Add(vp3);
            }

            calculateDaylightBoundaries(vantageStreet, vantagePoints, model);

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
                vp.DaylightBoundaries[0] = VantagePoint.GetPlanAngle(vp.CenterlineOffsetDist, -VantageDistance);
                vp.DaylightBoundariesPoints[0] = farPoint;
            }

            if (orderedStreetVantagePts.Count == 2)
            {
                foreach (var vp in orderedStreetVantagePts)
                {
                    var nearPointOnNearLot = new List<Vector3>() { vp.NearLotLine.Start, vp.NearLotLine.End }.OrderBy(pt => pt.DistanceTo(vp.FrontLotLine)).ToList()[0];
                    // Move intersection point of the near lot line and front lot line
                    // towards the rear, to the lesser of 100' or the centerline of the block from the front lot line
                    var pointForBounds = nearPointOnNearLot + vp.FrontDirection * Math.Min(Units.FeetToMeters(100), Units.FeetToMeters(vantageStreet.BlockDepthInFeet / 2));
                    var analysisPoint = vp.GetAnalysisPoint(pointForBounds);
                    vp.DaylightBoundaries[1] = analysisPoint.PlanAndSection.X;
                    vp.DaylightBoundariesPoints[1] = pointForBounds;
                }
            }

            if (orderedStreetVantagePts.Count == 3)
            {
                foreach (var vp in orderedStreetVantagePts.SkipLast(1))
                {
                    vp.DaylightBoundaries[1] = 0.0;
                }
                var vp1 = orderedStreetVantagePts[0];
                var vp2 = orderedStreetVantagePts[1];
                var vp3 = orderedStreetVantagePts[2];
                var vp1OnFrontLotLine = vp1.Point + (vp1.FrontDirection * vp1.CenterlineOffsetDist);
                var vp1analysis = vp3.GetAnalysisPoint(vp1OnFrontLotLine);
                var vp2OnFrontLotLine = vp2.Point + (vp2.FrontDirection * vp2.CenterlineOffsetDist);
                var vp2analysis = vp3.GetAnalysisPoint(vp2OnFrontLotLine);
                vp3.DaylightBoundaries[0] = vp1analysis.PlanAndSection.X;
                vp3.DaylightBoundaries[1] = vp2analysis.PlanAndSection.X;
                vp3.DaylightBoundariesPoints[0] = vp1OnFrontLotLine;
                vp3.DaylightBoundariesPoints[1] = vp2OnFrontLotLine;

                if (vp3.DaylightBoundaries[0] > vp3.DaylightBoundaries[1])
                {
                    vp3.DaylightBoundaries.Reverse();
                    vp3.DaylightBoundariesPoints.Reverse();
                }

            }
        }
    }
}