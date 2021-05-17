using System.Collections.Generic;
using Elements;
using Elements.Geometry;
using Elements.Spatial;
using System;
using System.Linq;

namespace NYCZR8127DaylightEvaluation
{
    public class GeoUtilities
    {
        public static Model Model; // For debug

        private static Random _random = new Random();

        private static Material _debugMaterial = new Material("DebugGeo", new Color(0.2, 0.5, 1, 0.5));

        public static List<Vector3> TransformedVertices(IList<Vertex> vertices, Transform transform)
        {
            return vertices.Select(v => TransformedPoint(v.Position, transform)).ToList();
        }

        public static Vector3 TransformedPoint(Vector3 point, Transform transform)
        {
            return transform != null ? transform.OfVector(point) : point;
        }

        public static List<Envelope> SliceAtHeight(MeshElement meshElement, double cutHeight, Boolean showDebugGeometry)
        {
            var bbox = new BBox3(TransformedVertices(meshElement.Mesh.Vertices, meshElement.Transform));
            var bottom = bbox.Min.Z;
            var top = bbox.Max.Z;
            var solids = new List<Elements.Geometry.Solids.SolidOperation>();
            var solid = new Elements.Geometry.Solids.Solid();
            foreach (var face in meshElement.Mesh.Triangles)
            {
                var vertices = TransformedVertices(face.Vertices, meshElement.Transform);
                solid.AddFace(new Polygon(vertices));
            }
            solids.Add(new Elements.Geometry.Solids.ConstructedSolid(solid));
            var rep = new Representation(solids);
            var env = new Envelope(Polygon.Rectangle(new Vector3(bbox.Min.X, bbox.Min.Y), new Vector3(bbox.Max.X, bbox.Max.Y)), bottom, top - bottom, Vector3.ZAxis, 0, new Transform(), _debugMaterial, rep, false, Guid.NewGuid(), "");
            return SliceAtHeight(env, cutHeight, showDebugGeometry);
        }

        public static List<Envelope> SliceAtHeight(Envelope envelope, double cutHeight, Boolean showDebugGeometry)
        {
            var debugMaterial = new Material("DebugSolid", new Color(1, 0, 0, 1));

            var plane = new Plane(new Vector3(0, 0, cutHeight), Vector3.ZAxis);
            var top = envelope.Elevation + envelope.Height;

            var envelopesForBlockage = new List<Envelope>();

            Polygon slice = null;

            var newUpperSolids = new List<Elements.Geometry.Solids.SolidOperation>();

            foreach (var solidOp in envelope.Representation.SolidOperations)
            {
                var intersections = new List<Vector3>();

                var newUpperSolid = new Elements.Geometry.Solids.Solid();

                foreach (var face in solidOp.Solid.Faces)
                {
                    var polygon = face.Value.Outer.ToPolygon();

                    var faceIntersections = new List<Vector3>();

                    foreach (var segment in polygon.Segments())
                    {
                        if (segment.Intersects(plane, out var intersection))
                        {
                            intersections.Add(intersection);
                            faceIntersections.Add(intersection);
                        }
                    }

                    if (faceIntersections.Count == 0)
                    {
                        if (polygon.Centroid().Z > cutHeight)
                        {
                            newUpperSolid.AddFace(polygon);
                        }
                    }
                    else if (faceIntersections.Count > 1)
                    {
                        faceIntersections = faceIntersections.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
                        var splitLine = new Polyline(faceIntersections);
                        var splits = polygon.Split(splitLine);
                        foreach (var split in splits)
                        {
                            if (split.Centroid().Z > cutHeight)
                            {
                                newUpperSolid.AddFace(split);
                            }
                        }
                    }
                }

                if (intersections.Count >= 3)
                {
                    slice = ConvexHull.FromPoints(intersections);
                    slice = slice.Project(new Plane(new Vector3(), Vector3.ZAxis));
                }
                else if (intersections.Count > 0)
                {
                    Console.WriteLine($"Failed to intersect polygon for East Midtown: Found {intersections.Count} point");
                }

                newUpperSolids.Add(new Elements.Geometry.Solids.ConstructedSolid(newUpperSolid));
            }

            if (slice != null)
            {
                var extrude1 = new Elements.Geometry.Solids.Extrude(slice, cutHeight, Vector3.ZAxis, false);
                var rep1 = new Representation(new List<Elements.Geometry.Solids.SolidOperation>() { extrude1 });
                var env1 = new Envelope(slice, 0, cutHeight, Vector3.ZAxis, 0, new Transform(), _debugMaterial, rep1, false, Guid.NewGuid(), "");
                envelopesForBlockage.Add(env1);

                var rep2 = new Representation(newUpperSolids);
                var env2 = new Envelope(slice, 0, cutHeight, Vector3.ZAxis, 0, new Transform(), _debugMaterial, rep2, false, Guid.NewGuid(), "");
                envelopesForBlockage.Add(env2);

                if (showDebugGeometry)
                {
                    Model.AddElement(env1);
                    Model.AddElement(env2);
                }
            }

            return envelopesForBlockage;
        }

        private static List<Elements.Geometry.Solids.Solid> SplitSolid(Elements.Geometry.Solids.Solid solid)
        {
            var solids = new List<Elements.Geometry.Solids.Solid>();
            return solids;
        }

        private static Material _nextMaterial()
        {
            var color = RandomExtensions.NextColor(_random);
            var colorWithAlpha = new Color(color.Red, color.Green, color.Blue, 0.7);
            return new Material(color.ToString(), colorWithAlpha, unlit: true);
        }
    }
}