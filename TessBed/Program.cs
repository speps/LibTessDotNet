using System;
using System.Windows.Forms;
using System.Collections.Generic;
using LibTessDotNet;
using System.Diagnostics;

namespace TessBed
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length >= 1)
            {
                if (string.Equals(args[0], "gentestdat", StringComparison.OrdinalIgnoreCase))
                {
                    UnitTests.GenerateTestData();
                }
                if (args.Length == 2 && string.Equals(args[0], "profile", StringComparison.OrdinalIgnoreCase))
                {
                    int count = 0;
                    if (!int.TryParse(args[1], out count))
                    {
                        return;
                    }
                    var stopwatch = new Stopwatch();
                    var loader = new DataLoader();
                    stopwatch.Start();
                    for (int i = 0; i < count; i++)
                    {
                        foreach (var name in loader.AssetNames)
                        {
                            var pset = loader.GetAsset(name).Polygons;

                            var lines = new List<string>();
                            var indices = new List<int>();

                            foreach (WindingRule winding in Enum.GetValues(typeof(WindingRule)))
                            {
                                var tess = new Tess();
                                PolyConvert.ToTess(pset, tess);
                                tess.Tessellate(winding, ElementType.Polygons, 3);
                            }
                        }
                    }
                    stopwatch.Stop();
                    Console.WriteLine("{0:F3}ms", stopwatch.Elapsed.TotalMilliseconds);
                }
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
