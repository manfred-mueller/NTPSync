using System;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace NTPSync
{
    static class Program
    {
        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
          Application.EnableVisualStyles();
          Application.SetCompatibleTextRenderingDefault(false);
          Application.Run(new NTPSync());
        }
    }
}
