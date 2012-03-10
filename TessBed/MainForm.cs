using System;
using System.Windows.Forms;
using LibTessDotNet;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Diagnostics;

namespace TessBed
{
    public partial class MainForm : Form
    {
        DataLoader _data = new DataLoader();
        string[] _assets;
        string[] _windingRules;
        WindingRule _windingRule;
        int _polySize;

        Canvas _canvas;
        Stopwatch _sw = new Stopwatch();
        Tess _tess = new Tess();

        public MainForm()
        {
            InitializeComponent();

            _canvas = new Canvas();
            _canvas.Dock = DockStyle.Fill;
            panel.Controls.Add(_canvas);

            _assets = _data.AssetNames;
            foreach (var asset in _assets)
            {
                toolStripAssets.Items.Add(asset);
            }
            toolStripAssets.SelectedIndexChanged += delegate(object sender, EventArgs e) { RefreshAsset(toolStripAssets.SelectedIndex); };

            _windingRules = Enum.GetNames(typeof(WindingRule));
            foreach (var windingRule in _windingRules)
            {
                toolStripWinding.Items.Add(windingRule);
            }
            toolStripWinding.SelectedIndexChanged += delegate(object sender, EventArgs e) {
                _windingRule = (WindingRule)toolStripWinding.SelectedIndex;
                RefreshAsset(toolStripAssets.SelectedIndex);
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

            SetAsset("clipper");
            SetPolySize(3);
            SetWindingRule(WindingRule.OddEven);
        }

        private void SetAsset(string name)
        {
            for (int i = 0; i < _assets.Length; i++)
            {
                if (_assets[i] == name)
                {
                    toolStripAssets.SelectedIndex = i;
                    break;
                }
            }
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
            RefreshAsset(toolStripAssets.SelectedIndex);
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

        private void RefreshAsset(int index)
        {
            RefreshAsset(_assets[index]);
        }

        private void RefreshAsset(string name)
        {
            var asset = _data.GetAsset(name);

            _sw.Reset();

            foreach (var poly in asset.Polygons)
            {
                var v = new float[poly.Count * 2];
                for (int i = 0; i < poly.Count; i++)
                {
                    v[i * 2 + 0] = poly[i].X;
                    v[i * 2 + 1] = poly[i].Y;
                }
                _sw.Start();
                _tess.AddContour(2, v);
                _sw.Stop();
            }

            _sw.Start();
            _tess.Tesselate(_windingRule, ElementType.Polygons, _polySize, 2, null);
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
                    var v = new PolygonPoint { X = _tess.Vertices[index * 2 + 0], Y = _tess.Vertices[index * 2 + 1] };
                    poly.Add(v);
                }
                output.Add(poly);
            }

            statusMain.Text = string.Format("{0:F3} ms - {1} polygons (of {2} vertices) {3}", _sw.Elapsed.TotalMilliseconds, _tess.ElementCount, _polySize, _polySize == 3 ? "... triangles" : "");

            _canvas.Input = asset.Polygons;
            _canvas.Output = output;
            _canvas.Invalidate();
        }
    }
}
