using System;
using System.Windows.Forms;

namespace BuildTools {
    using System.IO;

  internal static class Program {
        private static readonly string appdata = Environment.GetEnvironmentVariable("APPDATA");
        private static readonly string dir = appdata + "\\btgui\\";
        private static readonly string file = dir + "gaclientid";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main() {
            Guid guid;

            if (!Directory.Exists(dir) && File.Exists(dir)) {
                File.Delete(dir);
            }

            if (!Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(file)) {
                string text = File.ReadAllText(file);
                if (string.IsNullOrEmpty(text) || !Guid.TryParse(text, out guid)) {
                    guid = Guid.NewGuid();
                }
            } else {
                guid = Guid.NewGuid();
            }

            File.WriteAllText(file, guid.ToString());

            Job job = new Job();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new BuildTools(guid, job));
        }
    }
}
