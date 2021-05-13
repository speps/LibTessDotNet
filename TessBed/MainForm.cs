using System;
using System.Diagnostics;
using System.Windows.Forms;
using LibTessDotNet;
using System.Drawing;
using System.IO;
using System.Collections.Generic;

namespace TessBed
{
    public partial class MainForm : Form
    {
        DataLoader _data = new DataLoader();
        string[] _windingRules;
        WindingRule _windingRule;
        int _polySize = 3;

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
                    LoadFolder(dialog.SelectedPath);
                }
            };

            SetAsset("redbook-winding");
            SetShowInput(true);
            SetShowWinding(false);
            SetPolySize(3);
            SetWindingRule(WindingRule.Positive);
        }

        public void LoadFolder(string path)
        {
            var files = new List<string>();
            files.AddRange(Directory.GetFiles(path, "*.dat"));
            files.AddRange(Directory.GetFiles(path, "*.txt"));
            if (files.Count > 0)
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
            _sw.Start();
            var output = PolyConvert.Triangulate(PolyConvert.Library.Tess, _polys, _polySize, _windingRule);
            _sw.Stop();

            var input = new PolygonSet();
            foreach (var poly in _polys)
            {
                var projPoly = new Polygon();
                for (int i = 0; i < poly.Count; i++)
                {
                    var v = new PolygonPoint {
                        X = poly[i].X,
                        Y = poly[i].Y,
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
