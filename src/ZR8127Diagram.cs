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

        /// <summary>Projection without a factor applied at the 90 degree section line</summary>
        private static double rawProjectionAt90Deg = MapCoordinate(0.0, 90.0).Y;

        /// <summary>Calculated projection factor for section lines</summary>
        private static double projectionFactor = Settings.ChartHeight / rawProjectionAt90Deg;

        private static Material majorLinesMaterial = Settings.Materials[Settings.MaterialPalette.GridlinesMajor];
        private static Material minorLinesMaterial = Settings.Materials[Settings.MaterialPalette.GridlinesMinor];
        private static Material daylightBoundariesMaterial = Settings.Materials[Settings.MaterialPalette.DaylightBoundaries];
        private static Material profileCurveMaterial = Settings.Materials[Settings.MaterialPalette.ProfileCurves];
        private static Material daylightBlockageMaterial = Settings.Materials[Settings.MaterialPalette.BlockedDaylight];

        private static Grid1d sectionGrid1d = makeSectionGrid();
        #endregion PrivateStatics

        private VantagePoint vp;
        private Grid1d basePlanGrid;

        public Dictionary<(double, double), Square> Squares = new Dictionary<(double, double), Square>();
        public List<Polyline> ProfileCurves = new List<Polyline>();
        public List<Polygon> ProfilePolygons = new List<Polygon>();
        public List<Polygon> RawSilhouettes;
        public List<Polygon> DrawSilhouettes;


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

            sectionGrid.SplitAtPosition(Settings.SectionCutoffLine);

            var curSectionAngle = 10;

            while (curSectionAngle < sectionGrid[0].Domain.Max)
            {
                sectionGrid[0].SplitAtPosition(curSectionAngle);
                curSectionAngle = curSectionAngle + 10;
            }

            while (curSectionAngle < sectionDomain.Max)
            {
                sectionGrid[1].SplitAtPosition(curSectionAngle);
                curSectionAngle = curSectionAngle + 2;
            }

            foreach (var aboveOrBelow in sectionGrid.Cells)
            {
                foreach (var cell in aboveOrBelow.Cells)
                {
                    cell.SplitAtParameter(0.5);
                }
            }

            return sectionGrid;
        }

        /// <summary>
        /// Make a Grid1D with cells representing major plan angles
        /// and subcells for minor plan angles
        /// </summary>
        /// <returns></returns>
        private Grid1d makePlanGrid(double centerlineOffsetDist)
        {
            // Generate list of major and minor raw plan angles
            var majorPlanAngles = new List<double>();
            var minorPlanAngles = new List<List<double>>();
            var i = 0;
            for (double dFt = -VantagePoint.VantageDistanceInFt; dFt <= VantagePoint.VantageDistanceInFt; dFt += 5)
            {
                var isMajor = i % 5 == 0;
                var d = Units.FeetToMeters(dFt);
                var planAngle = VantagePoint.GetPlanAngle(centerlineOffsetDist, d);
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
        private void drawGrid(Model model, Transform transform = null, Boolean useRawAngles = false)
        {
            // Draw horizontal lines
            foreach (var aboveBelow in Diagram.sectionGrid1d.Cells)
            {
                foreach (var cell in aboveBelow.Cells)
                {
                    this.drawSectionGridline(model, cell.Domain.Min, majorLinesMaterial, transform, useRawAngles);

                    foreach (var subcell in cell.Cells.SkipLast(1))
                    {
                        this.drawSectionGridline(model, subcell.Domain.Max, minorLinesMaterial, transform, useRawAngles);
                    }
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

            foreach (var cell in this.basePlanGrid.Cells)
            {
                this.drawPlanGridline(model, cell.Domain.Min, majorLinesMaterial, yTop, transform);

                if (cell.Cells != null)
                {
                    foreach (var subcell in cell.Cells.SkipLast(1))
                    {
                        this.drawPlanGridline(model, subcell.Domain.Max, minorLinesMaterial, yTop, transform);
                    }
                }
            }
            // Last major plan gridline
            this.drawPlanGridline(model, this.basePlanGrid.Cells.Last().Domain.Max, majorLinesMaterial, yTop, transform);
            // Right edge of chart
            this.drawPlanGridline(model, 90.0, majorLinesMaterial, yTop, transform);

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

            // foreach (var rawProfilePolygon in this.ProfilePolygons)
            // {
            //     var coordinates = rawProfilePolygon.Vertices.Select(pt => this.vp.GetAnalysisPoint(pt.X, pt.Y, useRawAngles).DrawCoordinate).ToArray();
            //     var polygon = new Polygon(coordinates);
            //     var profilePolygon = new ModelCurve(polygon, profileCurveMaterial, transform);
            //     model.AddElement(profilePolygon);
            // }
        }

        private void makeProfileCurves()
        {
            this.ProfileCurves = new List<Polyline>();
            this.ProfilePolygons = new List<Polygon>();

            // Profile curve begins at 72 degree section and intersection of the far lot line and front lot line
            // For every 5' away from the front lot line, the section angle increases by 1 degree
            // This is a list of the relevant plan and section angles, one for negative and one for positive side of graph
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

            for (var i = 0; i < planSectionSets.Count; i++)
            {
                var planSectionSet = planSectionSets[i];
                var profileDomain = new Domain1d(planSectionSet[0].X, planSectionSet.Last().X);

                if (domainsOverlap(profileDomain, this.vp.DaylightBoundaries))
                {
                    // overlap
                    var profileCurve = new Polyline(planSectionSet);
                    this.ProfileCurves.Add(profileCurve);

                    var polylinePoints = new List<Vector3>(planSectionSet);
                    if (i == 0)
                    {
                        polylinePoints.Add(new Vector3(this.vp.DaylightBoundaries.Min, 90));
                    }
                    else
                    {
                        polylinePoints.Add(new Vector3(this.vp.DaylightBoundaries.Max, 90));

                    }
                    var profilePolygon = new Polygon(polylinePoints);
                    this.ProfilePolygons.Add(profilePolygon);

                }
            }
        }

        private void makeSquares()
        {
            // this.PlanSquares = new List<PlanSquare>();
            var planId = 1.0;
            var planIdx = 0;

            foreach (var baseCell in this.basePlanGrid.Cells)
            {
                if (domainsOverlap(baseCell.Domain, this.vp.DaylightBoundaries))
                {
                    var isFullyInDomain = baseCell.Domain.Max <= this.vp.DaylightBoundaries.Max && baseCell.Domain.Min >= this.vp.DaylightBoundaries.Min;

                    var planGrid1d = new Grid1d(
                        new Domain1d(
                            Math.Max(baseCell.Domain.Min, this.vp.DaylightBoundaries.Min),
                            Math.Min(baseCell.Domain.Max, this.vp.DaylightBoundaries.Max)
                        )
                    );

                    var planGrid = new PlanGrid(planId, planGrid1d);

                    // Set full and partial subcells, splitting and cullingat daylight boundaries
                    var subPlanCellId = 1.0;

                    foreach (var baseSubCell in baseCell.Cells)
                    {
                        if (domainsOverlap(baseSubCell.Domain, this.vp.DaylightBoundaries))
                        {
                            var subIsFullyInDomain = baseCell.Domain.Max <= this.vp.DaylightBoundaries.Max && baseCell.Domain.Min >= this.vp.DaylightBoundaries.Min;

                            var subPlanGrid1d = new Grid1d(
                                new Domain1d(
                                    Math.Max(baseSubCell.Domain.Min, this.vp.DaylightBoundaries.Min),
                                    Math.Min(baseSubCell.Domain.Max, this.vp.DaylightBoundaries.Max)
                                )
                            );

                            var subPlanMultiplier =
                                subIsFullyInDomain ?
                                1.0 / 5 :
                                ((subPlanGrid1d.Domain.Max - subPlanGrid1d.Domain.Min) / (baseSubCell.Domain.Max - baseSubCell.Domain.Min)) / 5;

                            var subPlanId = planId + subPlanCellId / 10;

                            var subPlanGrid = new PlanGrid(subPlanId, subPlanGrid1d, subPlanMultiplier);

                            planGrid.Multiplier += subPlanMultiplier;

                            planGrid.Children.Add(subPlanGrid);
                        }

                        subPlanCellId += 1;
                    }

                    foreach (var sectGridGroup in Diagram.sectionGrid1d.Cells)
                    {
                        foreach (var sectGrid1d in sectGridGroup.Cells)
                        {
                            var sectionGrid = new SectionGrid(sectGrid1d.Domain.Min, sectGrid1d);
                            var square = new Square(planGrid, sectionGrid, this.vp);
                            this.Squares[square.Id] = square;

                            foreach (var subPlanGrid in planGrid.Children)
                            {
                                foreach (var subSectGrid1d in sectGrid1d.Cells)
                                {
                                    var subSectionGrid = new SectionGrid(subSectGrid1d.Domain.Min, subSectGrid1d);
                                    var subSquare = new Square(subPlanGrid, subSectionGrid, this.vp);
                                    square.SubSquares.Add(subSquare);
                                }
                            }
                        }
                    }
                }

                planIdx++;

                if (planIdx < 10)
                {
                    planId++;
                }
                else if (planIdx > 10)
                {
                    planId--;
                }
            }
        }

        private void drawAndCalculateSilhouettes(Model model, List<SolidAnalysisObject> analysisObjects, Transform transform = null, Boolean useRawAngles = false)
        {
            var rawPolygons = new List<Polygon>();
            var drawPolygons = new List<Polygon>();

            foreach (var analysisObject in analysisObjects)
            {
                var analysisPoints = new Dictionary<long, AnalysisPoint>();

                foreach (var point in analysisObject.points)
                {
                    var analysisPoint = vp.GetAnalysisPoint(point.Value, useRawAngles);
                    analysisPoints.Add(point.Key, analysisPoint);
                }

                var edges = new Dictionary<long, List<AnalysisPoint>>();
                var edgeMaterial = Settings.Materials[Settings.MaterialPalette.BuildingEdges];

                foreach (var lineMapping in analysisObject.lines)
                {
                    var edgePoints = new List<AnalysisPoint>();

                    foreach (var coordinateId in lineMapping.Value)
                    {
                        analysisPoints.TryGetValue(coordinateId, out var analysisPoint);
                        edgePoints.Add(analysisPoint);
                    }

                    edges.Add(lineMapping.Key, edgePoints);
                }

                foreach (var surface in analysisObject.surfaces)
                {
                    var srfAPs = new List<AnalysisPoint>();

                    foreach (var edge in surface)
                    {
                        var isLeftToRight = edge.Vertex.Id == edge.Edge.Left.Vertex.Id;

                        if (edges.TryGetValue(edge.Edge.Id, out var points))
                        {
                            var edgePoints = isLeftToRight ? points.SkipLast(1) : points.AsEnumerable().Reverse().SkipLast(1);
                            var coordinates = edgePoints.Select(analysisPoint => analysisPoint).ToArray();
                            srfAPs.AddRange(coordinates);
                        }
                    }

                    try
                    {
                        var rawPolygon = new Polygon(srfAPs.Select(ap => ap.PlanAndSection).ToArray());
                        if (rawPolygon.Area() > 0)
                        {
                            rawPolygons.Add(rawPolygon);

                            if (useRawAngles)
                            {
                                drawPolygons.Add(rawPolygon);
                            }
                            else
                            {
                                try
                                {
                                    var drawPolygon = new Polygon(srfAPs.Select(ap => ap.DrawCoordinate).ToArray());
                                    if (drawPolygon.Area() > 0)
                                    {
                                        drawPolygons.Add(drawPolygon);
                                    }
                                }
                                catch (ArgumentException e)
                                {
                                    Console.WriteLine($"Failure to create projected polygon: {e.Message}");
                                }
                            }
                        }
                    }
                    catch (ArgumentException e)
                    {
                        Console.WriteLine($"Failure to create raw polygon: {e.Message}");
                    }
                }

                foreach (var edge in edges.Values)
                {
                    var points = edge.Select(ap => ap.DrawCoordinate).ToArray();
                    try
                    {
                        var polyline = new Polyline(points);
                        var modelCurve = new ModelCurve(polyline, edgeMaterial, transform);
                        model.AddElement(modelCurve);
                    }
                    catch (ArgumentException e)
                    {
                        Console.WriteLine($"Failure to draw curve: {e.Message}");
                        foreach (var point in points)
                        {
                            Console.WriteLine($"-- {point.X}, {point.Y}, {point.Z}");
                        }
                    }

                }
            }

            // Raw angle polygon(s), from which we will run our calculations
            this.RawSilhouettes = new List<Polygon>(Polygon.UnionAll(rawPolygons));

            if (useRawAngles)
            {
                this.DrawSilhouettes = this.RawSilhouettes;
            }
            else
            {
                this.DrawSilhouettes = new List<Polygon>(Polygon.UnionAll(drawPolygons));
            }

            foreach (var silhouette in this.DrawSilhouettes)
            {
                var panel = new Panel(silhouette, Settings.Materials[Settings.MaterialPalette.Silhouette], transform);
                model.AddElement(panel);
            }
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
            this.basePlanGrid = this.makePlanGrid(vp.CenterlineOffsetDist);
        }

        /// <summary>
        /// Calculates profile curves. Required to be called manually
        /// after a vantage point's daylight boundaries have been calculated.
        /// Makes both sides of profile curves, but only adds the ones that intersect
        /// with the daylight boundaries of the vantage point
        /// </summary>
        public void CalculateProfileCurvesAndBoundingSquares()
        {
            this.makeProfileCurves();
            this.makeSquares();
        }

        /// <summary>
        /// Draw the diagram
        /// </summary>
        public void Draw(Model model, List<SolidAnalysisObject> analysisObjects, Transform transform = null, Boolean useRawAngles = false)
        {
            this.drawGrid(model, transform, useRawAngles);
            this.drawAndCalculateSilhouettes(model, analysisObjects, transform, useRawAngles);

            var daylightBlockage = this.calculateDaylightBlockage(model, transform, useRawAngles);
            var unblockedDaylightCredit = this.calculateUnblockedDaylight(model, transform, useRawAngles);

            Console.WriteLine("--- ANALYSIS ---");
            Console.WriteLine($"- Daylight Blockage: {daylightBlockage}");
            Console.WriteLine($"- Unblocked Daylight Credit: {unblockedDaylightCredit}");
        }

        private double calculateDaylightBlockage(Model model = null, Transform transform = null, Boolean useRawAngles = false)
        {
            var daylightBlockage = 0.0;
            foreach (var rawSilhouette in this.RawSilhouettes)
            {
                foreach (var square in this.Squares.Values)
                {
                    if (square.SectionGrid.Grid.Domain.Min < Settings.SectionCutoffLine)
                    {
                        // Not applicable to daylight blockage
                        continue;
                    }

                    var intersects = square.Polygon.Intersects(rawSilhouette);

                    if (!intersects)
                    {
                        // We don't care about this square
                        continue;
                    }

                    foreach (var subSquare in square.SubSquares)
                    {
                        var subIntersects = subSquare.Polygon.Intersects(rawSilhouette);

                        if (!subIntersects)
                        {
                            // Subsquare cdoesn't count
                            continue;
                        }

                        daylightBlockage += subSquare.PotentialScore;

                        if (model != null)
                        {
                            var coordinates = subSquare.Polygon.Vertices.Select(pt => this.vp.GetAnalysisPoint(pt.X, pt.Y, useRawAngles).DrawCoordinate).ToArray();
                            var polygon = new Polygon(coordinates);
                            var panel = new Panel(polygon, material: daylightBlockageMaterial, transform: transform);
                            model.AddElement(panel);
                        }
                    }
                }
            }

            return daylightBlockage;
        }

        private double calculateUnblockedDaylight(Model model = null, Transform transform = null, Boolean useRawAngles = false)
        {
            if (this.vp.VantageStreet.StreetWallContinuity)
            {
                return 0.0;
            }
            return 0.0;
        }

        private static Boolean domainsOverlap(Domain1d domain1, Domain1d domain2)
        {
            return domain1.Min < domain2.Max && domain1.Max > domain2.Min;
        }
    }
}