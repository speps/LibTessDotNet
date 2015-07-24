using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Numerics;
using LibTessDotNet;

namespace TessBed
{
    public partial class BenchForm : Form
    {
        int _loops = 10;
        DataLoader _data = new DataLoader();

        public delegate LibResult Triangulate(PolygonSet p, int loops);

        public class Lib
        {
            public string Name { get; set; }
            public Triangulate Triangulate { get; set; }
        }

        public class LibResult
        {
            public double Time { get; set; }
            public PolygonSet Output { get; set; }
        }

        Lib[] _libs = new Lib[] {
            /*new Lib { Name = "Poly2Tri", Triangulate = (pset, loops) => {
                var result = new LibResult();
                // Output
                var rpset = PolyConvert.ToP2T(pset);
                Poly2Tri.P2T.Triangulate(rpset);
                result.Output = PolyConvert.FromP2T(rpset);
                // Time
                var sw = new Stopwatch();
                for (int i = 0; i < loops; i++)
                {
                    rpset = PolyConvert.ToP2T(pset);
                    sw.Start();
                    Poly2Tri.P2T.Triangulate(rpset);
                    sw.Stop();
                }
                result.Time = sw.Elapsed.TotalSeconds;

                return result;
            } },*/
            new Lib { Name = "LibTessDotNet", Triangulate = (pset, loops) => {
                var result = new LibResult();
                var tess = new Tess();
                // Output
                PolyConvert.ToTess(pset, tess);
                tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);
                result.Output = PolyConvert.FromTess(tess);
                // Time
                var sw = new Stopwatch();
                for (int i = 0; i < loops; i++)
                {
                    sw.Start();
                    PolyConvert.ToTess(pset, tess);
                    tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3);
                    sw.Stop();
                }
                result.Time = sw.Elapsed.TotalSeconds;
                return result;
            } }
        };

        public class TestResult
        {
            public string Name { get; set; }
            public List<LibResult> Libs = new List<LibResult>();
        }

        List<TestResult> _results = new List<TestResult>();
        BackgroundWorker _bgWorker = new BackgroundWorker();

        public BenchForm()
        {
            InitializeComponent();
            _bgWorker.WorkerReportsProgress = true;
            _bgWorker.DoWork += Worker;
            _bgWorker.ProgressChanged += (sender, e) => {
                toolStripProgressBar.ProgressBar.Value = e.ProgressPercentage;
            };
            _bgWorker.RunWorkerCompleted += WorkerCompleted;
            toolStripTextLoops.Text = _loops.ToString();
        }

        private void BenchForm_Load(object sender, EventArgs e)
        {
            data.AutoGenerateColumns = false;
            data.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
        }

        private void toolStripButtonStart_Click(object sender, EventArgs e)
        {
            _bgWorker.RunWorkerAsync();
        }

        private void Worker(object sender, DoWorkEventArgs e)
        {
            Invoke(new Action(() => { toolStripButtonStart.Enabled = false; }));

            var results = new List<TestResult>();
            var names = _data.AssetNames;
            for (int i = 0; i < names.Length; i++)
            {
                var asset = _data.GetAsset(names[i]);
                var testResult = new TestResult();
                testResult.Name = names[i];
                foreach (var lib in _libs)
                {
                    var libResult = lib.Triangulate(asset.Polygons, _loops);
                    testResult.Libs.Add(libResult);
                }
                results.Add(testResult);
                _bgWorker.ReportProgress(i * 100 / names.Length);
            }
            _bgWorker.ReportProgress(100);
            e.Result = results;
        }

        private void WorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var results = e.Result as List<TestResult>;

            data.CellFormatting -= data_CellFormatting;

            data.Columns.Clear();
            data.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Asset" });
            foreach (var lib in _libs)
                data.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = lib.Name });

            data.Rows.Clear();

            var buffer = new object[1 + _libs.Length];
            var rows = new List<DataGridViewRow>();
            foreach (var test in results)
            {
                buffer[0] = test.Name;
                for (int i = 0; i < test.Libs.Count; i++)
                    buffer[i + 1] = test.Libs[i].Time;

                rows.Add(new DataGridViewRow());
                rows[rows.Count - 1].CreateCells(data, buffer);
            }
            data.Rows.AddRange(rows.ToArray());

            data.CellFormatting += data_CellFormatting;

            data.Invalidate();

            Invoke(new Action(() => { toolStripButtonStart.Enabled = true; }));
        }

        private void toolStripTextLoops_TextChanged(object sender, EventArgs e)
        {
            int n = _loops;
            int.TryParse(toolStripTextLoops.Text, out n);
            _loops = n;
        }

        private void toolStripTextLoops_Validating(object sender, CancelEventArgs e)
        {
            int n = _loops;
            e.Cancel = !int.TryParse(toolStripTextLoops.Text, out n);
            if (e.Cancel)
                toolStripTextLoops.Text = _loops.ToString();
            else
                _loops = n;
        }

        private void data_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.ColumnIndex == 0) // Asset column
                return;
            int colMin = 1;
            var row = data.Rows[e.RowIndex];
            for (int i = 1; i < row.Cells.Count; i++)
            {
                double min = (double)row.Cells[colMin].Value;
                double val = (double)row.Cells[i].Value;
                if (val < min)
                {
                    colMin = i;
                }
            }
            if (e.ColumnIndex == colMin)
            {
                e.CellStyle.ForeColor = Color.Green;
            }
        }
    }
}
