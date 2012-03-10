using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using LibTessDotNet;

namespace TessBed
{
    public class Canvas : Control
    {
        public PolygonSet Input;
        public PolygonSet Output;
        public bool ShowInput = true;
        public bool ShowWinding = false;

        public Canvas()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        private float SignedArea(Polygon polygon)
        {
            float area = 0.0f;

            for (int i = 0; i < polygon.Count; i++)
            {
                var v0 = polygon[i];
                var v1 = polygon[(i + 1) % polygon.Count];

                area += v0.X * v1.Y;
                area -= v0.Y * v1.X;
            }

            return area * 0.5f;
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            var g = pe.Graphics;

            g.Clear(Color.White);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TranslateTransform(ClientSize.Width / 2, ClientSize.Height / 2);

            if (Input == null)
                return;

            float xmin = float.MaxValue, xmax = float.MinValue;
            float ymin = float.MaxValue, ymax = float.MinValue;

            foreach (var polygon in Input)
            {
                foreach (var point in polygon)
                {
                    xmin = Math.Min(xmin, point.X);
                    xmax = Math.Max(xmax, point.X);
                    ymin = Math.Min(ymin, point.Y);
                    ymax = Math.Max(ymax, point.Y);
                }
            }

            float zoom = 0.9f * Math.Min(ClientSize.Width / (xmax - xmin), ClientSize.Height / (ymax - ymin));
            float xmid = (xmin + xmax) / 2;
            float ymid = (ymin + ymax) / 2;

            Func<PolygonPoint, PointF> f = (p) => new PointF { X = (p.X - xmid) * zoom, Y = (p.Y - ymid) * zoom };

            using (var penContour = new Pen(Color.FromArgb(128, 0, 0, 128), 2.0f))
            using (var penWinding = new Pen(Color.FromArgb(255, 0, 0, 128), 6.0f))
            using (var brushPoint = new SolidBrush(Color.FromArgb(255, 0, 0, 0)))
            using (var penOutput = new Pen(Color.FromArgb(32, 0, 0, 0), 1.0f))
            using (var penPolys = new Pen(Color.FromArgb(64, 0, 0, 0), 1.0f))
            using (var brushPolys = new SolidBrush(Color.FromArgb(64, 255, 207, 130)))
            {
                penWinding.EndCap = LineCap.ArrowAnchor;
                if (ShowInput)
                {
                    foreach (var polygon in Input)
                    {
                        bool reverse = false;
                        if (polygon.Orientation != ContourOrientation.Original)
                        {
                            float area = SignedArea(polygon);
                            reverse = (polygon.Orientation == ContourOrientation.Clockwise && area < 0.0f) || (polygon.Orientation == ContourOrientation.CounterClockwise && area > 0.0f);
                        }

                        for (int i = 0; i < polygon.Count; i++)
                        {
                            int index = reverse ? polygon.Count - 1 - i : i;
                            var p0 = f(polygon[(index + (reverse ? 1 : 0)) % polygon.Count]);
                            var p1 = f(polygon[(index + (reverse ? 0 : 1)) % polygon.Count]);

                            g.DrawLine(ShowWinding ? penWinding : penContour, p0, p1);
                            g.FillEllipse(brushPoint, p0.X - 2.0f, p0.Y - 2.0f, 4.0f, 4.0f);
                        }
                    }
                }

                if (Output == null)
                    return;

                foreach (var polygon in Output)
                {
                    var pts = new PointF[polygon.Count];
                    for (int i = 0; i < polygon.Count; i++)
                    {
                        pts[i] = f(polygon[i]);
                    }

                    if (Input.HasColors)
                    {
                        var colors = new Color[pts.Length];
                        int[] mean = new int[4];
                        for (int i = 0; i < pts.Length; i++)
                        {
                            colors[i] = polygon[i].Color;
                            mean[0] += colors[i].R;
                            mean[1] += colors[i].G;
                            mean[2] += colors[i].B;
                            mean[3] += colors[i].A;
                        }

                        using (var brush = new PathGradientBrush(pts))
                        using (var pen = new Pen(brush, 20.0f))
                        {
                            brush.SurroundColors = colors;
                            brush.CenterColor = Color.FromArgb(mean[3] / colors.Length, mean[0] / colors.Length, mean[1] / colors.Length, mean[2] / colors.Length);

                            g.FillPolygon(brush, pts);
                        }
                    }
                    else
                    {
                        g.FillPolygon(brushPolys, pts);
                    }
                    for (int i = 0; i < pts.Length; i++)
                    {
                        var p0 = pts[i];
                        var p1 = pts[(i + 1) % polygon.Count];

                        g.DrawLine(ShowInput ? penOutput : penPolys, p0, p1);
                    }
                }
            }
        }
    }
}
