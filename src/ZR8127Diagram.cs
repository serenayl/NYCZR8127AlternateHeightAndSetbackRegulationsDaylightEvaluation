using System.Collections.Generic;
using Elements;
using Elements.Geometry;
using Elements.Spatial;
using System;
using System.Linq;

namespace NYCZR8127DaylightEvaluation
{
    public class Diagram
    {
        #region PrivateStatics

        /// <summary>Projection without a factor applied at the 90 degree section line</summary>
        private static double rawProjectionAt90Deg = MapCoordinate(0.0, 90.0).Y;

        /// <summary>Calculated projection factor for section lines</summary>
        private static double projectionFactor = Settings.ChartHeight / rawProjectionAt90Deg;

        private static (string, SVG.Style) buildingEdges = ("building-edges", Settings.SvgStyles[Settings.MaterialPalette.BuildingEdges]);
        private static (string, SVG.Style) buildingSilhouette = ("building-silhouette", Settings.SvgStyles[Settings.MaterialPalette.Silhouette]);
        private static (string, SVG.Style) daylightBlockageStyle = ("daylight-blockage", Settings.SvgStyles[Settings.MaterialPalette.BlockedDaylight]);
        private static (string, SVG.Style) daylightBoundariesStyle = ("daylight-boundaries", Settings.SvgStyles[Settings.MaterialPalette.DaylightBoundaries]);
        private static (string, SVG.Style) invisibleStyle = ("invisible", new SVG.Style(fill: new Color(0, 0, 0, 0)));
        private static (string, SVG.Style) majorLinesStyle = ("major-lines", Settings.SvgStyles[Settings.MaterialPalette.GridlinesMajor]);
        private static (string, SVG.Style) minorLinesStyle = ("minor-linse", Settings.SvgStyles[Settings.MaterialPalette.GridlinesMinor]);
        private static (string, SVG.Style) profileCurveStyle = ("profile-curves", Settings.SvgStyles[Settings.MaterialPalette.ProfileCurves]);
        private static (string, SVG.Style) profileEncroachmentStyle = ("profile-encroachment", Settings.SvgStyles[Settings.MaterialPalette.ProfileEncroachment]);
        private static (string, SVG.Style) unblockedCreditStyle = ("unblocked-credit", Settings.SvgStyles[Settings.MaterialPalette.UnblockedCredit]);

        private static Grid1d sectionGrid1d = makeSectionGrid();
        #endregion PrivateStatics

        private VantagePoint vp;
        private Grid1d basePlanGrid;
        private Elements.SVG svg = new Elements.SVG();

        public Dictionary<(double, double), Square> Squares = new Dictionary<(double, double), Square>();
        public Dictionary<(double, double), Square> SquaresWIthProfilePenalty = new Dictionary<(double, double), Square>();
        public Dictionary<(double, double), Square> SquaresBelowCutoff = new Dictionary<(double, double), Square>();
        public Dictionary<(double, double), Square> SquaresAboveCutoff = new Dictionary<(double, double), Square>();
        public List<Polyline> ProfileCurves = new List<Polyline>();
        public List<Polygon> ProfilePolygons = new List<Polygon>();

        public double DaylightBlockage;
        public double UnblockedDaylightCredit;
        public double ProfilePenalty;
        public double AvailableDaylight;
        public double DaylightRemaining;
        public double DaylightScore;

        public static Model Model; // For debugging only

