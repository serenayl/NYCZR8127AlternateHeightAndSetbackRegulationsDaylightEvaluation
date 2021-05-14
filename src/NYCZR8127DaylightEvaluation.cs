using Elements;
using Elements.Geometry;
using System.Collections.Generic;
using System;
using System.Linq;
using System.IO;

namespace NYCZR8127DaylightEvaluation
{
    public static class NYCZR8127DaylightEvaluation
    {
        /// <summary>
        /// C# in-progress version of this
        /// </summary>
        /// <param name="model">The input model.</param>
        /// <param name="input">The arguments to the execution.</param>
        /// <returns>A NYCZR8127DaylightEvaluationOutputs instance containing computed results and the model with any new elements.</returns>
        public static NYCZR8127DaylightEvaluationOutputs Execute(Dictionary<string, Model> inputModels, NYCZR8127DaylightEvaluationInputs input)
        {
            var model = new Model();

            inputModels.TryGetValue("Site", out var siteModel);

            if (!inputModels.TryGetValue("Envelope", out var envelopeModel))
            {
                string localFile = "/Users/serenali/Downloads/model 18.json";
                if (File.Exists(localFile))
                {
                    Console.WriteLine("Using local file");
                    string text = System.IO.File.ReadAllText(localFile);
                    var envModel = Model.FromJson(text);
                    inputModels["Envelope"] = envModel;
                }
                inputModels.TryGetValue("Envelope", out envelopeModel);
            }

            var siteInput = getSite(siteModel);
            var envelopes = getElementsOfType<Envelope>(envelopeModel);
            var meshElements = getElementsOfType<MeshElement>(envelopeModel);

            if (siteInput == null)
            {
                throw new ArgumentException("There were no sites found. Please make sure you either meet the dependency of 'Site' or the dependency of 'EnvelopeAndSite."); throw new ArgumentException("BOOO SITE IS NOT FOUND");
            }
            if ((envelopes == null || envelopes.Count < 1) && (meshElements == null || meshElements.Count < 1))
            {
                throw new ArgumentException("There were no envelopes found. Please make sure you either meet the dependency of 'Envelope' or the dependency of 'EnvelopeAndSite.");
            }

            var site = siteInput.Perimeter.Bounds();
            var siteRect = Polygon.Rectangle(new Vector3(site.Min.X, site.Min.Y), new Vector3(site.Max.X, site.Max.Y));

            model.AddElement(new ModelCurve(siteRect, name: "Site Bounds Used"));

            Diagram.Model = model;
            SolidAnalysisObject.Model = model;
            SolidAnalysisObject.SkipSubdivide = input.SkipSubdivide;

            var analysisObjects = SolidAnalysisObject.MakeFromEnvelopes(envelopes);

            foreach(var analysisObject in SolidAnalysisObject.MakeFromMeshes(meshElements.Select(meshElement => meshElement.Mesh).ToList())) {
                analysisObjects.Add(analysisObject);
            }

            // Only applicable for E Midtown
            List<Envelope> envelopesForBlockage = input.QualifyForEastMidtownSubdistrict ? getEastMidtownEnvelopes(envelopes, model) : null;
            var analysisObjectsForBlockage = input.QualifyForEastMidtownSubdistrict ? SolidAnalysisObject.MakeFromEnvelopes(envelopesForBlockage) : null;

            var margin = 20;
            var vsIndex = 0;
            var verticalOffsetBase = Settings.ChartHeight + margin;

            var streetScores = new List<double>();
            var streetLengths = 0.0;
            var streetScoresTimesLengths = 0.0;

            if (input.VantageStreets.Count < 1)
            {
                throw new Exception("Please provide at least one vantage street");
            }

            foreach (var vantageStreet in input.VantageStreets)
            {
                var vantagePoints = VantagePoint.GetVantagePoints(siteRect, vantageStreet, model);

                int vpIndex = 0;

                foreach (var vp in vantagePoints)
                {
                    var transform = new Transform(new Vector3(90.0 + vpIndex * 200.0, site.Max.Y + margin + verticalOffsetBase * vsIndex));

                    var name = $"{vantageStreet.Name}: VP {vpIndex + 1}";

                    vp.Diagram.Draw(name, model, analysisObjects, input, input.DebugVisualization, analysisObjectsForBlockage: analysisObjectsForBlockage);

                    var outputVp = new DaylightEvaluationVantagePoint(vp.Point, vp.Diagram.DaylightBlockage, vp.Diagram.UnblockedDaylightCredit, vp.Diagram.ProfilePenalty, vp.Diagram.AvailableDaylight, vp.Diagram.DaylightRemaining, vp.Diagram.DaylightScore, Guid.NewGuid(), name);
                    model.AddElement(outputVp);

                    vpIndex += 1;
                }

                var sumScores = vantagePoints.Aggregate(0.0, (sum, vp) => sum + vp.Diagram.DaylightScore);
                var vantageStreetScore = sumScores / vantagePoints.Count;
                var vantageStreetLength = vantagePoints[0].FrontLotLine.Length();

                streetScores.Add(vantageStreetScore);
                streetLengths += vantageStreetLength;
                streetScoresTimesLengths += vantageStreetScore * vantageStreetLength;

                var outputVantageStreet = new DaylightEvaluationVantageStreet(vantageStreetScore, vantagePoints.Count, vantagePoints[0].CenterlineOffsetDist, Guid.NewGuid(), vantageStreet.Name);
                model.AddElement(outputVantageStreet);

                vsIndex += 1;
            }

            var lowestStreetScore = new List<double>(streetScores).OrderBy(score => score).ToList()[0];
            var overallScore = streetScoresTimesLengths / streetLengths;

            var pass = true;

            if (lowestStreetScore < 66)
            {
                pass = false;
            }
            if (!input.QualifyForEastMidtownSubdistrict && overallScore < 75)
            {
                pass = false;
            }
            if (input.QualifyForEastMidtownSubdistrict && overallScore < 66)
            {
                pass = false;
            }

            Console.WriteLine($"LOWEST SCORE: {lowestStreetScore}");
            Console.WriteLine($"TOTAL SCORE: {overallScore}");

            var output = new NYCZR8127DaylightEvaluationOutputs(lowestStreetScore, overallScore, pass ? "PASS" : "FAIL");

            output.Model = model;

            return output;
        }

