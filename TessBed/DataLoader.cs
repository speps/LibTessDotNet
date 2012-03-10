using System.Collections.Generic;
using System.IO;
using System;
using System.Globalization;
using System.Reflection;
using System.Diagnostics;

namespace TessBed
{
    public struct PolygonPoint
    {
        public float X, Y;
    }

    public class Polygon : List<PolygonPoint>
    {
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

    }

    public class DataLoader
    {
        public class Asset
        {
            public string Name;
            public PolygonSet Polygons;
        }

        public PolygonSet LoadDat(Stream fileStream)
        {
            var points = new List<PolygonPoint>();
            var polys = new PolygonSet();
            int lineNum = 0;
            string line;
            bool skipLine = false;
            using (var stream = new StreamReader(fileStream))
            {
                while ((line = stream.ReadLine()) != null)
                {
                    ++lineNum;
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        if (points.Count > 0)
                        {
                            var p = new Polygon(points);
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
                    if (!skipLine && line.StartsWith("/*"))
                    {
                        skipLine = true;
                        continue;
                    }
                    else if (skipLine)
                    {
                        if (line.StartsWith("*/"))
                        {
                            skipLine = false;
                        }
                        continue;
                    }
                    {
                        float x, y;
                        var xy = line.Split(new[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        if (xy != null &&
                            xy.Length >= 2 &&
                            float.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x) &&
                            float.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y))
                        {
                            points.Add(new PolygonPoint { X = x, Y = y });
                        }
                        else
                        {
                            throw new InvalidDataException("Invalid input data");
                        }
                    }
                }

                if (points.Count > 0)
                {
                    Polygon p = new Polygon(points);
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
                var s = name.Split('.');
                if (s != null && s.Length > 0)
                {
                    if (s[s.Length - 1] == "dat")
                    {
                        _assets.Add(s[s.Length - 2], new Asset { Name = name });
                    }
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
