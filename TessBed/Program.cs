using System;
using System.Windows.Forms;
using System.Collections.Generic;
using LibTessDotNet;
using System.Diagnostics;
using System.IO;

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
            string folder = null;
            if (args.Length >= 1)
            {
                if (string.Equals(args[0], "gentestdat", StringComparison.OrdinalIgnoreCase))
                {
                    UnitTests.GenerateTestData();
                    return;
                }
                if (args.Length == 2 && string.Equals(args[0], "folder", StringComparison.OrdinalIgnoreCase))
                {
                    folder = args[1];
                    if (!Directory.Exists(folder))
                    {
                        folder = null;
                    }
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
                    for (int i = 0; i < count; i++)
                    {
                        foreach (var asset in loader.Assets)
                        {
                            var pset = asset.Polygons;

                            foreach (WindingRule winding in Enum.GetValues(typeof(WindingRule)))
                            {
                                var tess = new Tess();
                                PolyConvert.ToTess(pset, tess);
                                stopwatch.Start();
                                tess.Tessellate(winding, ElementType.Polygons, 3);
                                stopwatch.Stop();
                            }
                        }
                    }
                    Console.WriteLine("{0:F3}ms", stopwatch.Elapsed.TotalMilliseconds);
                    return;
                }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var form = new MainForm();
            if (folder != null)
            {
                form.LoadFolder(folder);
            }
            Application.Run(form);
        }
    }
}
