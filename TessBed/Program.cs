using System;
using System.Windows.Forms;

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
            if (args.Length == 1 && string.Equals(args[0], "gentestdat", StringComparison.OrdinalIgnoreCase))
            {
                UnitTests.GenerateTestData();
                return;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
