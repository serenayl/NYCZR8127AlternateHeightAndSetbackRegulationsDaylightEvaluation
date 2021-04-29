using System;
using System.Collections.Generic;
using System.Linq;
using Elements;
using Elements.Geometry;
using Elements.Spatial;

namespace NYCZR8127DaylightEvaluation
{

    public class SolidAnalysisObject
    {
        private long _maxVertexKey = 0;

        private static double DivisionLength = Units.FeetToMeters(10.0);

        // TEMP:
        public static Model Model;

        public static Boolean SkipSubdivide = false;
        public Dictionary<long, Vector3> Points = new Dictionary<long, Vector3>();
        public Dictionary<long, List<long>> Lines = new Dictionary<long, List<long>>();

        public List<List<(long edgeId, bool isLeftToRight)>> Surfaces = new List<List<(long edgeId, bool isLeftToRight)>>();

        private long AddPoint(Vector3 point, long? desiredKey = null)
        {
            long key = desiredKey == null ? this._maxVertexKey + 1 : (long)desiredKey;
            this.Points.Add(key, point);
            this._maxVertexKey = Math.Max(this._maxVertexKey, key);
            return key;
        }

        private void AddLineFromHalfEdge(Elements.Geometry.Solids.HalfEdge halfEdge)
        {
            var edge = halfEdge.Edge;
            var id = edge.Id;

            if (this.Lines.ContainsKey(id))
            {
                // We already added this edge
                return;
            }

            if (this.Points.TryGetValue(edge.Left.Vertex.Id, out var start) && this.Points.TryGetValue(edge.Right.Vertex.Id, out var end))
            {
                var dist = start.DistanceTo(end);
                var line = new Line(start, end);

                if (!SkipSubdivide && dist > DivisionLength && (start.X != end.X || start.Y != end.Y))
                {
                    var indices = new List<long>() { edge.Left.Vertex.Id };

                    var grid = new Grid1d(line);
                    grid.DivideByFixedLength(DivisionLength, FixedDivisionMode.RemainderNearMiddle);
                    var cells = grid.GetCells();

                    // Get lines representing each 10' cell
                    var cellLines = cells.Select(c => c.GetCellGeometry()).OfType<Line>().ToArray();

                    // Add end of each division except for last point
                    foreach (var cellLine in cellLines.SkipLast(1))
                    {
                        var point = cellLine.PointAt(1.0);
                        var index = this.AddPoint(point);
                        indices.Add(index);
                    }

                    // Add right point
                    indices.Add(edge.Right.Vertex.Id);

                    this.Lines.Add(id, indices);
                }
                else
                {
                    this.Lines.Add(id, new List<long>() { edge.Left.Vertex.Id, edge.Right.Vertex.Id });
                }
            }
            else
            {
                throw new Exception("Malformed geometry found: no vertex found at address for this edge.");
            }
        }

        private void AddHalfEdges(List<Elements.Geometry.Solids.HalfEdge> halfEdges, bool reverse = false)
        {
            foreach (var halfEdge in halfEdges)
            {
                this.AddLineFromHalfEdge(halfEdge);
            }

            // TODO: do not include faces that sit at zero

            this.Surfaces.Add(halfEdges.Select(hE =>
            {
                var isLeftToRight = hE.Vertex.Id == hE.Edge.Left.Vertex.Id;
                return (hE.Edge.Id, isLeftToRight);
            }).ToList());
        }

        public SolidAnalysisObject(Elements.Geometry.Solids.SolidOperation solid, Transform transform)
        {
            // Console.WriteLine($"Local transofrm: {solid.LocalTransform}");
            var solidTransform = solid.LocalTransform;

            foreach (var vertex in solid.Solid.Vertices)
            {
                var key = vertex.Key;
                var point = vertex.Value.Point;
                var locallyTransformedPoint = solidTransform == null ? new Vector3(point) : solidTransform.OfVector(vertex.Value.Point);
                var globallyTransformedPoint = transform.OfVector(locallyTransformedPoint);
                this.AddPoint(globallyTransformedPoint, key);
            }

            foreach (var face in solid.Solid.Faces.Values)
            {
                var polygon = face.Outer.ToPolygon();
                var edges = face.Outer.Edges.Select(he => he).ToList();
                this.AddHalfEdges(edges, polygon.IsClockWise());
            }
        }

        public static List<SolidAnalysisObject> MakeFromEnvelopes(List<Envelope> envelopes)
        {
            var list = new List<SolidAnalysisObject>();

            foreach (var envelope in envelopes)
            {
                var envelopeTransform = envelope.Transform;

                Console.WriteLine($"Envelope transform: {envelopeTransform}");

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