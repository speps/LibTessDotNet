using System;
using System.Diagnostics;
using System.Windows.Forms;
using LibTessDotNet;
using System.Drawing;
using System.IO;

namespace TessBed
{
    public partial class MainForm : Form
    {
        DataLoader _data = new DataLoader();
        string[] _windingRules;
        WindingRule _windingRule;
        int _polySize;

        Canvas _canvas;
        PolygonSet _polys;
        Stopwatch _sw = new Stopwatch();
        Tess _tess = new Tess();

        public MainForm()
        {
            InitializeComponent();

            _canvas = new Canvas();
            _canvas.Dock = DockStyle.Fill;
            panel.Controls.Add(_canvas);

            foreach (var asset in _data.Assets)
            {
                toolStripAssets.Items.Add(asset);
            }
            toolStripAssets.SelectedIndexChanged += delegate(object sender, EventArgs e) {
                if (toolStripAssets.SelectedIndex >= 0)
                {
                    var asset = toolStripAssets.SelectedItem as DataLoader.Asset;
                    _polys = asset.Polygons;
                    RefreshCanvas();
                }
            };

            _windingRules = Enum.GetNames(typeof(WindingRule));
            foreach (var windingRule in _windingRules)
            {
                toolStripWinding.Items.Add(windingRule);
            }
            toolStripWinding.SelectedIndexChanged += delegate(object sender, EventArgs e) {
                _windingRule = (WindingRule)toolStripWinding.SelectedIndex;
                RefreshCanvas();
            };

            toolStripPolySize.KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter)
                {
                    PolySizeEvent();
                }
            };
            toolStripPolySize.Leave += delegate(object sender, EventArgs e)
            {
                PolySizeEvent();
            };

            toolStripButtonShowInput.CheckedChanged += delegate(object sender, EventArgs e)
            {
                _canvas.ShowInput = toolStripButtonShowInput.Checked;
                toolStripButtonShowWinding.Enabled = _canvas.ShowInput;
                RefreshCanvas();
            };

            toolStripButtonShowWinding.CheckedChanged += delegate(object sender, EventArgs e)
            {
                _canvas.ShowWinding = toolStripButtonShowWinding.Checked;
                RefreshCanvas();
            };

            toolStripButtonNoEmpty.CheckedChanged += delegate(object sender, EventArgs e)
            {
                _tess.NoEmptyPolygons = toolStripButtonNoEmpty.Checked;
                RefreshCanvas();
            };

            toolStripButtonBench.Click += delegate(object sender, EventArgs e)
            {
                new BenchForm().ShowDialog(this);
            };

