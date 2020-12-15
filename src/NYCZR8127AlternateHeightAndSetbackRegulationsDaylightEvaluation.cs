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
                var nearLotLine = lotLines[0];
                var centerlineOffsetDist = Lookups.CenterlineDistances[vantageStreet.Width] / 2;
                var directionToStreet = new Vector3(nearLotLine.PointAt(0.5) - siteCentroid).Unitized() * centerlineOffsetDist;
                var centerline = new Line(nearLotLine.Start + directionToStreet, nearLotLine.End + directionToStreet);
                model.AddElement(new ModelCurve(centerline));
                var vantagePoints = VantagePoint.GetVantagePoints(centerline, nearLotLine, model);

                foreach (var vantagePoint in vantagePoints)
                {
                    foreach (var analysisObject in analysisObjects)
                    {
                        foreach (var point in analysisObject.points)
                        {
                            var planAndSectionAngle = vantagePoint.GetPlanAndSectionAngle(point.Value);
                        }
                    }
                }

                Projection.DrawDiagram(centerlineOffsetDist, model);
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