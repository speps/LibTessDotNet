using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using LibTessDotNet;
using System.Diagnostics;

namespace TessBed
{
    [DebuggerDisplay("{X}, {Y}, {Z}")]
    public struct PolygonPoint
    {
        public float X, Y, Z;
        public Color Color;
    }

    public class Polygon : List<PolygonPoint>
    {
        public ContourOrientation Orientation = ContourOrientation.Original;

        public Polygon()
        {

        }

        public Polygon(ICollection<PolygonPoint> points)
            : base(points)
        {

        }
    }

    public class PolygonSet : List<Polygon>
    {
        public bool HasColors = false;
    }

    public class DataLoader
    {
        public class Asset
        {
            public string Name;
            public PolygonSet Polygons;
        }

        public static PolygonSet LoadDat(Stream resourceStream)
        {
            var points = new List<PolygonPoint>();
            var polys = new PolygonSet();
            int lineNum = 0;
            string line;
            Color currentColor = Color.White;
            ContourOrientation currentOrientation = ContourOrientation.Original;
            using (var stream = new StreamReader(resourceStream))
            {
                while ((line = stream.ReadLine()) != null)
                {
                    ++lineNum;
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        if (points.Count > 0)
                        {
                            var p = new Polygon(points) { Orientation = currentOrientation };
                            currentOrientation = ContourOrientation.Original;
                            polys.Add(p);
                            points.Clear();
                        }
                        continue;
                    }
                    if (line.StartsWith("//") ||
                        line.StartsWith("#") ||
                        line.StartsWith(";"))
                    {
                        continue;
                    }
                    if (line.StartsWith("force", true, CultureInfo.InvariantCulture))
                    {
                        var force = line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (force.Length == 2)
                        {
                            if (string.Compare(force[1], "cw", true) == 0)
                            {
                                currentOrientation = ContourOrientation.Clockwise;
                            }
                            if (string.Compare(force[1], "ccw", true) == 0)
                            {
                                currentOrientation = ContourOrientation.CounterClockwise;
                            }
                        }
                    }
                    else if (line.StartsWith("color", true, CultureInfo.InvariantCulture))
                    {
                        var rgba = line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        int r = 255, g = 255, b = 255, a = 255;
                        if (rgba != null)
                        {
                            if (rgba.Length == 4 &&
                                int.TryParse(rgba[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out r) &&
                                int.TryParse(rgba[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out g) &&
                                int.TryParse(rgba[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out b))
                            {
                                currentColor = Color.FromArgb(r, g, b);
                                polys.HasColors = true;
                            }
                            else if (rgba.Length == 5 &&
                                int.TryParse(rgba[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out r) &&
                                int.TryParse(rgba[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out g) &&
                                int.TryParse(rgba[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out b) &&
                                int.TryParse(rgba[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out a))
                            {
                                currentColor = Color.FromArgb(a, r, g, b);
                                polys.HasColors = true;
                            }
                        }
                    }
                    else
                    {
                        float x = 0, y = 0, z = 0;
                        var xyz = line.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (xyz != null)
                        {
                            if (xyz.Length >= 1) float.TryParse(xyz[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x);
                            if (xyz.Length >= 2) float.TryParse(xyz[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y);
                            if (xyz.Length >= 3) float.TryParse(xyz[2], NumberStyles.Float, CultureInfo.InvariantCulture, out z);
                            points.Add(new PolygonPoint { X = x, Y = y, Z = z, Color = currentColor });
                        }
                        else
                        {
                            throw new InvalidDataException("Invalid input data");
                        }
                    }
                }

                if (points.Count > 0)
                {
                    Polygon p = new Polygon(points) { Orientation = currentOrientation };
                    polys.Add(p);
                }
            }

            return polys;
        }

        Dictionary<string, Asset> _assets = new Dictionary<string, Asset>();
        public string[] AssetNames
        {
            get
            {
                var names = new string[_assets.Count];
                int i = 0;
                foreach (var name in _assets.Keys)
                    names[i++] = name;
                return names;
            }
        }

        public DataLoader()
        {
            foreach (var name in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                var ext = Path.GetExtension(name);
                if (ext == ".dat")
                {
                    _assets.Add(name.Split('.').Reverse().Skip(1).FirstOrDefault(), new Asset { Name = name });
                }
            }
        }

        public Asset GetAsset(string name)
        {
            var asset = _assets[name];
            if (asset.Polygons == null)
            {
                asset.Polygons = LoadDat(Assembly.GetExecutingAssembly().GetManifestResourceStream(asset.Name));
            }
            return asset;
        }
    }
}
