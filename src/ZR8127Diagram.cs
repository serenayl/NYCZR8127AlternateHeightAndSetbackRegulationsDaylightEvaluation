using System.Collections.Generic;
using Elements;
using Elements.Geometry;
using Elements.Spatial;
using System;
using System.Linq;

namespace NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluation
{
    public class Diagram
    {
        private static double intersectionAt90Deg = 140.0;
        private static double rawProjectionAt90Deg = MapCoordinate(0.0, 90.0).Y;
        private static double projectionFactor = intersectionAt90Deg / rawProjectionAt90Deg;

        private static Material majorLinesMaterial = Settings.Materials[Settings.MaterialPalette.GridlinesMajor];
        private static Material minorLinesMaterial = Settings.Materials[Settings.MaterialPalette.GridlinesMinor];
        private static Material daylightBoundariesMaterial = Settings.Materials[Settings.MaterialPalette.DaylightBoundaries];

        public Grid1d SectionGrid;
        public Grid1d PlanGrid;

        private VantagePoint vp;

        public Diagram(VantagePoint vp, Model model = null)
        {
            this.vp = vp;

            // ----------------------------
            // SECTION GRID
            // ----------------------------
            // This is the same for every diagram no matter the vantage point,
            // but not sure how to get all this code into static content?

            // Note: We generate our grid with a domain instead of with a line
            // because in the case of plan angles which don't start neatly at 0,
            // the values of the interior domains were transformed to start at 0.
            var sectionDomain = new Domain1d(0, 90);
            var sectionGrid = new Grid1d(sectionDomain);

            var sectionAngles = new List<double>();
            var curSectionAngle = sectionDomain.Min;

            while (curSectionAngle < 70)
            {
                sectionAngles.Add(curSectionAngle);
                curSectionAngle = curSectionAngle + 10;
            }

            while (curSectionAngle < sectionDomain.Max)
            {
                sectionAngles.Add(curSectionAngle);
                curSectionAngle = curSectionAngle + 2;
            }

            sectionGrid.SplitAtPositions(sectionAngles);

            foreach (var cell in sectionGrid.Cells)
            {
                cell.SplitAtParameter(0.5);
            }

            this.SectionGrid = sectionGrid;

            // ----------------------------
            // PLAN GRID
            // ----------------------------

            // Generate list of major and minor raw plan angles
            var majorPlanAngles = new List<double>();
            var minorPlanAngles = new List<List<double>>();
            var i = 0;
            for (double dFt = -VantagePoint.VantageDistanceInFt; dFt <= VantagePoint.VantageDistanceInFt; dFt += 5)
            {
                var isMajor = i % 5 == 0;
                var d = Units.FeetToMeters(dFt);
                var planAngle = VantagePoint.GetPlanAngle(vp.CenterlineOffsetDist, d);
                if (isMajor)
                {
                    majorPlanAngles.Add(planAngle);
                    minorPlanAngles.Add(new List<double>());
                }
                else
                {
                    minorPlanAngles.Last().Add(planAngle);
                }
                i += 1;
            }

            // Create plan grid from above data
            var planDomain = new Domain1d(majorPlanAngles[0], majorPlanAngles.Last());
            var planGrid = new Grid1d(planDomain);
            planGrid.SplitAtPositions(majorPlanAngles);
            i = 0;
            foreach (var cell in planGrid.Cells)
            {
                var minorAnglesForCell = minorPlanAngles[i];
                cell.SplitAtPositions(minorAnglesForCell);
                i += 1;
            }

            this.PlanGrid = planGrid;
        }

        /// <summary>
        /// Draw a section grid line (the curvy horizontal ones, or straight if we're using raw angles)
        /// </summary>
        private Polyline drawSectionGridline(Model model, double sectionAngle, Material material, Transform transform = null, Boolean useRawAngles = false)
        {
            var coordinates = new List<Vector3>();

            for (double planAngle = -90.0; planAngle <= 90.0; planAngle += 1.0)
            {
                coordinates.Add(vp.GetAnalysisPoint(planAngle, sectionAngle, useRawAngles).DrawCoordinate);
            }

            var polyline = new Polyline(coordinates);
            var modelCurve = new ModelCurve(polyline, material, transform);
            model.AddElement(modelCurve);

            return polyline;
        }

        private Line drawPlanGridline(Model model, double planAngle, Material material, double yTop, Transform transform = null)
        {
            var line = new Line(
                new Vector3(planAngle, 0.0),
                new Vector3(planAngle, yTop)
            );
            var modelCurve = new ModelCurve(line, material, transform);
            model.AddElement(modelCurve);
            return line;
        }

        /// <summary>
        /// Draw the diagram
        /// </summary>
        public void Draw(Model model, Transform transform = null, Boolean useRawAngles = false)
        {
            // Draw horizontal lines
            foreach (var cell in this.SectionGrid.Cells)
            {
                this.drawSectionGridline(model, cell.Domain.Min, majorLinesMaterial, transform, useRawAngles);

                foreach (var subcell in cell.Cells.SkipLast(1))
                {
                    this.drawSectionGridline(model, subcell.Domain.Max, minorLinesMaterial, transform, useRawAngles);
                }
            }

            // Calculate top (height) of chart
            var yTop = useRawAngles ? 90.0 : MapCoordinate(0.0, 90.0).Y;

            // Draw 90degree top of chart
            var line = new Line(
                new Vector3(-90, yTop),
                new Vector3(90, yTop)
            );
            var modelCurve = new ModelCurve(line, majorLinesMaterial, transform);
            model.AddElement(modelCurve);

            this.drawPlanGridline(model, -90.0, majorLinesMaterial, yTop, transform);

            foreach (var cell in this.PlanGrid.Cells)
            {
                this.drawPlanGridline(model, cell.Domain.Min, majorLinesMaterial, yTop, transform);

                foreach (var subcell in cell.Cells.SkipLast(1))
                {
                    this.drawPlanGridline(model, subcell.Domain.Max, minorLinesMaterial, yTop, transform);
                }
            }
            this.drawPlanGridline(model, 90.0, majorLinesMaterial, yTop, transform);

            foreach (double planAngle in vp.DaylightBoundaries)
            {
                var boundary = new Line(
                    new Vector3(planAngle, 0.0),
                    new Vector3(planAngle, yTop)
                );
                model.AddElement(new ModelCurve(boundary, daylightBoundariesMaterial, transform));
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