using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LibTessDotNet;

namespace TessBed
{
    public static class PolyConvert
    {
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

        public static void ToTess(PolygonSet pset, Tess tess)
        {
            foreach (var poly in pset)
            {
                var v = new ContourVertex[poly.Count];
                for (int i = 0; i < poly.Count; i++)
                {
                    v[i].Position = new Vec3 { X = poly[i].X, Y = poly[i].Y, Z = poly[i].Z };
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
                        Y = tess.Vertices[index].Position.Y
                    };
                    poly.Add(v);
                }
                output.Add(poly);
            }
            return output;
        }
    }
}
