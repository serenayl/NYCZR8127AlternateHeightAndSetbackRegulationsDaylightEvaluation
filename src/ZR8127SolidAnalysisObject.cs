using System;
using System.Collections.Generic;
using System.Linq;
using Elements;
using Elements.Geometry;
using Elements.Spatial;

namespace NYCZR8127AlternateHeightAndSetbackRegulationsDaylightEvaluation
{

    public class SolidAnalysisObject
    {
        private static double DivisionLength = Units.FeetToMeters(10.0);
        public static Boolean SkipSubdivide = false;
        public Dictionary<long, Vector3> Points = new Dictionary<long, Vector3>();
        public Dictionary<long, List<long>> Lines = new Dictionary<long, List<long>>();
        public List<List<Elements.Geometry.Solids.HalfEdge>> Surfaces = new List<List<Elements.Geometry.Solids.HalfEdge>>();

        // TODO: delete when we don't need to debug anymore
        public static Model Model = null;

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
                this.Points.Add(key, globallyTransformedPoint);
                maxVertexKey = Math.Max(maxVertexKey, key);
            }

            foreach (var edge in solid.Solid.Edges)
            {
                if (this.Points.TryGetValue(edge.Value.Left.Vertex.Id, out var start) && this.Points.TryGetValue(edge.Value.Right.Vertex.Id, out var end))
                {
                    var dist = start.DistanceTo(end);
                    var line = new Line(start, end);

                    Model.AddElement(new ModelCurve(line));

                    if (!SkipSubdivide && dist > DivisionLength && (start.X != end.X || start.Y != end.Y))
                    {
                        var indices = new List<long>() { edge.Value.Left.Vertex.Id };

                        var grid = new Grid1d(line);
                        grid.DivideByFixedLength(DivisionLength, FixedDivisionMode.RemainderAtBothEnds);
                        var cells = grid.GetCells();

                        // Get lines representing each 10' cell
                        var cellLines = cells.Select(c => c.GetCellGeometry()).OfType<Line>().ToArray();

                        // Add end of each division except for last point
                        foreach (var cellLine in cellLines.SkipLast(1))
                        {
                            var index = maxVertexKey + 1;
                            var point = cellLine.PointAt(1.0);
                            this.Points.Add(index, point);
                            indices.Add(index);
                            maxVertexKey = index;
                        }

                        // Add right point
                        indices.Add(edge.Value.Right.Vertex.Id);
                        this.Lines.Add(edge.Key, indices);
                    }
                    else
                    {
                        this.Lines.Add(edge.Key, new List<long>() { edge.Value.Left.Vertex.Id, edge.Value.Right.Vertex.Id });
                    }

                }
                else
                {
                    throw new Exception("Malformed geometry found: no vertex found at address for this edge.");
                }
            }

            foreach (var face in solid.Solid.Faces.Values)
            {
                var edges = new List<Elements.Geometry.Solids.HalfEdge>();
                foreach (var edge in face.Outer.Edges)
                {
                    edges.Add(edge);
                }
                // TODO: do not include faces that sit at zero
                this.Surfaces.Add(edges);
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