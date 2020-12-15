using System.Collections.Generic;
using Elements;
using Elements.Geometry;
using System;

namespace NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluation
{
    public class Projection
    {
        private static double intersectionAt90Deg = 140.0;
        private static double rawProjectionAt90Deg = mapCoordinate(0.0, 90.0).Y;

        private static double projectionFactor = intersectionAt90Deg / rawProjectionAt90Deg;
        public static void DrawDiagram(double centerlineDistFromNearLot, Model model, Boolean useRawAngles = false)
        {
            var sectionAngles = new List<double>();
            var curSectionAngle = 0.0;
            var transform = new Transform(new Vector3(90.0, Units.FeetToMeters(100) + 20.0));

            while (curSectionAngle < 70)
            {
                curSectionAngle = curSectionAngle + 5.0;
                sectionAngles.Add(curSectionAngle);
            }

            while (curSectionAngle < 89)
            {
                curSectionAngle = curSectionAngle + 1.0;
                sectionAngles.Add(curSectionAngle);
            }

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
                        coordinates.Add(mapCoordinate(planAngle, sectionAngle));
                    }
                }

                var polyline = new Polyline(coordinates);
                var modelCurve = new ModelCurve(polyline, new Material("red", Colors.Red), transform);
                model.AddElement(modelCurve);
            }

            var yTop = useRawAngles ? 90.0 : mapCoordinate(0.0, 90.0).Y;

            for (double s = Units.FeetToMeters(-250.0); s <= Units.FeetToMeters(250.0); s += Units.FeetToMeters(5.0))
            {
                var planAngle = VantagePoint.GetPlanAngle(s, centerlineDistFromNearLot);
                var line = new Line(
                    new Vector3(planAngle, 0.0),
                    new Vector3(planAngle, yTop)
                );
                var modelCurve = new ModelCurve(line, new Material("red", Colors.Red), transform);
                model.AddElement(modelCurve);
            }
        }

        /// <summary>
        /// Map a coordinate from raw plan and section angles to projected visualization
        /// </summary>
        /// <param name="planAngle"></param>
        /// <param name="sectionAngle"></param>
        /// <returns></returns>
        public static Vector3 mapCoordinate(double planAngle, double sectionAngle)
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