        // Grab the biggest site's bounding box from the model
        private static Site getSite(Model model)
        {
            var sites = getElementsOfType<Site>(model);
            if (sites == null)
            {
                return null;
            }
            sites = sites.OrderByDescending(e => e.Perimeter.Area()).ToList();
            var site = sites[0];
            return site;
        }

        private static List<T> getElementsOfType<T>(Model model)
        {
            if (model == null)
            {
                return null;
            }
            var items = new List<T>();
            items.AddRange(model.AllElementsOfType<T>());
            return items;
        }

        private static List<Envelope> getEastMidtownEnvelopes(List<Envelope> envelopes, Model model)
        {
            var up = new Vector3(0, 0, 1);
            var cutHeight = Units.FeetToMeters(150.0);
            var plane = new Plane(new Vector3(0, 0, cutHeight), Vector3.ZAxis);
            var envelopesForBlockage = new List<Envelope>();

            foreach (var envelope in envelopes)
            {
                var bottom = envelope.Elevation;
                var top = bottom + envelope.Height;

                if (top < cutHeight)
                {
                    continue;
                }

                if (bottom >= cutHeight)
                {
                    // envelope is above the cutoff, use as-is
                    envelopesForBlockage.Add(envelope);
                }
                else
                {
                    Polygon profile = null;

                    foreach (var solidOp in envelope.Representation.SolidOperations)
                    {
                        var intersections = new List<Vector3>();

                        foreach (var face in solidOp.Solid.Faces)
                        {
                            var polygon = face.Value.Outer.ToPolygon();
                            foreach (var segment in polygon.Segments())
                            {
                                if (segment.Intersects(plane, out var intersection))
                                {
                                    intersections.Add(intersection);
                                }
                            }
                            if (intersections.Count >= 3)
                            {
                                profile = ConvexHull.FromPoints(intersections);
                                profile = profile.Project(new Plane(new Vector3(), Vector3.ZAxis));
                            }
                            else if (intersections.Count > 0)
                            {
                                Console.WriteLine($"Failed to intersect polygon for East Midtown: Found {intersections.Count} point");
                            }
                        }
                    }

                    if (profile != null)
                    {
                        var extrude1 = new Elements.Geometry.Solids.Extrude(profile, cutHeight, up, false);
                        var rep1 = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude1 });
                        var env1 = new Envelope(profile, 0, cutHeight, up, 0, new Transform(), envelope.Material, rep1, false, Guid.NewGuid(), "");
                        envelopesForBlockage.Add(env1);

                        var extrude2 = new Elements.Geometry.Solids.Extrude(profile, top - cutHeight, up, false);
                        var rep2 = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude2 });
                        var env2 = new Envelope(profile, cutHeight, top - cutHeight, up, 0, new Transform(new Vector3(0, 0, cutHeight)), envelope.Material, rep2, false, Guid.NewGuid(), "");
                        envelopesForBlockage.Add(env2);
                    }
                }
            }

            return envelopesForBlockage;
        }
    }
}