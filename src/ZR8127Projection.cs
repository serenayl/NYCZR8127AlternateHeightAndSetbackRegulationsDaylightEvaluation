using System.Collections.Generic;
using Elements;
using Elements.Geometry;
using System;

namespace NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluation
{
    public class Projection
    {
        private static double intersectionAt90Deg = 140.0;
        private static double rawProjectionAt90Deg = MapCoordinate(0.0, 90.0).Y;

        private static double projectionFactor = intersectionAt90Deg / rawProjectionAt90Deg;

        private static Material majorLinesMaterial = new Material("Major Lines", Colors.Blue);
        private static Material minorLinesMaterial = new Material("Minor Lines", Colors.Cyan);
        public static void DrawDiagram(double centerlineDistFromNearLot, Model model, Transform transform = null, Boolean useRawAngles = false)
        {
            var sectionAngles = new List<double>();
            var curSectionAngle = 0.0;

            while (curSectionAngle <= 70)
            {
                sectionAngles.Add(curSectionAngle);
                curSectionAngle = curSectionAngle + 5.0;
            }

            while (curSectionAngle < 90)
            {
                sectionAngles.Add(curSectionAngle);
                curSectionAngle = curSectionAngle + 1.0;
            }

            var i = 0;

            foreach (var sectionAngle in sectionAngles)
            {
                var coordinates = new List<Vector3>();

                for (double planAngle = -90.0; planAngle <= 90.0; planAngle += 1.0)
                {
                    if (useRawAngles)
                    {
                        coordinates.Add(new Vector3(planAngle, sectionAngle));
                    }
                    else
                    {
                        coordinates.Add(MapCoordinate(planAngle, sectionAngle));
                    }
                }

                var material = i % 2 == 0 ? majorLinesMaterial : minorLinesMaterial;

                var polyline = new Polyline(coordinates);
                var modelCurve = new ModelCurve(polyline, material, transform);
                model.AddElement(modelCurve);

                i += 1;
            }

            var j = 0;

            var yTop = useRawAngles ? 90.0 : MapCoordinate(0.0, 90.0).Y;

            for (double dFt = -250.0; dFt <= 250.0; dFt += 5.0)
            {
                var d = Units.FeetToMeters(dFt);
                var planAngle = VantagePoint.GetPlanAngle(centerlineDistFromNearLot, d);
                var line = new Line(
                    new Vector3(planAngle, 0.0),
                    new Vector3(planAngle, yTop)
                );
                var material = j % 5 == 0 ? majorLinesMaterial : minorLinesMaterial;
                var modelCurve = new ModelCurve(line, material, transform);
                model.AddElement(modelCurve);
                j += 1;
            }
        }

        /// <summary>
        /// Map a coordinate from raw plan and section angles to projected visualization
        /// </summary>
        /// <param name="planAngle"></param>
        /// <param name="sectionAngle"></param>
        /// <returns></returns>
        public static Vector3 MapCoordinate(double planAngle, double sectionAngle)
        {
            var angle = Math.Atan(
                Math.Tan(sectionAngle / 180.0 * Math.PI) * Math.Cos(planAngle / 180 * Math.PI)
            );
            var y = Math.Tan(0.625 * (angle + 0.13 * Math.Pow(angle, 2)));
            var factorToUse = projectionFactor > 0 ? projectionFactor : 1;
            return new Vector3(planAngle, y * factorToUse);
        }
    }
}