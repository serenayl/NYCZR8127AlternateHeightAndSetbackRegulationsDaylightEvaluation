using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System;
using System.Linq;

namespace NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluation
{
    public static class NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluation
    {

        /// <summary>
        /// C# in-progress version of this
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluationOutputs instance containing computed results and the model with any new elements.</returns>
        public static NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluationOutputs Execute(Dictionary<string, Model> inputModels, NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluationInputs input)
        {
            var model = new Model();

            var output = new NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluationOutputs();

            Site siteInput = null;
            List<Envelope> envelopes = null;

            inputModels.TryGetValue("EnvelopeAndSite", out var envelopeAndSite);

            if (envelopeAndSite == null)
            {
                inputModels.TryGetValue("Site", out var siteModel);
                inputModels.TryGetValue("Envelope", out var envelopeModel);

                siteInput = GetSite(inputModels, siteModel);
                envelopes = GetEnvelopes(inputModels, envelopeModel);
            }
            else
            {
                siteInput = GetSite(inputModels, envelopeAndSite);
                envelopes = GetEnvelopes(inputModels, envelopeAndSite);
            }

            if (siteInput == null)
            {
                throw new ArgumentException("There were no sites found. Please make sure you either meet the dependency of 'Site' or the dependency of 'EnvelopeAndSite."); throw new ArgumentException("BOOO SITE IS NOT FOUND");
            }
            if (envelopes == null || envelopes.Count < 1)
            {
                throw new ArgumentException("There were no envelopes found. Please make sure you either meet the dependency of 'Envelope' or the dependency of 'EnvelopeAndSite.");
            }

            SolidAnalysisObject.model = model;
            SolidAnalysisObject.skipSubdivide = input.SkipSubdivide;

            var analysisObjects = SolidAnalysisObject.MakeFromEnvelopes(envelopes);
            var site = siteInput.Perimeter.Bounds();
            var siteRect = Polygon.Rectangle(new Vector3(site.Min.X, site.Min.Y), new Vector3(site.Max.X, site.Max.Y));
            var siteCentroid = siteRect.Centroid();

            model.AddElement(new ModelCurve(siteRect, name: "Site Bounds Used"));

            foreach (var vantageStreet in input.VantageStreets)
            {
                var midpoint = vantageStreet.Line.PointAt(0.5);
                var lotLines = siteRect.Segments().OrderBy(segment => midpoint.DistanceTo(segment)).ToList();
                var frontLotLine = lotLines[0];
                var centerlineOffsetDist = Settings.CenterlineDistances[vantageStreet.Width] / 2;
                var directionToStreet = new Vector3(frontLotLine.PointAt(0.5) - siteCentroid).Unitized() * centerlineOffsetDist;
                var centerline = new Line(frontLotLine.Start + directionToStreet, frontLotLine.End + directionToStreet);
                model.AddElement(new ModelCurve(centerline));

                var vantagePoints = VantagePoint.GetVantagePoints(centerline, frontLotLine, model);

                int vpIndex = 0;

                foreach (var vantagePoint in vantagePoints)
                {
                    var transform = new Transform(new Vector3(90.0 + vpIndex * 200.0, Units.FeetToMeters(100) + 20.0));
                    Projection.DrawDiagram(centerlineOffsetDist, model, transform, input.DebugVisualization);

                    var polygons = new List<Polygon>();

                    foreach (var analysisObject in analysisObjects)
                    {
                        var coordinates = new Dictionary<long, Vector3>();

                        foreach (var point in analysisObject.points)
                        {
                            var planAndSectionAngle = vantagePoint.GetPlanAndSectionAngle(point.Value);
                            var coordinate = input.DebugVisualization ? new Vector3(planAndSectionAngle.plan, planAndSectionAngle.section) : Projection.MapCoordinate(planAndSectionAngle.plan, planAndSectionAngle.section);
                            coordinates.Add(point.Key, coordinate);
                        }

                        var edges = new Dictionary<long, List<Vector3>>();
                        var edgeMaterial = Settings.Materials[Settings.MaterialPalette.BuildingEdges];

                        foreach (var lineMapping in analysisObject.lines)
                        {
                            var points = new List<Vector3>();

                            foreach (var coordinateId in lineMapping.Value)
                            {
                                coordinates.TryGetValue(coordinateId, out var point);
                                points.Add(point);
                            }

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

                            edges.Add(lineMapping.Key, points);
                        }

                        foreach (var surface in analysisObject.surfaces)
                        {
                            var vertices = new List<Vector3>();

                            foreach (var edge in surface)
                            {
                                var isLeftToRight = edge.Vertex.Id == edge.Edge.Left.Vertex.Id;

                                if (edges.TryGetValue(edge.Edge.Id, out var points))
                                {
                                    vertices.AddRange(isLeftToRight ? points.SkipLast(1) : points.AsEnumerable().Reverse().SkipLast(1));
                                }
                            }

                            try
                            {
                                var polygon = new Polygon(vertices);
                                if (polygon.Area() > 0)
                                {
                                    polygons.Add(polygon);
                                }
                            }
                            catch (ArgumentException e)
                            {
                                Console.WriteLine($"Failure to create polygon: {e.Message}");
                            }
                        }
                    }

                    var unioned = Polygon.UnionAll(polygons);
                    foreach (var unionedSilhouette in unioned)
                    {
                        var panel = new Panel(unionedSilhouette, Settings.Materials[Settings.MaterialPalette.Silhouette], transform);
                        model.AddElement(panel);
                    }

                    vpIndex += 1;


                }
            }

            output.Model = model;

            return output;
        }

        // Grab the biggest site's bounding box from the model
        public static Site GetSite(Dictionary<string, Model> inputModels, Model model)
        {
            if (model == null)
            {
                return null;
            }
            var sites = new List<Site>();
            sites.AddRange(model.AllElementsOfType<Site>());
            sites = sites.OrderByDescending(e => e.Perimeter.Area()).ToList();
            var site = sites[0];
            return site;
        }

        // Grab envelopes from the model
        public static List<Envelope> GetEnvelopes(Dictionary<string, Model> inputModels, Model model)
        {
            if (model == null)
            {
                return null;
            }
            var envelopes = new List<Envelope>();
            envelopes.AddRange(model.AllElementsOfType<Envelope>());
            return envelopes;
        }


    }
}