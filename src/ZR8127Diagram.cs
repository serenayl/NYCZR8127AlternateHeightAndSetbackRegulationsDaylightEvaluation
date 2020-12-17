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
        #region PrivateStatics
        /// <summary>Height of the resulting projected chart</summary>
        private static double intersectionAt90Deg = 140.0;

        /// <summary>Projection without a factor applied at the 90 degree section line</summary>
        private static double rawProjectionAt90Deg = MapCoordinate(0.0, 90.0).Y;

        /// <summary>Calculated projection factor for section lines</summary>
        private static double projectionFactor = intersectionAt90Deg / rawProjectionAt90Deg;

        private static Material majorLinesMaterial = Settings.Materials[Settings.MaterialPalette.GridlinesMajor];
        private static Material minorLinesMaterial = Settings.Materials[Settings.MaterialPalette.GridlinesMinor];
        private static Material daylightBoundariesMaterial = Settings.Materials[Settings.MaterialPalette.DaylightBoundaries];
        private static Material profileCurveMaterial = Settings.Materials[Settings.MaterialPalette.ProfileCurves];
        #endregion PrivateStatics

        public Grid1d SectionGrid = makeSectionGrid();
        public Grid1d PlanGrid;

        public List<Polyline> ProfileCurves = new List<Polyline>();

        private VantagePoint vp;

        #region PrivateUtils
        /// <summary>
        /// Make a Grid1D with cells representing major section angles
        /// and subcells for minor section angles
        /// </summary>
        /// <returns></returns>
        private static Grid1d makeSectionGrid()
        {
            // Section Grid is the same for every diagram no matter the vantage point.
            // This could probably be a static method

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
            return sectionGrid;
        }
        /// <summary>
        /// Make a Grid1D with cells representing major plan angles
        /// and subcells for minor plan angles
        /// </summary>
        /// <returns></returns>
        private Grid1d makePlanGrid()
        {
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
            for (var j = 0; j < planGrid.Cells.Count; j++)
            {
                var cell = planGrid.Cells[j];
                var minorAnglesForCell = minorPlanAngles[j];
                cell.SplitAtPositions(minorAnglesForCell);
            }
            return planGrid;
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

        /// <summary>
        /// Draw a plan grid line (the straight vertical ones)
        /// </summary>
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
        /// Draws base/background grid
        /// </summary>
        private double drawGrid(Model model, Transform transform = null, Boolean useRawAngles = false)
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

            // Top of chart
            var line = new Line(
                new Vector3(-90, yTop),
                new Vector3(90, yTop)
            );
            var modelCurve = new ModelCurve(line, majorLinesMaterial, transform);
            model.AddElement(modelCurve);

            // Left edge of chart
            this.drawPlanGridline(model, -90.0, majorLinesMaterial, yTop, transform);

            foreach (var cell in this.PlanGrid.Cells)
            {
                this.drawPlanGridline(model, cell.Domain.Min, majorLinesMaterial, yTop, transform);

                foreach (var subcell in cell.Cells.SkipLast(1))
                {
                    this.drawPlanGridline(model, subcell.Domain.Max, minorLinesMaterial, yTop, transform);
                }
            }
            // Last major plan gridline
            this.drawPlanGridline(model, this.PlanGrid.Cells.Last().Domain.Max, majorLinesMaterial, yTop, transform);
            // Right edge of chart
            this.drawPlanGridline(model, 90.0, majorLinesMaterial, yTop, transform);

            return yTop;
        }

        #endregion PrivateUtils

        #region PublicStatics
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
        #endregion PublicStatics

        /// <summary>
        /// A diagram from a vantage point.
        /// Figures out grids and subgrids and profile curves and how to draw your building.
        /// </summary>
        public Diagram(VantagePoint vp)
        {
            this.vp = vp;
            this.PlanGrid = this.makePlanGrid();
        }

        /// <summary>
        /// Calculates profile curves. Required to be called manually
        /// after a vantage point's daylight boundaries have been calculated.
        /// Makes both sides of profile curves, but only adds the ones that intersect
        /// with the daylight boundaries of the vantage point
        /// </summary>
        public void calculateProfileCurves()
        {
            var profileCurves = new List<Polyline>();

            // Profile curve begins at 72 degree section and intersection of the far lot line and front lot line
            // For every 5' away from the front lot line, the section angle increases by 1 degree
            var planSectionSets = new List<List<Vector3>>() { new List<Vector3>(), new List<Vector3>() };

            for (var distFromFarLot = 0.0; distFromFarLot <= 90; distFromFarLot++)
            {
                var sectionAngle = distFromFarLot / 5 + 72;
                var s = this.vp.CenterlineOffsetDist + Units.FeetToMeters(distFromFarLot);
                var planAngle = VantagePoint.GetPlanAngle(s, VantagePoint.VantageDistance);

                planSectionSets[0].Add(new Vector3(-planAngle, sectionAngle));
                planSectionSets[1].Add(new Vector3(planAngle, sectionAngle));
            }

            planSectionSets[1].Reverse();

            foreach (var planSectionSet in planSectionSets)
            {
                var min = planSectionSet[0].X;
                var max = planSectionSet.Last().X;

                if (min <= this.vp.DaylightBoundaries.Max && max >= this.vp.DaylightBoundaries.Min)
                {
                    // overlap
                    var profileCurve = new Polyline(planSectionSet);
                    profileCurves.Add(profileCurve);
                }
            }

            this.ProfileCurves = profileCurves;
        }

        /// <summary>
        /// Draw the diagram
        /// </summary>
        public void Draw(Model model, Transform transform = null, Boolean useRawAngles = false)
        {
            var yTop = this.drawGrid(model, transform, useRawAngles);

            foreach (double planAngle in new List<double>() { vp.DaylightBoundaries.Min, vp.DaylightBoundaries.Max })
            {
                var boundary = new Line(
                    new Vector3(planAngle, 0.0),
                    new Vector3(planAngle, yTop)
                );
                model.AddElement(new ModelCurve(boundary, daylightBoundariesMaterial, transform));
            }

            foreach (var rawProfileCurve in this.ProfileCurves)
            {
                var coordinates = rawProfileCurve.Vertices.Select(pt => this.vp.GetAnalysisPoint(pt.X, pt.Y, useRawAngles).DrawCoordinate).ToArray();
                var polyline = new Polyline(coordinates);
                var profileCurve = new ModelCurve(polyline, profileCurveMaterial, transform);
                model.AddElement(profileCurve);
            }
        }
    }
}