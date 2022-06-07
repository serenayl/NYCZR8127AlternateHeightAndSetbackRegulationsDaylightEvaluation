using System;
using System.Collections.Generic;
using Elements;
using Elements.Geometry;
using System.Linq;

namespace NYCZR8127DaylightEvaluation
{
    public static class VantageElementUtils
    {
        /// <summary>
        /// Create an output vantage street from an input vantage street.
        /// </summary>
        public static NYCDaylightEvaluationVantageStreet CreateVantageStreet(Polygon rectangularSite, VantageStreets vantageStreet, out List<VantagePoint> vantagePoints, VantageStreetsOverride ovd = null, Model model = null)
        {
            if (vantageStreet.Line == null)
            {
                throw new Exception("Each vantage street must have a line designating its rough location. Please draw a line outside of your lot that represents the centerline of your vantage street. It does not need to be straight or exactly parallel to the lot line, but it must exist.");
            }
            var siteCentroid = rectangularSite.Centroid();
            var midpoint = vantageStreet.Line.PointAt(0.5);
            var lotLines = new List<Line>(rectangularSite.Segments()).OrderBy(segment => midpoint.DistanceTo(segment.PointAt(0.5))).ToList();
            var frontLotLine = ovd?.Value.FrontLotLine ?? lotLines[0];
            var centerlineOffsetDist = Settings.CenterlineDistances[vantageStreet.Width] / 2;
            var directionToStreet = new Vector3(frontLotLine.PointAt(0.5) - siteCentroid).Unitized() * centerlineOffsetDist;
            var centerline = new Line(frontLotLine.Start + directionToStreet, frontLotLine.End + directionToStreet);
            var outputVantageStreet = new NYCDaylightEvaluationVantageStreet(0, centerlineOffsetDist, frontLotLine, centerline, vantageStreet.StreetWallContinuity, lotLines, Units.FeetToMeters(vantageStreet.BlockDepthInFeet), name: vantageStreet.Name);
            if (ovd != null) {
                outputVantageStreet.AddOverrideIdentity(ovd);
            }
            vantagePoints = VantagePoint.GetVantagePoints(outputVantageStreet, model);

            return outputVantageStreet;
        }

    }

}