        /// <summary>
        /// A diagram from a vantage point.
        /// Figures out grids and subgrids and profile curves and how to draw your building.
        /// </summary>
        public Diagram(VantagePoint vp)
        {
            this.vp = vp;
            this.basePlanGrid = this.makePlanGrid(vp.CenterlineOffsetDist);

            this.svg.AddStyle("text", new SVG.Style(fontFamily: "Roboto,Helvetica,Arial,sans-serif", fill: Colors.Black));

            this.svg.AddStyle("text.label", new SVG.Style(
                fontSize: "3px"
            ));

            this.svg.AddStyle("text.result", new SVG.Style(
                fontSize: "6px"
            ));

            this.svg.AddStyle("text.support-result", new SVG.Style(
                fontSize: "4px",
                fill: Colors.Gray
            ));

            this.svg.AddStyle($"path.{buildingEdges.Item1}", buildingEdges.Item2);
            this.svg.AddStyle($"path.{buildingSilhouette.Item1}", buildingSilhouette.Item2);
            this.svg.AddStyle($"path.{daylightBlockageStyle.Item1}", daylightBlockageStyle.Item2);
            this.svg.AddStyle($"path.{daylightBoundariesStyle.Item1}", daylightBoundariesStyle.Item2);
            this.svg.AddStyle($"path.{invisibleStyle.Item1}", invisibleStyle.Item2);
            this.svg.AddStyle($"path.{majorLinesStyle.Item1}", majorLinesStyle.Item2);
            this.svg.AddStyle($"path.{minorLinesStyle.Item1}", minorLinesStyle.Item2);
            this.svg.AddStyle($"path.{profileCurveStyle.Item1}", profileCurveStyle.Item2);
            this.svg.AddStyle($"path.{profileEncroachmentStyle.Item1}", profileEncroachmentStyle.Item2);
            this.svg.AddStyle($"path.{unblockedCreditStyle.Item1}", unblockedCreditStyle.Item2);
        }

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
        private Polyline drawSectionGridline(double sectionAngle, string classes, Boolean useRawAngles = false)
        {
            var coordinates = new List<Vector3>();

            for (double planAngle = -90.0; planAngle <= 90.0; planAngle += 1.0)
            {
                coordinates.Add(vp.GetAnalysisPoint(planAngle, sectionAngle, useRawAngles).DrawCoordinate);
            }

            var polyline = new Polyline(coordinates);

            this.svg.AddGeometry(polyline, classes);

            return polyline;
        }

        /// <summary>
        /// Draw a plan grid line (the straight vertical ones)
        /// </summary>
        private Polyline drawPlanGridline(double planAngle, string classes, double yTop)
        {
            var line = new Polyline(new List<Vector3>() { new Vector3(planAngle, 0.0), new Vector3(planAngle, yTop) });
            this.svg.AddGeometry(line, classes);
            return line;
        }

