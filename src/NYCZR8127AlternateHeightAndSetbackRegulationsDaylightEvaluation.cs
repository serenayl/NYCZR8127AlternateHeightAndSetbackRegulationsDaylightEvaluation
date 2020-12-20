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
                Console.WriteLine("Using separate Envelope and Site dependencies");
                inputModels.TryGetValue("Site", out var siteModel);
                inputModels.TryGetValue("Envelope", out var envelopeModel);

                siteInput = GetSite(inputModels, siteModel);
                envelopes = GetEnvelopes(inputModels, envelopeModel);
            }
            else
            {
                Console.WriteLine("Using combo EnvelopeAndSite");
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

            model.AddElement(new ModelCurve(siteRect, name: "Site Bounds Used"));

            foreach (var vantageStreet in input.VantageStreets)
            {
                var vantagePoints = VantagePoint.GetVantagePoints(siteRect, vantageStreet, model);

                int vpIndex = 0;

                foreach (var vp in vantagePoints)
                {
                    var transform = new Transform(new Vector3(90.0 + vpIndex * 200.0, Units.FeetToMeters(100) + 20.0));
                    vp.Diagram.Draw(model, analysisObjects, transform, input.DebugVisualization);
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