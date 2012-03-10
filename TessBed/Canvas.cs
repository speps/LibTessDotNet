using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Drawing;

namespace TessBed
{
    public class Canvas : Control
    {
        public PolygonSet Input;
        public PolygonSet Output;

        public Canvas()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
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
            using (var brushPoint = new SolidBrush(Color.FromArgb(128, 0, 0, 0)))
            using (var penOutput = new Pen(Color.FromArgb(32, 0, 0, 0), 1.0f))
            {
                foreach (var polygon in Input)
                {
                    for (int i = 0; i < polygon.Count; i++)
                    {
                        var p0 = f(polygon[i]);
                        var p1 = f(polygon[(i + 1) % polygon.Count]);

                        g.DrawLine(penContour, p0, p1);
                        g.FillEllipse(brushPoint, p0.X - 2.0f, p0.Y - 2.0f, 4.0f, 4.0f);
                    }
                }

                if (Output == null)
                    return;

                foreach (var polygon in Output)
                {
                    for (int i = 0; i < polygon.Count; i++)
                    {
                        var p0 = f(polygon[i]);
                        var p1 = f(polygon[(i + 1) % polygon.Count]);

                        g.DrawLine(penOutput, p0, p1);
                    }
                }
            }
        }
    }
}
