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
            var rhinoBreps = getElementsOfType<RhinoBrep>(envelopeModel);
            var rhinoExtrusions = getElementsOfType<RhinoExtrusion>(envelopeModel);
            var meshEnvelopes = getElementsOfType<MeshElement>(envelopeModel);

            if (siteInput == null)
            {
                throw new ArgumentException("There were no sites found. Please make sure you either meet the dependency of 'Site' or the dependency of 'EnvelopeAndSite."); throw new ArgumentException("BOOO SITE IS NOT FOUND");
            }
            if ((envelopes == null || envelopes.Count < 1) && (meshEnvelopes == null || meshEnvelopes.Count < 1) && (rhinoBreps == null || rhinoBreps.Count < 1) && (rhinoExtrusions == null || rhinoExtrusions.Count < 1))
            {
                throw new ArgumentException("There were no envelopes found. Please make sure you either meet the dependency of 'Envelope' or the dependency of 'EnvelopeAndSite.");
            }

            var site = siteInput.Perimeter.Bounds();
            var siteRect = Polygon.Rectangle(new Vector3(site.Min.X, site.Min.Y), new Vector3(site.Max.X, site.Max.Y));

            model.AddElement(new ModelCurve(siteRect, name: "Site Bounds Used"));

            GeoUtilities.Model = model;
            Diagram.Model = model;
            SolidAnalysisObject.Model = model;
            SolidAnalysisObject.SkipSubdivide = input.SkipSubdivide;

            var analysisObjects = SolidAnalysisObject.MakeFromEnvelopes(envelopes);

            foreach (var analysisObject in SolidAnalysisObject.MakeFromMeshElements(meshEnvelopes))
            {
                analysisObjects.Add(analysisObject);
            }

            foreach (var analysisObject in SolidAnalysisObject.MakeFromRhinoBreps(rhinoBreps))
            {
                analysisObjects.Add(analysisObject);
            }

            foreach (var analysisObject in SolidAnalysisObject.MakeFromRhinoExtrusions(rhinoExtrusions))
            {
                analysisObjects.Add(analysisObject);
            }

            // Only applicable for E Midtown
            List<SolidAnalysisObject> analysisObjectsForBlockage = input.QualifyForEastMidtownSubdistrict ? getEastMidtownEnvelopes(envelopes, rhinoBreps, rhinoExtrusions, meshEnvelopes, model, input.DebugVisualization) : null;

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
                var matchingOverride = input.Overrides?.VantageStreets.FirstOrDefault(x => x.Identity.Name == vantageStreet.Name);

                var color = DebugUtilities.random.NextColor();
                var vantageStreetMaterial = new Material($"Vantage Street {vantageStreet.Name}", color) { EdgeDisplaySettings = new EdgeDisplaySettings { LineWidth = 2 } };
                var vantagePointMaterial = new Material($"Vantage Point on {vantageStreet.Name}", new Color(color.Red, color.Green, color.Blue, 0.15)) { EdgeDisplaySettings = new EdgeDisplaySettings { LineWidth = 2 } };
                var outputVantageStreet = VantageElementUtils.CreateVantageStreet(siteRect, vantageStreet, out var vantagePoints, matchingOverride, model);
                outputVantageStreet.Material = vantageStreetMaterial;

                int vpIndex = 0;

                foreach (var vp in vantagePoints)
                {
                    var transform = new Transform(new Vector3(90.0 + vpIndex * 200.0, site.Max.Y + margin + verticalOffsetBase * vsIndex));

                    var name = $"{vantageStreet.Name}: VP {vpIndex + 1}";

                    vp.Diagram.Draw(name, model, analysisObjects, input, input.DebugVisualization, analysisObjectsForBlockage: analysisObjectsForBlockage);

                    var outputVp = new NYCDaylightEvaluationVantagePoint(
                        vp.Diagram.DaylightBlockage,
                        vp.Diagram.UnblockedDaylightCredit,
                        vp.Diagram.ProfilePenalty,
                        vp.Diagram.AvailableDaylight,
                        vp.Diagram.DaylightRemaining,
                        vp.Diagram.DaylightScore,
                        outputVantageStreet,
                        material: vantagePointMaterial,
                        name: name
                    );
                    outputVp.SetViz(vp.GetViewCone());
                    model.AddElement(outputVp);

                    var vpHelper = vp.GetVisualizationHelpers();
                    vpHelper.Name = $"{outputVp.Name} Helper";
                    // vpHelper.AdditionalProperties["Target"] = outputVp.Id; // makes interface too messy
                    model.AddElement(vpHelper);

                    vpIndex += 1;
                }

                var sumScores = vantagePoints.Aggregate(0.0, (sum, vp) => sum + vp.Diagram.DaylightScore);
                var vantageStreetScore = sumScores / vantagePoints.Count;
                var vantageStreetLength = outputVantageStreet.FrontLotLine.Length();

                streetScores.Add(vantageStreetScore);
                streetLengths += vantageStreetLength;
                streetScoresTimesLengths += vantageStreetScore * vantageStreetLength;

                outputVantageStreet.Score = vantageStreetScore;
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

        private static List<T> getElementsOfType<T>(Model model) where T : Elements.Element
        {
            if (model == null)
            {
                return null;
            }
            var items = new List<T>();
            items.AddRange(model.AllElementsOfType<T>());
            return items;
        }

        private static List<SolidAnalysisObject> getEastMidtownEnvelopes(List<Envelope> envelopes, List<RhinoBrep> rhinoBreps, List<RhinoExtrusion> rhinoExtrusions, List<MeshElement> meshEnvelopes, Model model, Boolean showDebugGeometry)
        {
            var analysisObjects = new List<SolidAnalysisObject>();

            var up = new Vector3(0, 0, 1);
            var cutHeight = Units.FeetToMeters(150.0);
            var plane = new Plane(new Vector3(0, 0, cutHeight), Vector3.ZAxis);
            var envelopesForBlockage = new List<Envelope>();
            var meshElementsForBlockage = new List<MeshElement>();

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
                    analysisObjects.AddRange(SolidAnalysisObject.MakeFromEnvelopes(new List<Envelope>() { envelope }));
                }
                else
                {
                    envelopesForBlockage.AddRange(GeoUtilities.SliceAtHeight(envelope, cutHeight, showDebugGeometry));
                }
            }

            foreach (var rhinoBrep in rhinoBreps)
            {
                envelopesForBlockage.AddRange(GeoUtilities.SliceAtHeight(rhinoBrep, cutHeight, showDebugGeometry));
            }

            foreach (var rhinoExtrusion in rhinoExtrusions)
            {
                envelopesForBlockage.AddRange(GeoUtilities.SliceAtHeight(rhinoExtrusion, cutHeight, showDebugGeometry));
            }

            foreach (var meshElement in meshEnvelopes)
            {
                var bbox = new BBox3(GeoUtilities.TransformedVertices(meshElement.Mesh.Vertices, meshElement.Transform));
                var bottom = bbox.Min.Z;
                var top = bbox.Max.Z;

                if (top < cutHeight)
                {
                    continue;
                }

                if (bottom >= cutHeight)
                {
                    // envelope is above the cutoff, use as-is
                    meshElementsForBlockage.Add(meshElement);
                }
                else
                {
                    envelopesForBlockage.AddRange(GeoUtilities.SliceAtHeight(meshElement, cutHeight, showDebugGeometry));
                }
            }

            analysisObjects.AddRange(SolidAnalysisObject.MakeFromEnvelopes(envelopesForBlockage));
            analysisObjects.AddRange(SolidAnalysisObject.MakeFromMeshElements(meshElementsForBlockage));

            return analysisObjects;
        }
    }
}