        /// <summary>
        /// Draws base/background grid. Returns the point that marks the leftover area where we can write supporting text
        /// </summary>
        private Vector3 drawGrid(Boolean useRawAngles = false)
        {
            // Calculate top (height) of chart
            var yTop = useRawAngles ? 90.0 : MapCoordinate(0.0, 90.0).Y;

            // Draw invisible box for margins
            var bottomLeft = new Vector3(-95, -10);
            var topRight = new Vector3(95, yTop + 50);
            var marginRect = Polygon.Rectangle(bottomLeft, topRight);
            this.svg.AddGeometry(marginRect, Diagram.invisibleStyle.Item1);

            // Draw horizontal lines
            foreach (var aboveBelow in Diagram.sectionGrid1d.Cells)
            {
                foreach (var cell in aboveBelow.Cells)
                {
                    if (cell.Domain.Min > 0)
                    {
                        this.drawSectionGridline(cell.Domain.Min, Diagram.majorLinesStyle.Item1, useRawAngles);
                        this.svg.AddText(this.vp.GetAnalysisPoint(0, cell.Domain.Min, useRawAngles).DrawCoordinate, cell.Domain.Min.ToString(), "label", "middle");
                    }

                    foreach (var subcell in cell.Cells.SkipLast(1))
                    {
                        this.drawSectionGridline(subcell.Domain.Max, minorLinesStyle.Item1, useRawAngles);
                    }
                }
            }

            // Draw grid
            var gridRect = Polygon.Rectangle(new Vector3(-90, 0), new Vector3(90, yTop));
            this.svg.AddGeometry(gridRect, Diagram.majorLinesStyle.Item1);

            foreach (var cell in this.basePlanGrid.Cells)
            {
                this.drawPlanGridline(cell.Domain.Min, Diagram.majorLinesStyle.Item1, yTop);

                if (cell.Cells != null)
                {
                    foreach (var subcell in cell.Cells.SkipLast(1))
                    {
                        this.drawPlanGridline(subcell.Domain.Max, minorLinesStyle.Item1, yTop);
                    }
                }
            }
            // Last major plan gridline
            // this.drawPlanGridline(this.basePlanGrid.Cells.Last().Domain.Max, Diagram.majorLinesStyle, yTop);

            foreach (double planAngle in new List<double>() { vp.DaylightBoundaries.Min, vp.DaylightBoundaries.Max })
            {
                var boundary = new Polyline(new List<Vector3>(){
                    new Vector3(planAngle, 0.0),
                    new Vector3(planAngle, yTop)
                });
                this.svg.AddGeometry(boundary, daylightBoundariesStyle.Item1);
            }

            foreach (var rawProfileCurve in this.ProfileCurves)
            {
                var coordinates = rawProfileCurve.Vertices.Select(pt => this.vp.GetAnalysisPoint(pt.X, pt.Y, useRawAngles).DrawCoordinate).ToArray();
                var polyline = new Polyline(coordinates);
                this.svg.AddGeometry(polyline, profileCurveStyle.Item1);
            }

            // Draw ticks
            for (var i = -90; i <= 90; i++)
            {
                var isPrimary = i % 10 == 0;
                var tickLength = isPrimary ? 2 : 1;
                var topTickVertices = new List<Vector3>(){
                    new Vector3(i, yTop),
                    new Vector3(i, yTop + tickLength)
                };
                var bottomTickVertices = new List<Vector3>(){
                    new Vector3(i, 0),
                    new Vector3(i, 0 - tickLength)
                };
                var topTick = new Polyline(topTickVertices);
                var bottomTick = new Polyline(bottomTickVertices);
                this.svg.AddGeometry(topTick, Diagram.majorLinesStyle.Item1);
                this.svg.AddGeometry(bottomTick, Diagram.majorLinesStyle.Item1);
                if (isPrimary)
                {
                    var tickValue = i < 0 ? 90 + i : 90 - i;
                    this.svg.AddText(topTickVertices[1], tickValue.ToString(), "label", "middle");
                    this.svg.AddText(bottomTickVertices[1] + new Vector3(0, -3), tickValue.ToString(), "label", "middle");
                }
            }

            return new Vector3(bottomLeft.X, topRight.Y - 10);
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
                            var square = new Square(planGrid, sectionGrid);
                            this.Squares[square.Id] = square;

                            if (square.PotentialProfilePenalty != 0)
                            {
                                this.SquaresWIthProfilePenalty[square.Id] = square;
                            }

                            if (square.SectionGrid.Grid.Domain.Min >= Settings.SectionCutoffLine)
                            {
                                this.SquaresAboveCutoff[square.Id] = square;
                            }
                            else
                            {
                                this.SquaresBelowCutoff[square.Id] = square;
                            }

                            foreach (var subPlanGrid in planGrid.Children)
                            {
                                foreach (var subSectGrid1d in sectGrid1d.Cells)
                                {
                                    var subSectionGrid = new SectionGrid(subSectGrid1d.Domain.Min, subSectGrid1d);
                                    var subSquare = new Square(square, subPlanGrid, subSectionGrid);
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

        /// <summary>
        /// Returns raw silhouettes, and out param is the silhouettes to draw depending on whether we are using raw angles to draw
        /// </summary>
        private List<Polygon> calculateSilhouettes(List<SolidAnalysisObject> analysisObjects, out List<Polygon> drawSilhouettes, out List<Polyline> drawPolylines, Boolean useRawAngles = false)
        {
            var rawPolygons = new List<Polygon>();
            var drawPolygons = new List<Polygon>();

            drawPolylines = new List<Polyline>();

            foreach (var analysisObject in analysisObjects)
            {
                var analysisPoints = new Dictionary<long, AnalysisPoint>();

                foreach (var point in analysisObject.Points)
                {
                    var analysisPoint = vp.GetAnalysisPoint(point.Value, useRawAngles);
                    analysisPoints.Add(point.Key, analysisPoint);
                }

                var edges = new Dictionary<long, List<AnalysisPoint>>();

                foreach (var lineMapping in analysisObject.Lines)
                {
                    var edgePoints = new List<AnalysisPoint>();

                    foreach (var coordinateId in lineMapping.Value)
                    {
                        analysisPoints.TryGetValue(coordinateId, out var analysisPoint);
                        edgePoints.Add(analysisPoint);
                    }

                    edges.Add(lineMapping.Key, edgePoints);
                }

                // var i = 0;

                // var red = new Material("Red", new Color(1, 0, 0, 0.2));
                // var green = new Material("Green", new Color(0, 1, 0, 0.2));

                foreach (var surface in analysisObject.Surfaces)
                {
                    // i += 1;
                    var srfAPs = new List<AnalysisPoint>();

                    foreach (var analysisEdge in surface)
                    {
                        var isLeftToRight = !analysisEdge.Reversed;

                        if (edges.TryGetValue(analysisEdge.LineId, out var points))
                        {
                            var edgePoints = isLeftToRight ? points.SkipLast(1) : points.AsEnumerable().Reverse().SkipLast(1);
                            var coordinates = edgePoints.Select(analysisPoint => analysisPoint).ToArray();
                            srfAPs.AddRange(coordinates);
                        }
                    }

                    var rawPoints = srfAPs.Select(ap => ap.PlanAndSection).ToList();

                    try
                    {
                        var rawPolygon = new Polygon(rawPoints);
                        if (rawPolygon.IsClockWise())
                        {
                            rawPolygon = rawPolygon.Reversed();
                        }
                        if (Math.Abs(rawPolygon.Area()) > Vector3.EPSILON)
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
                                    if (drawPolygon.IsClockWise())
                                    {
                                        drawPolygon = drawPolygon.Reversed();
                                    }
                                    if (Math.Abs(drawPolygon.Area()) > 0)
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
                        drawPolylines.Add(polyline);
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

            if (rawPolygons.Count == 0)
            {
                drawSilhouettes = new List<Polygon>();
                return new List<Polygon>();
            }

           // Raw angle polygon(s), from which we will run our calculations
           var unionedRawPolygons = new List<Polygon>(Polygon.UnionAll(rawPolygons));

            drawSilhouettes = useRawAngles ? unionedRawPolygons : new List<Polygon>(Polygon.UnionAll(drawPolygons));

            return unionedRawPolygons;
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
            // Formula courtesy Luis Felipe Paris for finding a close match to zoning code arcs
            var angle = Math.Atan(
                Math.Tan(sectionAngle / 180.0 * Math.PI) * Math.Cos(planAngle / 180 * Math.PI)
            );
            var y = Math.Tan(0.625 * (angle + 0.13 * Math.Pow(angle, 2)));
            var factorToUse = projectionFactor > 0 ? projectionFactor : 1;
            return new Vector3(planAngle, y * factorToUse);
        }
        #endregion PublicStatics

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
        public void Draw(
            String name,
            Model model,
            List<SolidAnalysisObject> analysisObjects,
            NYCZR8127DaylightEvaluationInputs input,
            Boolean useRawAngles = false,
            List<SolidAnalysisObject> analysisObjectsForBlockage = null
        )
        {
            var resultStartPoint = this.drawGrid(useRawAngles);

            var rawSilhouettes = this.calculateSilhouettes(analysisObjects, out var drawSilhouettes, out var drawPolylines, useRawAngles);
            var blockageSilhouettes = analysisObjectsForBlockage == null ? rawSilhouettes : this.calculateSilhouettes(analysisObjectsForBlockage, out var drawSilhouettesBlockage, out var drawPolylinesBlockage, useRawAngles);

            this.DaylightBlockage = this.calculateDaylightBlockage(blockageSilhouettes, out var blockedDaylightSubsquares);
            this.UnblockedDaylightCredit = this.calculateUnblockedDaylight(rawSilhouettes, input, out var unblockedSubsquares);
            this.ProfilePenalty = this.calculateProfilePenalty(blockageSilhouettes, input, out var penaltySubsquares);
            this.AvailableDaylight = this.SquaresAboveCutoff.Values.Aggregate(0.0, (sum, square) => sum + square.PlanGrid.Multiplier);
            this.DaylightRemaining = this.DaylightBlockage + this.UnblockedDaylightCredit + this.ProfilePenalty + this.AvailableDaylight;
            this.DaylightScore = this.DaylightRemaining / this.AvailableDaylight * 100;

            this.drawSquares(blockedDaylightSubsquares, Diagram.daylightBlockageStyle.Item1, useRawAngles);
            this.drawSquares(unblockedSubsquares, Diagram.unblockedCreditStyle.Item1, useRawAngles);
            this.drawSquares(penaltySubsquares, Diagram.profileEncroachmentStyle.Item1, useRawAngles);

            foreach (var polyline in drawPolylines)
            {
                this.svg.AddGeometry(polyline, buildingEdges.Item1);
            }

            foreach (var silhouette in drawSilhouettes)
            {
                this.svg.AddGeometry(silhouette, buildingSilhouette.Item1);
            }

            Console.WriteLine("--- ANALYSIS ---");
            Console.WriteLine($"- Daylight Blockage: {this.DaylightBlockage}");
            Console.WriteLine($"- Unblocked Daylight Credit: {this.UnblockedDaylightCredit}");
            Console.WriteLine($"- Profile Penalty: {this.ProfilePenalty}");
            Console.WriteLine($"- Available daylight: {this.AvailableDaylight}");
            Console.WriteLine($"- Remaining daylight: {this.DaylightRemaining}");
            Console.WriteLine($"- Daylight score: {this.DaylightScore}");

            this.svg.AddText(resultStartPoint, $"Score: {Math.Truncate(this.DaylightScore * 100) / 100}", "result");
            this.svg.AddText(
                resultStartPoint + new Vector3(0, -8 * 1),
                $"Unblocked Daylight Credit: {Math.Truncate(this.UnblockedDaylightCredit * 100) / 100} | " +
                $"Available Daylight: {Math.Truncate(this.AvailableDaylight * 100) / 100}"
                ,
                "support-result"
            );
            this.svg.AddText(
                resultStartPoint + new Vector3(0, -8 * 2),
                $"Daylight Blockage: {Math.Truncate(this.DaylightBlockage * 100) / 100} | " +
                $"Profile Penalty: {Math.Truncate(this.ProfilePenalty * 100) / 100}"
                ,
                "support-result"
            );
            this.svg.AddText(
                resultStartPoint + new Vector3(0, -8 * 3),
                $"Remaining Daylight: {Math.Truncate(this.DaylightRemaining * 100) / 100}"
                ,
                "support-result"
            );

            var legendSize = 20;
            this.drawLegend(new Vector3(90 - legendSize, resultStartPoint.Y - legendSize), legendSize);

            var svgString = this.svg.SvgString();

            model.AddElement(new SVGGraphic(svgString, Guid.NewGuid(), name));
        }

        private void drawLegend(Vector3 startPoint, double maxSize)
        {
            var vertices = new List<Vector3>(){
                this.vp.Point,
                this.vp.FrontLotLine.Start,
                this.vp.FrontLotLine.End,
                this.vp.RearLotLine.Start,
                this.vp.RearLotLine.End,
                this.vp.NearLotLine.Start,
                this.vp.NearLotLine.End,
                this.vp.FarLotLine.Start,
                this.vp.FarLotLine.End,
                this.vp.Centerline.PointAt(0),
                this.vp.Centerline.PointAt(1),
                this.vp.DaylightBoundariesPoints[0],
                this.vp.DaylightBoundariesPoints[1]
            };
            var boundaries = new BBox3(vertices);
            var maxOriginalSize = Math.Max(boundaries.Max.X - boundaries.Min.X, boundaries.Max.Y - boundaries.Min.Y);
            var factor = maxSize / maxOriginalSize;

            foreach (var line in new List<Line> { this.vp.FrontLotLine, this.vp.RearLotLine, this.vp.NearLotLine, this.vp.FarLotLine })
            {
                this.svg.AddGeometry(new Polyline(new List<Vector3>(){
                    (line.Start - boundaries.Min) * factor + startPoint,
                    (line.End - boundaries.Min) * factor + startPoint
                }), majorLinesStyle.Item1);
            }

            this.svg.AddGeometry(new Polyline(new List<Vector3>(){
                (this.vp.Centerline.PointAt(0) - boundaries.Min) * factor + startPoint,
                (this.vp.Centerline.PointAt(1) - boundaries.Min) * factor + startPoint
            }), majorLinesStyle.Item1);

            this.svg.AddGeometry(new Polyline(new List<Vector3>(){
                (this.vp.DaylightBoundariesPoints[0] - boundaries.Min) * factor + startPoint,
                (this.vp.Point - boundaries.Min) * factor + startPoint,
                (this.vp.DaylightBoundariesPoints[1] - boundaries.Min) * factor + startPoint
            }), daylightBlockageStyle.Item1);

            this.svg.AddGeometry(new Circle((this.vp.Point - boundaries.Min) * factor + startPoint, 0.2).ToPolygon(), buildingSilhouette.Item1);

        }

        private void drawSquares(List<Square> subSqures, string classes, Boolean useRawAngles = false)
        {
            foreach (var subSquare in subSqures)
            {
                var coordinates = subSquare.Polygon.Vertices.Select(pt => this.vp.GetAnalysisPoint(pt.X, pt.Y, useRawAngles).DrawCoordinate).ToArray();
                var polygon = new Polygon(coordinates);
                this.svg.AddGeometry(polygon, classes);
            }
        }

        private double calculateDaylightBlockage(List<Polygon> rawSilhouettes, out List<Square> subSquares)
        {
            subSquares = new List<Square>();

            var daylightBlockage = 0.0;

            foreach (var rawSilhouette in rawSilhouettes)
            {
                foreach (var square in this.SquaresAboveCutoff.Values)
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
                            // Subsquare doesn't count
                            continue;
                        }

                        daylightBlockage += subSquare.PotentialScore;

                        subSquares.Add(subSquare);
                    }
                }
            }

            return daylightBlockage;
        }

        private double calculateUnblockedDaylight(List<Polygon> rawSilhouettes, NYCZR8127DaylightEvaluationInputs input, out List<Square> subSquares)
        {
            subSquares = new List<Square>();

            if (this.vp.VantageStreet.StreetWallContinuity && !input.QualifyForEastMidtownSubdistrict)
            {
                return 0.0;
            }
            var credit = 0.0;

            foreach (var rawSilhouette in rawSilhouettes)
            {
                foreach (var square in this.SquaresBelowCutoff.Values)
                {
                    if (square.PotentialScore <= 0.0)
                    {
                        // Not applicable to daylight credit
                        continue;
                    }

                    var contains = rawSilhouette.Contains(square.Polygon);

                    if (contains)
                    {
                        // No credit
                        continue;
                    }

                    foreach (var subSquare in square.SubSquares)
                    {
                        var subIntersects = subSquare.Polygon.Intersects(rawSilhouette);

                        if (subIntersects)
                        {
                            // No credit
                            continue;
                        }

                        subSquares.Add(subSquare);

                        credit += subSquare.PotentialScore;
                    }
                }
            }

            return credit;
        }

        private double calculateProfilePenalty(List<Polygon> rawSilhouettes, NYCZR8127DaylightEvaluationInputs input, out List<Square> subSquares)
        {
            subSquares = new List<Square>();

            var penalty = 0.0;

            if (input.QualifyForEastMidtownSubdistrict)
            {
                // Penalty curve does not apply
                return penalty;
            }

            foreach (var rawSilhouette in rawSilhouettes)
            {
                foreach (var profilePolygon in this.ProfilePolygons)
                {
                    var penaltyAreas = rawSilhouette.Intersection(profilePolygon);

                    if (penaltyAreas == null)
                    {
                        continue;
                    }

                    foreach (var penaltyArea in penaltyAreas)
                    {
                        foreach (var square in this.SquaresWIthProfilePenalty.Values)
                        {
                            var intersects = square.Polygon.Intersects(penaltyArea);

                            if (!intersects)
                            {
                                // square does not touch this piece of building that lies beyond profile curve
                                continue;
                            }

                            var contains = penaltyArea.Contains(square.Polygon);

                            if (contains)
                            {
                                // whole square is penalized
                                subSquares = subSquares.Concat(square.SubSquares).ToList();
                                penalty += square.PotentialProfilePenalty;
                                continue;
                            }

                            // if we did not totally contain, count up subsquares
                            foreach (var subSquare in square.SubSquares)
                            {
                                if (subSquare.Polygon.Intersects(penaltyArea))
                                {
                                    subSquares.Add(subSquare);
                                    penalty += subSquare.PotentialProfilePenalty;
                                }
                            }

                        }
                    }
                }
            }
            return penalty;
        }

        private static Boolean domainsOverlap(Domain1d domain1, Domain1d domain2)
        {
            return domain1.Min < domain2.Max && domain1.Max > domain2.Min;
        }
    }
}