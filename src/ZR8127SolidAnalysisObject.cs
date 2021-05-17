using System;
using System.Collections.Generic;
using System.Linq;
using Elements;
using Elements.Geometry;
using Elements.Spatial;

namespace NYCZR8127DaylightEvaluation
{

    public class AnalysisEdge
    {
        public long LineId;
        public bool Reversed;

        public AnalysisEdge(long lineId, bool reversed)
        {
            this.LineId = lineId;
            this.Reversed = reversed;
        }
    }

    public class SolidAnalysisObject
    {
        public static Model Model; // in case we want to draw for debugging

        private static double DivisionLength = Units.FeetToMeters(10.0);
        public static Boolean SkipSubdivide = false;
        public Dictionary<long, Vector3> Points = new Dictionary<long, Vector3>();
        public Dictionary<long, List<long>> Lines = new Dictionary<long, List<long>>();
        public List<List<AnalysisEdge>> Surfaces = new List<List<AnalysisEdge>>();

        private long _maxVertexKey = 0;

        private SolidAnalysisObject(MeshElement meshElement)
        {
            Dictionary<string, long> edgeLookup = new Dictionary<string, long>();

            long edgeIdx = 0;

            foreach (var vertex in meshElement.Mesh.Vertices)
            {
                var key = vertex.Index;
                var point = GeoUtilities.TransformedPoint(vertex.Position, meshElement.Transform);
                this.Points.Add(key, point);
                this._maxVertexKey = Math.Max(this._maxVertexKey, key);
            }

            var tIdx = 0;

            foreach (var triangle in meshElement.Mesh.Triangles)
            {
                var vertices = triangle.Vertices.ToList();
                var edges = new List<AnalysisEdge>();

                var vIdx = 0;

                foreach (var startVertex in vertices)
                {
                    var endVertex = vIdx == vertices.Count - 1 ? vertices[0] : vertices[vIdx + 1];

                    var lowerVertex = startVertex.Index < endVertex.Index ? startVertex : endVertex;
                    var higherVertex = startVertex.Index > endVertex.Index ? startVertex : endVertex;

                    var lineIdUniq = $"{lowerVertex.Index}_{higherVertex.Index}";

                    if (!edgeLookup.ContainsKey(lineIdUniq))
                    {
                        this.AddEdge(edgeIdx, lowerVertex.Index, higherVertex.Index);
                        edgeLookup.Add(lineIdUniq, edgeIdx);
                        edgeIdx += 1;
                    }

                    if (edgeLookup.TryGetValue(lineIdUniq, out long addedOrExistingEdgeIdx))
                    {
                        var isReversed = startVertex.Index != lowerVertex.Index;
                        edges.Add(new AnalysisEdge(addedOrExistingEdgeIdx, isReversed));
                    }

                    vIdx += 1;
                }
                this.Surfaces.Add(edges);
                tIdx += 1;
            }
        }

        private SolidAnalysisObject(Elements.Geometry.Solids.SolidOperation solid, Transform transform)
        {
            var solidTransform = solid.LocalTransform;

            foreach (var vertex in solid.Solid.Vertices)
            {
                var key = vertex.Key;
                var point = vertex.Value.Point;
                var locallyTransformedPoint = GeoUtilities.TransformedPoint(vertex.Value.Point, solidTransform);
                var globallyTransformedPoint = GeoUtilities.TransformedPoint(locallyTransformedPoint, transform);
                this.Points.Add(key, globallyTransformedPoint);
                this._maxVertexKey = Math.Max(this._maxVertexKey, key);
            }

            foreach (var edge in solid.Solid.Edges)
            {
                this.AddEdge(edge.Key, edge.Value.Left.Vertex.Id, edge.Value.Right.Vertex.Id);
            }

            foreach (var face in solid.Solid.Faces.Values)
            {
                var edges = new List<AnalysisEdge>();
                foreach (var edge in face.Outer.Edges)
                {
                    var isReversed = edge.Vertex.Id != edge.Edge.Left.Vertex.Id;
                    edges.Add(new AnalysisEdge(edge.Edge.Id, isReversed));
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

        public static List<SolidAnalysisObject> MakeFromMeshElements(List<MeshElement> meshElements)
        {
            var list = new List<SolidAnalysisObject>();

            foreach (var meshElement in meshElements)
            {
                var analysisObject = new SolidAnalysisObject(meshElement);
                list.Add(analysisObject);
            }

            return list;
        }

        private void AddEdge(long key, long startVertexId, long endVertexId)
        {
            if (this.Points.TryGetValue(startVertexId, out var start) && this.Points.TryGetValue(endVertexId, out var end))
            {
                var dist = start.DistanceTo(end);
                var line = new Line(start, end);

                if (!SkipSubdivide && dist > DivisionLength && (start.X != end.X || start.Y != end.Y))
                {
                    var indices = new List<long>() { startVertexId };

                    var grid = new Grid1d(line);
                    grid.DivideByFixedLength(DivisionLength, FixedDivisionMode.RemainderAtBothEnds);
                    var cells = grid.GetCells();

                    // Get lines representing each 10' cell
                    var cellLines = cells.Select(c => c.GetCellGeometry()).OfType<Line>().ToArray();

                    // Add end of each division except for last point
                    foreach (var cellLine in cellLines.SkipLast(1))
                    {
                        var index = this._maxVertexKey + 1;
                        var point = cellLine.PointAt(1.0);
                        this.Points.Add(index, point);
                        indices.Add(index);
                        this._maxVertexKey = index;
                    }

                    // Add right point
                    indices.Add(endVertexId);
                    this.Lines.Add(key, indices);
                }
                else
                {
                    this.Lines.Add(key, new List<long>() { startVertexId, endVertexId });
                }
            }
            else
            {
                throw new Exception("Malformed geometry found: no vertex found at address for this edge.");
            }
        }
    }
}