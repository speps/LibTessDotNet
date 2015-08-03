using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using LibTessDotNet;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace ModernTessBed
{
    public sealed partial class MainPage 
    {
        DataLoader _data;
        int _polySize;

        readonly Stopwatch _sw = new Stopwatch();
        readonly Tess _tess = new Tess();
        private PolygonSet _input;
        private PolygonSet _output;

        private CanvasSolidColorBrush _brushStrokeContour;
        private CanvasSolidColorBrush _brushStrokeWinding;
        private CanvasSolidColorBrush _brushFillPoint;
        private CanvasSolidColorBrush _brushFillPolys;
        private CanvasStrokeStyle _strokeStyleWinding;
        private CanvasSolidColorBrush _brushStrokeOutput;
        private CanvasSolidColorBrush _brushStrokePolys;
        private CanvasGeometry _inputGeometry;
        private CanvasTriangleVertices[] _tessellation;
        private CanvasSolidColorBrush _brushStrokeTess;

        public MainPage()
        {
            this.InitializeComponent();

            DataContext = this;
        }

        public static readonly DependencyProperty AssetsProperty = DependencyProperty.Register(
            "Assets", typeof (IEnumerable<string>), typeof (MainPage), new PropertyMetadata(null));

        public IEnumerable<string> Assets
        {
            get { return (IEnumerable<string>) GetValue(AssetsProperty); }
            set { SetValue(AssetsProperty, value); }
        }

        public static readonly DependencyProperty SelectedAssetProperty = DependencyProperty.Register(
            "SelectedAsset", typeof (string), typeof (MainPage), 
                new PropertyMetadata(null, (o, args) => ((MainPage)o).RefreshAsset((string) args.NewValue) ));

        public string SelectedAsset
        {
            get { return (string) GetValue(SelectedAssetProperty); }
            set { SetValue(SelectedAssetProperty, value); }
        }

        public static readonly DependencyProperty ShowWindingProperty = DependencyProperty.Register(
            "ShowWinding", typeof (bool?), typeof (MainPage),
            new PropertyMetadata(false, (o, args) => ((MainPage)o).Redraw()));

        public bool? ShowWinding
        {
            get { return (bool?) GetValue(ShowWindingProperty); }
            set { SetValue(ShowWindingProperty, value); }
        }

        public static readonly DependencyProperty ShowInputProperty = DependencyProperty.Register(
            "ShowInput", typeof (bool?), typeof (MainPage),
                new PropertyMetadata(false, (o, args) => ((MainPage) o).Redraw()));

        public bool? ShowInput
        {
            get { return (bool?) GetValue(ShowInputProperty); }
            set { SetValue(ShowInputProperty, value); }
        }

        public static readonly DependencyProperty ShowTessellationProperty = DependencyProperty.Register(
            "ShowTessellation", typeof (bool?), typeof (MainPage), 
                new PropertyMetadata(false, (o, args) => ((MainPage)o).Redraw()));

        public bool? ShowTessellation
        {
            get { return (bool?) GetValue(ShowTessellationProperty); }
            set { SetValue(ShowTessellationProperty, value); }
        }

        public static readonly DependencyProperty SelectedWindingRuleProperty = DependencyProperty.Register(
            "SelectedWindingRule", typeof (WindingRule), typeof (MainPage), 
                new PropertyMetadata(WindingRule.EvenOdd, (o, args) => ((MainPage)o).Redraw()));

        public WindingRule SelectedWindingRule
        {
            get { return (WindingRule) GetValue(SelectedWindingRuleProperty); }
            set { SetValue(SelectedWindingRuleProperty, value); }
        }

        public static readonly DependencyProperty WindingRulesProperty = DependencyProperty.Register(
            "WindingRules", typeof (IEnumerable<WindingRule>), typeof (MainPage), 
                new PropertyMetadata(null, (o, args) => ((MainPage)o).Redraw()));


        public IEnumerable<WindingRule> WindingRules
        {
            get { return (IEnumerable<WindingRule>) GetValue(WindingRulesProperty); }
            set { SetValue(WindingRulesProperty, value); }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            _data = new DataLoader();
            Assets = _data.AssetNames.OrderBy(o => o);

            SelectedAsset = "redbook-winding";
            ShowInput = true;
            ShowWinding = false;
            _polySize = 3;

            WindingRules = (WindingRule[])Enum.GetValues(typeof(WindingRule));
            SelectedWindingRule = WindingRule.EvenOdd;
        }

        private void RefreshAsset(string name)
        {
            var asset = _data.GetAsset(name);

            _sw.Reset();

            foreach (Polygon poly in asset.Polygons)
            {
                var v = new ContourVertex[poly.Count];
                for (int i = 0; i < poly.Count; i++)
                {
                    v[i].Position = new Vector3 { X = poly[i].X, Y = poly[i].Y };
                    v[i].Data = poly[i].Color;
                }
                _sw.Start();
                _tess.AddContour(v, poly.Orientation);
                _sw.Stop();
            }

            _sw.Start();
            _tess.Tessellate(SelectedWindingRule, ElementType.Polygons, _polySize, VertexCombine);
            _sw.Stop();

            var output = new PolygonSet();
            for (int i = 0; i < _tess.ElementCount; i++)
            {
                var poly = new Polygon();
                for (int j = 0; j < _polySize; j++)
                {
                    int index = _tess.Elements[i * _polySize + j];
                    if (index == -1)
                        continue;
                    var v = new PolygonPoint
                    {
                        X = _tess.Vertices[index].Position.X,
                        Y = _tess.Vertices[index].Position.Y,
                        Color = (Color)_tess.Vertices[index].Data
                    };
                    poly.Add(v);
                }
                output.Add(poly);
            }

            _textBlockStatus.Text =
                $"{_sw.Elapsed.TotalMilliseconds:F3} ms - {_tess.ElementCount} polygons (of {_polySize} vertices) {(_polySize == 3 ? "... triangles" : "")}";

            _input = asset.Polygons;
            _output = output;
            _canvas.Invalidate();
        }

        private object VertexCombine(Vector3 position, object[] data, float[] weights)
        {
            var colors = new[] { (Color)data[0], (Color)data[1], (Color)data[2], (Color)data[3] };
            var rgba = new[] {
                colors[0].R * weights[0] + colors[1].R * weights[1] + colors[2].R * weights[2] + colors[3].R * weights[3],
                colors[0].G * weights[0] + colors[1].G * weights[1] + colors[2].G * weights[2] + colors[3].G * weights[3],
                colors[0].B * weights[0] + colors[1].B * weights[1] + colors[2].B * weights[2] + colors[3].B * weights[3],
                colors[0].A * weights[0] + colors[1].A * weights[1] + colors[2].A * weights[2] + colors[3].A * weights[3]
            };
            return Color.FromArgb((byte)rgba[3], (byte)rgba[0], (byte)rgba[1], (byte)rgba[2]);
        }


        private void Redraw()
        {
            RefreshAsset(SelectedAsset);
        }

        private void Canvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
        {
            _brushStrokeContour = new CanvasSolidColorBrush(sender, Color.FromArgb(128, 0, 0, 128));

            _strokeStyleWinding = new CanvasStrokeStyle {EndCap = CanvasCapStyle.Triangle};
            _brushStrokeWinding = new CanvasSolidColorBrush(sender, Color.FromArgb(255, 0, 0, 128));
            _brushFillPoint = new CanvasSolidColorBrush(sender, Color.FromArgb(255, 0, 0, 0));
            _brushStrokeOutput = new CanvasSolidColorBrush(sender, Color.FromArgb(32, 0, 0, 0));
            _brushStrokePolys = new CanvasSolidColorBrush(sender, Color.FromArgb(64, 0, 0, 0));
            _brushFillPolys = new CanvasSolidColorBrush(sender, Color.FromArgb(64, 255, 207, 130));
            _brushStrokeTess = new CanvasSolidColorBrush(sender, Color.FromArgb(64, 128, 32, 32));
        }

        private void CanvasControl_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            using (var g = args.DrawingSession)
            {
                g.Clear(Colors.White);
                g.Antialiasing = CanvasAntialiasing.Antialiased;
               

                if (_input == null)
                    return;

                float xmin = float.MaxValue, xmax = float.MinValue;
                float ymin = float.MaxValue, ymax = float.MinValue;

                foreach (var polygon in _input)
                {
                    foreach (var point in polygon)
                    {
                        xmin = Math.Min(xmin, point.X);
                        xmax = Math.Max(xmax, point.X);
                        ymin = Math.Min(ymin, point.Y);
                        ymax = Math.Max(ymax, point.Y);
                    }
                }

                float zoom = (float) (0.9f*Math.Min(sender.ActualWidth/(xmax - xmin), sender.ActualHeight/(ymax - ymin)));
                float xmid = (xmin + xmax)/2f;
                float ymid = (ymin + ymax)/2f;

                g.Transform =// Matrix3x2.CreateTranslation(-xmid, -ymid)*
                              //Matrix3x2.CreateScale(zoom)*
                              Matrix3x2.CreateTranslation((float) (sender.ActualWidth/2), (float) (sender.ActualHeight/2));

                CanvasStrokeStyle strokeStyle = new CanvasStrokeStyle();
                strokeStyle.LineJoin = CanvasLineJoin.MiterOrBevel;
                strokeStyle.StartCap = CanvasCapStyle.Triangle;
                strokeStyle.EndCap = CanvasCapStyle.Triangle;
                strokeStyle.TransformBehavior = CanvasStrokeTransformBehavior.Fixed;

                Func<PolygonPoint, Vector2> f = p => new Vector2 {X = (p.X - xmid)*zoom, Y = (p.Y - ymid)*zoom};
                //Func<PolygonPoint, Vector2> f = p => new Vector2(p.X, p.Y);
                if (ShowInput == true)
                {
                    foreach (var polygon in _input)
                    {
                        bool reverse = false;
                        if (polygon.Orientation != ContourOrientation.Original)
                        {
                            float area = SignedArea(polygon);
                            reverse = (polygon.Orientation == ContourOrientation.Clockwise && area < 0.0f) ||
                                      (polygon.Orientation == ContourOrientation.CounterClockwise && area > 0.0f);
                        }

                        for (int i = 0; i < polygon.Count; i++)
                        {
                            int index = reverse ? polygon.Count - 1 - i : i;
                            Vector2 p0 = f(polygon[(index + (reverse ? 1 : 0))%polygon.Count]);
                            Vector2 p1 = f(polygon[(index + (reverse ? 0 : 1))%polygon.Count]);

                            if (ShowWinding == true)
                            {
                                

                                g.DrawLine(p0, p1, _brushStrokeWinding, 6f, strokeStyle);
                                DrawArrow(g, p0, p1);
                            }
                            else
                            {
                                g.DrawLine(p0, p1, _brushStrokeContour, 2f, strokeStyle);
                                g.FillEllipse(p0, 4f, 4f, _brushFillPoint);
                            }
                        }
                    }
                }

                if (ShowTessellation == true)
                {
                    _inputGeometry = null;
                    foreach (Polygon poly in _input)
                    {
                        var pathBuilder = new CanvasPathBuilder(_canvas);
                        pathBuilder.BeginFigure(f(poly[0]));
                        for (int i = 1; i < poly.Count; i++)
                        {
                            pathBuilder.AddLine(f(poly[i]));
                        }
                        pathBuilder.EndFigure(CanvasFigureLoop.Closed);

                        var geom = CanvasGeometry.CreatePath(pathBuilder);
                        if (_inputGeometry == null)
                            _inputGeometry = geom;
                        else
                            _inputGeometry.CombineWith(geom, Matrix3x2.Identity, CanvasGeometryCombine.Union);
                    }
                    _tessellation = _inputGeometry.Tessellate();

                    foreach (var triangle in _tessellation)
                    {
                        g.DrawLine(triangle.Vertex1, triangle.Vertex2, _brushStrokeTess, 1f, strokeStyle);
                        g.DrawLine(triangle.Vertex2, triangle.Vertex3, _brushStrokeTess, 1f, strokeStyle);
                        g.DrawLine(triangle.Vertex3, triangle.Vertex1, _brushStrokeTess, 1f, strokeStyle);
                    }
                }

                if (_output == null)
                    return;

                foreach (var polygon in _output)
                {
                    var pts = new Vector2[polygon.Count];
                    for (int i = 0; i < polygon.Count; i++)
                    {
                        pts[i] = f(polygon[i]);
                    }

                    var geometry = CanvasGeometry.CreatePolygon(sender, pts);

                    /* if (_input.HasColors)
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



                         using (var brush = new Canvabr
                         PathGradientBrush(pts))
                         using (var pen = new Pen(brush, 20.0f))
                         {
                             brush.SurroundColors = colors;
                             brush.CenterColor = Color.FromArgb(mean[3]/colors.Length, mean[0]/colors.Length,
                                 mean[1]/colors.Length, mean[2]/colors.Length);

                             g.FillPolygon(brush, pts);
                         }
                     }
                    else*/
                    {
                        g.FillGeometry(geometry, _brushFillPolys);
                    }

                    if (ShowTessellation == false)
                    {
                        g.DrawGeometry(geometry, ShowInput == true ? _brushStrokeOutput : _brushStrokePolys, 1f, strokeStyle);
                        /*for (int i = 0; i < pts.Length; i++)
                        {
                            var p0 = pts[i];
                            var p1 = pts[(i + 1)%polygon.Count];

                            if (ShowInput == true)
                                g.DrawLine(p0, p1, _brushStrokeOutput, 1f, strokeStyle);
                            else
                                g.DrawLine(p0, p1, _brushStrokePolys, 1f, strokeStyle);
                        }*/
                    }
                }
            }
        }

        private void DrawArrow(CanvasDrawingSession g, Vector2 p0, Vector2 p1)
        {
            const float arrowSize = 10.0f;

            float length = Vector2.Distance(p0, p1);
            if (length < arrowSize)
                return;

            var pointOnContourPath = Vector2.Lerp(p0, p1, 1 - 2*arrowSize/length);
            var tangentOnContourPath = Vector2.Normalize(p1 - p0);

            Vector2 tangentLeft = new Vector2(-tangentOnContourPath.Y, tangentOnContourPath.X);
            Vector2 tangentRight = new Vector2(tangentOnContourPath.Y, -tangentOnContourPath.X);
            Vector2 bisectorLeft = new Vector2(tangentOnContourPath.X + tangentLeft.X, tangentOnContourPath.Y + tangentLeft.Y);
            Vector2 bisectorRight = new Vector2(tangentOnContourPath.X + tangentRight.X, tangentOnContourPath.Y + tangentRight.Y);
            Vector2 arrowheadFront = new Vector2( pointOnContourPath.X + (tangentOnContourPath.X * arrowSize * 2), 
                pointOnContourPath.Y + (tangentOnContourPath.Y * arrowSize * 2));

            Vector2 arrowheadLeft = new Vector2(
                arrowheadFront.X - (bisectorLeft.X * arrowSize),
                arrowheadFront.Y - (bisectorLeft.Y * arrowSize));

            Vector2 arrowheadRight = new Vector2(
                arrowheadFront.X - (bisectorRight.X * arrowSize),
                arrowheadFront.Y - (bisectorRight.Y * arrowSize));

            const float strokeWidth = arrowSize / 4.0f;
           /* g.DrawLine(pointOnContourPath, arrowheadFront, _brushStrokeWinding, strokeWidth);
            g.DrawLine(arrowheadFront, arrowheadLeft, _brushStrokeWinding, strokeWidth);
            g.DrawLine(arrowheadFront, arrowheadRight, _brushStrokeWinding, strokeWidth);*/
            /*var path = new CanvasPathBuilder(_canvas);
            path.BeginFigure(arrowheadFront);
            path.AddLine(arrowheadLeft);
            path.AddLine(arrowheadRight);
            path.EndFigure(CanvasFigureLoop.Closed);*/
            var geom = CanvasGeometry.CreatePolygon(_canvas, new [] { arrowheadFront, arrowheadLeft, arrowheadRight});
            g.FillGeometry(geom, _brushStrokeWinding);

        }

        private static float SignedArea(Polygon polygon)
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
    }
}
