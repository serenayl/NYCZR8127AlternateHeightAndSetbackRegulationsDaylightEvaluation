
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
    public partial class NYCDaylightEvaluationVantageStreet : GeometricElement
    {
        private GraphicsBuffers ToGraphicsBuffers()
        {
            return (new List<Vector3>() { this.Centerline.Start, this.Centerline.End }).ToGraphicsBuffers();
        }

        public override bool TryToGraphicsBuffers(out List<GraphicsBuffers> graphicsBuffers, out string id, out glTFLoader.Schema.MeshPrimitive.ModeEnum? mode)
        {
            id = $"{this.Id}_curve";
            mode = glTFLoader.Schema.MeshPrimitive.ModeEnum.LINES;
            graphicsBuffers = new List<GraphicsBuffers>() { this.ToGraphicsBuffers() };
            return true;
        }
    }
}