using System;
using System.Windows.Forms;
using octonev2.Utils;

namespace octonev2
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            DecompilerDetection.StartDetectionLoop();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.Run(new Forms.LoginForm());
        }
    }
}
