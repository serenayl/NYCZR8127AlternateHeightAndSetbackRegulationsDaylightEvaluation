using System.Collections.Generic;
using Elements;
using Elements.Geometry;
using Elements.Spatial;
using System;
using System.Linq;

namespace NYCZR8127DaylightEvaluation
{
    public class DebugUtilities
    {
        public static Random random = new Random();
        public static EdgeDisplaySettings edgeDisplaySettings = new EdgeDisplaySettings();
        public static void Trace(string id, List<AnalysisPoint> aps, VantagePoint vp, Model model = null)
        {
            if (model == null)
            {
                return;
            }

            var start = 20;
            var end = aps.Count;

            Console.WriteLine("Tracing:");

            for (var k = start; k < end; k++)
            {
                var srfAP = aps[k];
                var progress = (k + 1) / (double)end;
                var color = random.NextColor();
                var name = $"{id}_{k}";
                var material = new Material(progress.ToString(), new Color(color.Red, color.Green, color.Blue, 1));

                if (k < end - 1)
                {
                    var nextAp = aps[k + 1];
                    var crv = new ModelCurve(new Line(srfAP.PlanAndSection, nextAp.PlanAndSection), material);
                    crv.SetSelectable(false);
                    model.AddElement(crv);
                }

                var origin = vp.Point;
                var original = srfAP.Original;
                var planAndSection = srfAP.PlanAndSection;
                var drawCoordinate = srfAP.DrawCoordinate;

                Console.WriteLine($"- {k}/{end}: {srfAP.Original} & {srfAP.PlanAndSection}");

                var polyline = new Polyline(origin, original, planAndSection, drawCoordinate);

                var lineFromVP = new ModelCurve(polyline, material, name: name);
                lineFromVP.AdditionalProperties.Add("original", $"{srfAP.Original.X}, {srfAP.Original.Y}, {srfAP.Original.Z}");
                lineFromVP.AdditionalProperties.Add("new", $"{srfAP.PlanAndSection.X}, {srfAP.PlanAndSection.Y}, {srfAP.PlanAndSection.Z}");
                model.AddElement(lineFromVP);

                var transform = new Transform(srfAP.PlanAndSection);
                var visualization = new Panel(new Circle(0.5).ToPolygon(), material, transform, name: name);
                model.AddElement(visualization);
            }

        }

    }
}