            toolStripButtonFile.Click += delegate(object sender, EventArgs e)
            {
                var dialog = new OpenFileDialog();
                dialog.Filter = "Test Files (*.dat)|*.dat|All Files (*.*)|*.*";
                dialog.FilterIndex = 1;
                dialog.RestoreDirectory = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var polygons = DataLoader.LoadDat(dialog.OpenFile());
                    _polys = polygons;
                    RefreshCanvas();
                    toolStripAssets.SelectedIndex = -1;
                }
            };

            toolStripButtonFolder.Click += delegate (object sender, EventArgs e)
            {
                var dialog = new FolderBrowserDialog();
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var files = Directory.GetFiles(dialog.SelectedPath, "*.dat");
                    if (files.Length > 0)
                    {
                        toolStripAssets.Items.Clear();
                        _polys = null;
                        foreach (var file in files)
                        {
                            using (var stream = new FileStream(file, FileMode.Open))
                            {
                                var polygons = DataLoader.LoadDat(stream);
                                if (_polys == null)
                                {
                                    _polys = polygons;
                                }
                                toolStripAssets.Items.Add(new DataLoader.Asset() { Name = Path.GetFileName(file), Polygons = polygons });
                            }
                        }
                        toolStripAssets.SelectedIndex = 0;
                        RefreshCanvas();
                    }
                }
            };

            SetAsset("redbook-winding");
            SetShowInput(true);
            SetShowWinding(false);
            SetPolySize(3);
            SetWindingRule(WindingRule.EvenOdd);
        }

        private void SetAsset(string name)
        {
            for (int i = 0; i < toolStripAssets.Items.Count; i++)
            {
                var item = toolStripAssets.Items[i] as DataLoader.Asset;
                if (item != null && item.Name == name)
                {
                    toolStripAssets.SelectedIndex = i;
                    return;
                }
            }
            toolStripAssets.SelectedIndex = -1;
        }

        private void SetWindingRule(WindingRule windingRule)
        {
            for (int i = 0; i < _windingRules.Length; i++)
            {
                if (_windingRules[i] == Enum.GetName(windingRule.GetType(), windingRule))
                {
                    toolStripWinding.SelectedIndex = i;
                    break;
                }
            }
        }

        private void SetShowInput(bool show)
        {
            toolStripButtonShowInput.Checked = show;
        }

        private void SetShowWinding(bool show)
        {
            toolStripButtonShowWinding.Checked = show;
        }

        private void PolySizeEvent()
        {
            int result;
            if (Int32.TryParse(toolStripPolySize.Text, out result))
            {
                _polySize = result;
                if (_polySize < 3)
                {
                    _polySize = 3;
                    toolStripPolySize.Text = _polySize.ToString();
                }
            }
            RefreshCanvas();
        }

        private void SetPolySize(int polySize)
        {
            if (polySize < 3)
            {
                polySize = 3;
            }
            toolStripPolySize.Text = polySize.ToString();
            PolySizeEvent();
        }

        private Vec3 Project(Vec3 v)
        {
            Vec3 norm = _tess.Normal;
            int i = Vec3.LongAxis(ref norm);

            Vec3 sUnit = Vec3.Zero;
            sUnit[i] = 0.0f;
            sUnit[(i + 1) % 3] = _tess.SUnitX;
            sUnit[(i + 2) % 3] = _tess.SUnitY;

            Vec3 tUnit = Vec3.Zero;
            tUnit[i] = 0.0f;
            tUnit[(i + 1) % 3] = norm[i] > 0.0f ? -_tess.SUnitY : _tess.SUnitY;
            tUnit[(i + 2) % 3] = norm[i] > 0.0f ? _tess.SUnitX : -_tess.SUnitX;

            Vec3 result = Vec3.Zero;
            // Project the vertices onto the sweep plane
            Vec3.Dot(ref v, ref sUnit, out result.X);
            Vec3.Dot(ref v, ref tUnit, out result.Y);
            return result;
        }

        private object VertexCombine(Vec3 position, object[] data, float[] weights)
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

        private void RefreshCanvas()
        {
            if (_polys == null)
            {
                _canvas.Input = null;
                _canvas.Output = null;
                _canvas.Invalidate();
                return;
            }

            _sw.Reset();

            foreach (var poly in _polys)
            {
                var v = new ContourVertex[poly.Count];
                for (int i = 0; i < poly.Count; i++)
                {
                    v[i].Position = new Vec3 { X = poly[i].X, Y = poly[i].Y, Z = poly[i].Z };
                    v[i].Data = poly[i].Color;
                }
                _sw.Start();
                _tess.AddContour(v, poly.Orientation);
                _sw.Stop();
            }

            _sw.Start();
            _tess.Tessellate(_windingRule, ElementType.Polygons, _polySize, VertexCombine);
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
                    var proj = Project(_tess.Vertices[index].Position);
                    var v = new PolygonPoint {
                        X = proj.X,
                        Y = proj.Y,
                        Color = (Color)_tess.Vertices[index].Data
                    };
                    poly.Add(v);
                }
                output.Add(poly);
            }

            var input = new PolygonSet();
            foreach (var poly in _polys)
            {
                var projPoly = new Polygon();
                for (int i = 0; i < poly.Count; i++)
                {
                    var proj = Project(new Vec3 { X = poly[i].X, Y = poly[i].Y, Z = poly[i].Z });
                    var v = new PolygonPoint {
                        X = proj.X,
                        Y = proj.Y,
                        Color = poly[i].Color
                    };
                    projPoly.Add(v);
                }
                input.Add(projPoly);
            }

            statusMain.Text = string.Format("{0:F3} ms - {1} polygons (of {2} vertices) {3}", _sw.Elapsed.TotalMilliseconds, _tess.ElementCount, _polySize, _polySize == 3 ? "... triangles" : "");

            _canvas.Input = input;
            _canvas.Output = output;
            _canvas.Invalidate();
        }
    }
}
