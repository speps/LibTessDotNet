using System;
using System.Collections.Generic;
using System.Drawing;
using LibTessDotNet;
using TessBed.Externals;

namespace TessBed
{
    public static class PolyConvert
    {
        public enum Library
        {
            Poly2Tri,
            Tess,
            LibTess2,
        }

        public static Poly2Tri.PolygonSet ToP2T(PolygonSet pset)
        {
            var rpset = new Poly2Tri.PolygonSet();
            foreach (var poly in pset)
            {
                var pts = new List<Poly2Tri.PolygonPoint>();
                foreach (var p in poly)
                    pts.Add(new Poly2Tri.PolygonPoint(p.X, p.Y));
                rpset.Add(new Poly2Tri.Polygon(pts));
            }
            return rpset;
        }

        public static PolygonSet FromP2T(Poly2Tri.PolygonSet pset)
        {
            var result = new PolygonSet();
            foreach (var poly in pset.Polygons)
            {
                foreach (var tri in poly.Triangles)
                {
                    var rtri = new Polygon();
                    rtri.Add(new PolygonPoint { X = tri.Points[0].Xf, Y = tri.Points[0].Yf });
                    rtri.Add(new PolygonPoint { X = tri.Points[1].Xf, Y = tri.Points[1].Yf });
                    rtri.Add(new PolygonPoint { X = tri.Points[2].Xf, Y = tri.Points[2].Yf });
                    result.Add(rtri);
                }
            }
            return result;
        }
        public static PolygonSet TriangulateP2T(PolygonSet pset)
        {
            var rpset = PolyConvert.ToP2T(pset);
            Poly2Tri.P2T.Triangulate(rpset);
            return PolyConvert.FromP2T(rpset);
        }

        public static void ToTess(PolygonSet pset, Tess tess)
        {
            foreach (var poly in pset)
            {
                var v = new ContourVertex[poly.Count];
                for (int i = 0; i < poly.Count; i++)
                {
                    v[i].Position = new Vec3(poly[i].X, poly[i].Y, poly[i].Z);
                    v[i].Data = poly[i].Color;
                }
                tess.AddContour(v, poly.Orientation);
            }
        }

        public static PolygonSet FromTess(Tess tess)
        {
            var output = new PolygonSet();
            for (int i = 0; i < tess.ElementCount; i++)
            {
                var poly = new Polygon();
                for (int j = 0; j < 3; j++)
                {
                    int index = tess.Elements[i * 3 + j];
                    if (index == -1)
                        continue;
                    var v = new PolygonPoint
                    {
                        X = tess.Vertices[index].Position.X,
                        Y = tess.Vertices[index].Position.Y,
                        Color = (Color)tess.Vertices[index].Data,
                    };
                    poly.Add(v);
                }
                output.Add(poly);
            }
            return output;
        }

        private static object VertexCombine(Vec3 position, object[] data, float[] weights)
        {
            var colors = new Color[] { (Color)data[0], (Color)data[1], (Color)data[2], (Color)data[3] };
            var rgba = new float[] {
                (float)colors[0].R * weights[0] + (float)colors[1].R * weights[1] + (float)colors[2].R * weights[2] + (float)colors[3].R * weights[3],
                (float)colors[0].G * weights[0] + (float)colors[1].G * weights[1] + (float)colors[2].G * weights[2] + (float)colors[3].G * weights[3],
                (float)colors[0].B * weights[0] + (float)colors[1].B * weights[1] + (float)colors[2].B * weights[2] + (float)colors[3].B * weights[3],
                (float)colors[0].A * weights[0] + (float)colors[1].A * weights[1] + (float)colors[2].A * weights[2] + (float)colors[3].A * weights[3]
            };
            return Color.FromArgb((int)rgba[3], (int)rgba[0], (int)rgba[1], (int)rgba[2]);
        }

        public static PolygonSet TriangulateTess(PolygonSet pset, int polySize, WindingRule rule)
        {
            var tess = new Tess();
            PolyConvert.ToTess(pset, tess);
            tess.Tessellate(rule, ElementType.Polygons, polySize, VertexCombine);
            return PolyConvert.FromTess(tess);
        }

        public static PolygonSet Triangulate(Library lib, PolygonSet pset, int polySize, WindingRule rule)
        {
            switch (lib)
            {
                case Library.Poly2Tri: return TriangulateP2T(pset);
                case Library.Tess: return TriangulateTess(pset, polySize, rule);
                case Library.LibTess2: return LibTess2.Tessellate(pset, polySize, rule);
            }
            return null;
        }
    }
}
