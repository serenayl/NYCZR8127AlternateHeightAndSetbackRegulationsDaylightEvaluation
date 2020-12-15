using System;
using System.Collections.Generic;
using Elements;
using Elements.Geometry;

namespace NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluation
{

    public class SolidAnalysisObject
    {
        private static double divisionLength = Units.FeetToMeters(10.0);
        public static Model model = null;
        public static Boolean skipSubdivide = false;
        public Dictionary<long, Vector3> points = new Dictionary<long, Vector3>();
        public Dictionary<long, List<long>> lines = new Dictionary<long, List<long>>();
        public List<List<long>> surfaces = new List<List<long>>();
        public SolidAnalysisObject(Elements.Geometry.Solids.SolidOperation solid, Transform transform)
        {
            var solidTransform = solid.LocalTransform;

            long maxVertexKey = 0;

            foreach (var vertex in solid.Solid.Vertices)
            {
                var key = vertex.Key;
                var point = vertex.Value.Point;
                var locallyTransformedPoint = solidTransform == null ? new Vector3(point) : solidTransform.OfVector(vertex.Value.Point);
                var globallyTransformedPoint = transform.OfVector(locallyTransformedPoint);
                this.points.Add(key, globallyTransformedPoint);
                maxVertexKey = Math.Max(maxVertexKey, key);
            }

            foreach (var edge in solid.Solid.Edges)
            {
                if (this.points.TryGetValue(edge.Value.Left.Vertex.Id, out var start) && this.points.TryGetValue(edge.Value.Right.Vertex.Id, out var end))
                {
                    var dist = start.DistanceTo(end);
                    var line = new Line(start, end);
                    model.AddElement(new ModelCurve(line));
                    if (!skipSubdivide && dist > divisionLength)
                    {
                        // TODO: subdivide this line
                        // currently no difference between the else
                        this.lines.Add(edge.Key, new List<long>() { edge.Value.Left.Vertex.Id, edge.Value.Right.Vertex.Id });
                    }
                    else
                    {
                        this.lines.Add(edge.Key, new List<long>() { edge.Value.Left.Vertex.Id, edge.Value.Right.Vertex.Id });
                    }

                }
                else
                {
                    throw new Exception("Malformed geometry found: no vertex found at address for this edge.");
                }
            }

            foreach (var face in solid.Solid.Faces.Values)
            {
                var edges = new List<long>();
                foreach (var edge in face.Outer.Edges)
                {
                    edges.Add(edge.Edge.Id);
                }
                // TODO: do not include faces that sit at zero
                this.surfaces.Add(edges);
            }
        }

        public static List<SolidAnalysisObject> MakeFromEnvelopes(List<Envelope> envelopes)
        {
            var list = new List<SolidAnalysisObject>();

            foreach (var envelope in envelopes)
            {
                var envelopeTransform = envelope.Transform;

                foreach (var solid in envelope.Representation.SolidOperations)
                {
                    var analysisObject = new SolidAnalysisObject(solid, envelopeTransform);
                    list.Add(analysisObject);
                }
            }

            return list;
        }
    }
}