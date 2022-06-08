
using Elements;
using Elements.GeoJSON;
using Elements.Geometry;
using Elements.Geometry.Solids;
using Elements.Spatial;
using Elements.Validators;
using Elements.Serialization.JSON;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Line = Elements.Geometry.Line;
using Polygon = Elements.Geometry.Polygon;

namespace Elements
{
    public partial class NYCDaylightEvaluationVantagePoint : GeometricElement
    {
        private Polyline viz;
        public void SetViz(Polyline polyline)
        {
            viz = polyline;
        }

        public override void UpdateRepresentations()
        {
            var polygon = new Polygon(this.viz.Vertices);
            var lamina = new Lamina(polygon);
            // TODO: make more robust. This is currently frail to assume origin is center of view cone.
            var circle = new Lamina(new Circle(this.viz.Vertices[1], 1).ToPolygon());
            this.Representation = new Representation(new List<SolidOperation> { lamina, circle });
        }
    }
}