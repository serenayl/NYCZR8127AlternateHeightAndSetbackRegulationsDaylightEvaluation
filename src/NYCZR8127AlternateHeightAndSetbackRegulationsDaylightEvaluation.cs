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

            // Make sure we have a site (represented as bounds)
            var site = getSite(inputModels);
            var siteRect = Polygon.Rectangle(new Vector3(site.Min.X, site.Min.Y), new Vector3(site.Max.X, site.Max.Y));
            var siteCentroid = siteRect.Centroid();

            model.AddElement(new ModelCurve(siteRect, name: "Site Bounds Used"));

            // Make sure we have envelopes
            var envelopes = GetEnvelopes(inputModels);

            foreach (var vantageStreet in input.VantageStreets)
            {
                var midpoint = vantageStreet.Line.PointAt(0.5);
                var lotLines = siteRect.Segments().OrderBy(segment => midpoint.DistanceTo(segment)).ToList();
                var nearLotLine = lotLines[0];
                var directionToStreet = new Vector3(midpoint - siteCentroid).Unitized() * CenterlineSettings.Lookup[vantageStreet.Width]/2;
                var centerline = new Line(nearLotLine.Start + directionToStreet, nearLotLine.End + directionToStreet);
                model.AddElement(new ModelCurve(centerline));
            }

            output.Model = model;

            return output;
        }

        // Grab the biggest site's bounding box from the model
        public static BBox3 getSite(Dictionary<string, Model> inputModels)
        {
            var sites = new List<Site>();
            inputModels.TryGetValue("Site", out var model);

            if (model == null)
            {
                throw new ArgumentException("No Site found.");
            }

            sites.AddRange(model.AllElementsOfType<Site>());
            sites = sites.OrderByDescending(e => e.Perimeter.Area()).ToList();

            var site = sites[0];
            var bounds = site.Perimeter.Bounds();
            return bounds;
        }

        // Grab envelopes from the model
        public static List<Envelope> GetEnvelopes(Dictionary<string, Model> inputModels)
        {
            var envelopes = new List<Envelope>();
            inputModels.TryGetValue("Envelope", out var model);

            if (model == null)
            {
                throw new ArgumentException("No Envelopes found.");
            }

            envelopes.AddRange(model.AllElementsOfType<Envelope>());

            if (envelopes.Count < 1)
            {
                throw new ArgumentException("No Envelopes found.");
            }

            return envelopes;
        }
    }
}